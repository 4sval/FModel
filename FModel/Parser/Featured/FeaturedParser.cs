using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FModel.Parser.Featured
{
    public partial class FeaturedParser
    {
        [JsonProperty("export_type")]
        public string ExportType { get; set; }

        [JsonProperty("TileImage")]
        public ImageLol TileImage { get; set; }

        [JsonProperty("DetailsImage")]
        public ImageLol DetailsImage { get; set; }

        [JsonProperty("Gradient")]
        public Gradient Gradient { get; set; }

        [JsonProperty("Background")]
        public Background Background { get; set; }
    }

    public class Background
    {
        [JsonProperty("r")]
        public double R { get; set; }

        [JsonProperty("g")]
        public double G { get; set; }

        [JsonProperty("b")]
        public double B { get; set; }

        [JsonProperty("a")]
        public long A { get; set; }
    }

    public class ImageLol
    {
        [JsonProperty("ImageSize")]
        public ImageSize ImageSize { get; set; }

        [JsonProperty("ResourceObject")]
        public string ResourceObject { get; set; }
    }

    public class ImageSize
    {
        [JsonProperty("x")]
        public long X { get; set; }

        [JsonProperty("y")]
        public long Y { get; set; }
    }

    public class Gradient
    {
        [JsonProperty("Start")]
        public Background Start { get; set; }

        [JsonProperty("Stop")]
        public Background Stop { get; set; }
    }

    public partial class FeaturedParser
    {
        public static FeaturedParser[] FromJson(string json) => JsonConvert.DeserializeObject<FeaturedParser[]>(json, Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this FeaturedParser[] self) => JsonConvert.SerializeObject(self, Converter.Settings);
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
