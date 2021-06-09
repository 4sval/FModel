using System.Collections.Generic;
using System.Diagnostics;
using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace FModel.ViewModels.ApiEndpoints.Models
{
    [DebuggerDisplay("{" + nameof(Version) + "}")]
    public class AesResponse
    {
        [J("version")] public string Version { get; private set; }
        [J("mainKey")] public string MainKey { get; set; }
        [J("dynamicKeys")] public List<DynamicKey> DynamicKeys { get; set; }

        public bool HasDynamicKeys => DynamicKeys is {Count: > 0};
    }

    [DebuggerDisplay("{" + nameof(Key) + "}")]
    public class DynamicKey
    {
        [J("fileName")] public string FileName { get; set; }
        [J("guid")] public string Guid { get; set; }
        [J("key")] public string Key { get; set; }
    }
}