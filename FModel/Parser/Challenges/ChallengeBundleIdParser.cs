using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FModel.Parser.Challenges
{
    public partial class ChallengeBundleIdParser
    {
        [JsonProperty("export_type")]
        public string ExportType { get; set; }

        [JsonProperty("QuestInfos")]
        public QuestInfo[] QuestInfos { get; set; }

        [JsonProperty("BundleCompletionRewards")]
        public BundleCompletionReward[] BundleCompletionRewards { get; set; }

        [JsonProperty("DisplayStyle")]
        public DisplayStyle DisplayStyle { get; set; }

        [JsonProperty("DisplayName")]
        public DisplayName DisplayName { get; set; }

        [JsonProperty("SmallPreviewImage")]
        public LargePreviewImage SmallPreviewImage { get; set; }

        [JsonProperty("LargePreviewImage")]
        public LargePreviewImage LargePreviewImage { get; set; }
    }

    public partial class DisplayName
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("source_string")]
        public string SourceString { get; set; }
    }

    public class BundleCompletionReward
    {
        [JsonProperty("CompletionCount")]
        public long CompletionCount { get; set; }

        [JsonProperty("Rewards")]
        public Reward[] Rewards { get; set; }
    }

    public class Reward
    {
        [JsonProperty("ItemDefinition")]
        public LargePreviewImage ItemDefinition { get; set; }

        [JsonProperty("TemplateId")]
        public string TemplateId { get; set; }

        [JsonProperty("Quantity")]
        public long Quantity { get; set; }

        [JsonProperty("RewardGiftBox")]
        public RewardGiftBox RewardGiftBox { get; set; }

        [JsonProperty("IsChaseReward")]
        public bool IsChaseReward { get; set; }

        [JsonProperty("RewardType")]
        public string RewardType { get; set; }
    }

    public class LargePreviewImage
    {
        [JsonProperty("asset_path_name")]
        public string AssetPathName { get; set; }

        [JsonProperty("sub_path_string")]
        public string SubPathString { get; set; }
    }

    public class RewardGiftBox
    {
        [JsonProperty("GiftBoxToUse")]
        public LargePreviewImage GiftBoxToUse { get; set; }

        [JsonProperty("GiftBoxFormatData")]
        public object[] GiftBoxFormatData { get; set; }
    }

    public class DisplayStyle
    {
        [JsonProperty("PrimaryColor")]
        public ColorChallenge PrimaryColor { get; set; }

        [JsonProperty("SecondaryColor")]
        public ColorChallenge SecondaryColor { get; set; }

        [JsonProperty("AccentColor")]
        public ColorChallenge AccentColor { get; set; }

        [JsonProperty("DisplayImage")]
        public LargePreviewImage DisplayImage { get; set; }
    }

    public class ColorChallenge
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

    public class QuestInfo
    {
        [JsonProperty("QuestDefinition")]
        public LargePreviewImage QuestDefinition { get; set; }

        [JsonProperty("QuestUnlockType")]
        public string QuestUnlockType { get; set; }

        [JsonProperty("UnlockValue")]
        public long UnlockValue { get; set; }

        [JsonProperty("RewardGiftBox")]
        public RewardGiftBox RewardGiftBox { get; set; }
    }

    public partial class ChallengeBundleIdParser
    {
        public static ChallengeBundleIdParser[] FromJson(string json) => JsonConvert.DeserializeObject<ChallengeBundleIdParser[]>(json, Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this ChallengeBundleIdParser[] self) => JsonConvert.SerializeObject(self, Converter.Settings);
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
