using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using SkiaSharp;

namespace FModel.Creator.Bases.MV;

public class BasePandaIcon : UCreator
{
    private float _y_offset;
    private ERewardRarity _rarity;
    private string _type;

    private readonly List<(SKBitmap, string)> _pictos;

    public BasePandaIcon(UObject uObject, EIconStyle style) : base(uObject, style)
    {
        Width = 1024;
        Margin = 30;
        DisplayNamePaint.TextSize = 50;
        DisplayNamePaint.TextAlign = SKTextAlign.Left;
        DisplayNamePaint.Color = SKColor.Parse("#191C33");
        DescriptionPaint.TextSize = 25;
        DefaultPreview = Utils.GetBitmap("/Game/Panda_Main/UI/PreMatch/Images/DiamondPortraits/0010_Random.0010_Random");

        _y_offset = Height / 2 + DescriptionPaint.TextSize;
        _pictos = new List<(SKBitmap, string)>();
    }

    public override void ParseForInfo()
    {
        _type = Object.ExportType;
        var t = _type switch
        {
            // "CharacterData" => ,
            "StatTrackingBundleData" => EItemType.Badge,
            "AnnouncerPackData" => EItemType.Announcer,
            "CharacterGiftData" => EItemType.ExperiencePoints,
            "ProfileIconData" => EItemType.ProfileIcon,
            "RingOutVfxData" => EItemType.Ringout,
            "BannerData" => EItemType.Banner,
            "EmoteData" => EItemType.Sticker,
            "TauntData" => EItemType.Emote,
            "SkinData" => EItemType.Variant,
            "PerkData" => EItemType.Perk,
            _ => EItemType.Unknown
        };

        _rarity = Object.GetOrDefault("Rarity", ERewardRarity.None);
        Background = GetRarityBackground(_rarity);

        if (Object.TryGetValue(out FSoftObjectPath rewardThumbnail, "RewardThumbnail", "DisplayTextureRef", "Texture"))
            Preview = Utils.GetBitmap(rewardThumbnail);
        else if (Object.TryGetValue(out FPackageIndex icon, "Icon"))
            Preview = Utils.GetBitmap(icon);

        if (Object.TryGetValue(out FText displayName, "DisplayName"))
            DisplayName = displayName.Text;
        if (Object.TryGetValue(out FText description, "Description"))
            Description = Utils.RemoveHtmlTags(description.Text);

        _pictos.Add((
            Utils.GetBitmap("/Game/Panda_Main/UI/Assets/Icons/ui_icons_unlocked.ui_icons_unlocked"),
            Utils.GetLocalizedResource(Object.GetOrDefault("UnlockLocation", EUnlockLocation.None))));
        if (Object.TryGetValue(out string slug, "Slug"))
        {
            t = _type switch
            {
                "HydraSyncedDataAsset" when slug == "gold" => EItemType.Gold,
                "HydraSyncedDataAsset" when slug == "match_toasts" => EItemType.Toast,
                _ => t
            };
            _pictos.Add((Utils.GetBitmap("/Game/Panda_Main/UI/Assets/Icons/ui_icons_link.ui_icons_link"), slug));
        }

        if (Object.TryGetValue(out int xpValue, "XPValue"))
            DisplayName += $" (+{xpValue})";

        if (Utils.TryLoadObject("/Game/Panda_Main/UI/Prototype/Foundation/Types/DT_EconomyGlossary.DT_EconomyGlossary", out UDataTable dataTable))
        {
            if (t != EItemType.Unknown &&
                dataTable.RowMap.ElementAt((int) t).Value.TryGetValue(out FText name, "Name_14_7F75AD6047CBDEA7B252B1BD76EF84B9"))
                _type = name.Text;
        }
    }

    public override SKBitmap[] Draw()
    {
        var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var c = new SKCanvas(ret);

        DrawBackground(c);
        DrawPreview(c);
        DrawDisplayName(c);
        DrawDescription(c);
        DrawPictos(c);

        return new[] { ret };
    }

