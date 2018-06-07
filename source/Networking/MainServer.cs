using System;
using System.Net.Sockets;

using Logging;
using ClientServer.Communication;
using AmaruCommon.Constants;
using AmaruServer.Constants;

namespace AmaruServer.Networking
{
    class MainServer : ASyncServerTCP
    {
        private static MainServer _instance = null;

        public static MainServer Instance { get => _instance ?? new MainServer(); }

        private MainServer():base(ServerConstants.ServerName, ServerConstants.ServerLogger)
        {
            MainServer._instance = this;
            this.Setup(NetworkConstants.ServerPort, ServerConstants.MaxUsers);
        }

        protected override void HandleNewConnection(Socket newSocket)
        {
            new User(newSocket, ServerConstants.ServerLogger);
        }
    }
}
