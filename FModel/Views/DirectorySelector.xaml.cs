using FModel.ViewModels;
using FModel.Views.Resources.Controls;
using Ookii.Dialogs.Wpf;
using Serilog;
using System.Windows;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxButtons = AdonisUI.Controls.MessageBoxButtons;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;
using MessageBoxModel = AdonisUI.Controls.MessageBoxModel;
using MessageBoxResult = AdonisUI.Controls.MessageBoxResult;

namespace FModel.Views;

/// <summary>
/// Logique d'interaction pour DirectorySelector.xaml
/// </summary>
public partial class DirectorySelector
{
    public DirectorySelector(GameSelectorViewModel gameSelectorViewModel)
    {
        DataContext = gameSelectorViewModel;
        InitializeComponent();
    }

    private void OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void OnBrowseDirectories(object sender, RoutedEventArgs e)
    {
        if (DataContext is not GameSelectorViewModel gameLauncherViewModel)
            return;

        var folderBrowser = new VistaFolderBrowserDialog {ShowNewFolderButton = false};
        if (folderBrowser.ShowDialog() == true)
        {
            gameLauncherViewModel.AddUndetectedDir(folderBrowser.SelectedPath);
        }
    }

    private void OnBrowseManualDirectories(object sender, RoutedEventArgs e)
    {
        var folderBrowser = new VistaFolderBrowserDialog {ShowNewFolderButton = false};
        if (folderBrowser.ShowDialog() == true)
        {
            HelloGameMyNameIsDirectory.Text = folderBrowser.SelectedPath;
            HelloMyNameIsGame.Text = Helper.GetGameName(folderBrowser.SelectedPath);
        }
    }

    private void OnAddDirectory(object sender, RoutedEventArgs e)
    {
        if (DataContext is not GameSelectorViewModel gameLauncherViewModel ||
            string.IsNullOrEmpty(HelloMyNameIsGame.Text) ||
            string.IsNullOrEmpty(HelloGameMyNameIsDirectory.Text))
            return;

        gameLauncherViewModel.AddUndetectedDir(HelloMyNameIsGame.Text, HelloGameMyNameIsDirectory.Text);
        HelloMyNameIsGame.Clear();
        HelloGameMyNameIsDirectory.Clear();
    }

    private void OnDeleteDirectory(object sender, RoutedEventArgs e)
    {
        if (DataContext is not GameSelectorViewModel gameLauncherViewModel)
            return;

        gameLauncherViewModel.DeleteSelectedGame();
    }

    private void OnClearDirectories(object sender, RoutedEventArgs e)
    {
        if (DataContext is not GameSelectorViewModel gameSelectorViewModel)
            return;

        var messageBox = new MessageBoxModel
        {
            Text = "Remove all saved game directories that no longer exist?",
            Caption = "Clear Empty Directories",
            Icon = MessageBoxImage.Question,
            Buttons = MessageBoxButtons.YesNo(),
            IsSoundEnabled = false
        };

        MessageBox.Show(messageBox);
        if (messageBox.Result != MessageBoxResult.Yes)
            return;

        var removedCount = gameSelectorViewModel.ClearMissingDirectories();
        var message = removedCount switch
        {
            0 => "No empty directories were found",
            1 => "Removed 1 empty game directory",
            _ => $"Removed {removedCount} empty game directories"
        };

        Log.Information("{Message}", message);
        if (FLogger.Logger is not null) // When only directory selector is open, FLogger.Logger is null
            FLogger.Append(ELog.Information, () => FLogger.Text(message, Constants.WHITE, true));
    }

    public void AddManualGame(string directory)
    {
        ManualGameExpander.IsExpanded = true;
        HelloMyNameIsGame.Text = Helper.GetGameName(directory);
        HelloGameMyNameIsDirectory.Text = directory;
    }
}
