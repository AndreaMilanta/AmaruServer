using System;
using System.Net;
using System.Net.Sockets;

using AmaruServer.Constants;

namespace AmaruServer.Networking
{
    class ClientTCP
    {
        private int _index;
        private uint _ip;
        public Socket socket;
        private bool _closing = false;
        private byte[] _buffer = new byte[NetworkConstants.BufferSize];

        public int Index { get => _index; set => _index = value; }
        public uint Ip { get => _ip; set => _ip = value; }
        public bool Closing { get => _closing; set => _closing = value; }

        public void startClient()
        {
            socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceviceCallback), socket);
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

        public void CloseClient(int index)
        {
            _closing = true;
            Console.WriteLine("Connection from {0} has been terminated", NetworkConstants.ip2str(_ip));
            socket.Close();
        }
    }
}
