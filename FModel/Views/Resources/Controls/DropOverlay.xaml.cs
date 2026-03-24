using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using FModel.Services;
using FModel.Settings;
using FModel.ViewModels;

namespace FModel.Views.Resources.Controls;

public partial class DropOverlay : UserControl
{
    enum DragStatus
    {
        None,
        File,
        Folder,
    }

    private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;
    private DragStatus _dragStatus = DragStatus.None;
    private string _path = null;

    public DropOverlay()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ResetState()
    {
        _dragStatus = DragStatus.None;
        _path = null;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        if (window is null)
            return;

        window.PreviewDragEnter += OnPreviewDragEnter;
        window.PreviewDragOver += OnPreviewDragOver;
        window.PreviewDragLeave += OnPreviewDragLeave;
        window.Drop += OnDrop;
    }

    private void OnPreviewDragEnter(object sender, DragEventArgs e)
    {
        GetValidTarget(sender, e);
        if (_dragStatus is DragStatus.None)
        {
            e.Effects = DragDropEffects.None;
        }
        else
        {
            Visibility = Visibility.Visible;
            e.Effects = DragDropEffects.Copy;

            if (_dragStatus is DragStatus.Folder)
            {
                TitleText.Text = "Drop folder to add new game";
                DescriptionText.Text = "Folder will be added to the directory selector";
            }
            else if (_dragStatus is DragStatus.File)
            {
                TitleText.Text = "Drop .usmap to import";
                DescriptionText.Text = "Mapping file will be applied immediately";
            }
        }
        e.Handled = true;
    }

    private void OnPreviewDragOver(object sender, DragEventArgs e)
    {
        e.Effects = _dragStatus is DragStatus.None ? DragDropEffects.None : DragDropEffects.Copy;
        e.Handled = true;
    }

    private void OnPreviewDragLeave(object sender, DragEventArgs e)
    {
        Visibility = Visibility.Collapsed;
        ResetState();
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        Visibility = Visibility.Collapsed;
        e.Handled = true;
        switch (_dragStatus)
        {
            case DragStatus.Folder:
                await Dispatcher.InvokeAsync(() => _applicationView.AddGameDirectory(_path));
                break;
            case DragStatus.File:
                UserSettings.IsEndpointValid(EEndpointType.Mapping, out var oldMappingsEndpoint);
                try
                {
                    var newMappingsEndpoint = new EndpointSettings() { Overwrite = true, FilePath = _path };
                    UserSettings.Default.CurrentDir.Endpoints[(int) EEndpointType.Mapping] = newMappingsEndpoint;
                    await _applicationView.CUE4Parse.InitMappings();
                    _applicationView.SettingsView.MappingEndpoint = newMappingsEndpoint;
                }
                catch (Exception ex)
                {
                    UserSettings.Default.CurrentDir.Endpoints[(int) EEndpointType.Mapping] = oldMappingsEndpoint;
                    FLogger.Append(ELog.Error, () =>
                    {
                        FLogger.Text($"Failed to load mapping file: {ex.Message}", Constants.WHITE, true);
                    });
                }
                break;
            default:
                break;
        }
        ResetState();
    }

    private void GetValidTarget(object sender, DragEventArgs e)
    {
        if (!_applicationView.Status.IsReady || !e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetData(DataFormats.FileDrop) is not string[] files)
            return;


        bool directorySelectorIsVisible = _applicationView.Status.Kind is EStatusKind.Configuring;
        if (!directorySelectorIsVisible && (Helper.IsWindowOpen<DictionaryEditor>() || Helper.IsWindowOpen<EndpointEditor>()))
        {
            _applicationView.Status.SetStatus(EStatusKind.Configuring);
            ResetState();
            return;
        }

        switch (sender)
        {
            case MainWindow or SettingsView when !directorySelectorIsVisible:
                foreach (var path in files)
                {
                    if (Directory.Exists(path))
                    {
                        _path = path;
                        _dragStatus = DragStatus.Folder;
                        return;
                    }
                    else if (File.Exists(path) && Path.GetExtension(path).Equals(".usmap", StringComparison.OrdinalIgnoreCase))
                    {
                        _path = path;
                        _dragStatus = DragStatus.File;
                        return;
                    }
                }
                break;
            case DirectorySelector:
                if (files.FirstOrDefault(f => Directory.Exists(f)) is { } folder)
                {
                    _path = folder;
                    _dragStatus = DragStatus.Folder;
                    return;
                }
                break;
            default:
                break;
        }
        ResetState();
    }
}
