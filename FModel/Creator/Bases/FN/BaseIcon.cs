using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using CUE4Parse.GameTypes.FN.Enums;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse_Conversion.Textures;
using FModel.Settings;
using SkiaSharp;

namespace FModel.Creator.Bases.FN;

public class BaseIcon : UCreator
{
    public SKBitmap SeriesBackground { get; protected set; }
    protected string ShortDescription { get; set; }
    protected string CosmeticSource { get; set; }
    protected Dictionary<string, SKBitmap> UserFacingFlags { get; set; }

    public BaseIcon(UObject uObject, EIconStyle style) : base(uObject, style) { }

    public void ParseForReward(bool isUsingDisplayAsset)
    {
        // rarity
        if (Object.TryGetValue(out FPackageIndex series, "Series")) GetSeries(series);
        else if (Object.TryGetValue(out FStructFallback componentContainer, "ComponentContainer")) GetSeries(componentContainer);
        else GetRarity(Object.GetOrDefault("Rarity", EFortRarity.Uncommon)); // default is uncommon

        if (Object.TryGetValue(out FInstancedStruct[] dataList, "DataList"))
        {
            GetSeries(dataList);
            Preview = Utils.GetBitmap(dataList);
        }

        // preview
        if (Preview is null)
        {
            if (isUsingDisplayAsset && Utils.TryGetDisplayAsset(Object, out var preview))
                Preview = preview;
            else if (Object.TryGetValue(out FPackageIndex itemDefinition, "HeroDefinition", "WeaponDefinition"))
                Preview = Utils.GetBitmap(itemDefinition);
            else if (Object.TryGetValue(out FSoftObjectPath largePreview, "LargePreviewImage", "EntryListIcon", "SmallPreviewImage", "BundleImage", "ItemDisplayAsset", "LargeIcon", "ToastIcon", "SmallIcon"))
                Preview = Utils.GetBitmap(largePreview);
            else if (Object.TryGetValue(out string s, "LargePreviewImage") && !string.IsNullOrEmpty(s))
                Preview = Utils.GetBitmap(s);
            else if (Object.TryGetValue(out FPackageIndex otherPreview, "SmallPreviewImage", "ToastIcon", "access_item"))
                Preview = Utils.GetBitmap(otherPreview);
            else if (Object.TryGetValue(out UMaterialInstanceConstant materialInstancePreview, "EventCalloutImage"))
                Preview = Utils.GetBitmap(materialInstancePreview);
            else if (Object.TryGetValue(out FStructFallback brush, "IconBrush") && brush.TryGetValue(out UTexture2D res, "ResourceObject"))
                Preview = Utils.GetBitmap(res);
        }

        // text
        if (Object.TryGetValue(out FText displayName, "DisplayName", "ItemName", "BundleName", "DefaultHeaderText", "UIDisplayName", "EntryName", "EventCalloutTitle"))
            DisplayName = displayName.Text;
        if (Object.TryGetValue(out FText description, "Description", "ItemDescription", "SetDescription", "BundleDescription", "GeneralDescription", "DefaultBodyText", "UIDescription", "UIDisplayDescription", "EntryDescription", "EventCalloutDescription"))
            Description = description.Text;
        else if (Object.TryGetValue(out FText[] descriptions, "Description"))
            Description = string.Join('\n', descriptions.Select(x => x.Text));
        if (Object.TryGetValue(out FText shortDescription, "ShortDescription", "UIDisplaySubName"))
            ShortDescription = shortDescription.Text;
        else if (Object.ExportType.Equals("AthenaItemWrapDefinition", StringComparison.OrdinalIgnoreCase))
            ShortDescription = Utils.GetLocalizedResource("Fort.Cosmetics", "ItemWrapShortDescription", "Wrap");

        // Only works on non-cataba designs
        if (Object.TryGetValue(out FStructFallback eventArrowColor, "EventArrowColor") &&
            eventArrowColor.TryGetValue(out FLinearColor specifiedArrowColor, "SpecifiedColor") &&
            Object.TryGetValue(out FStructFallback eventArrowShadowColor, "EventArrowShadowColor") &&
            eventArrowShadowColor.TryGetValue(out FLinearColor specifiedShadowColor, "SpecifiedColor"))
        {
            Background = new[] { SKColor.Parse(specifiedArrowColor.Hex), SKColor.Parse(specifiedShadowColor.Hex) };
            Border = new[] { SKColor.Parse(specifiedShadowColor.Hex), SKColor.Parse(specifiedArrowColor.Hex) };
        }

        Description = Utils.RemoveHtmlTags(Description);
    }

    public override void ParseForInfo()
    {
        ParseForReward(UserSettings.Default.CosmeticDisplayAsset);

        if (Object.TryGetValue(out FInstancedStruct[] dataList, "DataList"))
            CheckGameplayTags(dataList);
        if (Object.TryGetValue(out FGameplayTagContainer gameplayTags, "GameplayTags"))
            CheckGameplayTags(gameplayTags);
        if (Object.TryGetValue(out FPackageIndex cosmeticItem, "cosmetic_item"))
            CosmeticSource = cosmeticItem.Name;
    }

