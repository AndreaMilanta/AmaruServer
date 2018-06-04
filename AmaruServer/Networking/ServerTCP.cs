using System;
using System.Net;
using System.Net.Sockets;

using AmaruServer.Constants;
using AmaruServer.Logging;

namespace AmaruServer.Networking
{
    abstract class ServerTCP
    {
        protected Socket _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        protected string _name;
    
        //public Socket ServerSocket { get => _serverSocket; }

        /// <summary>
        /// setup Server properties
        /// </summary>
        public void setupServer(int port)
        {
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            _serverSocket.Listen(NetworkConstants.MaxUsers);
            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            Socket socket = _serverSocket.EndAccept(ar);
            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
            HandleNewConnection(socket);
        }

        protected abstract void HandleNewConnection(Socket newSocket);
    }
}
