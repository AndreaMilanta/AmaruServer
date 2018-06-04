using System;
using System.IO;
using System.Text;

using AmaruServer.Constants;

namespace AmaruServer.Logging
{
    class Logger
    {
        private StreamWriter _sw;
        private StringBuilder _content;

        public Logger(string logName)
        {
            string date = DateTime.Now.ToString(LogConstants.NAME_DATE_FORMAT);
            _sw = new StreamWriter(LogConstants.LOG_PATH + date + "_" + logName + ".log");
            _content = new StringBuilder();
        }

        public void LogException(string excEvent)
        {
            string logEntry = DateTime.Now.ToString(LogConstants.DATE_FORMAT) + " - !! " + excEvent + " !!";
            if (LoggerManager.ToConsole)
                Console.WriteLine(logEntry);
            lock (this._content)
                _content.AppendLine(logEntry);
        }

        public void Log(string logEvent)
        {
            string logEntry = DateTime.Now.ToString(LogConstants.DATE_FORMAT) + " - " + logEvent;
            if (LoggerManager.ToConsole)
                Console.WriteLine(logEntry);
            lock (this._content)
                _content.AppendLine(logEntry);
        }

        public void ToFile()
        {
            lock (this._content)
            {
                _sw.Write(_content.ToString());
                _content.Clear();
            }
        }

        public void Close()
        {
            this.ToFile();
            _sw.Close();
        }
    }
}
