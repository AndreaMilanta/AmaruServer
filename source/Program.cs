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
            TestFunction();
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

        /// <summary>
        /// Function called before everything
        /// put here code to test
        /// </summary>
        public static void TestFunction()
        {
            List<int> testList = new List<int>() { 1, 2, 3, 4, 5, 6, 7 };
            Console.Write("ordered:  ");
            testList.ForEach(i => Console.Write(i + ", "));
            Console.WriteLine();
            AmaruCommon.Constants.Tools.Shuffle(testList);
            Console.Write("shuffled: ");
            testList.ForEach(i => Console.Write(i + ", "));
            Console.WriteLine();
        }
    }
}
