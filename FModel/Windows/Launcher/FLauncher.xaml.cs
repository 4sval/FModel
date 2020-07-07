using FModel.Logger;
using FModel.Utils;
using FModel.ViewModels.ComboBox;
using FModel.Windows.CustomNotifier;
using Ookii.Dialogs.Wpf;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FModel.Windows.Launcher
{
    /// <summary>
    /// Logique d'interaction pour FLauncher.xaml
    /// </summary>
    public partial class FLauncher : Window
    {
        public string Path
        {
            get { return GamesPath_TxtBox.Text; }
        }

        public FLauncher()
        {
            InitializeComponent();
            ComboBoxVm.gamesCbViewModel.Clear();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            int i = 0;
            Games_CbBox.ItemsSource = ComboBoxVm.gamesCbViewModel;
            GamesPath_TxtBox.Text = Properties.Settings.Default.PakPath;

            string fortniteFilesPath = Paks.GetFortnitePakFilesPath();
            if (!string.IsNullOrEmpty(fortniteFilesPath))
            {
                DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[LauncherInstalled.dat]", $"Fortnite found at {fortniteFilesPath}");
                Globals.gNotifier.ShowCustomMessage("Fortnite", Properties.Resources.PathAutoDetected, "/FModel;component/Resources/fortnite.ico");
                ComboBoxVm.gamesCbViewModel.Add(new ComboBoxViewModel { Id = i++, Content = "Fortnite", Property = fortniteFilesPath });
            }

            string egl2FilesPath = EGL2.GetEGL2PakFilesPath();
            if (!string.IsNullOrEmpty(egl2FilesPath))
            {
                DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[EGL2]", $"Fortnite found at {egl2FilesPath}");
                Globals.gNotifier.ShowCustomMessage("Fortnite", Properties.Resources.PathAutoDetected, "/FModel;component/Resources/egl2.ico");
                ComboBoxVm.gamesCbViewModel.Add(new ComboBoxViewModel { Id = i++, Content = "Fortnite [EGL2]", Property = egl2FilesPath });
            }

            string valorantFilesPath = Paks.GetValorantPakFilesPath();
            if (!string.IsNullOrEmpty(valorantFilesPath))
            {
                DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[RiotClientInstalls.json]", $"Valorant found at {valorantFilesPath}");
                Globals.gNotifier.ShowCustomMessage("Valorant", Properties.Resources.PathAutoDetected, "/FModel;component/Resources/valorant.live.ico");
                ComboBoxVm.gamesCbViewModel.Add(new ComboBoxViewModel { Id = i++, Content = "Valorant", Property = valorantFilesPath });
            }

            string borderlands3FilesPath = Paks.GetBorderlands3PakFilesPath();
            if (!string.IsNullOrEmpty(borderlands3FilesPath))
            {
                DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[LauncherInstalled.dat]", $"Borderlands 3 found at {borderlands3FilesPath}");
                Globals.gNotifier.ShowCustomMessage("Borderlands 3", Properties.Resources.PathAutoDetected, "/FModel;component/Resources/borderlands3.ico");
                ComboBoxVm.gamesCbViewModel.Add(new ComboBoxViewModel { Id = i++, Content = "Borderlands 3", Property = borderlands3FilesPath });
            }
            
            string minecraftdungeonsFilesPath = Paks.GetMinecraftDungeonsPakFilesPath();
            if (!string.IsNullOrEmpty(minecraftdungeonsFilesPath))
            {
                DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[launcher_settings.json]", $"Minecraft Dungeons found at {minecraftdungeonsFilesPath}");
                Globals.gNotifier.ShowCustomMessage("Minecraft Dungeons", Properties.Resources.PathAutoDetected, "/FModel;component/Resources/minecraftdungeons.ico");
                ComboBoxVm.gamesCbViewModel.Add(new ComboBoxViewModel { Id = i++, Content = "Minecraft Dungeons", Property = minecraftdungeonsFilesPath });
            }

            string battlebreakersFilesPath = Paks.GetBattleBreakersPakFilesPath();
            if (!string.IsNullOrEmpty(battlebreakersFilesPath))
            {
                DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[LauncherInstalled.dat]", $"Battle Breakers found at {battlebreakersFilesPath}");
                Globals.gNotifier.ShowCustomMessage("Battle Breakers", Properties.Resources.PathAutoDetected, "/FModel;component/Resources/battlebreakers.ico");
                ComboBoxVm.gamesCbViewModel.Add(new ComboBoxViewModel { Id = i++, Content = "Battle Breakers", Property = battlebreakersFilesPath });
            }

            string spellbreakerFilesPath = Paks.GetSpellbreakPakFilesPath();
            if (!string.IsNullOrEmpty(spellbreakerFilesPath))
            {
                DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[LauncherInstalled.dat]", $"Spellbreak found at {spellbreakerFilesPath}");
                Globals.gNotifier.ShowCustomMessage("Spellbreak", Properties.Resources.PathAutoDetected, "/FModel;component/Resources/spellbreak.ico");
                ComboBoxVm.gamesCbViewModel.Add(new ComboBoxViewModel { Id = i++, Content = "Spellbreak", Property = spellbreakerFilesPath });
            }

            Games_CbBox.SelectedItem = ComboBoxVm.gamesCbViewModel.Where(x => x.Property.ToString() == Properties.Settings.Default.PakPath).FirstOrDefault();
        }

        private void OnClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            Close();
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxViewModel item)
                GamesPath_TxtBox.Text = item.Property.ToString();
        }

        private void OnInputClick(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog
            {
                ShowNewFolderButton = false,
                Description = Properties.Resources.SelectFolder,
                UseDescriptionForTitle = true
            };

            if ((bool)dialog.ShowDialog(this))
            {
                GamesPath_TxtBox.Text = dialog.SelectedPath;
            }
        }
    }
}
