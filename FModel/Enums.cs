using System;
using System.ComponentModel;
using FModel.Extensions;

namespace FModel;

public enum EBuildKind
{
    Debug,
    Release,
    Unknown
}

public enum EErrorKind
{
    Ignore,
    Restart,
    ResetSettings
}

public enum SettingsOut
{
    ReloadLocres,
    ReloadMappings
}

public enum EStatusKind
{
    Ready, // ready
    Configuring, // waiting for user input
    Loading, // doing stuff
    Stopping, // trying to stop
    Stopped, // stopped
    Failed, // crashed
    Completed // worked
}

public enum EAesReload
{
    [Description("Always")]
    Always,
    [Description("Never")]
    Never,
    [Description("Once Per Day")]
    OncePerDay
}

public enum EDiscordRpc
{
    [Description("Always")]
    Always,
    [Description("Never")]
    Never
}

public enum ELoadingMode
{
    [Description("Multiple")]
    Multiple,
    [Description("All")]
    All,
    [Description("All (New)")]
    AllButNew,
    [Description("All (Modified)")]
    AllButModified,
    [Description("All (Except Patched Assets)")]
    AllButPatched,
}

public enum ECompressedAudio
{
    [Description("Play the decompressed data")]
    PlayDecompressed,
    [Description("Play the compressed data (might not always be a valid audio data)")]
    PlayCompressed
}

public enum EIconStyle
{
    [Description("Default")]
    Default,
    [Description("No Background")]
    NoBackground,
    [Description("No Text")]
    NoText,
    [Description("Flat")]
    Flat,
    [Description("Cataba")]
    Cataba,
    // [Description("Community")]
    // CommunityMade
}

public enum EEndpointType
{
    Aes,
    Mapping
}

[Flags]
public enum EBulkType
{
    None =          0,
    Auto =          1 << 0,
    Properties =    1 << 1,
    Textures =      1 << 2,
    Meshes =        1 << 3,
    Animations =    1 << 4,
    Audio =         1 << 5,
    Code =          1 << 6,
    Raw =           1 << 7,
}

public enum EAssetCategory : uint
{
    All = AssetCategoryExtensions.CategoryBase + (0 << 16),
    Blueprints = AssetCategoryExtensions.CategoryBase + (1 << 16),
        BlueprintGeneratedClass = Blueprints + 1,
        WidgetBlueprintGeneratedClass = Blueprints + 2,
        AnimBlueprintGeneratedClass = Blueprints + 3,
        RigVMBlueprintGeneratedClass = Blueprints + 4,
        UserDefinedEnum = Blueprints + 5,
        UserDefinedStruct = Blueprints + 6,
        //Metadata
        Blueprint = Blueprints + 8,
        CookedMetaData = Blueprints + 9,
    Mesh = AssetCategoryExtensions.CategoryBase + (2 << 16),
        StaticMesh = Mesh + 1,
        SkeletalMesh = Mesh + 2,
        CustomizableObject = Mesh + 3,
        NaniteDisplacedMesh = Mesh + 4,
    Texture = AssetCategoryExtensions.CategoryBase + (3 << 16),
    Materials = AssetCategoryExtensions.CategoryBase + (4 << 16),
        Material = Materials + 1,
        MaterialEditorData = Materials + 2,
        MaterialFunction = Materials + 3,
        MaterialFunctionEditorData = Materials + 4,
        MaterialParameterCollection = Materials + 5,
    Animation = AssetCategoryExtensions.CategoryBase + (5 << 16),
        Skeleton = Animation + 1,
        Rig = Animation + 2,
    Level = AssetCategoryExtensions.CategoryBase + (6 << 16),
        World = Level + 1,
        BuildData = Level + 2,
        LevelSequence = Level + 3,
        Foliage = Level + 4,
    Data = AssetCategoryExtensions.CategoryBase + (7 << 16),
        ItemDefinitionBase = Data + 1,
        CurveBase = Data + 2,
        PhysicsAsset = Data + 3,
        ObjectRedirector = Data + 4,
        PhysicalMaterial = Data + 5,
        ByteCode = Data + 6,
    Media = AssetCategoryExtensions.CategoryBase + (8 << 16),
        Audio = Media + 1,
        Video = Media + 2,
        Font = Media + 3,
        SoundBank = Media + 4,
        AudioEvent = Media + 5,
    Particle = AssetCategoryExtensions.CategoryBase + (9 << 16),
    GameSpecific = AssetCategoryExtensions.CategoryBase + (10 << 16),
        Borderlands = GameSpecific + 1,
        Aion2 = GameSpecific + 2,
        RocoKingdomWorld = GameSpecific + 3,
        DeltaForce = GameSpecific + 4,
}
