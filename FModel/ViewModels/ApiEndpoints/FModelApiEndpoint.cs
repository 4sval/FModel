using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FModel.Services;
using FModel.Settings;
using FModel.ViewModels.ApiEndpoints.Models;
using RestSharp;
using Serilog;

namespace FModel.ViewModels.ApiEndpoints;

public class FModelApiEndpoint : AbstractApiProvider
{
#if USE_FMODEL_API
    private News _news;
    private Donator[] _donators;
    private Game _game;
    private readonly IDictionary<string, CommunityDesign> _communityDesigns = new Dictionary<string, CommunityDesign>();
#endif
    private ApiEndpointViewModel _apiEndpointView => ApplicationService.ApiEndpointView;

    public FModelApiEndpoint(RestClient client) : base(client) { }

#if USE_FMODEL_API
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
#else
    public News GetNews(CancellationToken token, string game) => null;
#endif

#if USE_FMODEL_API
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
#else
    public Donator[] GetDonators() => null;
#endif

#if USE_FMODEL_API
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
#else
    public Game GetGames(CancellationToken token, string gameName) => null;
#endif

#if USE_FMODEL_API
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
#else
    public CommunityDesign GetDesign(string designName) => null;
#endif

    public async Task<GitHubRelease> CheckForUpdatesAsync()
    {
        if (DateTime.Now < UserSettings.Default.NextUpdateCheck)
        {
            Log.Warning("Updates have been silenced until {DateTime}", UserSettings.Default.NextUpdateCheck);
            return null;
        }

        UserSettings.Default.LastUpdateCheck = DateTime.Now;

        var latestRelease = await _apiEndpointView.GitHubApi.GetLatestReleaseAsync().ConfigureAwait(false);
        if (latestRelease?.TagName == null)
        {
            Log.Warning("Could not check for updates: failed to fetch latest release from GitHub");
            return null;
        }

        var tagName = latestRelease.TagName.TrimStart('v');
        var versionPart = tagName.Contains('-') ? tagName[..tagName.IndexOf('-')] : tagName;
        if (!System.Version.TryParse(versionPart, out var latestVersion))
        {
            Log.Warning("Could not parse latest release tag: {Tag}", latestRelease.TagName);
            return null;
        }

        var currentVersionStr = Constants.APP_VERSION ?? "0.0.0.0";
        var currentVersionPart = currentVersionStr.Contains('-')
            ? currentVersionStr[..currentVersionStr.IndexOf('-')]
            : currentVersionStr;
        if (!System.Version.TryParse(currentVersionPart, out var currentVersion))
        {
            Log.Warning("Could not parse current application version: {Version}", currentVersionStr);
            return null;
        }

        if (latestVersion <= currentVersion)
        {
            Log.Information("FModel Linux is up to date (v{Version})", Constants.APP_VERSION);
            return null;
        }

        Log.Warning("A new version of FModel Linux is available: {Version}", latestRelease.TagName);
        return latestRelease;
    }
}
