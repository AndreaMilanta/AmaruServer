﻿using AmaruCommon.Actions;
using AmaruCommon.Actions.Targets;
using AmaruCommon.Communication.Messages;
using AmaruCommon.Constants;
using AmaruCommon.GameAssets.Cards;
using AmaruCommon.GameAssets.Characters;
using AmaruCommon.GameAssets.Players;
using AmaruCommon.Responses;
using AmaruServer.Game.Managing;
using ClientServer.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static ClientServer.Communication.ClientTCP;

namespace AmaruServer.Networking
{
    public class AIUser : User
    {
        private bool myTurn= false;
        MessageHandler messageHandler = null;
        Queue<PlayerAction> listOfActions;
        List<PlayerAction> listOfPossibleActions;
        private Dictionary<CharacterEnum, User> myEnemiesDict;
        ValidationVisitor myValidation;

        public AIUser(string logger) : base(logger)
        {
            listOfActions = new Queue<PlayerAction>();
            listOfPossibleActions = new List<PlayerAction>();
        }

        public override void SetPlayer(Player player, GameManager gameManager)
        {
            this.Player = player;
            this.GameManager = gameManager;
            this.messageHandler = this.GameManager.HandlePlayerMessage;
        }

        public override void Write(Message mex)
        {
            if (mex is ResponseMessage)
            {
                Response responseReceived = ((ResponseMessage)mex).Response;
                if (responseReceived is NewTurnResponse)
                {
                    
                    if (((NewTurnResponse)responseReceived).ActivePlayer == CharacterEnum.AMARU)
                    {
                        myTurn = true;
                        thinkToMove();
                        listOfActions.Enqueue(new EndTurnAction(CharacterEnum.AMARU, -1, GameManager.IsMainTurn));
                        
                    }
                } else if(myTurn && (responseReceived is MainTurnResponse))
                {
                    Think();
                    listOfActions.Enqueue(new EndTurnAction(CharacterEnum.AMARU, -1, GameManager.IsMainTurn));
                    myTurn = false;
                }
            }
        }

        private void thinkToMove()
        {
            LimitedList<CreatureCard> inner = Player.Inner;
            LimitedList<CreatureCard> outer = Player.Outer;
            List<int> moved = new List<int>();
            int countOuter = outer.Count;
            //per ogni giocatore in generale voglio sapere:
            List<Player> playerToClone = new List<Player>();

            myEnemiesDict = new Dictionary<CharacterEnum, User>(GameManager._userDict);
            foreach (User user in myEnemiesDict.Values)
            {
                playerToClone.Add(user.Player);
            }
            GameManager FakeGM = new GameManager(playerToClone, "AILogger");

            LimitedList<Card> myCards = Player.Hand;
            LimitedList<CreatureCard> myWarZone = Player.Outer;
            LimitedList<CreatureCard> myInnerZone = Player.Inner;

            //Prima implementazione di Ai, se può mettere creature fuori, ce le mette.
            foreach (CreatureCard cd in inner)
            {
                try
                {
                    MoveCreatureAction moveCreatureAction = new MoveCreatureAction(CharacterEnum.AMARU, cd.Id, Place.OUTER, 0);
                    moveCreatureAction.Visit(myValidation);
                    listOfPossibleActions.Add(moveCreatureAction);
                }
                catch { }

            }
            /*
            foreach (CreatureCard cd in outer)
            {
                try
                {
                    MoveCreatureAction moveCreatureAction = new MoveCreatureAction(CharacterEnum.AMARU, cd.Id, Place.INNER, 0);
                    moveCreatureAction.Visit(myValidation);
                    listOfPossibleActions.Add(moveCreatureAction);
                } catch { }

            }
            */
        }

