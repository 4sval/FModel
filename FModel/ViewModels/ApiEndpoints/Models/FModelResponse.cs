using System.Collections.Generic;
using System.Diagnostics;
using CUE4Parse.UE4.Versions;
using FModel.Creator;
using FModel.Extensions;
using SkiaSharp;
using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace FModel.ViewModels.ApiEndpoints.Models;

[DebuggerDisplay("{" + nameof(Messages) + "}")]
public class News
{
    [J] public string[] Messages { get; private set; }
    [J] public string[] Colors { get; private set; }
    [J] public string[] NewLines { get; private set; }
}

[DebuggerDisplay("{" + nameof(FileName) + "}")]
public class Backup
{
    [J] public string GameName { get; private set; }
    [J] public string FileName { get; private set; }
    [J] public string DownloadUrl { get; private set; }
    [J] public long FileSize { get; private set; }
}

[DebuggerDisplay("{" + nameof(DisplayName) + "}")]
public class Game
{
    [J] public string DisplayName { get; private set; }
    [J] public Dictionary<string, Version> Versions { get; private set; }
}

[DebuggerDisplay("{" + nameof(GameEnum) + "}")]
public class Version
{
    [J("game")] public string GameEnum { get; private set; }
    [J] public int UeVer { get; private set; }
    [J] public Dictionary<string, int> CustomVersions { get; private set; }
    [J] public Dictionary<string, bool> Options { get; private set; }
    [J] public Dictionary<string, KeyValuePair<string, string>> MapStructTypes { get; private set; } = new();
}

[DebuggerDisplay("{" + nameof(Mode) + "}")]
public class Info
{
    [J] public string Mode { get; private set; }
    [J] public string Version { get; private set; }
    [J] public string DownloadUrl { get; private set; }
    [J] public string ChangelogUrl { get; private set; }
    [J] public string CommunityDesign { get; private set; }
    [J] public string CommunityPreview { get; private set; }
}

[DebuggerDisplay("{" + nameof(Name) + "}")]
public class Community
{
    [J] public string Name { get; private set; }
    [J] public bool DrawSource { get; private set; }
    [J] public bool DrawSeason { get; private set; }
    [J] public bool DrawSeasonShort { get; private set; }
    [J] public bool DrawSet { get; private set; }
    [J] public bool DrawSetShort { get; private set; }
    [J] public IDictionary<string, Font> Fonts { get; private set; }
    [J] public GameplayTag GameplayTags { get; private set; }
    [J] public IDictionary<string, Rarity> Rarities { get; private set; }
}

public class Font
{
    [J] public IDictionary<string, string> Typeface { get; private set; }
    [J] public float FontSize { get; private set; }
    [J] public float FontScale { get; private set; }
    [J] public string FontColor { get; private set; }
    [J] public float SkewValue { get; private set; }
    [J] public byte ShadowValue { get; private set; }
    [J] public int MaxLineCount { get; private set; }
    [J] public string Alignment { get; private set; }
    [J] public int X { get; private set; }
    [J] public int Y { get; private set; }
}

public class FontDesign
{
    [J] public IDictionary<ELanguage, string> Typeface { get; set; }
    [J] public float FontSize { get; set; }
    [J] public float FontScale { get; set; }
    [J] public SKColor FontColor { get; set; }
    [J] public float SkewValue { get; set; }
    [J] public byte ShadowValue { get; set; }
    [J] public int MaxLineCount { get; set; }
    [J] public SKTextAlign Alignment { get; set; }
    [J] public int X { get; set; }
    [J] public int Y { get; set; }
}

public class GameplayTag
{
    [J] public int X { get; private set; }
    [J] public int Y { get; private set; }
    [J] public bool DrawCustomOnly { get; private set; }
    [J] public string Custom { get; private set; }
    [J] public IDictionary<string, string> Tags { get; private set; }
}

public class GameplayTagDesign
{
    [J] public int X { get; set; }
    [J] public int Y { get; set; }
    [J] public bool DrawCustomOnly { get; set; }
    [J] public SKBitmap Custom { get; set; }
    [J] public IDictionary<string, SKBitmap> Tags { get; set; }
}

public class Rarity
{
    [J] public string Background { get; private set; }
    [J] public string Upper { get; private set; }
    [J] public string Lower { get; private set; }
}

public class RarityDesign
{
    [J] public SKBitmap Background { get; set; }
    [J] public SKBitmap Upper { get; set; }
    [J] public SKBitmap Lower { get; set; }
}

public class CommunityDesign
{
    public bool DrawSource { get; }
    public bool DrawSeason { get; }
    public bool DrawSeasonShort { get; }
    public bool DrawSet { get; }
    public bool DrawSetShort { get; }
    public IDictionary<string, FontDesign> Fonts { get; }
    public GameplayTagDesign GameplayTags { get; }
    public IDictionary<string, RarityDesign> Rarities { get; }

    public CommunityDesign(Community response)
    {
        DrawSource = response.DrawSource;
        DrawSeason = response.DrawSeason;
        DrawSeasonShort = response.DrawSeasonShort;
        DrawSet = response.DrawSet;
        DrawSetShort = response.DrawSetShort;

        Fonts = new Dictionary<string, FontDesign>();
        foreach (var (k, font) in response.Fonts)
        {
            var typeface = new Dictionary<ELanguage, string>();
            foreach (var (key, value) in font.Typeface)
            {
                typeface[key.ToEnum(ELanguage.English)] = value;
            }

            Fonts[k] = new FontDesign
            {
                Typeface = typeface,
                FontSize = font.FontSize,
                FontScale = font.FontScale,
                FontColor = SKColor.Parse(font.FontColor),
                SkewValue = font.SkewValue,
                ShadowValue = font.ShadowValue,
                MaxLineCount = font.MaxLineCount,
                Alignment = font.Alignment.ToEnum(SKTextAlign.Center),
                X = font.X,
                Y = font.Y
            };
        }

        var tags = new Dictionary<string, SKBitmap>();
        foreach (var (key, value) in response.GameplayTags.Tags)
        {
            tags[key] = Utils.GetB64Bitmap(value);
        }

        GameplayTags = new GameplayTagDesign
        {
            X = response.GameplayTags.X,
            Y = response.GameplayTags.Y,
            DrawCustomOnly = response.GameplayTags.DrawCustomOnly,
            Custom = Utils.GetB64Bitmap(response.GameplayTags.Custom),
            Tags = tags
        };

        Rarities = new Dictionary<string, RarityDesign>();
        foreach (var (key, value) in response.Rarities)
        {
            Rarities[key] = new RarityDesign
            {
                Background = Utils.GetB64Bitmap(value.Background),
                Upper = Utils.GetB64Bitmap(value.Upper),
                Lower = Utils.GetB64Bitmap(value.Lower)
            };
        }
    }
}
