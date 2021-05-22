using RestSharp;

namespace FModel.ViewModels.ApiEndpoints
{
    public abstract class AbstractApiProvider
    {
        protected readonly IRestClient _client;

        public AbstractApiProvider(IRestClient client)
        {
            _client = client;
        }
    }
}