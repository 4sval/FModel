using System;
using System.ComponentModel;

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
    ReloadMappings,
    CheckForUpdates
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
    AllButModified
}

public enum EUpdateMode
{
    [Description("Stable")]
    Stable,
    [Description("Beta")]
    Beta,
    [Description("QA Testing")]
    Qa
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
    Skeletons =     1 << 4,
    Animations =    1 << 5
}
