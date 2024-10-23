using System;
using System.IO;
using System.Linq;
using FModel.ViewModels;
using Ookii.Dialogs.Wpf;
using System.Windows;
using CUE4Parse.Utils;

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

            // install_folder/
            //     ├─ Engine/
            //     ├─ GameName/
            //     │  ├─ Binaries/
            //     │  ├─ Content/
            //     │  │  ├─ Paks/
            // our goal is to get the GameName folder
            var currentFolder = folderBrowser.SelectedPath.SubstringAfterLast('\\');
            if (currentFolder.Equals("Paks", StringComparison.InvariantCulture))
            {
                var dir = new DirectoryInfo(folderBrowser.SelectedPath);
                if (dir.Parent is { Parent: not null } &&
                    dir.Parent.Name.Equals("Content", StringComparison.InvariantCulture) &&
                    dir.Parent.Parent.GetDirectories().Any(x => x.Name == "Binaries"))
                {
                    HelloMyNameIsGame.Text = dir.Parent.Parent.Name;
                    return;
                }
            }

            HelloMyNameIsGame.Text = folderBrowser.SelectedPath.SubstringAfterLast('\\');
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
}
