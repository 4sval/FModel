using FModel.Creator.Bundles;
using FModel.Creator.Icons;
using FModel.Creator.Rarities;
using FModel.Creator.Stats;
using FModel.Creator.Texts;
using FModel.ViewModels.ImageBox;
using PakReader.Parsers.Class;
using SkiaSharp;
using System.IO;

namespace FModel.Creator
{
    static class Creator
    {
        /// <summary>
        /// we draw based on the fist export type of the asset, no need to check others it's a waste of time
        /// i don't cache images because i don't wanna store a lot of SKCanvas in the memory
        /// </summary>
        /// <returns>true if an icon has been drawn</returns>
        public static bool TryDrawIcon(string assetPath, string exportType, IUExport export)
        {
            var d = new DirectoryInfo(assetPath);
            string assetName = d.Name;
            string assetFolder = d.Parent.Name;
            if (Text.TypeFaces.NeedReload(false))
                Text.TypeFaces = new Typefaces(); // when opening bundle creator settings without loading paks first

            // please respect my wave if you wanna add a new exportType
            // Athena first, then Fort, thank you
            switch (exportType)
            {
                case "AthenaConsumableEmoteItemDefinition":
                case "AthenaSkyDiveContrailItemDefinition":
                case "AthenaLoadingScreenItemDefinition":
                case "AthenaVictoryPoseItemDefinition":
                case "AthenaPetCarrierItemDefinition":
                case "AthenaMusicPackItemDefinition":
                case "AthenaBattleBusItemDefinition":
                case "AthenaCharacterItemDefinition":
                case "AthenaBackpackItemDefinition":
                case "AthenaPickaxeItemDefinition":
                case "AthenaGadgetItemDefinition":
                case "AthenaGliderItemDefinition":
                case "AthenaDailyQuestDefinition":
                case "AthenaSprayItemDefinition":
                case "AthenaDanceItemDefinition":
                case "AthenaEmojiItemDefinition":
                case "AthenaItemWrapDefinition":
                case "AthenaToyItemDefinition":
                case "FortHeroType":
                case "FortTokenType":
                case "FortAbilityKit":
                case "FortWorkerType":
                case "FortBannerTokenType":
                case "FortVariantTokenType":
                case "FortFeatItemDefinition":
                case "FortStatItemDefinition":
                case "FortTrapItemDefinition":
                case "FortAmmoItemDefinition":
                case "FortQuestItemDefinition":
                case "FortBadgeItemDefinition":
                case "FortAwardItemDefinition":
                case "FortGadgetItemDefinition":
                case "FortPlaysetItemDefinition":
                case "FortGiftBoxItemDefinition":
                case "FortSpyTechItemDefinition":
                case "FortAccoladeItemDefinition":
                case "FortCardPackItemDefinition":
                case "FortDefenderItemDefinition":
                case "FortCurrencyItemDefinition":
                case "FortResourceItemDefinition":
                case "FortSchematicItemDefinition":
                case "FortIngredientItemDefinition":
                case "FortWeaponMeleeItemDefinition":
                case "FortContextTrapItemDefinition":
                case "FortPlayerPerksItemDefinition":
                case "FortPlaysetPropItemDefinition":
                case "FortHomebaseNodeItemDefinition":
                case "FortWeaponRangedItemDefinition":
                case "FortNeverPersistItemDefinition":
                case "FortPlaysetGrenadeItemDefinition":
                case "FortPersonalVehicleItemDefinition":
                case "FortHardcoreModifierItemDefinition":
                case "FortConsumableAccountItemDefinition":
                case "FortConversionControlItemDefinition":
                case "FortPersistentResourceItemDefinition":
                case "FortCampaignHeroLoadoutItemDefinition":
                case "FortConditionalResourceItemDefinition":
                case "FortChallengeBundleScheduleDefinition":
                case "FortWeaponMeleeDualWieldItemDefinition":
                case "FortDailyRewardScheduleTokenDefinition":
                    {
                        BaseIcon icon = new BaseIcon(export, exportType, ref assetName);
                        int height = icon.Size + icon.AdditionalSize;
                        using (var ret = new SKBitmap(icon.Size, height, SKColorType.Rgba8888, SKAlphaType.Premul))
                        using (var c = new SKCanvas(ret))
                        {
                            if ((EIconDesign)Properties.Settings.Default.AssetsIconDesign != EIconDesign.NoBackground)
                            {
                                Rarity.DrawRarity(c, icon);
                            }

                            LargeSmallImage.DrawPreviewImage(c, icon);

                            if ((EIconDesign)Properties.Settings.Default.AssetsIconDesign != EIconDesign.NoBackground)
                            {
                                if ((EIconDesign)Properties.Settings.Default.AssetsIconDesign != EIconDesign.NoText)
                                {
                                    Text.DrawBackground(c, icon);
                                    Text.DrawDisplayName(c, icon);
                                    Text.DrawDescription(c, icon);
                                    if ((EIconDesign)Properties.Settings.Default.AssetsIconDesign != EIconDesign.Mini)
                                    {
                                        if (!icon.ShortDescription.Equals(icon.DisplayName) && !icon.ShortDescription.Equals(icon.Description))
                                            Text.DrawToBottom(c, icon, ETextSide.Left, icon.ShortDescription);
                                        Text.DrawToBottom(c, icon, ETextSide.Right, icon.CosmeticSource);
                                    }
                                }
                                UserFacingFlag.DrawUserFacingFlags(c, icon);

                                // has more things to show
                                if (height > icon.Size)
                                {
                                    Statistics.DrawStats(c, icon);
                                }
                            }

                            Watermark.DrawWatermark(c); // watermark should only be applied on icons with width = 512
                            ImageBoxVm.imageBoxViewModel.Set(ret, assetName);
                        }
                        return true;
                    }
                case "FortMtxOfferData":
                    {
                        BaseOffer icon = new BaseOffer(export);
                        using (var ret = new SKBitmap(icon.Size, icon.Size, SKColorType.Rgba8888, SKAlphaType.Premul))
                        using (var c = new SKCanvas(ret))
                        {
                            if ((EIconDesign)Properties.Settings.Default.AssetsIconDesign != EIconDesign.NoBackground)
                            {
                                icon.DrawBackground(c);
                            }
                            icon.DrawImage(c);

                            Watermark.DrawWatermark(c); // watermark should only be applied on icons with width = 512
                            ImageBoxVm.imageBoxViewModel.Set(ret, assetName);
                        }
                        return true;
                    }
                case "FortItemSeriesDefinition":
                    {
                        BaseIcon icon = new BaseIcon();
                        using (var ret = new SKBitmap(icon.Size, icon.Size, SKColorType.Rgba8888, SKAlphaType.Opaque))
                        using (var c = new SKCanvas(ret))
                        {
                            Serie.GetRarity(icon, export);
                            Rarity.DrawRarity(c, icon);

                            Watermark.DrawWatermark(c); // watermark should only be applied on icons with width = 512
                            ImageBoxVm.imageBoxViewModel.Set(ret, assetName);
                        }
                        return true;
                    }
                case "PlaylistUserOptionEnum":
                case "PlaylistUserOptionBool":
                case "PlaylistUserOptionString":
                case "PlaylistUserOptionIntEnum":
                case "PlaylistUserOptionIntRange":
                case "PlaylistUserOptionColorEnum":
                case "PlaylistUserOptionFloatEnum":
                case "PlaylistUserOptionFloatRange":
                case "PlaylistUserOptionPrimaryAsset":
                case "PlaylistUserOptionCollisionProfileEnum":
                    {
                        BaseUserOption icon = new BaseUserOption(export);
                        using (var ret = new SKBitmap(icon.Width, icon.Height, SKColorType.Rgba8888, SKAlphaType.Opaque))
                        using (var c = new SKCanvas(ret))
                        {
                            icon.Draw(c);

                            Watermark.DrawWatermark(c); // watermark should only be applied on icons with width = 512
                            ImageBoxVm.imageBoxViewModel.Set(ret, assetName);
                        }
                        return true;
                    }
                case "FortChallengeBundleItemDefinition":
                    {
                        BaseBundle icon = new BaseBundle(export, assetFolder);
                        using (var ret = new SKBitmap(icon.Width, icon.HeaderHeight + icon.AdditionalSize, SKColorType.Rgba8888, SKAlphaType.Opaque))
                        using (var c = new SKCanvas(ret))
                        {
                            HeaderStyle.DrawHeaderPaint(c, icon);
                            HeaderStyle.DrawHeaderText(c, icon);
                            QuestStyle.DrawQuests(c, icon);
                            QuestStyle.DrawCompletionRewards(c, icon);

                            ImageBoxVm.imageBoxViewModel.Set(ret, assetName);
                        }
                        return true;
                    }
            }
            return false;
        }
    }
}
