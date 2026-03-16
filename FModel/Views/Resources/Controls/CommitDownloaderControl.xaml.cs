using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FModel.ViewModels.ApiEndpoints.Models;

namespace FModel.Views.Resources.Controls;

public partial class CommitDownloaderControl : UserControl
{
    public CommitDownloaderControl()
    {
        InitializeComponent();
    }

    public static readonly StyledProperty<GitHubCommit?> CommitProperty =
        AvaloniaProperty.Register<CommitDownloaderControl, GitHubCommit?>(nameof(Commit));

    public GitHubCommit? Commit
    {
        get => GetValue(CommitProperty);
        set => SetValue(CommitProperty, value);
    }

    private void OnDownload(object? sender, RoutedEventArgs e)
    {
        Commit?.Download();
    }
}

