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

    public async Task<IDictionary<string, IDictionary<string, string>>> GetHotfixesAsync(CancellationToken token, string language = "en")
    {
        var request = new FRestRequest("https://fortnitecentral.genxgames.gg/api/v1/hotfixes")
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
