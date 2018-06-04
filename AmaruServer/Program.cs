using System;

using AmaruServer.Networking;
using AmaruServer.Logging;

namespace AmaruServer
{
    class Program
    {
        private static MainServer mainServer;
        static void Main(string[] args)
        {
            mainServer = MainServer.Instance;
            Console.ReadKey();
            LoggerManager.Close();
        }
    }
}