    protected void Draw(SKCanvas c)
    {
        switch (Style)
        {
            case EIconStyle.NoBackground:
                DrawPreview(c);
                break;
            case EIconStyle.NoText:
                DrawBackground(c);
                DrawPreview(c);
                DrawUserFacingFlags(c);
                break;
            default:
                DrawBackground(c);
                DrawPreview(c);
                DrawTextBackground(c);
                DrawDisplayName(c);
                DrawDescription(c);
                DrawToBottom(c, SKTextAlign.Right, CosmeticSource);
                if (Description != ShortDescription)
                    DrawToBottom(c, SKTextAlign.Left, ShortDescription);
                DrawUserFacingFlags(c);
                break;
        }
    }

    public override SKBitmap[] Draw()
    {
        var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var c = new SKCanvas(ret);

        Draw(c);

        return new[] { ret };
    }

    private void GetSeries(FPackageIndex s)
    {
        if (!Utils.TryGetPackageIndexExport(s, out UObject export)) return;

        GetSeries(export);
    }

    private void GetSeries(FInstancedStruct[] s)
    {
        if (s.FirstOrDefault(d => d.NonConstStruct?.TryGetValue(out FPackageIndex _, "Series") == true) is { } dl)
            GetSeries(dl.NonConstStruct?.Get<FPackageIndex>("Series"));
    }

    private void GetSeries(FStructFallback s)
    {
        if (!s.TryGetValue(out FPackageIndex[] components, "Components")) return;
        if (components.FirstOrDefault(c => c.Name.Contains("Series")) is not { } seriesDef ||
            !seriesDef.TryLoad(out var seriesDefObj) || seriesDefObj is null ||
            !seriesDefObj.TryGetValue(out UObject series, "Series")) return;

        GetSeries(series);
    }

    protected void GetSeries(UObject uObject)
    {
        if (uObject is UTexture2D texture2D)
        {
            SeriesBackground = texture2D.Decode();
            return;
        }

        if (uObject.TryGetValue(out FSoftObjectPath backgroundTexture, "BackgroundTexture"))
        {
            SeriesBackground = Utils.GetBitmap(backgroundTexture);
        }

        if (uObject.TryGetValue(out FStructFallback colors, "Colors") &&
            colors.TryGetValue(out FLinearColor color1, "Color1") &&
            colors.TryGetValue(out FLinearColor color2, "Color2") &&
            colors.TryGetValue(out FLinearColor color3, "Color3"))
        {
            Background = new[] { SKColor.Parse(color1.Hex), SKColor.Parse(color3.Hex) };
            Border = new[] { SKColor.Parse(color2.Hex), SKColor.Parse(color1.Hex) };
        }

        if (uObject.Name.Equals("PlatformSeries") &&
            uObject.TryGetValue(out FSoftObjectPath itemCardMaterial, "ItemCardMaterial") &&
            Utils.TryLoadObject(itemCardMaterial.AssetPathName.Text, out UMaterialInstanceConstant material))
        {
            foreach (var vectorParameter in material.VectorParameterValues)
            {
                if (vectorParameter.ParameterValue == null || !vectorParameter.ParameterInfo.Name.Text.Equals("ColorCircuitBackground"))
                    continue;

                Background[0] = SKColor.Parse(vectorParameter.ParameterValue.Value.Hex);
            }
        }
    }

    private void GetRarity(EFortRarity r)
    {
        if (!Utils.TryLoadObject("FortniteGame/Content/Balance/RarityData.RarityData", out UObject export)) return;

        if (export.GetByIndex<FStructFallback>((int) r) is { } data &&
            data.TryGetValue(out FLinearColor color1, "Color1") &&
            data.TryGetValue(out FLinearColor color2, "Color2") &&
            data.TryGetValue(out FLinearColor color3, "Color3"))
        {
            Background = new[] { SKColor.Parse(color1.Hex), SKColor.Parse(color3.Hex) };
            Border = new[] { SKColor.Parse(color2.Hex), SKColor.Parse(color1.Hex) };
        }
    }

    protected string GetCosmeticSet(string setName)
    {
        if (!Utils.TryLoadObject("FortniteGame/Content/Athena/Items/Cosmetics/Metadata/CosmeticSets.CosmeticSets", out UDataTable cosmeticSets))
            return string.Empty;

        if (!cosmeticSets.TryGetDataTableRow(setName, StringComparison.OrdinalIgnoreCase, out var uObject))
            return string.Empty;

        var name = string.Empty;
        if (uObject.TryGetValue(out FText displayName, "DisplayName"))
            name = displayName.Text;

        var format = Utils.GetLocalizedResource("Fort.Cosmetics", "CosmeticItemDescription_SetMembership_NotRich", "\nPart of the {0} set.");
        return string.Format(format, name);
    }

