using System;
using System.Linq;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using SkiaSharp;

namespace FModel.Creator.Bases.MV;

public class BaseFighter : UCreator
{
    private float _xOffset = 1f;
    private float _yOffset = 1f;
    private float _zoom = 1f;

    private readonly SKBitmap _pattern;
    private readonly SKBitmap _perk;
    private readonly SKBitmap _emote;
    private readonly SKBitmap _skin;

    private (SKBitmap, List<string>) _fighterType;
    private readonly List<SKBitmap> _recommendedPerks;
    private readonly List<SKBitmap> _availableTaunts;
    private readonly List<SKBitmap> _skins;

    public BaseFighter(UObject uObject, EIconStyle style) : base(uObject, style)
    {
        Width = 1024;
        DisplayNamePaint.TextAlign = SKTextAlign.Left;
        DisplayNamePaint.TextSize = 100;
        DescriptionPaint.TextSize = 25;
        DefaultPreview = Utils.GetBitmap("MultiVersus/Content/Panda_Main/UI/PreMatch/Images/DiamondPortraits/0010_Random.0010_Random");

        _pattern = Utils.GetBitmap("MultiVersus/Content/Panda_Main/UI/Assets/UI_Textures/halftone_jagged.halftone_jagged");
        _perk = Utils.GetBitmap("MultiVersus/Content/Panda_Main/UI/Assets/Icons/ui_icons_perks.ui_icons_perks");
        _emote = Utils.GetBitmap("MultiVersus/Content/Panda_Main/UI/Assets/Icons/ui_icons_emote.ui_icons_emote");
        _skin = Utils.GetBitmap("MultiVersus/Content/Panda_Main/UI/Assets/Icons/ui_icons_skins.ui_icons_skins");
        _fighterType.Item2 = new List<string>();
        _recommendedPerks = new List<SKBitmap>();
        _availableTaunts = new List<SKBitmap>();
        _skins = new List<SKBitmap>();
    }

    public override void ParseForInfo()
    {
        if (Object.TryGetValue(out FLinearColor backgroundColor, "BackgroundColor"))
            Background = new[] { SKColor.Parse(backgroundColor.Hex) };

        if (Object.TryGetValue(out FSoftObjectPath portraitMaterial, "CollectionsPortraitMaterial") &&
            portraitMaterial.TryLoad(out UMaterialInstanceConstant portrait))
        {
            _xOffset = Math.Abs(portrait.ScalarParameterValues.FirstOrDefault(x => x.ParameterInfo.Name.Text == "XOffset")?.ParameterValue ?? 1f);
            _yOffset = Math.Abs(portrait.ScalarParameterValues.FirstOrDefault(x => x.ParameterInfo.Name.Text == "YOffset")?.ParameterValue / 10 ?? 1f);
            _zoom = Math.Clamp(portrait.ScalarParameterValues.FirstOrDefault(x => x.ParameterInfo.Name.Text == "Zoom")?.ParameterValue ?? 1f, 0, 1);
            Preview = Utils.GetBitmap(portrait);
        }
        else if (Object.TryGetValue(out FSoftObjectPath portraitTexture, "NewCharacterSelectPortraitTexture", "HUDPortraitTexture"))
            Preview = Utils.GetBitmap(portraitTexture);

        if (Object.TryGetValue(out FText displayName, "DisplayName"))
            DisplayName = displayName.Text;

        GetFighterClassInfo(Object.GetOrDefault("Class", EFighterClass.Support));
        _fighterType.Item2.Add(GetFighterType(Object.GetOrDefault("Type", EFighterType.Horizontal)));
        if (Object.TryGetValue(out FText property, "Property"))
            _fighterType.Item2.Add(property.Text);

        if (Object.TryGetValue(out UScriptSet recommendedPerks, "RecommendedPerkDatas")) // PORCO DIO WB USE ARRAYS!!!!!!
        {
            foreach (var recommendedPerk in recommendedPerks.Properties)
            {
                if (recommendedPerk.GenericValue is not FPackageIndex packageIndex ||
                    !Utils.TryGetPackageIndexExport(packageIndex, out UObject export) ||
                    !export.TryGetValue(out FSoftObjectPath rewardThumbnail, "RewardThumbnail"))
                    continue;

                _recommendedPerks.Add(Utils.GetBitmap(rewardThumbnail));
            }
        }

        if (Object.TryGetValue(out FSoftObjectPath[] availableTaunts, "AvailableTauntData"))
        {
            foreach (var taunt in availableTaunts)
            {
                if (!Utils.TryLoadObject(taunt.AssetPathName.Text, out UObject export) ||
                    !export.TryGetValue(out FSoftObjectPath rewardThumbnail, "RewardThumbnail"))
                    continue;

                _availableTaunts.Add(Utils.GetBitmap(rewardThumbnail));
            }
        }

        if (Object.TryGetValue(out FSoftObjectPath[] skins, "Skins"))
        {
            foreach (var skin in skins)
            {
                if (!Utils.TryLoadObject(skin.AssetPathName.Text, out UObject export) ||
                    !export.TryGetValue(out FSoftObjectPath rewardThumbnail, "RewardThumbnail"))
                    continue;

                _skins.Add(Utils.GetBitmap(rewardThumbnail));
            }
        }
    }

