using System;
using AdonisUI.Controls;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AutoUpdaterDotNET;
using FModel.Extensions;
using FModel.Framework;
using FModel.Services;
using FModel.Settings;
using FModel.ViewModels.ApiEndpoints.Models;
using Newtonsoft.Json;
using RestSharp;
using Serilog;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxButton = AdonisUI.Controls.MessageBoxButton;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;
using MessageBoxResult = AdonisUI.Controls.MessageBoxResult;

namespace FModel.ViewModels.ApiEndpoints;

public class FModelApiEndpoint : AbstractApiProvider
{
    private GitHubCommit[] _commits;
    private News _news;
    private Info _infos;
    private Donator[] _donators;
    private Backup[] _backups;
    private Game _game;
    private readonly IDictionary<string, CommunityDesign> _communityDesigns = new Dictionary<string, CommunityDesign>();
    private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;

    public FModelApiEndpoint(RestClient client) : base(client) { }

    public async Task<GitHubCommit[]> GetGitHubCommitHistoryAsync(string branch = "dev", int page = 1, int limit = 20)
    {
        var request = new FRestRequest(Constants.GH_COMMITS_HISTORY);
        request.AddParameter("sha", branch);
        request.AddParameter("page", page);
        request.AddParameter("per_page", limit);
        var response = await _client.ExecuteAsync<GitHubCommit[]>(request).ConfigureAwait(false);
        return response.Data;
    }

    public async Task<GitHubRelease> GetGitHubReleaseAsync(string tag)
    {
        var request = new FRestRequest($"{Constants.GH_RELEASES}/tags/{tag}");
        var response = await _client.ExecuteAsync<GitHubRelease>(request).ConfigureAwait(false);
        return response.Data;
    }

