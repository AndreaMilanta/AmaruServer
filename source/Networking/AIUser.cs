﻿using AmaruCommon.Actions;
using AmaruCommon.Communication.Messages;
using AmaruCommon.GameAssets.Players;
using AmaruServer.Game.Managing;
using ClientServer.Messages;
using System;
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
        public AIUser(string logger) : base(logger)
        {

        }

        public override void SetPlayer(Player player, GameManager gameManager)
        {
            this.Player = player;
            this.GameManager = gameManager;
            this.messageHandler = this.GameManager.HandlePlayerMessage;
        }

        public override void Write(Message mex)
        {

        }

        public override Message ReadSync(int timeout_s)
        {
            return new ActionMessage(new EndTurnAction(AmaruCommon.GameAssets.Characters.CharacterEnum.AMARU, -1, GameManager.IsMainTurn));
            Player.PlayACreatureFromHand(Player.Hand[0].Id,AmaruCommon.Constants.Place.OUTER);
            return null;
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