    public override SKBitmap[] Draw()
    {
        var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var c = new SKCanvas(ret);

        DrawBackground(c);
        DrawPreview(c);
        DrawDisplayName(c);
        DrawFighterInfo(c);
        DrawRecommendedPerks(c);
        DrawAvailableTaunts(c);
        DrawSkins(c);

        return new[] { ret };
    }

    private void GetFighterClassInfo(EFighterClass clas)
    {
        if (!Utils.TryLoadObject("MultiVersus/Content/Panda_Main/UI/In-Game/Data/UICharacterClassInfo_Datatable.UICharacterClassInfo_Datatable", out UDataTable dataTable))
            return;

        var row = dataTable.RowMap.ElementAt((int) clas).Value;
        if (!row.TryGetValue(out FText displayName, "DisplayName_5_9DB5DDFF490E1F4AD72329866F96B81D") ||
            !row.TryGetValue(out FPackageIndex icon, "Icon_8_711534AD4F240D4B001AA6A471EA1895"))
            return;

        _fighterType.Item1 = Utils.GetBitmap(icon);
        _fighterType.Item2.Add(displayName.Text);
    }

    private string GetFighterType(EFighterType typ)
    {
        return typ switch
        {
            EFighterType.Horizontal => Utils.GetLocalizedResource("", "97A60DD54AA23D4B93D5B891F729BF5C", "Horizontal"),
            EFighterType.Vertical => Utils.GetLocalizedResource("", "2C55443D47164019BE73A5ABDC670F36", "Vertical"),
            EFighterType.Hybrid => Utils.GetLocalizedResource("", "B980C82D40FF37FD359C74A339CE1B3A", "Hybrid"),
            _ => typ.ToString()
        };
    }

    private new void DrawBackground(SKCanvas c)
    {
        c.DrawRect(new SKRect(0, 0, Width, Height),
            new SKPaint
            {
                IsAntialias = true, FilterQuality = SKFilterQuality.High, Color = Background[0]
            });

        if (!string.IsNullOrWhiteSpace(DisplayName))
        {
            c.DrawText(DisplayName, -50, 125, new()
            {
                IsAntialias = true, FilterQuality = SKFilterQuality.High,
                Typeface = Utils.Typefaces.DisplayName, TextSize = 200,
                TextScaleX = .95f, TextSkewX = -0.25f, Color = SKColors.Black.WithAlpha(25)
            });
        }

        c.DrawBitmap(_pattern, new SKRect(0, 256, Width, 512), new SKPaint
        {
            IsAntialias = true, FilterQuality = SKFilterQuality.High, BlendMode = SKBlendMode.SoftLight
        });

        var path = new SKPath { FillType = SKPathFillType.EvenOdd };
        path.MoveTo(0, 512);
        path.LineTo(0, 492);
        path.LineTo(Width, 452);
        path.LineTo(Width, 512);
        path.Close();
        c.DrawPath(path, new SKPaint
        {
            IsAntialias = true,
            FilterQuality = SKFilterQuality.High,
            Color = SKColor.Parse("#141629")
        });
    }

