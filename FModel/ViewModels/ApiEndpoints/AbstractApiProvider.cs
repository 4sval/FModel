using RestSharp;

namespace FModel.ViewModels.ApiEndpoints;

public abstract class AbstractApiProvider
{
    protected readonly RestClient _client;

    protected AbstractApiProvider(RestClient client)
    {
        _client = client;
    }
}
