using System.Collections.Generic;
using System.Diagnostics;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using I = Newtonsoft.Json.JsonIgnoreAttribute;

namespace FModel.ViewModels.ApiEndpoints.Models;

[DebuggerDisplay("{" + nameof(Version) + "}")]
public class AesResponse
{
    [I][J("version")] public string Version { get; private set; }
    [J("mainKey")] public string MainKey { get; set; }
    [J("dynamicKeys")] public List<DynamicKey> DynamicKeys { get; set; }

    public AesResponse()
    {
        MainKey = string.Empty;
        DynamicKeys = new List<DynamicKey>();
    }

    [I] public bool HasDynamicKeys => DynamicKeys is { Count: > 0 };
    [I] public bool IsValid => !string.IsNullOrEmpty(MainKey);
}

[DebuggerDisplay("{" + nameof(Key) + "}")]
public class DynamicKey
{
    [J("name")] public string Name { get; set; }
    [J("guid")] public string Guid { get; set; }
    [J("key")] public string Key { get; set; }

    [I] public bool IsValid => !string.IsNullOrEmpty(Guid) &&
                              !string.IsNullOrEmpty(Key);
}
