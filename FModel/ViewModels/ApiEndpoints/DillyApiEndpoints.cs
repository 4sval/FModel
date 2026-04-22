using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FModel.Framework;
using FModel.ViewModels.ApiEndpoints.Models;
using RestSharp;
using Serilog;

namespace FModel.ViewModels.ApiEndpoints;

public class DillyApiEndpoint : AbstractApiProvider
{
    private Backup[] _backups;
    private ManifestInfoDilly[] _manifests;

    public DillyApiEndpoint(RestClient client) : base(client) { }

    public async Task<Backup[]> GetBackupsAsync(CancellationToken token)
    {
        var request = new FRestRequest($"https://export-service-new.dillyapis.com/v1/backups");
        var response = await _client.ExecuteAsync<Backup[]>(request, token).ConfigureAwait(false);
        Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, response.ResponseUri?.OriginalString);
        return response.Data;
    }

    public Backup[] GetBackups(CancellationToken token)
    {
        return _backups ??= GetBackupsAsync(token).GetAwaiter().GetResult();
    }

    public async Task<ManifestInfoDilly[]> GetManifestsAsync(CancellationToken token)
    {
        var request = new FRestRequest($"https://export-service-new.dillyapis.com/v1/manifests");
        var response = await _client.ExecuteAsync<ManifestInfoDilly[]>(request, token).ConfigureAwait(false);
        Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, response.ResponseUri?.OriginalString);
        return response.Data;
    }

    public ManifestInfoDilly[] GetManifests(CancellationToken token)
    {
        return _manifests ??= GetManifestsAsync(token).GetAwaiter().GetResult();
    }

    public async Task<IDictionary<string, IDictionary<string, string>>> GetHotfixesAsync(CancellationToken token, string language = "en")
    {
        var request = new FRestRequest("https://api.fortniteapi.com/v1/cloudstorage/hotfixes")
        {
            Interceptors = [_interceptor]
        };
        request.AddParameter("lang", language);
        var response = await _client.ExecuteAsync<IDictionary<string, IDictionary<string, string>>>(request, token).ConfigureAwait(false);
        Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, response.ResponseUri?.OriginalString);
        return response.Data;
    }

    public IDictionary<string, IDictionary<string, string>> GetHotfixes(CancellationToken token, string language = "en")
    {
        return GetHotfixesAsync(token, language).GetAwaiter().GetResult();
    }
}
