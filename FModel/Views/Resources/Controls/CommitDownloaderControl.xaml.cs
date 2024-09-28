using System.Windows;
using System.Windows.Controls;
using AutoUpdaterDotNET;
using FModel.ViewModels.ApiEndpoints.Models;

namespace FModel.Views.Resources.Controls;

public partial class CommitDownloaderControl : UserControl
{
    public CommitDownloaderControl()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty CommitAssetProperty =
        DependencyProperty.Register("CommitAsset", typeof(GitHubAsset), typeof(CommitDownloaderControl), new PropertyMetadata(null));

    public GitHubAsset CommitAsset
    {
        get { return (GitHubAsset)GetValue(CommitAssetProperty); }
        set { SetValue(CommitAssetProperty, value); }
    }

    public static readonly DependencyProperty IsCurrentProperty =
        DependencyProperty.Register("IsCurrent", typeof(bool), typeof(CommitDownloaderControl), new PropertyMetadata(null));

    public bool IsCurrent
    {
        get { return (bool)GetValue(IsCurrentProperty); }
        set { SetValue(IsCurrentProperty, value); }
    }

    private void OnDownload(object sender, RoutedEventArgs e)
    {
        AutoUpdater.DownloadUpdate(new UpdateInfoEventArgs { DownloadURL = CommitAsset.BrowserDownloadUrl });
    }
}

