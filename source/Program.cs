using System;

using Logging;

using AmaruServer.Constants;
using AmaruServer.Networking;

namespace AmaruServer
{
    class Program
    {
        private static MainServer mainServer;
        static void Main(string[] args)
        {
            LoggerManager.SetupLoggerManager(ServerConstants.LOG_PATH);
            mainServer = MainServer.Instance;
            mainServer.Start();
            Console.ReadKey();
            ConnectionManager.Instance.Shutdown();
            LoggerManager.Instance.Close();
        }
    }
}
