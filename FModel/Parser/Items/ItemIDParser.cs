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
        public FTextInfos DisplayName { get; set; }

        [JsonProperty("ShortDescription")]
        public FTextInfos ShortDescription { get; set; }

        [JsonProperty("Description")]
        public FTextInfos Description { get; set; }

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

        [JsonProperty("AttributeInitKey")]
        public AttributeInitKey AttributeInitKey { get; set; }

        [JsonProperty("MaxStackSize")]
        public long MaxStackSize { get; set; }

        [JsonProperty("MinLevel")]
        public long MinLevel { get; set; }

        [JsonProperty("MaxLevel")]
        public long MaxLevel { get; set; }

        [JsonProperty("WeaponStatHandle")]
        public WeaponStatHandle WeaponStatHandle { get; set; }
    }

    public partial class FTextInfos
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("source_string")]
        public string SourceString { get; set; }
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

    public partial class AttributeInitKey
    {
        [JsonProperty("AttributeInitCategory")]
        public string AttributeInitCategory { get; set; }

        [JsonProperty("AttributeInitSubCategory")]
        public string AttributeInitSubCategory { get; set; }
    }

    public partial class WeaponStatHandle
    {
        [JsonProperty("DataTable")]
        public string DataTable { get; set; }

        [JsonProperty("RowName")]
        public string RowName { get; set; }
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
