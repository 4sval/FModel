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

    private async void OnClick(object sender, RoutedEventArgs e)
    {
        var restart = _applicationView.SettingsView.Save(out var whatShouldIDo);
        if (restart)
            _applicationView.RestartWithWarning();

        Close();

        foreach (var dOut in whatShouldIDo)
        {
            switch (dOut)
            {
                case SettingsOut.ReloadLocres:
                    _applicationView.CUE4Parse.LocalizedResourcesCount = 0;
                    _applicationView.CUE4Parse.LocalResourcesDone = false;
                    _applicationView.CUE4Parse.HotfixedResourcesDone = false;
                    await _applicationView.CUE4Parse.LoadLocalizedResources();
                    break;
                case SettingsOut.ReloadMappings:
                    await _applicationView.CUE4Parse.InitMappings();
                    break;
            }
        }

        _applicationView.CUE4Parse.Provider.ReadScriptData = UserSettings.Default.ReadScriptData;
    }

    private void OnBrowseOutput(object sender, RoutedEventArgs e)
    {
        if (!TryBrowse(out var path)) return;
        UserSettings.Default.OutputDirectory = path;
        if (_applicationView.SettingsView.UseCustomOutputFolders) return;

        path = Path.Combine(path, "Exports");
        UserSettings.Default.RawDataDirectory = path;
        UserSettings.Default.PropertiesDirectory = path;
        UserSettings.Default.TextureDirectory = path;
        UserSettings.Default.AudioDirectory = path;
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

    private void OnBrowseMappings(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Select a mapping file",
            InitialDirectory = Path.Combine(UserSettings.Default.OutputDirectory, ".data"),
            Filter = "USMAP Files (*.usmap)|*.usmap|All Files (*.*)|*.*"
        };

        if (!openFileDialog.ShowDialog().GetValueOrDefault())
            return;

        _applicationView.SettingsView.MappingEndpoint.FilePath = openFileDialog.FileName;
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

    private void OpenCustomVersions(object sender, RoutedEventArgs e)
    {
        var editor = new DictionaryEditor(_applicationView.SettingsView.SelectedCustomVersions, "Versioning Configuration (Custom Versions)");
        var result = editor.ShowDialog();
        if (!result.HasValue || !result.Value)
            return;

        _applicationView.SettingsView.SelectedCustomVersions = editor.CustomVersions;
    }

    private void OpenOptions(object sender, RoutedEventArgs e)
    {
        var editor = new DictionaryEditor(_applicationView.SettingsView.SelectedOptions, "Versioning Configuration (Options)");
        var result = editor.ShowDialog();
        if (!result.HasValue || !result.Value)
            return;

        _applicationView.SettingsView.SelectedOptions = editor.Options;
    }

    private void OpenMapStructTypes(object sender, RoutedEventArgs e)
    {
        var editor = new DictionaryEditor(_applicationView.SettingsView.SelectedMapStructTypes, "Versioning Configuration (MapStructTypes)");
        var result = editor.ShowDialog();
        if (!result.HasValue || !result.Value)
            return;

        _applicationView.SettingsView.SelectedMapStructTypes = editor.MapStructTypes;
    }

    private void OpenAesEndpoint(object sender, RoutedEventArgs e)
    {
        var editor = new EndpointEditor(
            _applicationView.SettingsView.AesEndpoint, "Endpoint Configuration (AES)", EEndpointType.Aes);
        editor.ShowDialog();
    }

    private void OpenMappingEndpoint(object sender, RoutedEventArgs e)
    {
        var editor = new EndpointEditor(
            _applicationView.SettingsView.MappingEndpoint, "Endpoint Configuration (Mapping)", EEndpointType.Mapping);
        editor.ShowDialog();
    }
}
