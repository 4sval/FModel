using FModel.Logger;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace FModel.Utils
{
    static class Endpoints
    {
        public const string BENBOT_AES = "https://benbotfn.tk/api/v1/aes";
        public const string BENBOT_HOTFIXES = "https://benbotfn.tk/api/v1/hotfixes";
        public const string FMODEL_JSON = "https://dl.dropbox.com/s/sxyaqo6zu1drlea/FModel.json?dl=0";

        public static async Task<string> GetStringEndpoint(string url) => await GetStringEndpoint(url, string.Empty);
        public static async Task<string> GetStringEndpoint(string url, string token)
        {
            DebugHelper.WriteLine("{0} {1} {2} {3}", "[FModel]", "[Endpoints]", "[GET]", url);

            using (HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) })
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(url)))
            {
                if (!string.IsNullOrEmpty(token))
                    request.Headers.Add("Authorization", $"bearer {token}");

                try
                {
                    using HttpResponseMessage httpResponseMessage = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                    using Stream stream = await httpResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        return await Streams.StreamToStringAsync(stream).ConfigureAwait(false);
                    }
                }
                catch (Exception)
                {
                    /* TaskCanceledException
                     * HttpRequestException */
                }
            }

            return string.Empty;
        }

        public static async Task<T> GetJsonEndpoint<T>(string url) => await GetJsonEndpoint<T>(url, string.Empty);
        public static async Task<T> GetJsonEndpoint<T>(string url, string query)
        {
            if (url.Equals(BENBOT_HOTFIXES) && !string.IsNullOrEmpty(query))
                url += $"?lang={Uri.EscapeDataString(query)}";
            else if (url.Equals(BENBOT_AES) && !string.IsNullOrEmpty(query))
                url += $"?version={Uri.EscapeDataString(query)}";

            DebugHelper.WriteLine("{0} {1} {2} {3}", "[FModel]", "[Endpoints]", "[GET]", url);
            using (HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) })
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(url)))
            {
                try
                {
                    using HttpResponseMessage httpResponseMessage = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                    using Stream stream = await httpResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        return Streams.DeserializeJsonFromStream<T>(stream);
                    }
                }
                catch (Exception)
                {
                    /* TaskCanceledException
                     * HttpRequestException */
                }
            }

            return default;
        }
    }
}