    protected (int, int) GetInternalSID(int number)
    {
        static int GetSeasonsInChapter(int chapter) => chapter switch
        {
            1 => 10,
            2 => 8,
            3 => 4,
            4 => 5,
            5 => 5,
            _ => 5
        };

        var chapterIdx = 0;
        var seasonIdx = 0;
        while (number > 0)
        {
            var seasonsInChapter = GetSeasonsInChapter(++chapterIdx);
            if (number > seasonsInChapter)
                number -= seasonsInChapter;
            else
            {
                seasonIdx = number;
                number = 0;
            }
        }
        return (chapterIdx, seasonIdx);
    }

    protected string GetCosmeticSeason(string seasonNumber)
    {
        var s = seasonNumber["Cosmetics.Filter.Season.".Length..];
        var initial = int.Parse(s);
        (int chapterIdx, int seasonIdx) = GetInternalSID(initial);

        var season = Utils.GetLocalizedResource("AthenaSeasonItemDefinitionInternal", "SeasonTextFormat", "Season {0}");
        var introduced = Utils.GetLocalizedResource("Fort.Cosmetics", "CosmeticItemDescription_Season", "\nIntroduced in <SeasonText>{0}</>.");
        if (initial <= 10) return Utils.RemoveHtmlTags(string.Format(introduced, string.Format(season, s)));

        var chapter = Utils.GetLocalizedResource("AthenaSeasonItemDefinitionInternal", "ChapterTextFormat", "Chapter {0}");
        var chapterFormat = Utils.GetLocalizedResource("AthenaSeasonItemDefinitionInternal", "ChapterSeasonTextFormat", "{0}, {1}");
        var d = string.Format(chapterFormat, string.Format(chapter, chapterIdx), string.Format(season, seasonIdx));
        return Utils.RemoveHtmlTags(string.Format(introduced, d));
    }

    protected void CheckGameplayTags(FInstancedStruct[] dataList)
    {
        if (dataList.FirstOrDefault(d => d.NonConstStruct?.TryGetValue(out FGameplayTagContainer _, "Tags") ?? false) is { NonConstStruct: not null } tags)
        {
            CheckGameplayTags(tags.NonConstStruct.Get<FGameplayTagContainer>("Tags"));
        }
    }

    protected virtual void CheckGameplayTags(FGameplayTagContainer gameplayTags)
    {
        if (gameplayTags.TryGetGameplayTag("Cosmetics.Source.", out var source))
            CosmeticSource = source.Text["Cosmetics.Source.".Length..];
        else if (gameplayTags.TryGetGameplayTag("Athena.ItemAction.", out var action))
            CosmeticSource = action.Text["Athena.ItemAction.".Length..];

        if (gameplayTags.TryGetGameplayTag("Cosmetics.Set.", out var set))
            Description += GetCosmeticSet(set.Text);
        if (gameplayTags.TryGetGameplayTag("Cosmetics.Filter.Season.", out var season))
            Description += GetCosmeticSeason(season.Text);

        GetUserFacingFlags(gameplayTags.GetAllGameplayTags(
            "Cosmetics.UserFacingFlags.", "Homebase.Class.", "NPC.CharacterType.Survivor.Defender."));
    }

    protected void GetUserFacingFlags(IList<string> userFacingFlags)
    {
        if (userFacingFlags.Count < 1 || !Utils.TryLoadObject("FortniteGame/Content/Items/ItemCategories.ItemCategories", out UObject itemCategories))
            return;

        if (!itemCategories.TryGetValue(out FStructFallback[] tertiaryCategories, "TertiaryCategories"))
            return;

        UserFacingFlags = new Dictionary<string, SKBitmap>(userFacingFlags.Count);
        foreach (var flag in userFacingFlags)
        {
            if (flag.Equals("Cosmetics.UserFacingFlags.HasUpgradeQuests", StringComparison.OrdinalIgnoreCase))
            {
                if (Object.ExportType.Equals("AthenaPetCarrierItemDefinition", StringComparison.OrdinalIgnoreCase))
                    UserFacingFlags[flag] = SKBitmap.Decode(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/T-Icon-Pets-64.png"))?.Stream);
                else UserFacingFlags[flag] = SKBitmap.Decode(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/T-Icon-Quests-64.png"))?.Stream);
            }
            else
            {
                foreach (var category in tertiaryCategories)
                {
                    if (category.TryGetValue(out FGameplayTagContainer tagContainer, "TagContainer") && tagContainer.TryGetGameplayTag(flag, out _) &&
                        category.TryGetValue(out FStructFallback categoryBrush, "CategoryBrush") && categoryBrush.TryGetValue(out FStructFallback brushXxs, "Brush_XXS") &&
                        brushXxs.TryGetValue(out FPackageIndex resourceObject, "ResourceObject") && Utils.TryGetPackageIndexExport(resourceObject, out UTexture2D texture))
                    {
                        UserFacingFlags[flag] = Utils.GetBitmap(texture);
                    }
                }
            }
        }
    }

    private void DrawUserFacingFlags(SKCanvas c)
    {
        if (UserFacingFlags == null) return;

        const int size = 25;
        var x = Margin * (int) 2.5;
        foreach (var flag in UserFacingFlags.Values.Where(flag => flag != null))
        {
            c.DrawBitmap(flag.Resize(size), new SKPoint(x, Margin * (int) 2.5), ImagePaint);
            x += size;
        }
    }
}
