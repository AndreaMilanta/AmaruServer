using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Timers;

using AmaruServer.Constants;

namespace AmaruServer.Logging
{
    static class LoggerManager
    {
        static private bool _toConsole = true;
        static public bool  ToConsole { get => _toConsole;  }

        static private Timer timer = new Timer(LogConstants.TIMER_TIME_s * 1000);

        static public Logger NetworkLogger = new Logger("NetworkLogger");
        static LoggerManager()
        {
            CultureInfo.CurrentCulture = new CultureInfo(LogConstants.CULTURE);
            timer.Elapsed += LogToFile;
            timer.Start();
        }

        private static void LogToFile(Object source, ElapsedEventArgs e)
        {
            Console.WriteLine("Logged to File");
            NetworkLogger.ToFile();
        }

        public static void Close()
        {
            NetworkLogger.Close();
        }
    }
}
