using FModel.Logger;
using FModel.Utils;
using FModel.Windows.CustomNotifier;
using Newtonsoft.Json;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace FModel.Grabber.Mappings
{
    static class MappingsData
    {
        public static async Task<Mapping[]> GetData()
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                Mapping[] data = await Endpoints.GetJsonEndpoint<Mapping[]>(Endpoints.BENBOT_MAPPINGS, string.Empty).ConfigureAwait(false);
                return data;
            }
            else
            {
                Globals.gNotifier.ShowCustomMessage("Mappings", Properties.Resources.NoInternet, "/FModel;component/Resources/wifi-strength-off.ico");
                DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Mappings]", "No internet");
                return null;
            }
        }
    }

    public class Mapping
    {
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("fileName")]
        public string FileName { get; set; }
        [JsonProperty("hash")]
        public string Hash { get; set; }
        [JsonProperty("length")]
        public string Length { get; set; }
        [JsonProperty("uploaded")]
        public string Uploaded { get; set; }
        [JsonProperty("meta")]
        public Metadata Meta { get; set; }
    }
    public class Metadata
    {
        [JsonProperty("version")]
        public string Version { get; set; }
        [JsonProperty("compressionMethod")]
        public string CompressionMethod { get; set; }
    }
}
