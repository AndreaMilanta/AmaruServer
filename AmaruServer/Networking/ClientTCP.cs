using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

using AmaruServer.Constants;
using AmaruServer.Networking.Communication;
using AmaruServer.Logging;

namespace AmaruServer.Networking
{
    class ClientTCP
    {
        private Socket _socket = null;
        private byte[] _buffer = new byte[NetworkConstants.BufferSize];
        private IFormatter formatter = new BinaryFormatter();
        Stream _stream = null;

        public int Index { get; private set; }
        public string Ip { get => NetworkConstants.ip2str(((IPEndPoint)_socket.RemoteEndPoint).Address); }
        public int Port { get => ((IPEndPoint)_socket.RemoteEndPoint).Port; }
        public bool Closing { get; private set; } = false;

        public ClientTCP(Socket soc)
        {
            this._socket = soc;
            this._stream = new MemoryStream(_buffer);
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

                //Read Data
                _stream.Position = 0;
                byte[] databuffer = new byte[receivedSize];
                _socket.Receive(databuffer, receivedSize, 0);
                Array.Copy(_buffer, databuffer, receivedSize);

                //Get original type
                Message recMex = (Message)formatter.Deserialize(_stream);

                //Handle data

            }
            catch
            {
                LoggerManager.NetworkLogger.LogException("Error Reading from sokcet");
                CloseClient(Index);
            }
        }

        public void Write(Message mex)
        {
            formatter.Serialize(_stream, mex);
            _stream.Flush();
            _socket.Send(_buffer, _buffer.Length, 0);
            _stream.Position = 0;
        }

        public void CloseClient(int index)
        {
            this.Closing = true;
            this._socket.Close();
            LoggerManager.NetworkLogger.Log("Connection from " + this.Ip + " has been terminated");
        }
    }
}
