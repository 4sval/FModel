using FModel.Creator.Icons;
using FModel.Creator.Rarities;
using FModel.Creator.Stats;
using FModel.Creator.Texts;
using FModel.Utils;
using PakReader.Parsers.Class;
using PakReader.Parsers.PropertyTagData;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Windows;

namespace FModel.Creator
{
    public class BaseIcon
    {
        public SKBitmap FallbackImage;
        public SKBitmap IconImage;
        public SKBitmap RarityBackgroundImage;
        public SKBitmap[] UserFacingFlags;
        public SKColor[] RarityBackgroundColors;
        public SKColor[] RarityBorderColor;
        public string DisplayName;
        public string Description;
        public string ShortDescription;
        public string CosmeticSource;
        public int Size = 512; // keep it 512 (or a multiple of 512) if you don't want blurry icons
        public int AdditionalSize = 0; // must be increased if there are weapon stats, hero abilities or more to draw/show
        public int Margin = 2;
        public List<Statistic> Stats;

        public BaseIcon()
        {
            FallbackImage = SKBitmap.Decode(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/T_Placeholder_Item_Image.png")).Stream);
            IconImage = FallbackImage;
            RarityBackgroundImage = null;
            UserFacingFlags = null;
            RarityBackgroundColors = new SKColor[2] { SKColor.Parse("5EBC36"), SKColor.Parse("305C15") };
            RarityBorderColor = new SKColor[2] { SKColor.Parse("74EF52"), SKColor.Parse("74EF52") };
            DisplayName = "";
            Description = "";
            ShortDescription = "";
            CosmeticSource = "";
            Stats = new List<Statistic>();
        }

        /// <summary>
        /// used to get low res icons ONLY
        /// </summary>
        /// <param name="export"></param>
        /// <param name="assetName"></param>
        public BaseIcon(IUExport export, string assetName) : this()
        {
            if (export.GetExport<ObjectProperty>("HeroDefinition", "WeaponDefinition") is ObjectProperty itemDef)
                LargeSmallImage.GetPreviewImage(this, itemDef, assetName, false);
            else if (export.GetExport<SoftObjectProperty>("SmallPreviewImage", "SmallImage") is SoftObjectProperty previewImage)
                LargeSmallImage.GetPreviewImage(this, previewImage);
        }

        /// <summary>
        /// Order:
        ///     1. Rarity
        ///     2. Image
        ///     3. Text
        ///         1. DisplayName
        ///         2. Description
        ///         3. Misc
        ///     4. GameplayTags
        ///         1. order doesn't matter
        ///         2. the importance here is to get the description before gameplay tags
        /// </summary>
        public BaseIcon(IUExport export, string exportType, string assetName) : this()
        {
            // rarity
            if (export.GetExport<ObjectProperty>("Series") is ObjectProperty series)
                Serie.GetRarity(this, series);
            else if (Properties.Settings.Default.UseGameColors) // override default green
                Rarity.GetInGameRarity(this, export.GetExport<EnumProperty>("Rarity")); // uncommon will be triggered by Rarity being null
            else if (export.GetExport<EnumProperty>("Rarity") is EnumProperty rarity)
                Rarity.GetHardCodedRarity(this, rarity);

            // image
            if (Properties.Settings.Default.UseItemShopIcon &&
                DisplayAssetImage.GetDisplayAssetImage(this, export.GetExport<SoftObjectProperty>("DisplayAssetPath"), assetName))
            { } // ^^^^ will return false if image not found, if so, we try to get the normal icon
            else if (export.GetExport<ObjectProperty>("HeroDefinition", "WeaponDefinition") is ObjectProperty itemDef)
                LargeSmallImage.GetPreviewImage(this, itemDef, assetName);
            else if (export.GetExport<SoftObjectProperty>("LargePreviewImage", "SmallPreviewImage", "ItemDisplayAsset") is SoftObjectProperty previewImage)
                LargeSmallImage.GetPreviewImage(this, previewImage);
            else if (export.GetExport<StructProperty>("IconBrush") is StructProperty iconBrush) // abilities
                LargeSmallImage.GetPreviewImage(this, iconBrush);

            // text
            if (export.GetExport<TextProperty>("DisplayName", "DefaultHeaderText", "UIDisplayName") is TextProperty displayName)
                DisplayName = Text.GetTextPropertyBase(displayName);
            if (export.GetExport<TextProperty>("Description", "DefaultBodyText") is TextProperty description)
                Description = Text.GetTextPropertyBase(description);
            else if (export.GetExport<ArrayProperty>("Description") is ArrayProperty arrayDescription) // abilities
                Description = Text.GetTextPropertyBase(arrayDescription);
            if (export.GetExport<StructProperty>("MaxStackSize") is StructProperty maxStackSize)
                ShortDescription = Text.GetMaxStackSize(maxStackSize);
            else if (export.GetExport<TextProperty>("ShortDescription") is TextProperty shortDescription)
                ShortDescription = Text.GetTextPropertyBase(shortDescription);
            else if (exportType.Equals("AthenaItemWrapDefinition")) // if no ShortDescription it's most likely a wrap
                ShortDescription = Localizations.GetLocalization("Fort.Cosmetics", "ItemWrapShortDescription", "Wrap");

            // gameplaytags
            if (export.GetExport<StructProperty>("GameplayTags") is StructProperty gameplayTags)
                GameplayTag.GetGameplayTags(this, gameplayTags, exportType);
            else if (export.GetExport<ObjectProperty>("cosmetic_item") is ObjectProperty cosmeticItem) // variants
                CosmeticSource = cosmeticItem.Value.Resource.ObjectName.String;

            if (export.GetExport<SoftObjectProperty>("AmmoData") is SoftObjectProperty ammoData)
                Statistics.GetAmmoData(this, ammoData);
            if (export.GetExport<StructProperty>("WeaponStatHandle") is StructProperty weaponStatHandle)
                Statistics.GetWeaponStats(this, weaponStatHandle);
            if (export.GetExport<ObjectProperty>("HeroGameplayDefinition") is ObjectProperty heroGameplayDefinition)
                Statistics.GetHeroStats(this, heroGameplayDefinition);

            /* Please do not add Schematics support because it takes way too much memory */
            /* Thank the STW Dev Team for using a 5,69Mb file to get... Oh nvm, they all left */

            AdditionalSize = 48 * Stats.Count;
        }
    }
}
