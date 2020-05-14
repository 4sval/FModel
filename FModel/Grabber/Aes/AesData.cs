using FModel.Logger;
using FModel.Utils;
using FModel.Windows.CustomNotifier;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace FModel.Grabber.Aes
{
    static class AesData
    {
        public static async Task<BenResponse> GetData()
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                BenResponse data = await Endpoints.GetJsonEndpoint<BenResponse>(Endpoints.BENBOT_AES).ConfigureAwait(false);
                return data;
            }
            else
            {
                Globals.gNotifier.ShowCustomMessage(Properties.Resources.AES, Properties.Resources.NoInternet, "/FModel;component/Resources/wifi-strength-off.ico");
                DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[AES]", "No internet");
                return null;
            }
        }
    }

    public class BenResponse
    {
        [JsonProperty("mainKey")]
        public string MainKey { get; set; }

        [JsonProperty("dynamicKeys")]
        public Dictionary<string, string> DynamicKeys { get; set; }
    }
}
