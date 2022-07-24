using FModel.Framework;
using FModel.ViewModels.ApiEndpoints;
using RestSharp;

namespace FModel.ViewModels;

public class ApiEndpointViewModel
{
    private readonly RestClient _client = new RestClient
    {
        Options =
        {
            UserAgent = $"FModel/{Constants.APP_VERSION}",
            MaxTimeout = 3 * 1000
        }
    }.UseSerializer<JsonNetSerializer>();

    public FortniteApiEndpoint FortniteApi { get; }
    public ValorantApiEndpoint ValorantApi { get; }
    public BenbotApiEndpoint BenbotApi { get; }
    public EpicApiEndpoint EpicApi { get; }
    public FModelApi FModelApi { get; }

    public ApiEndpointViewModel()
    {
        FortniteApi = new FortniteApiEndpoint(_client);
        ValorantApi = new ValorantApiEndpoint(_client);
        BenbotApi = new BenbotApiEndpoint(_client);
        EpicApi = new EpicApiEndpoint(_client);
        FModelApi = new FModelApi(_client);
    }
}
