using Newtonsoft.Json;
using System.Collections.Generic;

namespace FModel.Grabber.Paks
{
    public class InstallsJson
    {
        [JsonProperty("associated_client")]
        public Dictionary<string, string> AssociatedClient;
    }
}
