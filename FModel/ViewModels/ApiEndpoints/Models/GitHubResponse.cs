using System;
using System.Windows;
using AdonisUI.Controls;
using AutoUpdaterDotNET;
using FModel.Framework;
using FModel.Settings;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxButton = AdonisUI.Controls.MessageBoxButton;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;
using MessageBoxResult = AdonisUI.Controls.MessageBoxResult;
using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace FModel.ViewModels.ApiEndpoints.Models;

public class GitHubRelease
{
    [J("assets")] public GitHubAsset[] Assets { get; private set; }
}

public class GitHubAsset : ViewModel
{
    [J("name")] public string Name { get; private set; }
    [J("size")] public int Size { get; private set; }
    [J("download_count")] public int DownloadCount { get; private set; }
    [J("browser_download_url")] public string BrowserDownloadUrl { get; private set; }
    [J("created_at")] public DateTime CreatedAt { get; private set; }
    [J("uploader")] public Author Uploader { get; private set; }

    private bool _isLatest;
    public bool IsLatest
    {
        get => _isLatest;
        set => SetProperty(ref _isLatest, value);
    }
}

public class GitHubCommit : ViewModel
{
    private string _sha;
    [J("sha")]
    public string Sha
    {
        get => _sha;
        set
        {
            SetProperty(ref _sha, value);
            RaisePropertyChanged(nameof(IsCurrent));
            RaisePropertyChanged(nameof(ShortSha));
        }
    }

    [J("commit")] public Commit Commit { get; set; }
    [J("author")] public Author Author { get; set; }

    private GitHubAsset _asset;
    public GitHubAsset Asset
    {
        get => _asset;
        set
        {
            SetProperty(ref _asset, value);
            RaisePropertyChanged(nameof(IsDownloadable));
        }
    }

    public bool IsCurrent => Sha == Constants.APP_COMMIT_ID;
    public string ShortSha => Sha[..7];
    public bool IsDownloadable => Asset != null;

    public void Download()
    {
        if (IsCurrent)
        {
            MessageBox.Show(new MessageBoxModel
            {
                Text = "You are already on the latest version.",
                Caption = "Update FModel",
                Icon = MessageBoxImage.Information,
                Buttons = [MessageBoxButtons.Ok()],
                IsSoundEnabled = false
            });
            return;
        }

        var messageBox = new MessageBoxModel
        {
            Text = $"Are you sure you want to update to version '{ShortSha}'?{(!Asset.IsLatest ? "\nThis is not the latest version." : "")}",
            Caption = "Update FModel",
            Icon = MessageBoxImage.Question,
            Buttons = MessageBoxButtons.YesNo(),
            IsSoundEnabled = false
        };

        MessageBox.Show(messageBox);
        if (messageBox.Result != MessageBoxResult.Yes) return;

        try
        {
            if (AutoUpdater.DownloadUpdate(new UpdateInfoEventArgs { DownloadURL = Asset.BrowserDownloadUrl }))
            {
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

public class Commit
{
    [J("author")] public Author Author { get; set; }
    [J("message")] public string Message { get; set; }
}

public class Author
{
    [J("name")] public string Name { get; set; }
    [J("login")] public string Login { get; set; }
    [J("date")] public DateTime Date { get; set; }
    [J("avatar_url")] public string AvatarUrl { get; set; }
    [J("html_url")] public string HtmlUrl { get; set; }
}
