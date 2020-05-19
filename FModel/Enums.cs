namespace FModel
{
    public enum EGame
    {
        Unknown,
        Fortnite,
        Valorant
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
        French,
        German,
        Italian,
        Spanish,
        SpanishLatin,
        Arabic,
        Japanese,
        Korean,
        Polish,
        PortugueseBrazil,
        Russian,
        Turkish,
        Chinese,
        TraditionalChinese
    }

    public enum EIconDesign : long
    {
        Default,
        NoText,
        Mini,
        Flat
    }
}
