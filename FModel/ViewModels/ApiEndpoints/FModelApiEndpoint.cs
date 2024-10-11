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
using FModel.Views;
using Newtonsoft.Json;
using RestSharp;
using Serilog;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxButton = AdonisUI.Controls.MessageBoxButton;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;

namespace FModel.ViewModels.ApiEndpoints;

public class FModelApiEndpoint : AbstractApiProvider
{
    private News _news;
    private Info _infos;
    private Donator[] _donators;
    private Backup[] _backups;
    private Game _game;
    private readonly IDictionary<string, CommunityDesign> _communityDesigns = new Dictionary<string, CommunityDesign>();
    private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;

    public FModelApiEndpoint(RestClient client) : base(client) { }

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

    public void CheckForUpdates(bool launch = false)
    {
        if (DateTime.Now < UserSettings.Default.NextUpdateCheck) return;

        if (launch)
        {
            AutoUpdater.ParseUpdateInfoEvent += ParseUpdateInfoEvent;
            AutoUpdater.CheckForUpdateEvent += CheckForUpdateEvent;
        }
        AutoUpdater.Start("https://api.fmodel.app/v1/infos/Qa");
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
                    CommitHash = _infos.Version.SubstringAfter('+')
                }
            };
        }
    }

    private void CheckForUpdateEvent(UpdateInfoEventArgs args)
    {
        if (args is { CurrentVersion: { } })
        {
            UserSettings.Default.LastUpdateCheck = DateTime.Now;

            if (((CustomMandatory)args.Mandatory).CommitHash == Constants.APP_COMMIT_ID)
            {
                if (UserSettings.Default.ShowChangelog)
                    ShowChangelog(args);

                return;
            }

            var currentVersion = new System.Version(args.CurrentVersion);
            UserSettings.Default.ShowChangelog = currentVersion != args.InstalledVersion;

            const string message = "A new update is available!";
            Helper.OpenWindow<AdonisWindow>(message, () => new UpdateView { Title = message, ResizeMode = ResizeMode.NoResize }.ShowDialog());
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
        if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content)) return;

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
