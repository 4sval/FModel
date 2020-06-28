using FModel.Discord;
using FModel.Logger;
using FModel.Utils;
using FModel.ViewModels.StatusBar;
using FModel.Windows.DarkMessageBox;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace FModel
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal static Stopwatch StartTimer { get; private set; }
        static bool framerateSet = false;

        protected override void OnStartup(StartupEventArgs e)
        {
            StartTimer = Stopwatch.StartNew();

            DebugHelper.Init(LogsFilePath); // get old settings too

            if (FModel.Properties.Settings.Default.UseEnglish) // use old settings here
                Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");

            DebugHelper.WriteLine("{0} {1}", "[FModel]", "––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––");
            DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Version]", Assembly.GetExecutingAssembly().GetName().Version.ToString());
            DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Build]", Globals.Build);
            DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[OS]", Logger.Logger.GetOperatingSystemProductName(true));
            DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Runtime]", RuntimeInformation.FrameworkDescription);
            DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Culture]", Thread.CurrentThread.CurrentUICulture);

            StatusBarVm.statusBarViewModel.Set(FModel.Properties.Resources.Initializing, FModel.Properties.Resources.Loading);

            base.OnStartup(e);
        }

        public static string LogsFilePath
        {
            get
            {
                string filename = string.Format("FModel-Log-{0:yyyy-MM-dd}.txt", DateTime.Now);

                // Copy user settings from previous application version if necessary
                if (FModel.Properties.Settings.Default.UpdateSettings)
                    FModel.Properties.Settings.Default.Upgrade();

                Folders.LoadFolders();

                return Path.Combine(FModel.Properties.Settings.Default.OutputPath + "\\Logs", filename);
            }
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            string errorMessage = string.Format(FModel.Properties.Resources.UnhandledExceptionOccured, e.Exception.Message);
            DebugHelper.WriteException(e.Exception, "thrown in App.xaml.cs by OnDispatcherUnhandledException");
            DarkMessageBoxHelper.Show(errorMessage, FModel.Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        internal static void SetFramerate()
        {
            if (!framerateSet)
            {
                System.Windows.Media.Animation.Timeline.DesiredFrameRateProperty.OverrideMetadata(
                    typeof(System.Windows.Media.Animation.Timeline),
                    new FrameworkPropertyMetadata { DefaultValue = 10 });
                framerateSet = true;
            }
        }
    }
}
