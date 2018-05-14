using System;
using System.Net;
using System.Net.Sockets;

using AmaruServer.Constants;

namespace AmaruServer.Networking
{
    abstract class ServerTCP
    {
        private Socket _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private byte[] _buffer = new byte[NetworkConstants.BufferSize];
    
        //public static Socket ServerSocket { get => _serverSocket; set => _serverSocket = value; }

        /// <summary>
        /// setup Server properties
        /// </summary>
        public void setupServer()
        {
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, NetworkConstants.ServerPort));
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
