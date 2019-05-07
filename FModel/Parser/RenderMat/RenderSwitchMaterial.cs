using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FModel.Parser.RenderMat
{
    public partial class RenderSwitchMaterial
    {
        [JsonProperty("export_type")]
        public string ExportType { get; set; }

        [JsonProperty("Parent")]
        public string Parent { get; set; }

        [JsonProperty("ScalarParameterValues")]
        public ScalarParameterValue[] ScalarParameterValues { get; set; }

        [JsonProperty("TextureParameterValues")]
        public TextureParameterValue[] TextureParameterValues { get; set; }

        [JsonProperty("BasePropertyOverrides")]
        public BasePropertyOverrides BasePropertyOverrides { get; set; }
    }

    public class BasePropertyOverrides
    {
        [JsonProperty("BlendMode")]
        public string BlendMode { get; set; }

        [JsonProperty("ShadingModel")]
        public string ShadingModel { get; set; }

        [JsonProperty("OpacityMaskClipValue")]
        public double OpacityMaskClipValue { get; set; }
    }

    public class ScalarParameterValue
    {
        [JsonProperty("ParameterInfo")]
        public ParameterInfo ParameterInfo { get; set; }

        [JsonProperty("ParameterValue")]
        public long ParameterValue { get; set; }

        [JsonProperty("ExpressionGUID")]
        public string ExpressionGuid { get; set; }
    }

    public class ParameterInfo
    {
        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Association")]
        public string Association { get; set; }

        [JsonProperty("Index")]
        public long Index { get; set; }
    }

    public class TextureParameterValue
    {
        [JsonProperty("ParameterInfo")]
        public ParameterInfo ParameterInfo { get; set; }

        [JsonProperty("ParameterValue")]
        public string ParameterValue { get; set; }

        [JsonProperty("ExpressionGUID")]
        public string ExpressionGuid { get; set; }
    }

    public partial class RenderSwitchMaterial
    {
        public static RenderSwitchMaterial[] FromJson(string json) => JsonConvert.DeserializeObject<RenderSwitchMaterial[]>(json, Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this RenderSwitchMaterial[] self) => JsonConvert.SerializeObject(self, Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            }
        };
    }
}