    private new void DrawPreview(SKCanvas c)
    {
        var img = (Preview ?? DefaultPreview).ResizeWithRatio(_zoom);
        var x_offset = img.Width * _xOffset;
        var y_offset = img.Height * -_yOffset;
        c.DrawBitmap(img, new SKRect(Width + x_offset - img.Width, y_offset, Width + x_offset, img.Height + y_offset), ImagePaint);
    }

    private new void DrawDisplayName(SKCanvas c)
    {
        if (string.IsNullOrWhiteSpace(DisplayName)) return;
        c.DrawText(DisplayName.ToUpper(), 50, 100, DisplayNamePaint);
    }

    private void DrawFighterInfo(SKCanvas c)
    {
        if (_fighterType.Item1 != null)
            c.DrawBitmap(_fighterType.Item1, new SKRect(50, 112.5f, 98, 160.5f), ImagePaint);

        c.DrawText(string.Join(" | ", _fighterType.Item2), 98, 145, DescriptionPaint);
    }

    private void DrawRecommendedPerks(SKCanvas c)
    {
        const int x = 50;
        const int y = 200;
        const int size = 64;

        ImagePaint.ImageFilter = null;
        ImagePaint.BlendMode = SKBlendMode.SoftLight;
        c.DrawBitmap(_perk, new SKRect(x, y, x + size / 2, y + size / 2), ImagePaint);
        if (_recommendedPerks.Count < 1) return;

        ImagePaint.BlendMode = SKBlendMode.SrcOver;
        ImagePaint.ImageFilter = SKImageFilter.CreateDropShadow(0, 0, 2.5f, 2.5f, SKColors.Black);
        c.DrawBitmap(_recommendedPerks[1], new SKRect(161, y, 225, y + size), ImagePaint);
        c.DrawBitmap(_recommendedPerks[2], new SKRect(193, y + size / 2, 257, y + size * 1.5f), ImagePaint);
        c.DrawBitmap(_recommendedPerks[3], new SKRect(161, y + size, 225, y + size * 2), ImagePaint);

        ImagePaint.ImageFilter = SKImageFilter.CreateDropShadow(0, 0, 5, 5, SKColors.Black.WithAlpha(150));
        c.DrawBitmap(_recommendedPerks[0], new SKRect(x, y, x + size * 2, y + size * 2), ImagePaint);
    }

    private void DrawAvailableTaunts(SKCanvas c)
    {
        var x = 300;
        const int y = 232;
        const int size = 64;

        ImagePaint.ImageFilter = null;
        ImagePaint.BlendMode = SKBlendMode.SoftLight;
        c.DrawBitmap(_emote, new SKRect(x, y - size / 2, x + size / 2, y), ImagePaint);
        if (_availableTaunts.Count < 1) return;

        ImagePaint.BlendMode = SKBlendMode.SrcOver;
        ImagePaint.ImageFilter = SKImageFilter.CreateDropShadow(0, 0, 1.5f, 1.5f, SKColors.Black);

        foreach (var taunt in _availableTaunts)
        {
            c.DrawBitmap(taunt, new SKRect(x, y, x + size, y + size), ImagePaint);
            x += size;
        }
    }

    private void DrawSkins(SKCanvas c)
    {
        var x = 50;
        const int y = 333;
        const int size = 128;

        ImagePaint.ImageFilter = null;
        ImagePaint.BlendMode = SKBlendMode.SoftLight;
        c.DrawBitmap(_skin, new SKRect(x, y, x + size / 4, y + size / 4), ImagePaint);
        if (_skins.Count < 1) return;

        ImagePaint.BlendMode = SKBlendMode.SrcOver;
        ImagePaint.ImageFilter = SKImageFilter.CreateDropShadow(0, 0, 1.5f, 1.5f, SKColors.Black);
        foreach (var skin in _skins)
        {
            c.DrawBitmap(skin, new SKRect(x, y, x + size, y + size), ImagePaint);
            x += size;
        }
    }
}

public enum EFighterClass : byte
{
    Mage = 4,
    Tank = 3,
    Fighter = 2,
    Bruiser = 2,
    Assassin = 1,
    Support = 0 // Default
}

public enum EFighterType : byte
{
    Hybrid = 2,
    Vertical = 1,
    Horizontal = 0 // Default
}
