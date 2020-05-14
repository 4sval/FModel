using System;
using System.Configuration;
using System.Diagnostics;

namespace FModel.Logger
{
    static class DebugHelper
    {
        public static Logger Logger { get; private set; }

        public static void Init(string logFilePath) => Logger = new Logger(logFilePath);

        public static void WriteLine(string message = "")
        {
            if (Logger != null)
                Logger.WriteLine(message);
            else
                Debug.WriteLine(message);
        }

        public static void WriteLine(string format, params object[] args) => WriteLine(string.Format(format, args));

        public static void WriteException(string exception, string message = "Exception")
        {
            if (Logger != null)
                Logger.WriteException(exception, message);
            else
                Debug.WriteLine(exception);
        }

        public static void WriteException(Exception exception, string message = "Exception") => WriteException(exception.ToString(), message);

        public static void WriteUserSettings()
        {
            foreach (SettingsProperty currentProperty in Properties.Settings.Default.Properties)
            {
                WriteLine("{0} {1} {2} {3}", "[FModel]", "[User Settings]", $"[{currentProperty.Name}]", Properties.Settings.Default[currentProperty.Name]);
            }
        }
    }
}
