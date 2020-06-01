using FModel.Logger;
using FModel.Utils;
using FModel.ViewModels.MenuItem;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace FModel.Grabber.Cdn
{
    static class CdnDataGrabber
    {
        private static CdnResponse _data = null; // avoid multiple requests on same endpoint

        public static async Task DoCDNStuff()
        {
            await PopulateBackups().ConfigureAwait(false); // step by step
            await ShowGMessages().ConfigureAwait(false); // step by step
        }

        public static async Task PopulateBackups()
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                // Backup PAKs Menu Item
                MenuItems.backupFiles.Add(new BackupMenuItemViewModel
                {
                    Header = Properties.Resources.BackupPaks,
                    Icon = new Image { Source = new BitmapImage(new Uri("Resources/backup-restore.png", UriKind.Relative)) }
                });
                MenuItems.backupFiles.Add(new Separator { });
            });

            List<BackupMenuItemViewModel> backupsInfos = await GetBackups().ConfigureAwait(false);
            if (backupsInfos.Any())
            {
                foreach (BackupMenuItemViewModel b in backupsInfos)
                {
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[CDN]", $"{b.Header} is available to download");
                        MenuItems.backupFiles.Add(b);
                    });
                }
            }
        }

        public static async Task<FUpdater> GetUpdateData()
        {
            if (_data == null)
                _data = await CdnData.GetData().ConfigureAwait(false);

            if (_data != null)
            {
                return _data.Updater;
            }
            return null;
        }

        private static async Task<List<BackupMenuItemViewModel>> GetBackups()
        {
            if (_data == null)
                _data = await CdnData.GetData().ConfigureAwait(false);

            if (_data != null)
            {
                return JsonConvert.DeserializeObject<List<BackupMenuItemViewModel>>(JsonConvert.SerializeObject(_data.Backups[Globals.Game.ActualGame.ToString()]));
            }
            return new List<BackupMenuItemViewModel>();
        }

        public static async Task ShowGMessages()
        {
            Dictionary<string, GlobalMessage[]> globalMessages = await GetGlobalMessages().ConfigureAwait(false);
            if (globalMessages.Any())
            {
                string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                if (globalMessages.ContainsKey(version))
                    if (!string.IsNullOrEmpty(globalMessages[version][0].Message))
                        foreach (GlobalMessage gm in globalMessages[version])
                            FConsole.AppendText(gm.Message, gm.Color, gm.NewLine);
            }
        }

        private static async Task<Dictionary<string, GlobalMessage[]>> GetGlobalMessages()
        {
            if (_data == null && CdnData.bInternet) // if data is still null after getting backups, that means there's no internet, do not try again
                _data = await CdnData.GetData().ConfigureAwait(false); // ^ will also avoid showing "No internet" notifier twice

            if (_data != null)
            {
                return JsonConvert.DeserializeObject<Dictionary<string, GlobalMessage[]>>(JsonConvert.SerializeObject(_data.GlobalMessages));
            }
            return new Dictionary<string, GlobalMessage[]>();
        }
    }
}
