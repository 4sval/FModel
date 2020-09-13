using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using FModel.Logger;

namespace FModel.Utils
{
    public static class Oodle
    {
        private const string WARFRAME_CDN_HOST = "https://origin.warframe.com";
        private const string WARFRAME_INDEX_PATH = "/origin/E926E926/index.txt.lzma";
        private const string WARFRAME_INDEX_URL = WARFRAME_CDN_HOST + WARFRAME_INDEX_PATH;
        public const string OODLE_DLL_NAME = "oo2core_8_win64.dll";
        
        public static bool LoadOodleDll()
        {
            if (File.Exists(OODLE_DLL_NAME))
            {
                return true;
            }
            return DownloadOodleDll().Result;
        }

        private static async Task<bool> DownloadOodleDll()
        {
            using var client = new HttpClient {Timeout = TimeSpan.FromSeconds(2)};
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get,
                new Uri(WARFRAME_INDEX_URL));
            try
            {
                using var httpResponseMessage = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                var lzma = await httpResponseMessage.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                var input = new MemoryStream(lzma);
                var output = new MemoryStream();
                LZMA.Decompress(input, output);
                output.Position = 0;
                using var reader = new StreamReader(output);
                string line, dllUrl = null;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains(OODLE_DLL_NAME))
                    {
                        dllUrl = WARFRAME_CDN_HOST + line.Substring(0, line.IndexOf(','));
                        break;
                    }
                }
                if (dllUrl == null)
                {
                    DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Oodle]", "Warframe index did not contain oodle dll");
                    return default;
                }

                using var dllRequest = new HttpRequestMessage(HttpMethod.Get, new Uri(dllUrl));
                using var dllResponse = await client.SendAsync(dllRequest, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                var dllLzma = await dllResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                input = new MemoryStream(dllLzma);
                output = new MemoryStream();
                LZMA.Decompress(input, output);
                output.Position = 0;
                await File.WriteAllBytesAsync(OODLE_DLL_NAME, output.ToArray()).ConfigureAwait(false);
                DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Oodle]", "Successfully downloaded oodle dll");
                return true;
            }
            catch (Exception e)
            {
                DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Oodle]", $"Uncaught exception while downloading oodle dll: {e.GetType()}: {e.Message}");
                /* TaskCanceledException
                 * HttpRequestException */
            }
            return default;
        }
    }
}