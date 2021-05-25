using System.Diagnostics;
using AdonisUI.Controls;
using FModel.Framework;
using FModel.Settings;
using FModel.Views;
using FModel.Views.Resources.Controls;

namespace FModel.ViewModels.Commands
{
    public class MenuCommand : ViewModelCommand<ApplicationViewModel>
    {
        public MenuCommand(ApplicationViewModel contextViewModel) : base(contextViewModel)
        {
        }

        public override void Execute(ApplicationViewModel contextViewModel, object parameter)
        {
            switch (parameter)
            {
                case "Directory_Selector":
                    contextViewModel.AvoidEmptyGameDirectoryAndSetEGame(true);
                    break;
                case "Directory_AES":
                    Helper.OpenWindow<AdonisWindow>("AES Manager", () => new AesManager().Show());
                    break;
                case "Directory_Backup":
                    Helper.OpenWindow<AdonisWindow>("Backup Manager", () => new BackupManager(contextViewModel.CUE4Parse.Provider.GameName).Show());
                    break;
                case "Views_AudioPlayer":
                    Helper.OpenWindow<AdonisWindow>("Audio Player", () => new AudioPlayer().Show());
                    break;
                case "Views_MapViewer":
                    Helper.OpenWindow<AdonisWindow>("Map Viewer", () => new MapViewer().Show());
                    break;
                case "Views_ImageMerger":
                    Helper.OpenWindow<AdonisWindow>("Image Merger", () => new ImageMerger().Show());
                    break;
                case "Settings":
                    Helper.OpenWindow<AdonisWindow>("Settings", () => new SettingsView().Show());
                    break;
                case "Help_About":
                    Helper.OpenWindow<AdonisWindow>("About", () => new About().Show());
                    break;
                case "Help_Donate":
                    Process.Start(new ProcessStartInfo {FileName = Constants.DONATE_LINK, UseShellExecute = true});
                    break;
                case "Help_Changelog":
                    Process.Start(new ProcessStartInfo {FileName = Constants.CHANGELOG_LINK, UseShellExecute = true});
                    break;
                case "Help_BugsReport":
                    Process.Start(new ProcessStartInfo {FileName = Constants.ISSUE_LINK, UseShellExecute = true});
                    break;
                case "Help_Discord":
                    Process.Start(new ProcessStartInfo {FileName = Constants.DISCORD_LINK, UseShellExecute = true});
                    break;
                case "ToolBox_Clear_Logs":
                    FLogger.Logger.Text = string.Empty;
                    break;
                case "ToolBox_Open_Output_Directory":
                    Process.Start(new ProcessStartInfo {FileName = UserSettings.Default.OutputDirectory, UseShellExecute = true});
                    break;
            }
        }
    }
}