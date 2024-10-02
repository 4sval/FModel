using System;
using System.Windows;
using System.Windows.Controls;
using AdonisUI.Controls;
using AutoUpdaterDotNET;
using FModel.Settings;
using FModel.ViewModels.ApiEndpoints.Models;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxButton = AdonisUI.Controls.MessageBoxButton;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;
using MessageBoxResult = AdonisUI.Controls.MessageBoxResult;

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
        if (Commit.IsCurrent) return;

        var messageBox = new MessageBoxModel
        {
            Text = $"Are you sure you want to update to version '{Commit.ShortSha}'?{(!Commit.Asset.IsLatest ? "\nThis is not the latest version." : "")}",
            Caption = "Update FModel",
            Icon = MessageBoxImage.Question,
            Buttons = MessageBoxButtons.YesNo(),
            IsSoundEnabled = false
        };

        MessageBox.Show(messageBox);
        if (messageBox.Result != MessageBoxResult.Yes) return;

        try
        {
            if (AutoUpdater.DownloadUpdate(new UpdateInfoEventArgs { DownloadURL = Commit.Asset.BrowserDownloadUrl }))
            {
                UserSettings.Default.CommitHash = Commit.Sha;
                Application.Current.Shutdown();
            }
        }
        catch (Exception exception)
        {
            UserSettings.Default.ShowChangelog = false;
            MessageBox.Show(exception.Message, exception.GetType().ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

