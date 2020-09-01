using AutoUpdaterDotNET;
using FModel.Grabber.Cdn;
using FModel.Windows.DarkMessageBox;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Windows;

namespace FModel.Utils
{
    static class Updater
    {
        public static void CheckForUpdate()
        {
            AutoUpdater.ParseUpdateInfoEvent += ParseUpdateInfoEvent;
            AutoUpdater.CheckForUpdateEvent += CheckForUpdateEvent;
            AutoUpdater.Start(Endpoints.FMODEL_JSON);
        }

        private static void ParseUpdateInfoEvent(ParseUpdateInfoEventArgs args)
        {
            CdnResponse json = JsonConvert.DeserializeObject<CdnResponse>(args.RemoteData);
            if (json != null)
            {
                args.UpdateInfo = new UpdateInfoEventArgs
                {
                    CurrentVersion = json.Updater.Version,
                    ChangelogURL = json.Updater.Changelog,
                    DownloadURL = json.Updater.Url,
                    Mandatory = json.Updater.Mandatory
                };
            }
        }

        private static void CheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            if (args != null)
            {
                if (args.IsUpdateAvailable)
                {
                    MessageBoxResult dialogResult = DarkMessageBoxHelper.ShowYesNoCancel(
                        string.Format(Properties.Resources.UpdateAvailableConfirm, args.CurrentVersion, args.InstalledVersion),
                        Properties.Resources.UpdateAvailable,
                        Properties.Resources.YesShowChangelog, // Yes
                        Properties.Resources.MaybeLater, // No
                        Properties.Resources.SkipThisVersion); // Cancel

                    if (dialogResult == MessageBoxResult.Yes)
                        Process.Start(new ProcessStartInfo { FileName = args.ChangelogURL, UseShellExecute = true });
                    if (dialogResult == MessageBoxResult.Cancel)
                    {
                        Properties.Settings.Default.SkipVersion = true;
                        Properties.Settings.Default.Save();
                    }

                    if (dialogResult == MessageBoxResult.Yes || dialogResult == MessageBoxResult.OK)
                    {
                        try
                        {
                            if (AutoUpdater.DownloadUpdate(args))
                            {
                                Application.Current.Shutdown();
                            }
                        }
                        catch (Exception exception)
                        {
                            DarkMessageBoxHelper.ShowOK(exception.Message, exception.GetType().ToString(), Properties.Resources.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            else DarkMessageBoxHelper.ShowOK(
                Properties.Resources.UpdateReachServerProblem,
                Properties.Resources.UpdateCheckFailed,
                Properties.Resources.OK,
                MessageBoxImage.Error);
        }
    }
}
