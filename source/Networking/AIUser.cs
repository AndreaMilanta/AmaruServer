using AmaruCommon.GameAssets.Player;
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
            this.messageHandler = this.HandlePlayerMessage;
        }

        public override void Write(Message mex)
        {

        }

        public override void ReadSync(int timeout_s)
        {

        }

        public override void ReadASync(bool continuous)
        {
            
        }

        public override void Close(Message notification = null)
        {

        }
    }
}
