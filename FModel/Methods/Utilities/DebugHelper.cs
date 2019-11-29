using System;
using System.Diagnostics;

namespace FModel.Methods.Utilities
{
    class DebugHelper
    {
        public static Logger Logger { get; private set; }

        public static void Init(string logFilePath)
        {
            Logger = new Logger(logFilePath);
        }

        public static void WriteLine(string message = "")
        {
            if (Logger != null)
            {
                Logger.WriteLine(message);
            }
            else
            {
                Debug.WriteLine(message);
            }
        }

        public static void WriteLine(string format, params object[] args)
        {
            WriteLine(string.Format(format, args));
        }

        public static void WriteException(string exception, string message = "Exception")
        {
            if (Logger != null)
            {
                Logger.WriteException(exception, message);
            }
            else
            {
                Debug.WriteLine(exception);
            }
        }

        public static void WriteException(Exception exception, string message = "Exception")
        {
            WriteException(exception.ToString(), message);
        }
    }
}
