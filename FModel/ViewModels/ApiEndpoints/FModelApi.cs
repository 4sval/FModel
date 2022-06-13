using System;
using AdonisUI.Controls;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AutoUpdaterDotNET;
using FModel.Extensions;
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

public class FModelApi : AbstractApiProvider
{
    private News _news;
    private Info _infos;
    private Backup[] _backups;
    private Game _game;
    private readonly IDictionary<string, CommunityDesign> _communityDesigns = new Dictionary<string, CommunityDesign>();
    private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;

    public FModelApi(RestClient client) : base(client)
    {
    }

    public async Task<News> GetNewsAsync(CancellationToken token)
    {
        var request = new RestRequest($"https://api.fmodel.app/v1/news/{Constants.APP_VERSION}");
        var response = await _client.ExecuteAsync<News>(request, token).ConfigureAwait(false);
        Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, request.Resource);
        return response.Data;
    }

    public News GetNews(CancellationToken token)
    {
        return _news ??= GetNewsAsync(token).GetAwaiter().GetResult();
    }

    public async Task<Info> GetInfosAsync(CancellationToken token, EUpdateMode updateMode)
    {
        var request = new RestRequest($"https://api.fmodel.app/v1/infos/{updateMode}");
        var response = await _client.ExecuteAsync<Info>(request, token).ConfigureAwait(false);
        Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, request.Resource);
        return response.Data;
    }

    public Info GetInfos(CancellationToken token, EUpdateMode updateMode)
    {
        return _infos ?? GetInfosAsync(token, updateMode).GetAwaiter().GetResult();
    }

    public async Task<Backup[]> GetBackupsAsync(CancellationToken token, string gameName)
    {
        var request = new RestRequest($"https://api.fmodel.app/v1/backups/{gameName}");
        var response = await _client.ExecuteAsync<Backup[]>(request, token).ConfigureAwait(false);
        Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, request.Resource);
        return response.Data;
    }

    public Backup[] GetBackups(CancellationToken token, string gameName)
    {
        return _backups ??= GetBackupsAsync(token, gameName).GetAwaiter().GetResult();
    }

    public async Task<Game> GetGamesAsync(CancellationToken token, string gameName)
    {
        var request = new RestRequest($"https://api.fmodel.app/v1/games/{gameName}");
        var response = await _client.ExecuteAsync<Game>(request, token).ConfigureAwait(false);
        Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, request.Resource);
        return response.Data;
    }

    public Game GetGames(CancellationToken token, string gameName)
    {
        return _game ??= GetGamesAsync(token, gameName).GetAwaiter().GetResult();
    }

    public async Task<CommunityDesign> GetDesignAsync(string designName)
    {
        var request = new RestRequest($"https://api.fmodel.app/v1/designs/{designName}");
        var response = await _client.ExecuteAsync<Community>(request).ConfigureAwait(false);
        Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, request.Resource);
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

    public void CheckForUpdates(EUpdateMode updateMode)
    {
        AutoUpdater.ParseUpdateInfoEvent += ParseUpdateInfoEvent;
        AutoUpdater.CheckForUpdateEvent += CheckForUpdateEvent;
        AutoUpdater.Start($"https://api.fmodel.app/v1/infos/{updateMode}");
    }

    private void ParseUpdateInfoEvent(ParseUpdateInfoEventArgs args)
    {
        _infos = JsonConvert.DeserializeObject<Info>(args.RemoteData);
        if (_infos != null)
        {
            args.UpdateInfo = new UpdateInfoEventArgs
            {
                CurrentVersion = _infos.Version,
                ChangelogURL = _infos.ChangelogUrl,
                DownloadURL = _infos.DownloadUrl
            };
        }
    }

    private void CheckForUpdateEvent(UpdateInfoEventArgs args)
    {
        if (args is { CurrentVersion: { } })
        {
            var currentVersion = new System.Version(args.CurrentVersion);
            if (currentVersion == args.InstalledVersion)
            {
                if (UserSettings.Default.ShowChangelog)
                    ShowChangelog(args);
                return;
            }

            var downgrade = currentVersion < args.InstalledVersion;
            var messageBox = new MessageBoxModel
            {
                Text = $"The latest version of FModel {UserSettings.Default.UpdateMode} is {args.CurrentVersion}. You are using version {args.InstalledVersion}. Do you want to {(downgrade ? "downgrade" : "update")} the application now?",
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
                    UserSettings.Default.ShowChangelog = true;
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
        var request = new RestRequest(args.ChangelogURL);
        var response = _client.Execute(request);
        if (string.IsNullOrEmpty(response.Content)) return;

        _applicationView.CUE4Parse.TabControl.AddTab($"Release Notes: {args.CurrentVersion}");
        _applicationView.CUE4Parse.TabControl.SelectedTab.Highlighter = AvalonExtensions.HighlighterSelector("changelog");
        _applicationView.CUE4Parse.TabControl.SelectedTab.SetDocumentText(response.Content, false);
        UserSettings.Default.ShowChangelog = false;
    }
}
