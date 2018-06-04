using System;
using System.Net;
using System.Net.Sockets;

using AmaruServer.Constants;
using AmaruServer.Networking.Communication;
using AmaruServer.Logging;

namespace AmaruServer.Networking
{
    class ClientTCP
    {
        private int _index;
        private IPEndPoint _ipEndPoint;
        private Socket _socket;
        private bool _closing = false;
        private byte[] _buffer = new byte[NetworkConstants.BufferSize];

        public int Index { get => _index; set => _index = value; }
        public string Ip { get => NetworkConstants.ip2str(_ipEndPoint.Address) ; }
        public int Port { get => _ipEndPoint.Port; }
        public bool Closing { get => _closing; set => _closing = value; }

        public ClientTCP(Socket soc)
        {
            this._socket = soc;
            this._ipEndPoint = (IPEndPoint)_socket.RemoteEndPoint;
        }

        public void startClient()
        {
            _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceviceCallback), _socket);
            Closing = false;
        }

        private void ReceviceCallback(IAsyncResult ar)
        {
            Socket socket = (Socket)ar.AsyncState;
            try
            {
                int receivedSize = socket.EndReceive(ar);
                if (receivedSize <= 0)
                    CloseClient(_index);
                else
                {
                    byte[] databuffer = new byte[receivedSize];
                    Array.Copy(_buffer, databuffer, receivedSize);
                    //Handle data

                }
            }
            catch
            {
                CloseClient(_index);
            }
        }

        public void Write(Message mex) { }

        public void CloseClient(int index)
        {
            this._closing = true;
            this._socket.Close();
            LoggerManager.NetworkLogger.Log("Connection from " + this.Ip + " has been terminated");
        }
    }
}
