using System;
using System.IO;

namespace AmaruServer.Constants
{
    class ServerConstants
    {
        // Logging
        public const string ServerLogger = "MainServer";
        public const string ConnMngLogger = "ConnectionManagerLogger";
        public static string LOG_PATH {
            get {
                if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\Logs"))
                    Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "\\Logs");
                return AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\";
            }
        }

        // Server specific
        public const string ServerName = ServerLogger;

        // Server parameters
        public const int MaxUsers = 100;            // Maximum amount of connected users

        // Threading parameters
        public const int SleepTime_ms = 10;         // Thread sleeping time in ms
    }
}
