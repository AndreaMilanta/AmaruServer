using System;
using System.Collections.Generic;

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
            Tester.Execute();
            try 
            {
                LoggerManager.SetupLoggerManager(ServerConstants.LOG_PATH, timerDt: 1);
                mainServer = MainServer.Instance;
                mainServer.Start();
                Console.ReadKey();
                ConnectionManager.Instance.Shutdown();
                LoggerManager.Instance.Close();
            }
            catch(Exception e) { LoggerManager.Instance.Close(); throw e; }
        }
    }
}
