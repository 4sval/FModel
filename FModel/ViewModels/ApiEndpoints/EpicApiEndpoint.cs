using System;
using System.Threading;
using System.Threading.Tasks;
using EpicManifestParser.Objects;
using FModel.Settings;
using FModel.ViewModels.ApiEndpoints.Models;
using RestSharp;
using Serilog;

namespace FModel.ViewModels.ApiEndpoints;

public class EpicApiEndpoint : AbstractApiProvider
{
    private const string _OAUTH_URL = "https://account-public-service-prod03.ol.epicgames.com/account/api/oauth/token";
    private const string _BASIC_TOKEN = "basic MzQ0NmNkNzI2OTRjNGE0NDg1ZDgxYjc3YWRiYjIxNDE6OTIwOWQ0YTVlMjVhNDU3ZmI5YjA3NDg5ZDMxM2I0MWE=";
    private const string _LAUNCHER_ASSETS = "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/public/assets/v2/platform/Windows/namespace/fn/catalogItem/4fe75bbc5a674f4f9b356b5c90567da5/app/Fortnite/label/Live";

    public EpicApiEndpoint(RestClient client) : base(client)
    {
    }

    public async Task<ManifestInfo> GetManifestAsync(CancellationToken token)
    {
        if (IsExpired())
        {
            var auth = await GetAuthAsync(token);
            if (auth != null)
            {
                UserSettings.Default.LastAuthResponse = auth;
            }
        }

        var request = new RestRequest(_LAUNCHER_ASSETS);
        request.AddHeader("Authorization", $"bearer {UserSettings.Default.LastAuthResponse.AccessToken}");
        var response = await _client.ExecuteAsync(request, token).ConfigureAwait(false);
        Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, request.Resource);
        return new ManifestInfo(response.Content);
    }

    public ManifestInfo GetManifest(CancellationToken token)
    {
        return GetManifestAsync(token).GetAwaiter().GetResult();
    }

    private async Task<AuthResponse> GetAuthAsync(CancellationToken token)
    {
        var request = new RestRequest(_OAUTH_URL, Method.Post);
        request.AddHeader("Authorization", _BASIC_TOKEN);
        request.AddParameter("grant_type", "client_credentials");
        var response = await _client.ExecuteAsync<AuthResponse>(request, token).ConfigureAwait(false);
        Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, request.Resource);
        return response.Data;
    }

    private bool IsExpired()
    {
        if (string.IsNullOrEmpty(UserSettings.Default.LastAuthResponse.AccessToken)) return true;
        return DateTime.Now.Subtract(TimeSpan.FromHours(1)) >= UserSettings.Default.LastAuthResponse.ExpiresAt;
    }
}
