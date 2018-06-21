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
        LimitedList<LimitedList<CreatureCard>> OtherPlayersOuterField;
        LimitedList<LimitedList<CreatureCard>> OtherPlayersInnerField;
        private Dictionary<CharacterEnum, User> myEnemiesDict;

        public AIUser(string logger) : base(logger)
        {
            listOfActions = new Queue<PlayerAction>();
            OtherPlayersInnerField = new LimitedList<LimitedList<CreatureCard>>(4);
            OtherPlayersOuterField = new LimitedList<LimitedList<CreatureCard>>(4);
            
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
            List<EnemyInfo> enemyInfo = new List<EnemyInfo>();

            myEnemiesDict = new Dictionary<CharacterEnum, User>( GameManager._userDict);
            myEnemiesDict.Remove(CharacterEnum.AMARU);
            foreach (User target in myEnemiesDict.Values)
            {
                //inutile prendere l'asEnemy immagino
                enemyInfo.Add(target.Player.AsEnemy);
                OtherPlayersOuterField.Add(target.Player.AsEnemy.Outer);
                OtherPlayersInnerField.Add(target.Player.AsEnemy.Inner);
            }
            LimitedList<Card> myCards =Player.Hand;
            LimitedList<CreatureCard> myWarZone = Player.Outer;
            int tempMana = Player.Mana;
            Log(tempMana.ToString());
            foreach (Card c in myCards)
            {
                Log(c.Name);
                if (c.Cost <= tempMana)
                {
                    if (c is CreatureCard)
                    {
                        if (Player.Outer.Count<= Player.Outer.MaxSize)
                        {
                            listOfActions.Enqueue(new PlayACreatureFromHandAction(CharacterEnum.AMARU, c.Id, Place.OUTER, Player.Outer.Count));
                        }
                        else if (Player.Inner.Count <= Player.Inner.MaxSize)
                        {
                            listOfActions.Enqueue(new PlayACreatureFromHandAction(CharacterEnum.AMARU, c.Id, Place.INNER, Player.Inner.Count));
                        }
                        tempMana -= c.Cost;
                    }
                    else
                    {
                        //lista null, bisogno di discutere
                        listOfActions.Enqueue(new PlayASpellFromHandAction(CharacterEnum.AMARU, c.Id, null));
                    }
                }
            }
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
                                listOfActions.Enqueue(new AttackPlayerAction(CharacterEnum.AMARU, c.Id, Property.ATTACK, new PlayerTarget(myTarget)));
                                temp -= c.Attack.Cost;
                            }
                            else
                            {
                                listOfActions.Enqueue(new AttackCreatureAction(CharacterEnum.AMARU, c.Id, Property.ATTACK, new CardTarget(myTarget, myEnemiesDict[myTarget].Player.Outer[rnd.Next(6)])));
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
                    catch (Exception e) { }

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
