using System.IO;
using System.Windows;
using System.Windows.Controls;
using FModel.Services;
using FModel.Settings;
using FModel.ViewModels;
using FModel.Views.Resources.Controls;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;

namespace FModel.Views;

public partial class SettingsView
{
    private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;

    public SettingsView()
    {
        DataContext = _applicationView;
        _applicationView.SettingsView.Initialize();

        InitializeComponent();

        var i = 0;
        foreach (var item in SettingsTree.Items)
        {
            if (item is not TreeViewItem { Visibility: Visibility.Visible } treeItem) continue;
            treeItem.IsSelected = i == UserSettings.Default.LastOpenedSettingTab;
            i++;
        }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _applicationView.SettingsView.InitPresets(_applicationView.CUE4Parse.Provider.GameName);
    }

    private async void OnClick(object sender, RoutedEventArgs e)
    {
        var whatShouldIDo = _applicationView.SettingsView.Save();
        if (whatShouldIDo == SettingsOut.Restart)
            _applicationView.RestartWithWarning();

        Close();

        switch (whatShouldIDo)
        {
            case SettingsOut.ReloadLocres:
                _applicationView.CUE4Parse.LocalizedResourcesCount = 0;
                await _applicationView.CUE4Parse.LoadLocalizedResources();
                break;
            case SettingsOut.CheckForUpdates:
                ApplicationService.ApiEndpointView.FModelApi.CheckForUpdates(UserSettings.Default.UpdateMode);
                break;
        }
    }

    private void OnBrowseOutput(object sender, RoutedEventArgs e)
    {
        if (TryBrowse(out var path)) UserSettings.Default.OutputDirectory = path;
    }

    private void OnBrowseDirectories(object sender, RoutedEventArgs e)
    {
        if (TryBrowse(out var path)) UserSettings.Default.GameDirectory = path;
    }

    private void OnBrowseRawData(object sender, RoutedEventArgs e)
    {
        if (TryBrowse(out var path)) UserSettings.Default.RawDataDirectory = path;
    }

    private void OnBrowseProperties(object sender, RoutedEventArgs e)
    {
        if (TryBrowse(out var path)) UserSettings.Default.PropertiesDirectory = path;
    }

    private void OnBrowseTexture(object sender, RoutedEventArgs e)
    {
        if (TryBrowse(out var path)) UserSettings.Default.TextureDirectory = path;
    }

    private void OnBrowseAudio(object sender, RoutedEventArgs e)
    {
        if (TryBrowse(out var path)) UserSettings.Default.AudioDirectory = path;
    }

    private void OnBrowseModels(object sender, RoutedEventArgs e)
    {
        if (TryBrowse(out var path)) UserSettings.Default.ModelDirectory = path;
    }

    private async void OnBrowseMappings(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Select a mapping file",
            InitialDirectory = Path.Combine(UserSettings.Default.OutputDirectory, ".data"),
            Filter = "USMAP Files (*.usmap)|*.usmap|All Files (*.*)|*.*"
        };

        if (!openFileDialog.ShowDialog().GetValueOrDefault()) return;
        UserSettings.Default.MappingFilePath = openFileDialog.FileName;
        await _applicationView.CUE4Parse.InitBenMappings();
    }

    private bool TryBrowse(out string path)
    {
        var folderBrowser = new VistaFolderBrowserDialog { ShowNewFolderButton = false };
        if (folderBrowser.ShowDialog() == true)
        {
            path = folderBrowser.SelectedPath;
            return true;
        }

        path = string.Empty;
        return false;
    }

    private void OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        var i = 0;
        foreach (var item in SettingsTree.Items)
        {
            if (item is not TreeViewItem { Visibility: Visibility.Visible } treeItem)
                continue;
            if (!treeItem.IsSelected)
            {
                i++;
                continue;
            }

            UserSettings.Default.LastOpenedSettingTab = i;
            break;
        }
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox { SelectedItem: string s }) return;
        if (s == Constants._NO_PRESET_TRIGGER) _applicationView.SettingsView.ResetPreset();
        else _applicationView.SettingsView.SwitchPreset(s);
    }

    private void OpenCustomVersions(object sender, RoutedEventArgs e)
    {
        var dictionary = new DictionaryEditor(
            _applicationView.SettingsView.SelectedCustomVersions,
            "Versioning Configuration (Custom Versions)",
            _applicationView.SettingsView.EnableElements);
        var result = dictionary.ShowDialog();
        if (!result.HasValue || !result.Value)
            return;

        _applicationView.SettingsView.SelectedCustomVersions = dictionary.CustomVersions;
    }

    private void OpenOptions(object sender, RoutedEventArgs e)
    {
        var dictionary = new DictionaryEditor(
            _applicationView.SettingsView.SelectedOptions,
            "Versioning Configuration (Options)",
            _applicationView.SettingsView.EnableElements);
        var result = dictionary.ShowDialog();
        if (!result.HasValue || !result.Value)
            return;

        _applicationView.SettingsView.SelectedOptions = dictionary.Options;
    }
}