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
        TheCycle
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
        English = 0,
        AustralianEnglish = 15,
        BritishEnglish = 16,
        French = 1,
        German = 2,
        Italian = 3,
        Spanish = 4,
        SpanishLatin = 5,
        SpanishMexico = 17,
        Arabic = 6,
        Japanese = 7,
        Korean = 8,
        Polish = 9,
        PortugueseBrazil = 10,
        PortuguesePortugal = 18,
        Russian = 11,
        Turkish = 12,
        Chinese = 13,
        TraditionalChinese = 14,
        Swedish = 19,
        Thai = 20,
        Indonesian = 21,
        VietnameseVietnam = 22
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
