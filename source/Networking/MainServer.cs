using System;
using System.Net.Sockets;

using Logging;
using ClientServer.Communication;
using AmaruCommon.Constants;
using AmaruCommon.Client;
using AmaruServer.Constants;

namespace AmaruServer.Networking
{
    class MainServer : ASyncServerTCP
    {
        private static MainServer _instance = null;

        public static MainServer Instance { get => _instance ?? new MainServer(); }

        private MainServer():base(ServerConstants.ServerName, ServerConstants.ServerLogger)
        {
            this.Setup(NetworkConstants.ServerPort, ServerConstants.MaxUsers);
        }

        protected override void HandleNewConnection(Socket newSocket)
        {
            Client client = new Client(newSocket, NetworkConstants.BufferSize, ServerConstants.ServerLogger);
        }
    }
}
