using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using CUE4Parse.UE4.Lua.unluac;
using FModel.Extensions;
using FModel.Extensions.Themes;
using FModel.Framework;
using FModel.Services;
using FModel.Settings;
using FModel.ViewModels;
using FModel.Views.Resources.Controls;
using ICSharpCode.AvalonEdit;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;

namespace FModel.Views;

public partial class SettingsView
{
    private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;
    private SettingsViewModel _settingsView => _applicationView.SettingsView;

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
        _applicationView.CUE4Parse.Provider.ReadShaderMaps = UserSettings.Default.ReadShaderMaps;

        UserSettings.Save();
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
        UserSettings.Default.CodeDirectory = path;
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
            InitialDirectory = CacheManager.MappingsDirectory,
            Filter = "USMAP Files (*.usmap, *.jmap, *.jmap.gz)|*.usmap;*.jmap;*.jmap.gz|All Files (*.*)|*.*"
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
        if (_applicationView.Status.IsReady)
            _applicationView.Status.SetStatus(EStatusKind.Configuring);
        var result = editor.ShowDialog();
        if (_applicationView.Status.IsReady)
            _applicationView.Status.SetStatus(EStatusKind.Ready);
        if (!result.HasValue || !result.Value)
            return;

        _applicationView.SettingsView.SelectedCustomVersions = editor.CustomVersions;
    }

    private void OpenOptions(object sender, RoutedEventArgs e)
    {
        var editor = new DictionaryEditor(_applicationView.SettingsView.SelectedOptions, "Versioning Configuration (Options)");
        if (_applicationView.Status.IsReady)
            _applicationView.Status.SetStatus(EStatusKind.Configuring);
        var result = editor.ShowDialog();
        if (_applicationView.Status.IsReady)
            _applicationView.Status.SetStatus(EStatusKind.Ready);
        if (!result.HasValue || !result.Value)
            return;

        _applicationView.SettingsView.SelectedOptions = editor.Options;
    }

    private void OpenMapStructTypes(object sender, RoutedEventArgs e)
    {
        var editor = new DictionaryEditor(_applicationView.SettingsView.SelectedMapStructTypes, "Versioning Configuration (MapStructTypes)");
        if (_applicationView.Status.IsReady)
            _applicationView.Status.SetStatus(EStatusKind.Configuring);
        var result = editor.ShowDialog();
        if (_applicationView.Status.IsReady)
            _applicationView.Status.SetStatus(EStatusKind.Ready);
        if (!result.HasValue || !result.Value)
            return;

        _applicationView.SettingsView.SelectedMapStructTypes = editor.MapStructTypes;
    }

    private void OpenAesEndpoint(object sender, RoutedEventArgs e)
    {
        var editor = new EndpointEditor(
            _applicationView.SettingsView.AesEndpoint, "Endpoint Configuration (AES)", EEndpointType.Aes);
        if (_applicationView.Status.IsReady)
            _applicationView.Status.SetStatus(EStatusKind.Configuring);
        editor.ShowDialog();
        if (_applicationView.Status.IsReady)
            _applicationView.Status.SetStatus(EStatusKind.Ready);
    }

    private void OpenMappingEndpoint(object sender, RoutedEventArgs e)
    {
        var editor = new EndpointEditor(
            _applicationView.SettingsView.MappingEndpoint, "Endpoint Configuration (Mapping)", EEndpointType.Mapping);
        if (_applicationView.Status.IsReady)
            _applicationView.Status.SetStatus(EStatusKind.Configuring);
        editor.ShowDialog();
        if (_applicationView.Status.IsReady)
            _applicationView.Status.SetStatus(EStatusKind.Ready);
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

    private void OnHyperlinkClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not Hyperlink hyperlink)
            return;

        Process.Start(new ProcessStartInfo(hyperlink.NavigateUri.AbsoluteUri) { UseShellExecute = true });
    }

    private async void OnDecompileLuaChanged(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox { IsChecked: true } && UnluacHelper.Instance is null)
            await ApplicationViewModel.InitUnluac();
    }

    private void OnUnluacFlagChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox cb || cb.Tag is not string name) return;
        if (!Enum.TryParse<EUnluacFlags>(name, true, out var flag)) return;

        var current = UserSettings.Default.UnluacFlags;
        var isChecked = cb.IsChecked == true;

        UserSettings.Default.UnluacFlags = isChecked ? (current | flag) : (current & ~flag);
    }

    private const string JsonThemePreviewText =
    """
    {
      "title": "This is an example JSON",
      "environment": "production",
      "enabled": true,
      "version": 4,
      "scale": 0.92,
      "features": {
        "previewAssets": true,
        "autoSave": false,
        "maxRecentFiles": 12
      },
      "export": {
        "rootDirectory": "C:\\Exports\\Assets",
        "keepDirectoryStructure": true,
        "formats": [
          "json",
          "png",
          "wav"
        ]
      },
      "paths": [
        "/Game/Characters/Hero",
        "/Game/UI/Widgets",
        "/Game/Audio/Music"
      ],
      "metadata": {
        "lastOpened": "2026-06-20T14:30:00Z",
        "experimental": false,
        "fallbackTheme": null,
        "escapeExample": "Line one\nLine two\tTabbed",
        "accentColor": "#FFC857"
      }
    }
    """;

    private void OnJsonThemePreviewLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not TextEditor editor)
            return;

        editor.SyntaxHighlighting = AvalonExtensions.HighlighterSelector("json");
        editor.Text = JsonThemePreviewText;
        ApplyJsonThemePreview(editor);
    }

    private void OnJsonHighlightThemeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: TextEditor editor })
            Dispatcher.BeginInvoke(() => ApplyJsonThemePreview(editor));
    }

    private void ApplyJsonThemePreview(TextEditor editor)
    {
        editor.SyntaxHighlighting ??= AvalonExtensions.HighlighterSelector("json");
        editor.SyntaxHighlighting.ApplyJsonTheme(_applicationView.SettingsView.SelectedJsonHighlightTheme);
        editor.TextArea.TextView.Redraw();
    }
}
