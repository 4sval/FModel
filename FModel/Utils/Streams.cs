using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FModel.Utils
{
    static class Streams
    {
        public static MemoryStream AsStream(this ArraySegment<byte> me) => new MemoryStream(me.Array, me.Offset, me.Count, false, true);

        public static T DeserializeJsonFromStream<T>(Stream stream)
        {
            if (stream == null || stream.CanRead == false)
                return default;

            using var sr = new StreamReader(stream);
            using var jtr = new JsonTextReader(sr);
            var js = new JsonSerializer();
            var searchResult = js.Deserialize<T>(jtr);
            return searchResult;
        }

        public static async Task<string> StreamToStringAsync(Stream stream)
        {
            string content = null;

            if (stream != null)
                using (var sr = new StreamReader(stream))
                    content = await sr.ReadToEndAsync();

            return content;
        }
    }
}
