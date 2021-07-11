using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using CUE4Parse_Fortnite.Enums;
using FModel.Extensions;
using FModel.Framework;
using FModel.Services;
using FModel.Settings;
using FModel.ViewModels.ApiEndpoints.Models;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace FModel.Creator.Bases.FN
{
    public class BaseCommunity : BaseIcon
    {
        private readonly CommunityDesign _design;
        private string _rarityName;
        private string _source;
        private string _season;
        private bool _lowerDrawn;

        public BaseCommunity(UObject uObject, EIconStyle style, string designName) : base(uObject, style)
        {
            Margin = 0;
            _lowerDrawn = false;
            _design = ApplicationService.ApiEndpointView.FModelApi.GetDesign(designName);
        }

        public override void ParseForInfo()
        {
            ParseForReward(UserSettings.Default.CosmeticDisplayAsset == EEnabledDisabled.Enabled);

            if (Object.TryGetValue(out FPackageIndex series, "Series") && Utils.TryGetPackageIndexExport(series, out UObject export))
                _rarityName = export.Name;
            else
                _rarityName = GetRarityName(Object.GetOrDefault<FName>("Rarity"));

            if (Object.TryGetValue(out FGameplayTagContainer gameplayTags, "GameplayTags"))
                CheckGameplayTags(gameplayTags);
            if (Object.TryGetValue(out FPackageIndex cosmeticItem, "cosmetic_item"))
                CosmeticSource = cosmeticItem.Name.ToUpper();

            DisplayName = DisplayName.ToUpper();
            Description = Description.ToUpper();
        }

        public override SKImage Draw()
        {
            using var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var c = new SKCanvas(ret);

            if (_design == null)
            {
                base.Draw(c);
            }
            else
            {
                DrawBackground(c);
                DrawPreview(c);
                DrawTextBackground(c);
                DrawDisplayName(c);
                DrawDescription(c);
                if (_design.DrawSeason && _design.Fonts.TryGetValue("Season", out var font))
                    DrawToBottom(c, font, _season);
                if (_design.DrawSource && _design.Fonts.TryGetValue("Source", out font))
                    DrawToBottom(c, font, _source);
                DrawUserFacingFlags(c, _design.GameplayTags.DrawCustomOnly);
            }

            return SKImage.FromBitmap(ret);
        }

        private void CheckGameplayTags(FGameplayTagContainer gameplayTags)
        {
            if (_design == null) return;
            if (_design.DrawSource)
            {
                if (gameplayTags.TryGetGameplayTag("Cosmetics.Source.", out var source))
                    _source = source.Text["Cosmetics.Source.".Length..].ToUpper();
                else if (gameplayTags.TryGetGameplayTag("Athena.ItemAction.", out var action))
                    _source = action.Text["Athena.ItemAction.".Length..].ToUpper();
            }

            if (_design.DrawSet && gameplayTags.TryGetGameplayTag("Cosmetics.Set.", out var set))
                Description += GetCosmeticSet(set.Text, _design.DrawSetShort);
            if (_design.DrawSeason && gameplayTags.TryGetGameplayTag("Cosmetics.Filter.Season.", out var season))
                _season = GetCosmeticSeason(season.Text, _design.DrawSeasonShort);

            var triggers = _design.GameplayTags.DrawCustomOnly ? new[] {"Cosmetics.UserFacingFlags."} : new[] {"Cosmetics.UserFacingFlags.", "Homebase.Class.", "NPC.CharacterType.Survivor.Defender."};
            GetUserFacingFlags(gameplayTags.GetAllGameplayTags(triggers));
        }

        private string GetCosmeticSet(string setName, bool bShort)
        {
            return bShort ? setName["Cosmetics.Set.".Length..] : base.GetCosmeticSet(setName);
        }

        private string GetCosmeticSeason(string seasonNumber, bool bShort)
        {
            if (!bShort) return base.GetCosmeticSeason(seasonNumber);
            var s = seasonNumber["Cosmetics.Filter.Season.".Length..];
            var number = int.Parse(s);
            if (number == 10)
                s = "X";

            return number > 10 ? $"C{number / 10 + 1} S{s[^1..]}" : $"C1 S{s}";
        }

        private string GetRarityName(FName r)
        {
            var rarity = EFortRarity.Uncommon;
            switch (r.Text)
            {
                case "EFortRarity::Common":
                case "EFortRarity::Handmade":
                    rarity = EFortRarity.Common;
                    break;
                case "EFortRarity::Rare":
                case "EFortRarity::Sturdy":
                    rarity = EFortRarity.Rare;
                    break;
                case "EFortRarity::Epic":
                case "EFortRarity::Quality":
                    rarity = EFortRarity.Epic;
                    break;
                case "EFortRarity::Legendary":
                case "EFortRarity::Fine":
                    rarity = EFortRarity.Legendary;
                    break;
                case "EFortRarity::Mythic":
                case "EFortRarity::Elegant":
                    rarity = EFortRarity.Mythic;
                    break;
                case "EFortRarity::Transcendent":
                case "EFortRarity::Masterwork":
                    rarity = EFortRarity.Transcendent;
                    break;
                case "EFortRarity::Unattainable":
                case "EFortRarity::Badass":
                    rarity = EFortRarity.Unattainable;
                    break;
            }

            return rarity.GetDescription();
        }

        private new void DrawBackground(SKCanvas c)
        {
            if (_design.Rarities.TryGetValue(_rarityName, out var rarity))
            {
                c.DrawBitmap(rarity.Background, 0, 0, ImagePaint);
                c.DrawBitmap(rarity.Upper, 0, 0, ImagePaint);
            }
            else
            {
                base.DrawBackground(c);
            }
        }

        private new void DrawTextBackground(SKCanvas c)
        {
            if (!_lowerDrawn && string.IsNullOrEmpty(DisplayName) && string.IsNullOrEmpty(Description)) return;

            _lowerDrawn = true;
            if (_design.Rarities.TryGetValue(_rarityName, out var rarity))
            {
                c.DrawBitmap(rarity.Lower, 0, 0, ImagePaint);
            }
            else
            {
                base.DrawTextBackground(c);
            }
        }

        private new void DrawDisplayName(SKCanvas c)
        {
            if (string.IsNullOrEmpty(DisplayName)) return;
            if (_design.Fonts.TryGetValue(nameof(DisplayName), out var font))
            {
                DisplayNamePaint.TextSize = font.FontSize;
                DisplayNamePaint.TextScaleX = font.FontScale;
                DisplayNamePaint.Color = font.FontColor;
                DisplayNamePaint.TextSkewX = font.SkewValue;
                DisplayNamePaint.TextAlign = font.Alignment;
                if (font.ShadowValue > 0)
                    DisplayNamePaint.ImageFilter = SKImageFilter.CreateDropShadow(2, 2, 4, 4, new SKColor(0, 0, 0, font.ShadowValue));
                if (font.Typeface.TryGetValue(UserSettings.Default.AssetLanguage, out var path) ||
                    font.Typeface.TryGetValue(ELanguage.English, out path))
                    DisplayNamePaint.Typeface = Utils.Typefaces.OnTheFly(path);

                while (DisplayNamePaint.MeasureText(DisplayName) > Width - Margin * 2)
                {
                    DisplayNamePaint.TextSize -= 1;
                }

                var shaper = new CustomSKShaper(DisplayNamePaint.Typeface);
                var shapedText = shaper.Shape(DisplayName, DisplayNamePaint);
                var x = font.Alignment switch
                {
                    SKTextAlign.Center => (Width - shapedText.Points[^1].X) / 2f,
                    _ => font.X
                };

                c.DrawShapedText(shaper, DisplayName, x, font.Y, DisplayNamePaint);
            }
            else
            {
                base.DrawDisplayName(c);
            }
        }

        private new void DrawDescription(SKCanvas c)
        {
            if (string.IsNullOrEmpty(Description)) return;
            if (_design.Fonts.TryGetValue(nameof(Description), out var font))
            {
                DescriptionPaint.TextSize = font.FontSize;
                DescriptionPaint.TextScaleX = font.FontScale;
                DescriptionPaint.Color = font.FontColor;
                DescriptionPaint.TextSkewX = font.SkewValue;
                DescriptionPaint.TextAlign = font.Alignment;
                if (font.ShadowValue > 0)
                    DescriptionPaint.ImageFilter = SKImageFilter.CreateDropShadow(2, 2, 4, 4, new SKColor(0, 0, 0, font.ShadowValue));
                if (font.Typeface.TryGetValue(UserSettings.Default.AssetLanguage, out var path) ||
                    font.Typeface.TryGetValue(ELanguage.English, out path))
                    DescriptionPaint.Typeface = Utils.Typefaces.OnTheFly(path);

                while (DescriptionPaint.MeasureText(Description) > Width - Margin * 2)
                {
                    DescriptionPaint.TextSize -= 1;
                }

                var shaper = new CustomSKShaper(DescriptionPaint.Typeface);
                var shapedText = shaper.Shape(Description, DescriptionPaint);
                var x = font.Alignment switch
                {
                    SKTextAlign.Center => (Width - shapedText.Points[^1].X) / 2f,
                    _ => font.X
                };

                if (font.MaxLineCount < 2)
                {
                    c.DrawShapedText(shaper, Description, x, font.Y, DescriptionPaint);
                }
                else
                {
                    Utils.DrawMultilineText(c, Description, Width - Margin * 2, Margin, DescriptionPaint.TextAlign,
                        new SKRect(Margin, font.Y, Width - Margin, Height), DescriptionPaint, out _);
                }
            }
            else
            {
                base.DrawDescription(c);
            }
        }

        private void DrawToBottom(SKCanvas c, FontDesign font, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (!_lowerDrawn)
            {
                _lowerDrawn = true;
                DrawTextBackground(c);
            }

            DisplayNamePaint.TextSize = font.FontSize;
            DisplayNamePaint.TextScaleX = font.FontScale;
            DisplayNamePaint.Color = font.FontColor;
            DisplayNamePaint.TextSkewX = font.SkewValue;
            DisplayNamePaint.TextAlign = font.Alignment;
            if (font.ShadowValue > 0)
                DisplayNamePaint.ImageFilter = SKImageFilter.CreateDropShadow(2, 2, 4, 4, new SKColor(0, 0, 0, font.ShadowValue));
            if (font.Typeface.TryGetValue(UserSettings.Default.AssetLanguage, out var path) ||
                font.Typeface.TryGetValue(ELanguage.English, out path))
                DisplayNamePaint.Typeface = Utils.Typefaces.OnTheFly(path);

            var shaper = new CustomSKShaper(DisplayNamePaint.Typeface);
            var shapedText = shaper.Shape(text, DisplayNamePaint);
            var x = font.Alignment switch
            {
                SKTextAlign.Center => (Width - shapedText.Points[^1].X) / 2f,
                SKTextAlign.Right => font.X - DisplayNamePaint.MeasureText(text),
                _ => font.X
            };

            c.DrawShapedText(shaper, text, x, font.Y, DisplayNamePaint);
        }

        private void DrawUserFacingFlags(SKCanvas c, bool customOnly)
        {
            if (UserFacingFlags == null || UserFacingFlags.Count < 1) return;
            if (customOnly)
            {
                c.DrawBitmap(_design.GameplayTags.Custom, 0, 0, ImagePaint);
            }
            else
            {
                // add size to api
                // draw
            }
        }
    }
}