using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FModel.Parser.Quests
{
    public partial class QuestParser
    {
        [JsonProperty("export_type")]
        public string ExportType { get; set; }

        [JsonProperty("QuestType")]
        public string QuestType { get; set; }

        [JsonProperty("bAthenaMustCompleteInSingleMatch")]
        public bool BAthenaMustCompleteInSingleMatch { get; set; }

        [JsonProperty("bIncludedInCategories")]
        public bool BIncludedInCategories { get; set; }

        [JsonProperty("ObjectiveCompletionCount")]
        public long ObjectiveCompletionCount { get; set; }

        [JsonProperty("Rewards")]
        public Reward[] Rewards { get; set; }

        [JsonProperty("HiddenRewards")]
        public HiddenRewards[] HiddenRewards { get; set; }

        [JsonProperty("Objectives")]
        public Objective[] Objectives { get; set; }

        [JsonProperty("Weight")]
        public double Weight { get; set; }

        [JsonProperty("CompletionText")]
        public string CompletionText { get; set; }

        [JsonProperty("GrantToProfileType")]
        public string GrantToProfileType { get; set; }

        [JsonProperty("DisplayName")]
        public string DisplayName { get; set; }

        [JsonProperty("Description")]
        public string Description { get; set; }

        [JsonProperty("GameplayTags")]
        public GameplayTags GameplayTags { get; set; }

        [JsonProperty("SmallPreviewImage")]
        public LargePreviewImage SmallPreviewImage { get; set; }

        [JsonProperty("LargePreviewImage")]
        public LargePreviewImage LargePreviewImage { get; set; }
    }

    public class GameplayTags
    {
        [JsonProperty("gameplay_tags")]
        public string[] GameplayTagsGameplayTags { get; set; }
    }

    public class LargePreviewImage
    {
        [JsonProperty("asset_path_name")]
        public string AssetPathName { get; set; }

        [JsonProperty("sub_path_string")]
        public string SubPathString { get; set; }
    }

    public class Objective
    {
        [JsonProperty("BackendName")]
        public string BackendName { get; set; }

        [JsonProperty("ObjectiveStatHandle")]
        public ObjectiveStatHandle ObjectiveStatHandle { get; set; }

        [JsonProperty("AlternativeStatHandles")]
        public object[] AlternativeStatHandles { get; set; }

        [JsonProperty("ItemEvent")]
        public string ItemEvent { get; set; }

        [JsonProperty("bHidden")]
        public bool BHidden { get; set; }

        [JsonProperty("bRequirePrimaryMissionCompletion")]
        public bool BRequirePrimaryMissionCompletion { get; set; }

        [JsonProperty("bCanProgressInZone")]
        public bool BCanProgressInZone { get; set; }

        [JsonProperty("bDisplayDynamicAnnouncementUpdate")]
        public bool BDisplayDynamicAnnouncementUpdate { get; set; }

        [JsonProperty("DynamicStatusUpdateType")]
        public string DynamicStatusUpdateType { get; set; }

        [JsonProperty("LinkVaultTab")]
        public string LinkVaultTab { get; set; }

        [JsonProperty("LinkToItemManagement")]
        public string LinkToItemManagement { get; set; }

        [JsonProperty("ItemReference")]
        public LargePreviewImage ItemReference { get; set; }

        [JsonProperty("ItemTemplateIdOverride")]
        public string ItemTemplateIdOverride { get; set; }

        [JsonProperty("LinkSquadID")]
        public string LinkSquadId { get; set; }

        [JsonProperty("LinkSquadIndex")]
        public long LinkSquadIndex { get; set; }

        [JsonProperty("Description")]
        public string Description { get; set; }

        [JsonProperty("HudShortDescription")]
        public string HudShortDescription { get; set; }

        [JsonProperty("HudIcon")]
        public LargePreviewImage HudIcon { get; set; }

        [JsonProperty("Count")]
        public long Count { get; set; }

        [JsonProperty("Stage")]
        public long Stage { get; set; }

        [JsonProperty("DynamicStatusUpdatePercentInterval")]
        public long DynamicStatusUpdatePercentInterval { get; set; }

        [JsonProperty("DynamicUpdateCompletionDelay")]
        public long DynamicUpdateCompletionDelay { get; set; }

        [JsonProperty("ScriptedAction")]
        public LargePreviewImage ScriptedAction { get; set; }
    }

    public class ObjectiveStatHandle
    {
        [JsonProperty("DataTable")]
        public string DataTable { get; set; }

        [JsonProperty("RowName")]
        public string RowName { get; set; }
    }

    public class Reward
    {
        [JsonProperty("ItemPrimaryAssetId")]
        public ItemPrimaryAssetId ItemPrimaryAssetId { get; set; }

        [JsonProperty("Quantity")]
        public long Quantity { get; set; }
    }

    public class HiddenRewards
    {
        [JsonProperty("TemplateId")]
        public string TemplateId { get; set; }

        [JsonProperty("Quantity")]
        public long Quantity { get; set; }
    }

    public class ItemPrimaryAssetId
    {
        [JsonProperty("PrimaryAssetType")]
        public PrimaryAssetType PrimaryAssetType { get; set; }

        [JsonProperty("PrimaryAssetName")]
        public string PrimaryAssetName { get; set; }
    }

    public class PrimaryAssetType
    {
        [JsonProperty("Name")]
        public string Name { get; set; }
    }

    public partial class QuestParser
    {
        public static QuestParser[] FromJson(string json) => JsonConvert.DeserializeObject<QuestParser[]>(json, Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this QuestParser[] self) => JsonConvert.SerializeObject(self, Converter.Settings);
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
