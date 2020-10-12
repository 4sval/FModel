namespace FModel
{
    public enum EGame
    {
        Unknown,
        Fortnite,
        Valorant,
        DeadByDaylight,
        Borderlands3,
        MinecraftDungeons,
        BattleBreakers,
        Spellbreak,
        StateOfDecay2, // WIP
        TheCycleEA // TODO: Early Access version, change when game is released
    }

    public enum EFModel
    {
        Debug,
        Release,
        Unknown
    }

    public enum EPakLoader
    {
        Single,
        All,
        New,
        Modified,
        NewModified
    }

    public enum ECopy
    {
        Path,
        PathNoExt,
        PathNoFile,
        File,
        FileNoExt
    }

    public enum ELanguage : long
    {
        English,
        AustralianEnglish,
        BritishEnglish,
        French,
        German,
        Italian,
        Spanish,
        SpanishLatin,
        SpanishMexico,
        Arabic,
        Japanese,
        Korean,
        Polish,
        PortugueseBrazil,
        PortuguesePortugal,
        Russian,
        Turkish,
        Chinese,
        TraditionalChinese,
        Swedish,
        Thai,
        Indonesian,
        VietnameseVietnam
    }

    public enum EJsonType: long
    {
        Default,
        Positioned
    }

    public enum EIconDesign : long
    {
        Default,
        NoBackground,
        NoText,
        Mini,
        Flat
    }
}
