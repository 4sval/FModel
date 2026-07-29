using System.ComponentModel;

namespace FModel.Extensions.Themes;

public enum EJsonHighlightTheme
{
    [Description("Default")]
    Default,
    [Description("Mint Lavender")]
    MintLavender,
    [Description("Soft Blue")]
    SoftBlueGreen,
    [Description("Purple Cyan")]
    PurpleCyan,
    [Description("Neutral Warm")]
    NeutralWarm,
    [Description("Nord")]
    Nord,
    [Description("Mocha")]
    Mocha,
    [Description("Tokyo Night")]
    TokyoNight,
    [Description("One Dark")]
    OneDark,
    [Description("Gruvbox Dark")]
    GruvboxDark,
    [Description("Rosé Pine")]
    RosePine,
    [Description("Monokai")]
    Monokai,
    [Description("Oceanic")]
    Oceanic,
    [Description("Forest")]
    Forest,
    [Description("Amber")]
    Amber,
    [Description("Iceberg")]
    Iceberg
}

public static class JsonHighlightThemes
{
    public sealed record JsonHighlightPalette(
        HighlightColor FieldName,
        HighlightColor String,
        HighlightColor Number,
        HighlightColor Bool,
        HighlightColor Null,
        HighlightColor Punctuation,
        HighlightColor Escape
    );

