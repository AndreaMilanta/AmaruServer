using AmaruCommon.Actions;
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
        private Dictionary<CharacterEnum, User> myEnemiesDict;
        ValidationVisitor myValidation;

        public AIUser(string logger) : base(logger)
        {
            listOfActions = new Queue<PlayerAction>();            
        }

        public override void SetPlayer(Player player, GameManager gameManager)
        {
            this.Player = player;
            this.GameManager = gameManager;
            this.messageHandler = this.GameManager.HandlePlayerMessage;
            this.myValidation = new ValidationVisitor(gameManager);
        }

        public override void Write(Message mex)
        {
            if (mex is ResponseMessage)
            {
                Response responseReceived = ((ResponseMessage)mex).Response;
                if (responseReceived is NewTurnResponse)
                {
                    
                    if (((NewTurnResponse)responseReceived).ActivePlayer ==CharacterEnum.AMARU)
                    {
                        myTurn = true;
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

        private void Think()
        {
            Dictionary<CharEnumerator, ValuesEnumerator> valuesPerPlayer;
            //per ogni giocatore in generale voglio sapere:
            List<Player> playerToClone = new List<Player>();

            myEnemiesDict = new Dictionary<CharacterEnum, User>( GameManager._userDict);
            foreach (User user in myEnemiesDict.Values)
            {
                playerToClone.Add(user.Player);
            }
            GameManager gm = new GameManager(playerToClone, "AILogger");

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
        internal class ValuesEnumerator
        {

        }
    }
}