    public async Task<News> GetNewsAsync(CancellationToken token, string game)
    {
        var request = new FRestRequest($"https://api.fmodel.app/v1/news/{Constants.APP_VERSION}");
        request.AddParameter("game", game);
        var response = await _client.ExecuteAsync<News>(request, token).ConfigureAwait(false);
        Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, response.ResponseUri?.OriginalString);
        return response.Data;
    }

    public News GetNews(CancellationToken token, string game)
    {
        return _news ??= GetNewsAsync(token, game).GetAwaiter().GetResult();
    }

    public async Task<Info> GetInfosAsync(CancellationToken token, EUpdateMode updateMode)
    {
        var request = new FRestRequest($"https://api.fmodel.app/v1/infos/{updateMode}");
        var response = await _client.ExecuteAsync<Info>(request, token).ConfigureAwait(false);
        Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, response.ResponseUri?.OriginalString);
        return response.Data;
    }

    public Info GetInfos(CancellationToken token, EUpdateMode updateMode)
    {
        return _infos ?? GetInfosAsync(token, updateMode).GetAwaiter().GetResult();
    }

    public async Task<Donator[]> GetDonatorsAsync()
    {
        var request = new FRestRequest($"https://api.fmodel.app/v1/donations/donators");
        var response = await _client.ExecuteAsync<Donator[]>(request).ConfigureAwait(false);
        Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, response.ResponseUri?.OriginalString);
        return response.Data;
    }

    public Donator[] GetDonators()
    {
        return _donators ??= GetDonatorsAsync().GetAwaiter().GetResult();
    }

    public async Task<Backup[]> GetBackupsAsync(CancellationToken token, string gameName)
    {
        var request = new FRestRequest($"https://api.fmodel.app/v1/backups/{gameName}");
        var response = await _client.ExecuteAsync<Backup[]>(request, token).ConfigureAwait(false);
        Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, response.ResponseUri?.OriginalString);
        return response.Data;
    }

    public Backup[] GetBackups(CancellationToken token, string gameName)
    {
        return _backups ??= GetBackupsAsync(token, gameName).GetAwaiter().GetResult();
    }

    public async Task<Game> GetGamesAsync(CancellationToken token, string gameName)
    {
        var request = new FRestRequest($"https://api.fmodel.app/v1/games/{gameName}");
        var response = await _client.ExecuteAsync<Game>(request, token).ConfigureAwait(false);
        Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, response.ResponseUri?.OriginalString);
        return response.Data;
    }

    public Game GetGames(CancellationToken token, string gameName)
    {
        return _game ??= GetGamesAsync(token, gameName).GetAwaiter().GetResult();
    }

    public async Task<CommunityDesign> GetDesignAsync(string designName)
    {
        var request = new FRestRequest($"https://api.fmodel.app/v1/designs/{designName}");
        var response = await _client.ExecuteAsync<Community>(request).ConfigureAwait(false);
        Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, response.ResponseUri?.OriginalString);
        return response.Data != null ? new CommunityDesign(response.Data) : null;
    }

    public CommunityDesign GetDesign(string designName)
    {
        if (_communityDesigns.TryGetValue(designName, out var communityDesign) && communityDesign != null)
            return communityDesign;

        communityDesign = GetDesignAsync(designName).GetAwaiter().GetResult();
        _communityDesigns[designName] = communityDesign;
        return communityDesign;
    }

    public void CheckForUpdates(EUpdateMode updateMode, bool launch = false)
    {
        if (launch)
        {
            AutoUpdater.ParseUpdateInfoEvent += ParseUpdateInfoEvent;
            AutoUpdater.CheckForUpdateEvent += CheckForUpdateEvent;
        }
        AutoUpdater.Start($"https://api.fmodel.app/v1/infos/{updateMode}");
    }

    private void ParseUpdateInfoEvent(ParseUpdateInfoEventArgs args)
    {
        _infos = JsonConvert.DeserializeObject<Info>(args.RemoteData);
        if (_infos != null)
        {
            args.UpdateInfo = new UpdateInfoEventArgs
            {
                CurrentVersion = _infos.Version.SubstringBefore('-'),
                ChangelogURL = _infos.ChangelogUrl,
                DownloadURL = _infos.DownloadUrl,
                Mandatory = new CustomMandatory
                {
                    Value = UserSettings.Default.UpdateMode == EUpdateMode.Qa,
                    CommitHash = _infos.Version.SubstringAfter('+')
                }
            };
        }
    }

    private void CheckForUpdateEvent(UpdateInfoEventArgs args)
    {
        if (args is { CurrentVersion: { } })
        {
            var qa = (CustomMandatory) args.Mandatory;
            var currentVersion = new System.Version(args.CurrentVersion);
            if ((qa.Value && qa.CommitHash == UserSettings.Default.CommitHash) || // qa branch : same commit id
                (!qa.Value && currentVersion == args.InstalledVersion && args.CurrentVersion == UserSettings.Default.CommitHash)) // stable - beta branch : same version + commit id = version
            {
                if (UserSettings.Default.ShowChangelog)
                    ShowChangelog(args);
                return;
            }

            var downgrade = currentVersion < args.InstalledVersion;
            var messageBox = new MessageBoxModel
            {
                Text = $"The latest version of FModel {UserSettings.Default.UpdateMode.GetDescription()} is {(qa.Value ? qa.ShortCommitHash : args.CurrentVersion)}. You are using version {(qa.Value ? UserSettings.Default.ShortCommitHash : args.InstalledVersion)}. Do you want to {(downgrade ? "downgrade" : "update")} the application now?",
                Caption = $"{(downgrade ? "Downgrade" : "Update")} Available",
                Icon = MessageBoxImage.Question,
                Buttons = MessageBoxButtons.YesNo(),
                IsSoundEnabled = false
            };

            MessageBox.Show(messageBox);
            if (messageBox.Result != MessageBoxResult.Yes) return;

            try
            {
                if (AutoUpdater.DownloadUpdate(args))
                {
                    UserSettings.Default.ShowChangelog = currentVersion != args.InstalledVersion;
                    UserSettings.Default.CommitHash = qa.CommitHash;
                    Application.Current.Shutdown();
                }
            }
            catch (Exception exception)
            {
                UserSettings.Default.ShowChangelog = false;
                MessageBox.Show(exception.Message, exception.GetType().ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
            MessageBox.Show(
                "There is a problem reaching the update server, please check your internet connection or try again later.",
                "Update Check Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShowChangelog(UpdateInfoEventArgs args)
    {
        var request = new FRestRequest(args.ChangelogURL);
        var response = _client.Execute(request);
        if (string.IsNullOrEmpty(response.Content)) return;

        _applicationView.CUE4Parse.TabControl.AddTab($"Release Notes: {args.CurrentVersion}");
        _applicationView.CUE4Parse.TabControl.SelectedTab.Highlighter = AvalonExtensions.HighlighterSelector("changelog");
        _applicationView.CUE4Parse.TabControl.SelectedTab.SetDocumentText(response.Content, false, false);
        UserSettings.Default.ShowChangelog = false;
    }
}

public class CustomMandatory : Mandatory
{
    public string CommitHash { get; set; }
    public string ShortCommitHash => CommitHash[..7];
}
