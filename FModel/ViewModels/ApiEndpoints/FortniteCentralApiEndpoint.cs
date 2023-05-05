using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FModel.Framework;
using RestSharp;
using Serilog;

namespace FModel.ViewModels.ApiEndpoints;

public class FortniteCentralApiEndpoint : AbstractApiProvider
{
    public FortniteCentralApiEndpoint(RestClient client) : base(client) { }

    public async Task<Dictionary<string, Dictionary<string, string>>> GetHotfixesAsync(CancellationToken token, string language = "en")
    {
        var request = new FRestRequest("https://fortnitecentral.genxgames.gg/api/v1/hotfixes")
        {
            OnBeforeDeserialization = resp => { resp.ContentType = "application/json; charset=utf-8"; }
        };
        request.AddParameter("lang", language);
        var response = await _client.ExecuteAsync<Dictionary<string, Dictionary<string, string>>>(request, token).ConfigureAwait(false);
        Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, response.ResponseUri?.OriginalString);
        return response.Data;
    }

    public Dictionary<string, Dictionary<string, string>> GetHotfixes(CancellationToken token, string language = "en")
    {
        return GetHotfixesAsync(token, language).GetAwaiter().GetResult();
    }
}
