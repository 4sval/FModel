using System.Windows;
using System.Windows.Controls;
using FModel.Services;
using FModel.Settings;
using FModel.ViewModels;

namespace FModel.Views.Resources.Controls.ContextMenus;

public partial class FolderContextMenuDictionary
{
    private ThreadWorkerViewModel _threadWorkerView => ApplicationService.ThreadWorkerView;
    private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;

    public FolderContextMenuDictionary()
    {
        InitializeComponent();
    }

    private async void OnFolderExportClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: TreeItem folder })
            return;

        await _threadWorkerView.Begin(cancellationToken => { _applicationView.CUE4Parse.ExportFolder(cancellationToken, folder); });
        FLogger.Append(ELog.Information, () =>
        {
            FLogger.Text("Successfully exported ", Constants.WHITE);
            FLogger.Link(folder.PathAtThisPoint, UserSettings.Default.RawDataDirectory, true);
        });
    }

    private async void OnFolderSaveClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: TreeItem folder })
            return;

        await _threadWorkerView.Begin(cancellationToken => { _applicationView.CUE4Parse.SaveFolder(cancellationToken, folder); });
        FLogger.Append(ELog.Information, () =>
        {
            FLogger.Text("Successfully saved ", Constants.WHITE);
            FLogger.Link(folder.PathAtThisPoint, UserSettings.Default.PropertiesDirectory, true);
        });
    }

    private async void OnFolderTextureClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: TreeItem folder })
            return;

        await _threadWorkerView.Begin(cancellationToken => { _applicationView.CUE4Parse.TextureFolder(cancellationToken, folder); });
        FLogger.Append(ELog.Information, () =>
        {
            FLogger.Text("Successfully saved textures from ", Constants.WHITE);
            FLogger.Link(folder.PathAtThisPoint, UserSettings.Default.TextureDirectory, true);
        });
    }

    private async void OnFolderModelClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: TreeItem folder })
            return;

        await _threadWorkerView.Begin(cancellationToken => { _applicationView.CUE4Parse.ModelFolder(cancellationToken, folder); });
        FLogger.Append(ELog.Information, () =>
        {
            FLogger.Text("Successfully saved models from ", Constants.WHITE);
            FLogger.Link(folder.PathAtThisPoint, UserSettings.Default.ModelDirectory, true);
        });
    }

    private async void OnFolderAnimationClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: TreeItem folder })
            return;

        await _threadWorkerView.Begin(cancellationToken => { _applicationView.CUE4Parse.AnimationFolder(cancellationToken, folder); });
        FLogger.Append(ELog.Information, () =>
        {
            FLogger.Text("Successfully saved animations from ", Constants.WHITE);
            FLogger.Link(folder.PathAtThisPoint, UserSettings.Default.ModelDirectory, true);
        });
    }

    private async void OnFolderAudioClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: TreeItem folder })
            return;

        await _threadWorkerView.Begin(cancellationToken => { _applicationView.CUE4Parse.AudioFolder(cancellationToken, folder); });
        FLogger.Append(ELog.Information, () =>
        {
            FLogger.Text("Successfully saved audio from ", Constants.WHITE);
            FLogger.Link(folder.PathAtThisPoint, UserSettings.Default.AudioDirectory, true);
        });
    }

    private void OnFavoriteDirectoryClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: TreeItem folder })
            return;

        _applicationView.CustomDirectories.Add(new CustomDirectory(folder.Header, folder.PathAtThisPoint));
        FLogger.Append(ELog.Information, () =>
            FLogger.Text($"Successfully saved '{folder.PathAtThisPoint}' as a new favorite directory", Constants.WHITE, true));
    }

    private void OnCopyDirectoryPathClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: TreeItem folder })
            return;

        Clipboard.SetText(folder.PathAtThisPoint);
    }
}
