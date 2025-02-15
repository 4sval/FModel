using RestSharp;
using RestSharp.Interceptors;

namespace FModel.ViewModels.ApiEndpoints;

public abstract class AbstractApiProvider
{
    protected readonly RestClient _client;
    protected readonly Interceptor _interceptor;

    protected AbstractApiProvider(RestClient client)
    {
        _client = client;
        _interceptor = new CompatibilityInterceptor
        {
            OnBeforeDeserialization = resp => { resp.ContentType = "application/json; charset=utf-8"; }
        };
    }
}
