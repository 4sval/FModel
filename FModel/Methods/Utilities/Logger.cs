using FModel.Methods.MessageBox;
using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FModel.Methods.Utilities
{
    class Logger
    {
        public delegate void MessageAddedEventHandler(string message);

        public event MessageAddedEventHandler MessageAdded;

        public string MessageFormat { get; set; } = "{0:yyyy-MM-dd HH:mm:ss.fff} - {1}";
        public bool AsyncWrite { get; set; } = true;
        public bool DebugWrite { get; set; } = Program.Build == Program.FModelBuild.Debug;
        public bool StringWrite { get; set; } = true;
        public bool FileWrite { get; set; } = false;
        public string LogFilePath { get; private set; }

        private readonly object loggerLock = new object();
        private readonly ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();
        private readonly StringBuilder sbMessages = new StringBuilder();

        public Logger()
        {
        }

        public Logger(string logFilePath)
        {
            FileWrite = true;
            LogFilePath = logFilePath;
            CreateDirectoryFromFilePath(LogFilePath);
        }

        protected void OnMessageAdded(string message)
        {
            if (MessageAdded != null)
            {
                MessageAdded(message);
            }
        }

        private void ProcessMessageQueue()
        {
            lock (loggerLock)
            {
                while (messageQueue.TryDequeue(out string message))
                {
                    if (DebugWrite)
                    {
                        Debug.Write(message);
                    }

                    if (StringWrite && sbMessages != null)
                    {
                        sbMessages.Append(message);
                    }

                    if (FileWrite && !string.IsNullOrEmpty(LogFilePath))
                    {
                        try
                        {
                            File.AppendAllText(LogFilePath, message, Encoding.UTF8);
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e);
                        }
                    }

                    OnMessageAdded(message);
                }
            }
        }

        public void Write(string message)
        {
            if (message != null)
            {
                message = string.Format(MessageFormat, DateTime.Now, message);
                messageQueue.Enqueue(message);

                if (AsyncWrite)
                {
                    Task.Run(() => ProcessMessageQueue());
                }
                else
                {
                    ProcessMessageQueue();
                }
            }
        }

        public void Write(string format, params object[] args)
        {
            Write(string.Format(format, args));
        }

        public void WriteLine(string message)
        {
            Write(message + Environment.NewLine);
        }

        public void WriteLine(string format, params object[] args)
        {
            WriteLine(string.Format(format, args));
        }

        public void WriteException(string exception, string message = "Exception")
        {
            WriteLine($"{message}:{Environment.NewLine}{exception}");
        }

        public void WriteException(Exception exception, string message = "Exception")
        {
            WriteException(exception.ToString(), message);
        }

        public void Clear()
        {
            lock (loggerLock)
            {
                if (sbMessages != null)
                {
                    sbMessages.Clear();
                }
            }
        }

        public override string ToString()
        {
            lock (loggerLock)
            {
                if (sbMessages != null && sbMessages.Length > 0)
                {
                    return sbMessages.ToString();
                }

                return string.Empty;
            }
        }

        public static void CreateDirectoryFromFilePath(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                string directoryPath = Path.GetDirectoryName(filePath);
                CreateDirectoryFromDirectoryPath(directoryPath);
            }
        }

        public static void CreateDirectoryFromDirectoryPath(string directoryPath)
        {
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                try
                {
                    Directory.CreateDirectory(directoryPath);
                }
                catch (Exception e)
                {
                    DebugHelper.WriteException(e);
                    DarkMessageBox.Show("Could not create directory.\r\n\r\n" + e, "FModel - Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public static string GetOperatingSystemProductName(bool includeBit = false)
        {
            string productName = null;

            try
            {
                productName = GetRegistryValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName", RegistryHive.LocalMachine);
            }
            catch
            {
                //hello world :)
            }

            if (string.IsNullOrEmpty(productName))
            {
                productName = Environment.OSVersion.VersionString;
            }

            if (includeBit)
            {
                string bit;

                if (Environment.Is64BitOperatingSystem)
                {
                    bit = "64";
                }
                else
                {
                    bit = "32";
                }

                productName = $"{productName} ({bit}-bit)";
            }

            return productName;
        }

        public static string GetRegistryValue(string path, string name = null, RegistryHive root = RegistryHive.CurrentUser)
        {
            using (RegistryKey rk = RegistryKey.OpenBaseKey(root, RegistryView.Default).OpenSubKey(path))
            {
                if (rk != null)
                {
                    return rk.GetValue(name, null) as string;
                }
            }

            return null;
        }
    }
}
