using RestSharp;
using Newtonsoft.Json.Linq;

namespace FModel
{
    static class Keychain
    {
        /// <summary>
        /// get url content as string with authentication
        /// </summary>
        /// <param name="url"></param>
        /// <param name="auth"></param>
        /// <returns> url content </returns>
        public static string GetEndpoint(string url)
        {
            RestClient EndpointClient = new RestClient(url);
            RestRequest EndpointRequest = new RestRequest(Method.GET);

            string response = EndpointClient.Execute(EndpointRequest).Content;

            return response;
        }
    }
}
