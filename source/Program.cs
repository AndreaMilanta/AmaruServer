using System;

using AmaruServer.Networking;

namespace AmaruServer
{
    class Program
    {
        private static MainServer mainServer;
        static void Main(string[] args)
        {
            //Logger
            mainServer = MainServer.Instance;
            Console.ReadKey();
            //LoggerManager.Close();
        }
    }
}
