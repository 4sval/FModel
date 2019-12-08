using FModel.Methods.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using FProp = FModel.Properties.Settings;

namespace FModel
{
    static class Program
    {
        private static bool isClosing;
        public enum FModelBuild
        {
            Debug,
            Release,
            Unknown
        }
        public const FModelBuild Build =
#if RELEASE
            FModelBuild.Release;
#elif DEBUG
            FModelBuild.Debug;
#else
            FModelBuild.Unknown;
#endif
        internal static Stopwatch StartTimer { get; private set; }


        [STAThreadAttribute]
        public static void Main()
        {
            StartTimer = Stopwatch.StartNew();

            DebugHelper.Init(LogsFilePath);
            DebugHelper.WriteLine("=================================================================== FModel ===================================================================");
            DebugHelper.WriteLine("FModel starting.");
            DebugHelper.WriteLine("Version: " + Assembly.GetExecutingAssembly().GetName().Version.ToString());
            DebugHelper.WriteLine("Build: " + Build);
            DebugHelper.WriteLine("OS: " + Logger.GetOperatingSystemProductName(true));

            Dictionary<string, Assembly> assemblies = new Dictionary<string, Assembly>();
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            IEnumerable<string> resources = executingAssembly.GetManifestResourceNames().Where(n => n.EndsWith(".dll"));

            foreach (string resource in resources)
            {
                using (Stream stream = executingAssembly.GetManifestResourceStream(resource))
                {
                    if (stream == null) { continue; }

                    byte[] bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, bytes.Length);
                    try
                    {
                        assemblies.Add(resource, Assembly.Load(bytes));
                    }
                    catch (Exception ex)
                    {
                        DebugHelper.WriteLine(string.Format("Failed to load: {0}, Exception: {1}", resource, ex.Message));
                    }
                }
            }

            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                AssemblyName assemblyName = new AssemblyName(e.Name);
                string path = string.Format("{0}.dll", assemblyName.Name);

                if (assemblies.ContainsKey(path))
                {
                    return assemblies[path];
                }

                return null;
            };
            App.Main();

            CloseWithLogs();
        }

        public static void CloseWithLogs()
        {
            if (!isClosing)
            {
                isClosing = true;

                DebugHelper.Logger.AsyncWrite = false;
                Properties.Settings.Default.Save();

                DebugHelper.WriteLine("FModel closed.");
            }
        }

        public static string LogsFilePath
        {
            get
            {
                string filename = string.Format("FModel-Log-{0:yyyy-MM-dd}.txt", DateTime.Now);

                // Copy user settings from previous application version if necessary
                if (FProp.Default.FUpdateSettings)
                {
                    FProp.Default.Upgrade();
                    FProp.Default.FUpdateSettings = false;
                    FProp.Default.Save();

                    DebugHelper.WriteLine("User settings copied from previous version");
                }
                FoldersUtility.LoadFolders();

                return Path.Combine(Properties.Settings.Default.FOutput_Path + "\\Logs", filename);
            }
        }
    }
}
