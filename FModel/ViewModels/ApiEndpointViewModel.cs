using System;
using System.IO;
using System.Threading.Tasks;
using FModel.Framework;
using FModel.ViewModels.ApiEndpoints;
using RestSharp;

namespace FModel.ViewModels;

public class ApiEndpointViewModel
{
    private readonly RestClient _client = new (new RestClientOptions
    {
        UserAgent = $"FModel/{Constants.APP_VERSION}",
        Timeout = TimeSpan.FromSeconds(5)
    }, configureSerialization: s => s.UseSerializer<JsonNetSerializer>());

    public FortniteApiEndpoint FortniteApi { get; }
    public ValorantApiEndpoint ValorantApi { get; }
    public FortniteCentralApiEndpoint CentralApi { get; }
    public EpicApiEndpoint EpicApi { get; }
    public FModelApiEndpoint FModelApi { get; }
    public DynamicApiEndpoint DynamicApi { get; }

    public ApiEndpointViewModel()
    {
        FortniteApi = new FortniteApiEndpoint(_client);
        ValorantApi = new ValorantApiEndpoint(_client);
        CentralApi = new FortniteCentralApiEndpoint(_client);
        EpicApi = new EpicApiEndpoint(_client);
        FModelApi = new FModelApiEndpoint(_client);
        DynamicApi = new DynamicApiEndpoint(_client);
    }

    public async Task DownloadFileAsync(string fileLink, string installationPath)
    {
        var request = new FRestRequest(fileLink);
        var data = _client.DownloadData(request) ?? Array.Empty<byte>();
        await File.WriteAllBytesAsync(installationPath, data);
    }

    public void DownloadFile(string fileLink, string installationPath)
    {
        DownloadFileAsync(fileLink, installationPath).GetAwaiter().GetResult();
    }
}
