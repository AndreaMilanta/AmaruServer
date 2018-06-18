
namespace AmaruServer.Constants
{
    class ServerConstants
    {
        // Logging
        public const string ServerLogger = "MainServer";
        public const string ConnMngLogger = "ConnMngLogger";
        public const string LOG_PATH =  ".\\..\\Projects\\Visual Studio 2017\\AmaruServer\\Logs\\";

        // Server specific
        public const string ServerName = ServerLogger;

        // Server parameters
        public const int MaxUsers = 100;            // Maximum amount of connected users

        // Threading parameters
        public const int SleepTime_ms = 10;         // Thread sleeping time in ms
    }
}
