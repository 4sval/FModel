using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FModel.Services;
using FModel.Settings;
using FModel.ViewModels;
using FModel.Views.Resources.Controls;
// using Microsoft.Win32; // TODO(P4-004): OpenFileDialog not available on Linux
// using Ookii.Dialogs.Wpf; // TODO(P4-004): VistaFolderBrowserDialog not available on Linux

namespace FModel.Views;

public partial class SettingsView : Window
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
            if (item is not TreeViewItem { IsVisible: true } treeItem)
                continue;
            treeItem.IsSelected = i == UserSettings.Default.LastOpenedSettingTab;
            i++;
        }
    }

    private async void OnClick(object sender, RoutedEventArgs e)
    {
        var restart = _applicationView.SettingsView.Save(out var whatShouldIDo);
        if (restart)
            await _applicationView.RestartWithWarningAsync(this);

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
        _applicationView.CUE4Parse.Provider.ReadShaderMaps = UserSettings.Default.ReadShaderMaps;
    }

    private async void OnBrowseOutput(object sender, RoutedEventArgs e)
    {
        var path = await PickFolderAsync();
        if (path is null)
            return;
        UserSettings.Default.OutputDirectory = path;
        if (_applicationView.SettingsView.UseCustomOutputFolders)
            return;

        path = Path.Combine(path, "Exports");
        UserSettings.Default.RawDataDirectory = path;
        UserSettings.Default.PropertiesDirectory = path;
        UserSettings.Default.TextureDirectory = path;
        UserSettings.Default.AudioDirectory = path;
    }

    private async void OnBrowseDirectories(object sender, RoutedEventArgs e)
    {
        var path = await PickFolderAsync();
        if (path is not null)
            UserSettings.Default.GameDirectory = path;
    }

    private async void OnBrowseRawData(object sender, RoutedEventArgs e)
    {
        var path = await PickFolderAsync();
        if (path is not null)
            UserSettings.Default.RawDataDirectory = path;
    }

    private async void OnBrowseProperties(object sender, RoutedEventArgs e)
    {
        var path = await PickFolderAsync();
        if (path is not null)
            UserSettings.Default.PropertiesDirectory = path;
    }

    private async void OnBrowseTexture(object sender, RoutedEventArgs e)
    {
        var path = await PickFolderAsync();
        if (path is not null)
            UserSettings.Default.TextureDirectory = path;
    }

    private async void OnBrowseAudio(object sender, RoutedEventArgs e)
    {
        var path = await PickFolderAsync();
        if (path is not null)
            UserSettings.Default.AudioDirectory = path;
    }

    private async void OnBrowseModels(object sender, RoutedEventArgs e)
    {
        var path = await PickFolderAsync();
        if (path is not null)
            UserSettings.Default.ModelDirectory = path;
    }

    private async void OnBrowseMappings(object sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return;
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("USMAP Files") { Patterns = new[] { "*.usmap" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
            }
        });
        if (files.Count == 0)
            return;
        _applicationView.SettingsView.MappingEndpoint.FilePath = files[0].Path.LocalPath;
    }

    private async Task<string?> PickFolderAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return null;
        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { AllowMultiple = false });
        return folders.Count > 0 ? folders[0].Path.LocalPath : null;
    }

    private void OnSelectedItemChanged(object sender, SelectionChangedEventArgs e)
    {
        var i = 0;
        foreach (var item in SettingsTree.Items)
        {
            if (item is not TreeViewItem { IsVisible: true } treeItem)
                continue;
            if (!treeItem.IsSelected)
            {
                i++;
                continue;
            }

            UserSettings.Default.LastOpenedSettingTab = i;

            // Select the DataTemplate that matches the TreeViewItem's Tag.
            if (treeItem.Tag is string tagKey &&
                this.TryFindResource(tagKey, out var resource) &&
                resource is Avalonia.Controls.Templates.IDataTemplate dt)
            {
                SettingsContentControl.ContentTemplate = dt;
            }
            else
            {
                // Clear stale template if a tag key is missing or invalid.
                SettingsContentControl.ContentTemplate = null;
            }

            break;
        }
    }

    private async void OpenCustomVersions(object sender, RoutedEventArgs e)
    {
        var editor = new DictionaryEditor(_applicationView.SettingsView.SelectedCustomVersions, "Versioning Configuration (Custom Versions)");
        if (await editor.ShowDialog<bool?>(this) != true)
            return;

        _applicationView.SettingsView.SelectedCustomVersions = editor.CustomVersions;
    }

    private async void OpenOptions(object sender, RoutedEventArgs e)
    {
        var editor = new DictionaryEditor(_applicationView.SettingsView.SelectedOptions, "Versioning Configuration (Options)");
        if (await editor.ShowDialog<bool?>(this) != true)
            return;

        _applicationView.SettingsView.SelectedOptions = editor.Options;
    }

    private async void OpenMapStructTypes(object sender, RoutedEventArgs e)
    {
        var editor = new DictionaryEditor(_applicationView.SettingsView.SelectedMapStructTypes, "Versioning Configuration (MapStructTypes)");
        if (await editor.ShowDialog<bool?>(this) != true)
            return;

        _applicationView.SettingsView.SelectedMapStructTypes = editor.MapStructTypes;
    }

    private async void OpenAesEndpoint(object sender, RoutedEventArgs e)
    {
        var editor = new EndpointEditor(_applicationView.SettingsView.AesEndpoint, "Endpoint Configuration (AES)", EEndpointType.Aes);
        await editor.ShowDialog<bool?>(this);
    }

    private async void OpenMappingEndpoint(object sender, RoutedEventArgs e)
    {
        var editor = new EndpointEditor(_applicationView.SettingsView.MappingEndpoint, "Endpoint Configuration (Mapping)", EEndpointType.Mapping);
        await editor.ShowDialog<bool?>(this);
    }

    private void CriwareKeyBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;

        textBox.Text = _applicationView.SettingsView.CriwareDecryptionKey.ToString();
    }

    private void CriwareKeyBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;

        string input = textBox.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(input))
            return;

        if (TryParseKey(input, out ulong parsed))
            _applicationView.SettingsView.CriwareDecryptionKey = parsed;
    }

    private static bool TryParseKey(string text, out ulong value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        bool isHex = false;
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            isHex = true;
            text = text[2..];
        }
        else if (text.Any(char.IsLetter))
        {
            isHex = true;
        }

        int numberBase = text.All(Uri.IsHexDigit) ? 16 : 10;
        return ulong.TryParse(
            text,
            isHex ? NumberStyles.HexNumber : NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out value
        );
    }
}
