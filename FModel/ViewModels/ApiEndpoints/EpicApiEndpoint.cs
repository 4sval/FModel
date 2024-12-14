using System.Threading;
using System.Threading.Tasks;

using EpicManifestParser.Api;

using FModel.Framework;
using FModel.Settings;
using FModel.ViewModels.ApiEndpoints.Models;

using RestSharp;

using Serilog;

namespace FModel.ViewModels.ApiEndpoints;

public class EpicApiEndpoint : AbstractApiProvider
{
    private const string _OAUTH_URL = "https://account-public-service-prod03.ol.epicgames.com/account/api/oauth/token";
    private const string _BASIC_TOKEN = "basic ZWM2ODRiOGM2ODdmNDc5ZmFkZWEzY2IyYWQ4M2Y1YzY6ZTFmMzFjMjExZjI4NDEzMTg2MjYyZDM3YTEzZmM4NGQ=";
    private const string _APP_URL = "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/public/assets/v2/platform/Windows/namespace/fn/catalogItem/4fe75bbc5a674f4f9b356b5c90567da5/app/Fortnite/label/Live";

    public EpicApiEndpoint(RestClient client) : base(client) { }

    public async Task<ManifestInfo> GetManifestAsync(CancellationToken token)
    {
        await VerifyAuth(token).ConfigureAwait(false);

        var request = new FRestRequest(_APP_URL);
        request.AddHeader("Authorization", $"bearer {UserSettings.Default.LastAuthResponse.AccessToken}");
        var response = await _client.ExecuteAsync(request, token).ConfigureAwait(false);
        Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, response.ResponseUri?.OriginalString);
        return response.IsSuccessful ? ManifestInfo.Deserialize(response.RawBytes) : null;
    }

    public ManifestInfo GetManifest(CancellationToken token)
    {
        return GetManifestAsync(token).GetAwaiter().GetResult();
    }

    public async Task VerifyAuth(CancellationToken token)
    {
        if (await IsExpired().ConfigureAwait(false))
        {
            var auth = await GetAuthAsync(token).ConfigureAwait(false);
            if (auth != null)
            {
                UserSettings.Default.LastAuthResponse = auth;
            }
        }
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
