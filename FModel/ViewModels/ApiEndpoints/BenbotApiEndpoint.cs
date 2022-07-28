using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FModel.Framework;
using FModel.ViewModels.ApiEndpoints.Models;
using RestSharp;
using Serilog;

namespace FModel.ViewModels.ApiEndpoints;

public class BenbotApiEndpoint : AbstractApiProvider
{
    public BenbotApiEndpoint(RestClient client) : base(client)
    {
    }

    public async Task<Dictionary<string, Dictionary<string, string>>> GetHotfixesAsync(CancellationToken token, string language = "en-US")
    {
        var request = new FRestRequest("https://benbot.app/api/v1/hotfixes")
        {
            OnBeforeDeserialization = resp => { resp.ContentType = "application/json; charset=utf-8"; }
        };
        request.AddParameter("lang", language);
        var response = await _client.ExecuteAsync<Dictionary<string, Dictionary<string, string>>>(request, token).ConfigureAwait(false);
        Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, response.ResponseUri?.OriginalString);
        return response.Data;
    }

    public Dictionary<string, Dictionary<string, string>> GetHotfixes(CancellationToken token, string language = "en-US")
    {
        return GetHotfixesAsync(token, language).GetAwaiter().GetResult();
    }

    public async Task DownloadFileAsync(string fileLink, string installationPath)
    {
        var request = new FRestRequest(fileLink);
        var data = _client.DownloadData(request);
        await File.WriteAllBytesAsync(installationPath, data);
    }

    public void DownloadFile(string fileLink, string installationPath)
    {
        DownloadFileAsync(fileLink, installationPath).GetAwaiter().GetResult();
    }
}
