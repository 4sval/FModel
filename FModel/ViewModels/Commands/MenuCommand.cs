using System.Diagnostics;
using AdonisUI.Controls;
using FModel.Extensions;
using FModel.Framework;
using FModel.Services;
using FModel.Settings;
using FModel.Views;
using FModel.Views.Resources.Controls;
using Newtonsoft.Json;

namespace FModel.ViewModels.Commands;

public class MenuCommand : ViewModelCommand<ApplicationViewModel>
{
    public MenuCommand(ApplicationViewModel contextViewModel) : base(contextViewModel)
    {
    }

    public override async void Execute(ApplicationViewModel contextViewModel, object parameter)
    {
        switch (parameter)
        {
            case "Directory_Selector":
                contextViewModel.AvoidEmptyGameDirectory(true);
                break;
            case "Directory_AES":
                Helper.OpenWindow<AdonisWindow>("AES Manager", () => new AesManager().Show());
                break;
            case "Directory_Backup":
                Helper.OpenWindow<AdonisWindow>("Backup Manager", () => new BackupManager(contextViewModel.CUE4Parse.Provider.ProjectName).Show());
                break;
            case "Directory_ArchivesInfo":
                ApplicationService.ApplicationView.IsAssetsExplorerVisible = false;
                contextViewModel.CUE4Parse.TabControl.AddTab("Archives Info");
                contextViewModel.CUE4Parse.TabControl.SelectedTab.Highlighter = AvalonExtensions.HighlighterSelector("json");
                contextViewModel.CUE4Parse.TabControl.SelectedTab.SetDocumentText(JsonConvert.SerializeObject(contextViewModel.CUE4Parse.GameDirectory.DirectoryFiles, Formatting.Indented), false, false);
                break;
            case "Views_3dViewer":
                contextViewModel.CUE4Parse.SnooperViewer.Run();
                break;
            case "Views_ExportSession":
                Helper.OpenWindow<AdonisWindow>("Export Session", () => new ExportSessionWindow().Show());
                break;
            case "Views_AudioPlayer":
                Helper.OpenWindow<AdonisWindow>("Audio Player", () => new AudioPlayer().Show());
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
                Process.Start(new ProcessStartInfo { FileName = Constants.DONATE_LINK, UseShellExecute = true });
                break;
            case "Help_Releases":
                Helper.OpenWindow<AdonisWindow>("Releases", () => new UpdateView().Show());
                break;
            case "Help_BugsReport":
                Process.Start(new ProcessStartInfo { FileName = Constants.ISSUE_LINK, UseShellExecute = true });
                break;
            case "Help_Discord":
                Process.Start(new ProcessStartInfo { FileName = Constants.DISCORD_LINK, UseShellExecute = true });
                break;
            case "ToolBox_Clear_Logs":
                FLogger.ClearLogs();
                break;
            case "ToolBox_Open_Output_Directory":
                Process.Start(new ProcessStartInfo { FileName = UserSettings.Default.OutputDirectory, UseShellExecute = true });
                break;
            case "ToolBox_Collapse_All":
                contextViewModel.CUE4Parse.AssetsFolder.CollapseAll();
                break;
            case TreeItem selectedFolder:
                MainWindow.Instance.SelectFolder(selectedFolder);
                break;
        }
    }

}
