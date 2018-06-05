using System;
using System.Linq;
using System.Net;

namespace AmaruServer.Constants
{
    static class NetworkConstants
    {
        public const string MainServerName = "MainServer";

        public const int BufferSize = 1024;         // Network Receiving Buffer size
        public const int ServerPort = 5555;         // Main Server Receiving Port

        public const int MaxUsers = 100;            // Maximum amount of connected users
    }
}