    private SKColor[] GetRarityBackground(ERewardRarity rarity)
    {
        return rarity switch // the colors here are the base color and brighter color that the game uses for rarities from the "Rarity to Color" blueprint function
        {
            ERewardRarity.Common => new[]
            {
                SKColor.Parse(new FLinearColor(0.068478f, 0.651406f, 0.016807f, 1.000000f).Hex),
                SKColor.Parse(new FLinearColor(0.081422f, 1.000000f, 0.000000f, 1.000000f).Hex)
            },
            ERewardRarity.Rare => new[]
            {
                SKColor.Parse(new FLinearColor(0.035911f, 0.394246f, 0.900000f, 1.000000f).Hex),
                SKColor.Parse(new FLinearColor(0.033333f, 0.434207f, 1.000000f, 1.000000f).Hex)
            },
            ERewardRarity.Epic => new[]
            {
                SKColor.Parse(new FLinearColor(0.530391f, 0.060502f, 0.900000f, 1.000000f).Hex),
                SKColor.Parse(new FLinearColor(0.579907f, 0.045833f, 1.000000f, 1.000000f).Hex)
            },
            ERewardRarity.Legendary => new[]
            {
                SKColor.Parse(new FLinearColor(1.000000f, 0.223228f, 0.002428f, 1.000000f).Hex),
                SKColor.Parse(new FLinearColor(1.000000f, 0.479320f, 0.030713f, 1.000000f).Hex)
            },
            _ => new[]
            {
                SKColor.Parse(new FLinearColor(0.194618f, 0.651406f, 0.630757f, 1.000000f).Hex),
                SKColor.Parse(new FLinearColor(0.273627f, 0.955208f, 0.914839f, 1.000000f).Hex)
            }
        };
    }

    private new void DrawBackground(SKCanvas c)
    {
        c.DrawRect(new SKRect(0, 0, Width, Height),
            new SKPaint
            {
                IsAntialias = true, FilterQuality = SKFilterQuality.High, Color = SKColor.Parse("#F3FCF0")
            });

        var has_tr = _rarity != ERewardRarity.None;
        var tr = Utils.GetLocalizedResource(_rarity);
        var tr_paint = new SKPaint
        {
            IsAntialias = true,
            FilterQuality = SKFilterQuality.High,
            TextAlign = SKTextAlign.Right,
            TextSize = 35,
            Color = SKColors.White,
            Typeface = Utils.Typefaces.DisplayName
        };

        var path = new SKPath { FillType = SKPathFillType.EvenOdd };
        path.MoveTo(0, Height);
        path.LineTo(14, Height);
        path.LineTo(20, 20);
        if (has_tr)
        {
            const int margin = 15;
            var width = tr_paint.MeasureText(tr);
            path.LineTo(Width - width - margin * 2, 15);
            path.LineTo(Width - width - margin * 2.5f, 60);
            path.LineTo(Width, 55);
        }
        else
        {
            path.LineTo(Width, 14);
        }
        path.LineTo(Width, 0);
        path.LineTo(0, 0);
        path.LineTo(0, Height);
        path.Close();
        c.DrawPath(path, new SKPaint
        {
            IsAntialias = true,
            FilterQuality = SKFilterQuality.High,
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(Width / 2, Height), new SKPoint(Width, Height / 4), Background, SKShaderTileMode.Clamp)
        });

