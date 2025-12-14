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

// public enum EUpdateMode
// {
//     [Description("Stable")]
//     Stable,
//     [Description("Beta")]
//     Beta,
//     [Description("QA Testing")]
//     Qa
// }

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
    Skeletons =     1 << 4,
    Animations =    1 << 5,
    Audio =         1 << 6
}

public enum EAssetCategory : uint
{
    All = AssetCategoryExtensions.CategoryBase + (0 << 16),
    Texture = AssetCategoryExtensions.CategoryBase + (1 << 16),
    Mesh = AssetCategoryExtensions.CategoryBase + (2 << 16),
        StaticMesh = Mesh + 1,
        SkeletalMesh = Mesh + 2,
    Skeleton = AssetCategoryExtensions.CategoryBase + (4 << 16),
    Materials = AssetCategoryExtensions.CategoryBase + (5 << 16),
        Material = Materials + 1,
        MaterialEditorData = Materials + 2,
    Blueprint = AssetCategoryExtensions.CategoryBase + (6 << 16),
    Audio = AssetCategoryExtensions.CategoryBase + (7 << 16),
    Animation = AssetCategoryExtensions.CategoryBase + (8 << 16),
    Font = AssetCategoryExtensions.CategoryBase + (9 << 16),
    PhysicsAsset = AssetCategoryExtensions.CategoryBase + (10 << 16),
    Video = AssetCategoryExtensions.CategoryBase + (11 << 16),
    Data = AssetCategoryExtensions.CategoryBase + (12 << 16),
    Map = AssetCategoryExtensions.CategoryBase + (13 << 16),
        World = Map + 1,
        BuildData = Map + 2,
    Particle = AssetCategoryExtensions.CategoryBase + (14 << 16)
}
