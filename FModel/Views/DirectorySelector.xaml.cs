using System;
using System.IO;
using System.Linq;
using FModel.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CUE4Parse.Utils;
// TODO(P4-004): Ookii.Dialogs.Wpf not available on Linux — replace with StorageProvider.OpenFolderPickerAsync
// using Ookii.Dialogs.Wpf;

namespace FModel.Views;

/// <summary>
/// Logique d'interaction pour DirectorySelector.xaml
/// </summary>
public partial class DirectorySelector : Window
{
    public DirectorySelector(GameSelectorViewModel gameSelectorViewModel)
    {
        DataContext = gameSelectorViewModel;
        InitializeComponent();
    }

    private void OnClick(object sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void OnBrowseDirectories(object sender, RoutedEventArgs e)
    {
        // TODO(P4-004): VistaFolderBrowserDialog not available on Linux
        // Replace with: await TopLevel.GetTopLevel(this).StorageProvider.OpenFolderPickerAsync(...)
    }

    private void OnBrowseManualDirectories(object sender, RoutedEventArgs e)
    {
        // TODO(P4-004): VistaFolderBrowserDialog not available on Linux
        // Replace with: await TopLevel.GetTopLevel(this).StorageProvider.OpenFolderPickerAsync(...)
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
