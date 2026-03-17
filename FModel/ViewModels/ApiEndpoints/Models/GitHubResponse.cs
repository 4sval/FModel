using System;
using System.Diagnostics;
using System.Linq;
using Avalonia.Media.Imaging;
using FModel.Framework;
using FModel.Settings;
using Serilog;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JI = Newtonsoft.Json.JsonIgnoreAttribute;

namespace FModel.ViewModels.ApiEndpoints.Models;

public class GitHubRelease
{
    [J("tag_name")] public string TagName { get; private set; }
    [J("html_url")] public string HtmlUrl { get; private set; }
    [J("body")] public string Body { get; private set; }
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
    [J("sha")] public string Sha
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

    private Author[] _coAuthors = [];
    public Author[] CoAuthors
    {
        get => _coAuthors;
        set
        {
            SetProperty(ref _coAuthors, value);
            RaisePropertyChanged(nameof(Authors));
            RaisePropertyChanged(nameof(AuthorNames));
        }
    }

    public Author[] Authors => Author != null ? new[] { Author }.Concat(CoAuthors).ToArray() : CoAuthors;

    public string AuthorNames
    {
        get
        {
            var authors = Authors;
            return authors.Length switch
            {
                0 => string.Empty,
                1 => authors[0].Login,
                2 => $"{authors[0].Login} and {authors[1].Login}",
                _ => string.Join(", ", authors.Take(authors.Length - 1).Select(a => a.Login)) + $", and {authors[^1].Login}"
            };
        }
    }

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
        if (IsCurrent) return;

        var url = Asset?.BrowserDownloadUrl;
        if (string.IsNullOrEmpty(url))
        {
            Log.Warning("Download skipped: no asset URL available for commit {Sha}", ShortSha);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "Could not open download URL: {Url}", url);
        }
    }
}

public class Commit : ViewModel
{
    private Author _author = null!;
    [J("author")] public Author Author
    {
        get => _author;
        set => SetProperty(ref _author, value);
    }

    private string _message = null!;
    [J("message")] public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }
}

public class Author : ViewModel
{
    [J("name")] public string Name { get; set; } = null!;
    [J("login")] public string Login { get; set; } = null!;
    [J("date")] public DateTime Date { get; set; }
    [J("avatar_url")] public string AvatarUrl { get; set; } = null!;
    [J("html_url")] public string HtmlUrl { get; set; } = null!;

    private Bitmap? _avatarImage;

    [JI]
    public Bitmap? AvatarImage
    {
        get => _avatarImage;
        set => SetProperty(ref _avatarImage, value);
    }
}
