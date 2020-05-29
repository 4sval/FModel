using FModel.Windows.UserInput;
using Newtonsoft.Json;
using PakReader.Pak;
using PakReader.Parsers.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace FModel.ViewModels.MenuItem
{
    static class MenuItems
    {
        // dynamic = PakMenuItemViewModel + Separator
        public static ObservableCollection<dynamic> pakFiles = new ObservableCollection<dynamic>();

        // dynamic = BackupMenuItemViewModel + Separator
        public static ObservableCollection<dynamic> backupFiles = new ObservableCollection<dynamic>();

        // dynamic = GotoMenuItemViewModel + Separator
        public static ObservableCollection<dynamic> customGoTos = new ObservableCollection<dynamic>
        {
            new GotoMenuItemViewModel { Header = Properties.Resources.AddDirectory, Icon = new Image { Source = new BitmapImage(new Uri("/FModel;component/Resources/sign-direction-plus.png", UriKind.Relative)) } },
            new Separator()
        };

        public static void FeedCustomGoTos()
        {
            GotoMenuItemViewModel[] customs = JsonConvert.DeserializeObject<GotoMenuItemViewModel[]>(Properties.Settings.Default.CustomGoTos);
            if (customs != null)
            {
                foreach (var mi in customs)
                {
                    mi.Childrens = new ObservableCollection<GotoMenuItemViewModel>
                    {
                        new GotoMenuItemViewModel
                        {
                            Header = Properties.Resources.GoTo,
                            Icon = new Image { Source = new BitmapImage(new Uri("/FModel;component/Resources/share.png", UriKind.Relative)) },
                            Parent = mi
                        },
                        new GotoMenuItemViewModel
                        {
                            Header = Properties.Resources.EditDirectory,
                            Icon = new Image { Source = new BitmapImage(new Uri("/FModel;component/Resources/pencil.png", UriKind.Relative)) },
                            Parent = mi
                        },
                        new GotoMenuItemViewModel
                        {
                            Header = Properties.Resources.RemoveDirectory,
                            Icon = new Image { Source = new BitmapImage(new Uri("/FModel;component/Resources/delete-forever.png", UriKind.Relative)) },
                            StaysOpenOnClick = true,
                            Parent = mi
                        }
                    };
                    customGoTos.Add(mi);
                }
            }
        }

        public static void AddCustomGoTo() => AddCustomGoTo(string.Empty, string.Empty);
        public static void AddCustomGoTo(string name, string dir)
        {
            var input = new GoToUserInput(name, dir);
            if ((bool)input.ShowDialog())
            {
                var newDir = new GotoMenuItemViewModel
                {
                    Header = input.Name,
                    DirectoryPath = input.Directory
                };
                newDir.Childrens = new ObservableCollection<GotoMenuItemViewModel>
                {
                    new GotoMenuItemViewModel
                    {
                        Header = Properties.Resources.GoTo,
                        Icon = new Image { Source = new BitmapImage(new Uri("/FModel;component/Resources/share.png", UriKind.Relative)) },
                        Parent = newDir
                    },
                    new GotoMenuItemViewModel
                    {
                        Header = Properties.Resources.EditDirectory,
                        Icon = new Image { Source = new BitmapImage(new Uri("/FModel;component/Resources/pencil.png", UriKind.Relative)) },
                        Parent = newDir
                    },
                    new GotoMenuItemViewModel
                    {
                        Header = Properties.Resources.RemoveDirectory,
                        Icon = new Image { Source = new BitmapImage(new Uri("/FModel;component/Resources/delete-forever.png", UriKind.Relative)) },
                        StaysOpenOnClick = true,
                        Parent = newDir
                    }
                };

                customGoTos.Add(newDir);
                Properties.Settings.Default.CustomGoTos = JsonConvert.SerializeObject(customGoTos.Skip(2), Formatting.None);
                Properties.Settings.Default.Save();
            }
        }

        // PLEASE DON'T USE THIS FOR BACKUP FILES
        // comment methods you don't use, thx
        public static bool AtLeastOnePak(this ObservableCollection<dynamic> o) => 
            Application.Current.Dispatcher.Invoke(() => o.Any(x => !x.GetType().Equals(typeof(Separator)) && x.PakFile != null));
        public static IEnumerable<PakMenuItemViewModel> GetMenuItemWithPakFiles(this ObservableCollection<dynamic> o) => 
            Application.Current.Dispatcher.Invoke(() => o.Where(x => !x.GetType().Equals(typeof(Separator)) && x.PakFile != null).Select(x => (PakMenuItemViewModel)x));
        public static int GetPakCount(this ObservableCollection<dynamic> o) =>
            Application.Current.Dispatcher.Invoke(() => o.GetMenuItemWithPakFiles().Count());
        public static IEnumerable<PakFileReader> GetPakFileReaders(this ObservableCollection<dynamic> o) =>
            Application.Current.Dispatcher.Invoke(() => o.GetMenuItemWithPakFiles().Select(x => x.PakFile));
        public static IEnumerable<PakFileReader> GetDynamicPakFileReaders(this ObservableCollection<dynamic> o) =>
            Application.Current.Dispatcher.Invoke(() => o.GetPakFileReaders().Where(x => !x.Info.EncryptionKeyGuid.Equals(new FGuid(0u, 0u, 0u, 0u))));
    }
}