        private void Think()
        {
            //per ogni giocatore in generale voglio sapere:
            List<Player> playerToClone = new List<Player>();

            myEnemiesDict = new Dictionary<CharacterEnum, User>( GameManager._userDict);
            foreach (User user in myEnemiesDict.Values)
            {
                playerToClone.Add(user.Player);
            }
            GameManager FakeGM = new GameManager(playerToClone, "AILogger");

            LimitedList<Card> myCards =Player.Hand;
            LimitedList<CreatureCard> myWarZone = Player.Outer;
            LimitedList<CreatureCard> myInnerZone = Player.Inner;

            //This way it will try to play all possible cards
            foreach (Card c in myCards)
            {
                try
                {
                    if (c is CreatureCard)
                    {
                        PlayACreatureFromHandAction myIntention = new PlayACreatureFromHandAction(CharacterEnum.AMARU, c.Id, Place.OUTER, Player.Outer.Count);
                        myIntention.Visit(myValidation);
                        listOfActions.Enqueue(myIntention);
                    }
                    else if (c is SpellCard)
                    {

                        //Amaru ha solo spell senza target
                        PlayASpellFromHandAction myIntention = new PlayASpellFromHandAction(CharacterEnum.AMARU, c.Id, null);
                        myIntention.Visit(myValidation);
                        listOfActions.Enqueue(myIntention);
                    }
                }
                catch {
                }
            }
            
            // attacco random 
            foreach (CreatureCard c in myWarZone){
                int temp = c.Energy;
                bool stop= false;
                Random rnd = new Random();
                while (temp == 0 || stop)
                {
                    try
                    {
                        double action = rnd.NextDouble();
                        if (action <= 0.50)
                        {
                            CharacterEnum myTarget = (CharacterEnum)rnd.Next(4);
                            if (action >= 0.10)
                            {
                                AttackPlayerAction myIntention = new AttackPlayerAction(CharacterEnum.AMARU, c.Id, Property.ATTACK, new PlayerTarget(myTarget));
                                myIntention.Visit(myValidation);
                                listOfActions.Enqueue(myIntention);
                                temp -= c.Attack.Cost;
                            }
                            else
                            {
                                AttackCreatureAction myIntention = new AttackCreatureAction(CharacterEnum.AMARU, c.Id, Property.ATTACK, new CardTarget(myTarget, myEnemiesDict[myTarget].Player.Outer[rnd.Next(6)]));
                                myIntention.Visit(myValidation);
                                listOfActions.Enqueue(myIntention);
                                temp -= c.Attack.Cost;
                            }
                        }
                        else if (action >= 0.85)
                        {
                            //non fare nulla
                            break;
                        }
                        else
                        {
                            //abilità
                            //temp -= c.Ability.Cost;
                        }
                    }
                    catch { }

                }
                
            }

        }

        public override Message ReadSync(int timeout_s)
        {
            return new ActionMessage(listOfActions.Dequeue());
        }

        public override void ReadASync(bool continuous)
        {
            // Asyncronously calls messageHabndler(Message)
        }

        public override void Close(Message notification = null)
        {

        }
        public double ValueGoalDiscontentment(GameManager gm)
        {
            double value = 0;
            List<Player> lp = new List<Player>();
            Player me = gm._userDict[CharacterEnum.AMARU].Player;
            gm._userDict.Remove(CharacterEnum.AMARU);
            foreach (User p in gm._userDict.Values){
                Player player = p.Player;
                if (player.IsAlive)
                {
                    lp.Add(p.Player);
                }
                else
                {
                    //Perdo Punti per ogni giocatore vivo
                    value -= 20;
                }
            }

            //Somma vita mia e delle mie creature, il mio mana (moltiplicato per 2 per dargli più valore)
            value += me.Health;
            value += calculateHpOnField(me);
            value += me.Outer.Count;
            value += me.Inner.Count;
            value += me.Mana*2;

            //sommo la vita degli altri e la deviazione standard, la vita media nei loro campi e la deviazione standard
            List<double> listHpField = new List<double>();
            List<double> listHpPlayers = new List<double>();
            foreach (Player p in lp)
            {
                listHpField.Add(calculateHpOnField(p));
                listHpPlayers.Add(p.Health);
            }
            double meanHp = listHpPlayers.Average();
            double meanHpField = listHpField.Average();

            value -= meanHpField;
            value -= meanHp;
            value -= Tools.calculateStd(listHpPlayers);
            value -= Tools.calculateStd(listHpField);

            return value;
        }

        private double calculateHpOnField(Player p)
        {
            double hp = 0;
            int t = 0;
            foreach (CreatureCard c in p.Outer)
            {
                hp += c.Health;
                t += 1;
            }
            foreach (CreatureCard c in p.Inner)
            {
                hp += c.Health;
                t += 1;
            }
            hp /= t;
            return hp;
        }
    }
}
