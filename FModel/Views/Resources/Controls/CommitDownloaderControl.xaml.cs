using System.Windows;
using System.Windows.Controls;
using FModel.ViewModels.ApiEndpoints.Models;

namespace FModel.Views.Resources.Controls;

public partial class CommitDownloaderControl : UserControl
{
    public CommitDownloaderControl()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty CommitProperty =
        DependencyProperty.Register(nameof(Commit), typeof(GitHubCommit), typeof(CommitDownloaderControl), new PropertyMetadata(null));

    public GitHubCommit Commit
    {
        get { return (GitHubCommit)GetValue(CommitProperty); }
        set { SetValue(CommitProperty, value); }
    }

    private void OnDownload(object sender, RoutedEventArgs e)
    {
        Commit.Download();
    }
}