    public static JsonHighlightPalette Get(EJsonHighlightTheme theme) => theme switch
    {
        EJsonHighlightTheme.MintLavender => new JsonHighlightPalette(
            FieldName: new HighlightColor("#CBA6F7"),
            String: new HighlightColor("#8FE3CF"),
            Number: new HighlightColor("#FFD166"),
            Bool: new HighlightColor("#FF9CAC", Bold: true),
            Null: new HighlightColor("#A7B0C0", Italic: true),
            Punctuation: new HighlightColor("#D7DEE9"),
            Escape: new HighlightColor("#5DD9C1", Bold: true)
        ),
        EJsonHighlightTheme.SoftBlueGreen => new JsonHighlightPalette(
            FieldName: new HighlightColor("#7CC7FF"),
            String: new HighlightColor("#A6E3A1"),
            Number: new HighlightColor("#FFA36C"),
            Bool: new HighlightColor("#FF6B8A", Bold: true),
            Null: new HighlightColor("#8FA3BF", Italic: true),
            Punctuation: new HighlightColor("#C7D0DD"),
            Escape: new HighlightColor("#66E3D4", Bold: true)
        ),
        EJsonHighlightTheme.PurpleCyan => new JsonHighlightPalette(
            FieldName: new HighlightColor("#BD93F9"),
            String: new HighlightColor("#7EE7F2"),
            Number: new HighlightColor("#FFB86C"),
            Bool: new HighlightColor("#FF6B8B", Bold: true),
            Null: new HighlightColor("#9AA7B8", Italic: true),
            Punctuation: new HighlightColor("#CBD5E1"),
            Escape: new HighlightColor("#5DE4C7", Bold: true)
        ),

        EJsonHighlightTheme.NeutralWarm => new JsonHighlightPalette(
            FieldName: new HighlightColor("#F8C291"),
            String: new HighlightColor("#A3E635"),
            Number: new HighlightColor("#FB923C"),
            Bool: new HighlightColor("#F87171", Bold: true),
            Null: new HighlightColor("#9CA3AF", Italic: true),
            Punctuation: new HighlightColor("#D1D5DB"),
            Escape: new HighlightColor("#67E8F9", Bold: true)
        ),

        EJsonHighlightTheme.Nord => new JsonHighlightPalette(
            FieldName: new HighlightColor("#88C0D0"),
            String: new HighlightColor("#A3BE8C"),
            Number: new HighlightColor("#D08770"),
            Bool: new HighlightColor("#BF616A", Bold: true),
            Null: new HighlightColor("#81A1C1", Italic: true),
            Punctuation: new HighlightColor("#D8DEE9"),
            Escape: new HighlightColor("#8FBCBB", Bold: true)
        ),
        EJsonHighlightTheme.Default => new JsonHighlightPalette(
            FieldName: new HighlightColor("#FFCB6B"),
            String: new HighlightColor("#C3E88D"),
            Number: new HighlightColor("#F78C6C"),
            Bool: new HighlightColor("#61AFEF", Bold: true),
            Null: new HighlightColor("#7F848E", Italic: true),
            Punctuation: new HighlightColor("#89DDFF"),
            Escape: new HighlightColor("#4DD0E1", Bold: true)
        ),
        EJsonHighlightTheme.Mocha => new JsonHighlightPalette(
            FieldName: new HighlightColor("#CBA6F7"),
            String: new HighlightColor("#A6E3A1"),
            Number: new HighlightColor("#FAB387"),
            Bool: new HighlightColor("#F38BA8", Bold: true),
            Null: new HighlightColor("#9399B2", Italic: true),
            Punctuation: new HighlightColor("#CDD6F4"),
            Escape: new HighlightColor("#94E2D5", Bold: true)
        ),
        EJsonHighlightTheme.TokyoNight => new JsonHighlightPalette(
            FieldName: new HighlightColor("#7AA2F7"),
            String: new HighlightColor("#9ECE6A"),
            Number: new HighlightColor("#FF9E64"),
            Bool: new HighlightColor("#F7768E", Bold: true),
            Null: new HighlightColor("#565F89", Italic: true),
            Punctuation: new HighlightColor("#A9B1D6"),
            Escape: new HighlightColor("#7DCFFF", Bold: true)
        ),
        EJsonHighlightTheme.OneDark => new JsonHighlightPalette(
            FieldName: new HighlightColor("#E5C07B"),
            String: new HighlightColor("#98C379"),
            Number: new HighlightColor("#D19A66"),
            Bool: new HighlightColor("#61AFEF", Bold: true),
            Null: new HighlightColor("#5C6370", Italic: true),
            Punctuation: new HighlightColor("#ABB2BF"),
            Escape: new HighlightColor("#56B6C2", Bold: true)
        ),
        EJsonHighlightTheme.GruvboxDark => new JsonHighlightPalette(
            FieldName: new HighlightColor("#FABD2F"),
            String: new HighlightColor("#B8BB26"),
            Number: new HighlightColor("#FE8019"),
            Bool: new HighlightColor("#FB4934", Bold: true),
            Null: new HighlightColor("#928374", Italic: true),
            Punctuation: new HighlightColor("#EBDBB2"),
            Escape: new HighlightColor("#8EC07C", Bold: true)
        ),
        EJsonHighlightTheme.RosePine => new JsonHighlightPalette(
            FieldName: new HighlightColor("#C4A7E7"),
            String: new HighlightColor("#9CCFD8"),
            Number: new HighlightColor("#F6C177"),
            Bool: new HighlightColor("#EB6F92", Bold: true),
            Null: new HighlightColor("#6E6A86", Italic: true),
            Punctuation: new HighlightColor("#E0DEF4"),
            Escape: new HighlightColor("#31748F", Bold: true)
        ),
        EJsonHighlightTheme.Monokai => new JsonHighlightPalette(
            FieldName: new HighlightColor("#A6E22E"),
            String: new HighlightColor("#E6DB74"),
            Number: new HighlightColor("#AE81FF"),
            Bool: new HighlightColor("#F92672", Bold: true),
            Null: new HighlightColor("#75715E", Italic: true),
            Punctuation: new HighlightColor("#F8F8F2"),
            Escape: new HighlightColor("#66D9EF", Bold: true)
        ),
        EJsonHighlightTheme.Oceanic => new JsonHighlightPalette(
            FieldName: new HighlightColor("#5FB3B3"),
            String: new HighlightColor("#99C794"),
            Number: new HighlightColor("#F99157"),
            Bool: new HighlightColor("#EC5F67", Bold: true),
            Null: new HighlightColor("#65737E", Italic: true),
            Punctuation: new HighlightColor("#D8DEE9"),
            Escape: new HighlightColor("#6699CC", Bold: true)
        ),
        EJsonHighlightTheme.Forest => new JsonHighlightPalette(
            FieldName: new HighlightColor("#86EFAC"),
            String: new HighlightColor("#B4D455"),
            Number: new HighlightColor("#FACC15"),
            Bool: new HighlightColor("#FB7185", Bold: true),
            Null: new HighlightColor("#6B7280", Italic: true),
            Punctuation: new HighlightColor("#D1FAE5"),
            Escape: new HighlightColor("#34D399", Bold: true)
        ),
        EJsonHighlightTheme.Amber => new JsonHighlightPalette(
            FieldName: new HighlightColor("#FBBF24"),
            String: new HighlightColor("#D9F99D"),
            Number: new HighlightColor("#FDBA74"),
            Bool: new HighlightColor("#F87171", Bold: true),
            Null: new HighlightColor("#A8A29E", Italic: true),
            Punctuation: new HighlightColor("#FDE68A"),
            Escape: new HighlightColor("#67E8F9", Bold: true)
        ),
        EJsonHighlightTheme.Iceberg => new JsonHighlightPalette(
            FieldName: new HighlightColor("#84A0C6"),
            String: new HighlightColor("#B4BE82"),
            Number: new HighlightColor("#E2A478"),
            Bool: new HighlightColor("#E27878", Bold: true),
            Null: new HighlightColor("#6B7089", Italic: true),
            Punctuation: new HighlightColor("#D2D4DE"),
            Escape: new HighlightColor("#89B8C2", Bold: true)
        ),
        _ => Get(EJsonHighlightTheme.Default)
    };
}
