using FModel.Logger;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Fortnite_API;

namespace FModel.Utils
{
    static class Endpoints
    {
        public static readonly FortniteApi FortniteAPI = new FortniteApi($"FModel/{Assembly.GetExecutingAssembly().GetName().Version}");
        public const string BENBOT_AES = "https://benbotfn.tk/api/v1/aes";
        public const string BENBOT_MAPPINGS = "https://benbotfn.tk/api/v1/mappings";
        public const string BENBOT_HOTFIXES = "https://benbotfn.tk/api/v1/hotfixes";
        public const string FMODEL_JSON = "https://dl.dropbox.com/s/sxyaqo6zu1drlea/FModel.json?dl=0";
        public const string OAUTH_URL = "https://account-public-service-prod03.ol.epicgames.com/account/api/oauth/token";
        private const string _BASIC_FN_TOKEN = "basic MzQ0NmNkNzI2OTRjNGE0NDg1ZDgxYjc3YWRiYjIxNDE6OTIwOWQ0YTVlMjVhNDU3ZmI5YjA3NDg5ZDMxM2I0MWE=";

        public static byte[] GetRawData(Uri uri) => GetRawDataAsync(uri).GetAwaiter().GetResult();
        public static async Task<byte[]> GetRawDataAsync(Uri uri)
        {
            using HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            try
            {
                var data = await client.GetByteArrayAsync(uri).ConfigureAwait(false);
                return data;
            }
            catch
            {
                return null;
            }
        }

        public static Task<string> GetStringEndpoint(string url) => GetStringEndpoint(url, string.Empty);
        public static async Task<string> GetStringEndpoint(string url, string token)
        {
            DebugHelper.WriteLine("{0} {1} {2} {3}", "[FModel]", "[Endpoints]", "[GET]", url);

            using (HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) })
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(url)))
            {
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Add("Authorization", $"bearer {token}");
                    request.Content = new StringContent("", Encoding.UTF8, "application/json");
                }

                try
                {
                    using HttpResponseMessage httpResponseMessage = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                    return await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                catch (Exception)
                {
                    /* TaskCanceledException
                     * HttpRequestException */
                }
            }

            return string.Empty;
        }

        public static Task<T> GetJsonEndpoint<T>(string url) => GetJsonEndpoint<T>(url, string.Empty);
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

        public static async Task<OAuth> GetOAuthInfo()
        {
            DebugHelper.WriteLine("{0} {1} {2} {3}", "[FModel]", "[Endpoints]", "[GET]", OAUTH_URL);

            using (HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) })
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, new Uri(OAUTH_URL)))
            {
                request.Headers.Add("Authorization", _BASIC_FN_TOKEN);
                request.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");
                try
                {
                    using HttpResponseMessage httpResponseMessage = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                    using Stream stream = await httpResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        return Streams.DeserializeJsonFromStream<OAuth>(stream);
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

    public class OAuth
    {
        [JsonProperty("access_token")]
        public string AccessToken;
        [JsonProperty("expires_in")]
        public long ExpiresIn;
    }
}
