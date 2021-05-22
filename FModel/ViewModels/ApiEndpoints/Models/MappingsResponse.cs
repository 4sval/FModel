using System.Diagnostics;
using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace FModel.ViewModels.ApiEndpoints.Models
{
    [DebuggerDisplay("{" + nameof(FileName) + "}")]
    public class MappingsResponse
    {
        [J] public string Url { get; private set; }
        [J] public string FileName { get; private set; }
        [J] public string Hash { get; private set; }
        [J] public long Length { get; private set; }
        [J] public string Uploaded { get; private set; }
        [J] public Meta Meta { get; private set; }
    }

    [DebuggerDisplay("{" + nameof(CompressionMethod) + "}")]
    public class Meta
    {
        [J] public string Version { get; private set; }
        [J] public string CompressionMethod { get; private set; }
    }
}