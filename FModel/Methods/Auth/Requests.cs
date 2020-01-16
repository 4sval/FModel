using RestSharp;

namespace FModel.Methods.Auth
{
    static class Requests
    {
        public static string GetLauncherEndpoint(string url)
        {
            var client = new RestClient(url);
            var request = new RestRequest(Method.GET)
                .AddHeader("Authorization", "bearer " + Properties.Settings.Default.ELauncherToken);

            IRestResponse reqRes = client.Execute(request);
            if (reqRes.StatusCode == System.Net.HttpStatusCode.OK)
                return reqRes.Content;

            return string.Empty;
        }
    }
}
