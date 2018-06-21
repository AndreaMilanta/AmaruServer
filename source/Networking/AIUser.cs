using AmaruCommon.Actions;
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
        MessageHandler messageHandler = null;
        Queue<Message> listOfActions;
        LimitedList<LimitedList<CreatureCard>> OtherPlayersOuterField;
        LimitedList<LimitedList<CreatureCard>> OtherPlayersInnerField;
        private Dictionary<CharacterEnum, User> myEnemiesDict;

        public AIUser(string logger) : base(logger)
        {
            listOfActions = new Queue<Message>();
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
                    if (((NewTurnResponse)responseReceived).ActivePlayer == AmaruCommon.GameAssets.Characters.CharacterEnum.AMARU)
                    {
                        listOfActions.Enqueue(new ActionMessage(new EndTurnAction(AmaruCommon.GameAssets.Characters.CharacterEnum.AMARU, -1, GameManager.IsMainTurn)));
                        
                    }
                }
                if(responseReceived is MainTurnResponse)
                {  
                       //think()
                    listOfActions.Enqueue(new ActionMessage(new EndTurnAction(AmaruCommon.GameAssets.Characters.CharacterEnum.AMARU, -1, GameManager.IsMainTurn)));
                    
                }
            }
        }


        public override Message ReadSync(int timeout_s)
        {
            return listOfActions.Dequeue();
        }

        public override void ReadASync(bool continuous)
        {
            // Asyncronously calls messageHabndler(Message)
        }

        public override void Close(Message notification = null)
        {

        }
    }
}
