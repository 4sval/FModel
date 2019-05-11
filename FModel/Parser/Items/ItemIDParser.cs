using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FModel.Parser.Items
{
    public partial class ItemsIdParser
    {
        [JsonProperty("export_type")]
        public string ExportType { get; set; }

        [JsonProperty("cosmetic_item")]
        public string CosmeticItem { get; set; }

        [JsonProperty("CharacterParts")]
        public string[] CharacterParts { get; set; }

        [JsonProperty("HeroDefinition")]
        public string HeroDefinition { get; set; }

        [JsonProperty("WeaponDefinition")]
        public string WeaponDefinition { get; set; }

        [JsonProperty("Rarity")]
        public string Rarity { get; set; }

        [JsonProperty("Series")]
        public string Series { get; set; }

        [JsonProperty("DisplayName")]
        public string DisplayName { get; set; }

        [JsonProperty("ShortDescription")]
        public string ShortDescription { get; set; }

        [JsonProperty("Description")]
        public string Description { get; set; }

        [JsonProperty("GameplayTags")]
        public GameplayTags GameplayTags { get; set; }

        [JsonProperty("SmallPreviewImage")]
        public PreviewImage SmallPreviewImage { get; set; }

        [JsonProperty("LargePreviewImage")]
        public PreviewImage LargePreviewImage { get; set; }

        [JsonProperty("DisplayAssetPath")]
        public DisplayAssetPath DisplayAssetPath { get; set; }

        [JsonProperty("AmmoData")]
        public AmmoData AmmoData { get; set; }
    }

    public class GameplayTags
    {
        [JsonProperty("gameplay_tags")]
        public string[] GameplayTagsGameplayTags { get; set; }
    }

    public class PreviewImage
    {
        [JsonProperty("asset_path_name")]
        public string AssetPathName { get; set; }

        [JsonProperty("sub_path_string")]
        public string SubPathString { get; set; }
    }

    public class DisplayAssetPath
    {
        [JsonProperty("asset_path_name")]
        public string AssetPathName { get; set; }

        [JsonProperty("sub_path_string")]
        public string SubPathString { get; set; }
    }

    public partial class AmmoData
    {
        [JsonProperty("asset_path_name")]
        public string AssetPathName { get; set; }

        [JsonProperty("sub_path_string")]
        public string SubPathString { get; set; }
    }

    public partial class ItemsIdParser
    {
        public static ItemsIdParser[] FromJson(string json) => JsonConvert.DeserializeObject<ItemsIdParser[]>(json, Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this ItemsIdParser[] self) => JsonConvert.SerializeObject(self, Converter.Settings);
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
