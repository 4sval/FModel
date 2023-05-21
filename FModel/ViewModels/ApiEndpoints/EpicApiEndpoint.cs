using System.Threading;
using System.Threading.Tasks;
using EpicManifestParser.Objects;
using FModel.Framework;
using FModel.Settings;
using FModel.ViewModels.ApiEndpoints.Models;
using RestSharp;
using Serilog;

namespace FModel.ViewModels.ApiEndpoints;

public class EpicApiEndpoint : AbstractApiProvider
{
    private const string _OAUTH_URL = "https://account-public-service-prod03.ol.epicgames.com/account/api/oauth/token";
    private const string _BASIC_TOKEN = "basic MzQ0NmNkNzI2OTRjNGE0NDg1ZDgxYjc3YWRiYjIxNDE6OTIwOWQ0YTVlMjVhNDU3ZmI5YjA3NDg5ZDMxM2I0MWE=";
    private const string _APP_URL = "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/public/assets/v2/platform/Windows/namespace/fn/catalogItem/4fe75bbc5a674f4f9b356b5c90567da5/app/Fortnite/label/Live";
    private const string _CBM_URL = "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/public/assets/Windows/5cb97847cee34581afdbc445400e2f77/FortniteContentBuilds";

    public EpicApiEndpoint(RestClient client) : base(client) { }

    public async Task<ManifestInfo> GetManifestAsync(CancellationToken token)
    {
        if (await IsExpired().ConfigureAwait(false))
        {
            var auth = await GetAuthAsync(token).ConfigureAwait(false);
            if (auth != null)
            {
                UserSettings.Default.LastAuthResponse = auth;
            }
        }

        var request = new FRestRequest(_APP_URL);
        request.AddHeader("Authorization", $"bearer {UserSettings.Default.LastAuthResponse.AccessToken}");
        var response = await _client.ExecuteAsync(request, token).ConfigureAwait(false);
        Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, response.ResponseUri?.OriginalString);
        return response.IsSuccessful ? new ManifestInfo(response.Content) : null;
    }

    public async Task<ContentBuildManifestInfo> GetContentBuildManifestAsync(CancellationToken token, string label)
    {
        if (await IsExpired().ConfigureAwait(false))
        {
            var auth = await GetAuthAsync(token).ConfigureAwait(false);
            if (auth != null)
            {
                UserSettings.Default.LastAuthResponse = auth;
            }
        }

        var request = new FRestRequest(_CBM_URL);
        request.AddHeader("Authorization", $"bearer {UserSettings.Default.LastAuthResponse.AccessToken}");
        request.AddQueryParameter("label", label);
        var response = await _client.ExecuteAsync(request, token).ConfigureAwait(false);
        Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, response.ResponseUri?.OriginalString);
        return response.IsSuccessful ? new ContentBuildManifestInfo(response.Content) : null;
    }

    public ManifestInfo GetManifest(CancellationToken token)
    {
        return GetManifestAsync(token).GetAwaiter().GetResult();
    }

    public ContentBuildManifestInfo GetContentBuildManifest(CancellationToken token, string label)
    {
        return GetContentBuildManifestAsync(token, label).GetAwaiter().GetResult();
    }

    private async Task<AuthResponse> GetAuthAsync(CancellationToken token)
    {
        var request = new FRestRequest(_OAUTH_URL, Method.Post);
        request.AddHeader("Authorization", _BASIC_TOKEN);
        request.AddParameter("grant_type", "client_credentials");
        var response = await _client.ExecuteAsync<AuthResponse>(request, token).ConfigureAwait(false);
        Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, response.ResponseUri?.OriginalString);
        return response.Data;
    }

    private async Task<bool> IsExpired()
    {
        if (string.IsNullOrEmpty(UserSettings.Default.LastAuthResponse.AccessToken)) return true;
        var request = new FRestRequest("https://account-public-service-prod.ol.epicgames.com/account/api/oauth/verify");
        request.AddHeader("Authorization", $"bearer {UserSettings.Default.LastAuthResponse.AccessToken}");
        var response = await _client.ExecuteGetAsync(request).ConfigureAwait(false);
        return !response.IsSuccessful;
    }
}
