using System.Diagnostics;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using I = Newtonsoft.Json.JsonIgnoreAttribute;

namespace FModel.ViewModels.ApiEndpoints.Models;

[DebuggerDisplay("{" + nameof(FileName) + "}")]
public class MappingsResponse
{
    [J] public string Url { get; set; }
    [J] public string FileName { get; set; }
    [I][J] public string Hash { get; private set; }
    [I][J] public long Length { get; private set; }
    [I][J] public string Uploaded { get; private set; }
    [J] public Meta Meta { get; set; }

    public MappingsResponse()
    {
        Url = string.Empty;
        FileName = string.Empty;
        Meta = new Meta();
    }

    [I] public bool IsValid => !string.IsNullOrEmpty(Url) &&
                              !string.IsNullOrEmpty(FileName) &&
                              Meta != null;
}

[DebuggerDisplay("{" + nameof(CompressionMethod) + "}")]
public class Meta
{
    [I][J] public string Version { get; private set; }
    [J] public string CompressionMethod { get; set; }

    public Meta()
    {
        CompressionMethod = "Oodle";
    }

    [I] public bool IsValid => CompressionMethod == "Oodle";
}
