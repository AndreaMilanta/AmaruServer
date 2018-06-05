using System;
using System.Net.Sockets;

using AmaruServer.Constants;
using AmaruServer.Logging;

namespace AmaruServer.Networking
{
    class MainServer : ServerTCP
    {
        private static MainServer _instance = null;

        public static MainServer Instance { get => _instance == null ? new MainServer() : _instance; }

        private MainServer()
        {
            this._name = NetworkConstants.MainServerName;
            this.setupServer(NetworkConstants.ServerPort);
            LoggerManager.NetworkLogger.Log("MainServer online");
        }

        protected override void HandleNewConnection(Socket newSocket)
        {
            throw new System.NotImplementedException();
        }
    }
}
