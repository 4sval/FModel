using RestSharp;

namespace FModel.ViewModels.ApiEndpoints;

public abstract class AbstractApiProvider
{
    protected readonly IRestClient _client;

    protected AbstractApiProvider(IRestClient client)
    {
        _client = client;
    }
}