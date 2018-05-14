using System;
using System.Linq;
using System.Net;

namespace AmaruServer.Constants
{
    static class NetworkConstants
    {
        public const int BufferSize = 1024;         // Network Receiving Buffer size
        public const int ServerPort = 5555;         // Main Server Receiving Port

        public const int MaxUsers = 100;            // Maximum amount of connected users

        /// <summary>
        /// converts IP address in standard string format to uint32
        /// </summary>
        public static uint ip2uint(string ip) => BitConverter.ToUInt32(IPAddress.Parse(ip).GetAddressBytes().Reverse().ToArray(), 0);

        /// <summary>
        /// Converts IP address as uint to standard string format
        /// </summary>
        public static string ip2str(uint ip) => new IPAddress(BitConverter.GetBytes(ip).Reverse().ToArray()).ToString();
    }
}
