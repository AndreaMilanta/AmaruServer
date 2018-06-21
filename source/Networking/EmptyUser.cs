using AmaruCommon.GameAssets.Players;
using ClientServer.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AmaruServer.Networking
{
    public class EmptyUser : User
    {
        public EmptyUser(Player player, string logger) : base(logger)
        {
            this.Player = player;
        }

        public override void Write(Message mex)
        {
        }
    }
}
