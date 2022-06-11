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
    Restart,
    ReloadLocres,
    CheckForUpdates,
    Nothing
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

public enum FGame
{
    [Description("Unknown")]
    Unknown,
    [Description("Fortnite")]
    FortniteGame,
    [Description("Valorant")]
    ShooterGame,
    [Description("Dead By Daylight")]
    DeadByDaylight,
    [Description("Borderlands 3")]
    OakGame,
    [Description("Minecraft Dungeons")]
    Dungeons,
    [Description("Battle Breakers")]
    WorldExplorers,
    [Description("Spellbreak")]
    g3,
    [Description("State Of Decay 2")]
    StateOfDecay2,
    [Description("The Cycle")]
    Prospect,
    [Description("The Outer Worlds")]
    Indiana,
    [Description("Rogue Company")]
    RogueCompany,
    [Description("Star Wars: Jedi Fallen Order")]
    SwGame,
    [Description("Core")]
    Platform,
    [Description("Days Gone")]
    BendGame,
    [Description("PLAYERUNKNOWN'S BATTLEGROUNDS")]
    TslGame,
    [Description("Splitgate")]
    PortalWars,
    [Description("GTA: The Trilogy - Definitive Edition")]
    Gameface,
    [Description("Sea of Thieves")]
    Athena
}

public enum ELoadingMode
{
    [Description("Single")]
    Single,
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
    Beta
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