        if (has_tr)
        {
            var x = Width - 20f;
            foreach (var a in tr.Select(character => character.ToString()).Reverse())
            {
                c.DrawText(a, x, 40, tr_paint);
                x -= tr_paint.MeasureText(a) - 2;
            }
        }
    }

    private new void DrawPreview(SKCanvas c)
    {
        const int size = 384;
        var y = Height - size - Margin * 2;
        c.DrawBitmap(Preview ?? DefaultPreview, new SKRect(Margin, y, size + Margin, y + size), ImagePaint);
    }

    private new void DrawDisplayName(SKCanvas c)
    {
        if (string.IsNullOrWhiteSpace(DisplayName))
            return;

        var x = 450f;
        var y = Height / 2 - DisplayNamePaint.TextSize / 4;
        while (DisplayNamePaint.MeasureText(DisplayName) > Width - x)
        {
            DisplayNamePaint.TextSize -= 1;
        }

        foreach (var a in DisplayName.Select(character => character.ToString()))
        {
            c.DrawText(a, x, y, DisplayNamePaint);
            x += DisplayNamePaint.MeasureText(a) - 4;
        }
    }

    private new void DrawDescription(SKCanvas c)
    {
        const int x = 450;
        DescriptionPaint.Color = Background[0];
        c.DrawText(_type.ToUpper(), x, 170, DescriptionPaint);

        if (string.IsNullOrWhiteSpace(Description)) return;

        DescriptionPaint.Color = SKColor.Parse("#191C33");
        Utils.DrawMultilineText(c, Description, Width - x, Margin, SKTextAlign.Left,
            new SKRect(x, _y_offset, Width - Margin, Height - Margin), DescriptionPaint, out _y_offset);
    }

    private void DrawPictos(SKCanvas c)
    {
        if (_pictos.Count < 1) return;

        const float x = 450f;
        const int size = 24;
        var color = SKColor.Parse("#495B6E");
        var paint = new SKPaint
        {
            IsAntialias = true,
            FilterQuality = SKFilterQuality.High,
            TextSize = 27,
            Color = color,
            Typeface = Utils.Typefaces.Default
        };

        ImagePaint.ColorFilter = SKColorFilter.CreateBlendMode(color, SKBlendMode.SrcIn);

        foreach (var picto in _pictos)
        {
            c.DrawBitmap(picto.Item1, new SKRect(x, _y_offset + 10, x + size, _y_offset + 10 + size), ImagePaint);
            c.DrawText(picto.Item2, x + size + 10, _y_offset + size + 6, paint);
            _y_offset += size + 5;
        }
    }
}

public enum ERewardRarity : byte
{
    [Description("0D4B15CE4FB6F2BC5E5F5FAA9E8B376C")]
    None = 0, // Default

    [Description("0FCDEF47485E2C3D0D477988C481D8E3")]
    Common = 1,

    [Description("18241CA7441AE16AAFB6EFAB499FF981")]
    Rare = 2,

    [Description("D999D9CB4754D1078BF9A1B34A231005")]
    Epic = 3,

    [Description("705AE967407D6EF8870E988A08C6900E")]
    Legendary = 4
}

public enum EUnlockLocation : byte
{
    [Description("0D4B15CE4FB6F2BC5E5F5FAA9E8B376C")]
    None = 0, // Default

    [Description("0AFBCE5F41D930D6E9B5138C8EBCFE87")]
    Shop = 1,

    [Description("062F178B4EE74502C9AD9D878F3D7CEA")]
    AccountLevel = 2,

    [Description("1AE7A5DF477B2B5F4B3CCC8DCD732884")]
    CharacterMastery = 3,

    [Description("0B37731C49DC9AE1EAC566950C1A329D")]
    Battlepass = 4,

    [Description("16F160084187479E5D471786190AE5B7")]
    CharacterAffinity = 5,

    [Description("E5C1E35C406C585E83B5D18A817FA0B4")]
    GuildBoss = 6,

    [Description("4A89F5DD432113750EF52D8B58977DCE")]
    Tutorial = 7
}

public enum EItemType
{
    Unknown = -1,
    Announcer,
    Badge,
    Banner,
    BattlePassPoints,
    Emote,
    ExperiencePoints,
    Gleamium,
    Gold,
    MasteryLevel,
    Mission,
    Perk,
    PlayerLevel,
    ProfileIcon,
    Rested,
    Ringout,
    SignaturePerk,
    Sticker,
    Toast,
    Variant
}
