using FModel.Creator.Bases;
using FModel.Creator.Bundles;
using FModel.Creator.Icons;
using FModel.Creator.Rarities;
using FModel.Creator.Stats;
using FModel.Creator.Texts;
using FModel.ViewModels.ImageBox;
using SkiaSharp;
using System.IO;
using FModel.PakReader.Parsers.Class;
using FModel.PakReader.Parsers.Objects;

namespace FModel.Creator
{
    static class Creator
    {
        /// <summary>
        /// i don't cache images because i don't wanna store a lot of SKCanvas in the memory
        /// </summary>
        /// <returns>true if an icon has been drawn</returns>
        public static bool TryDrawIcon(string assetPath, FName[] exportTypes, IUExport[] exports)
        {
            var d = new DirectoryInfo(assetPath);
            string assetName = d.Name;
            string assetFolder = d.Parent.Name;
            if (Text.TypeFaces.NeedReload(false))
                Text.TypeFaces = new Typefaces(); // when opening bundle creator settings without loading paks first

            int index;
            {
                if (Globals.Game.ActualGame == EGame.Valorant || Globals.Game.ActualGame == EGame.Spellbreak)
                    index = 1;
                else
                    index = 0;
            }
            string exportType;
            {
                if (exportTypes.Length > index && (exportTypes[index].String == "BlueprintGeneratedClass" || exportTypes[index].String == "FortWeaponAdditionalData_AudioVisualizerData" || exportTypes[index].String == "FortWeaponAdditionalData_SingleWieldState"))
                    index++;

                exportType = exportTypes.Length > index ? exportTypes[index].String : string.Empty;
            }

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
                case "FortAlterationItemDefinition":
                case "AthenaBackpackItemDefinition":
                case "AthenaPickaxeItemDefinition":
                case "AthenaGadgetItemDefinition":
                case "AthenaGliderItemDefinition":
                case "AthenaDailyQuestDefinition":
                case "FortBackpackItemDefinition":
                case "AthenaSprayItemDefinition":
                case "AthenaDanceItemDefinition":
                case "AthenaEmojiItemDefinition":
                case "AthenaItemWrapDefinition":
                case "AthenaToyItemDefinition":
                case "FortHeroType":
                case "FortTokenType":
                case "FortAbilityKit":
                case "FortWorkerType":
                case "RewardGraphToken":
                case "FortBannerTokenType":
                case "FortVariantTokenType":
                case "FortDecoItemDefinition":
                case "FortFeatItemDefinition":
                case "FortStatItemDefinition":
                case "FortTrapItemDefinition":
                case "FortAmmoItemDefinition":
                case "FortTandemCharacterData":
                case "FortQuestItemDefinition":
                case "FortBadgeItemDefinition":
                case "FortAwardItemDefinition":
                case "FortGadgetItemDefinition":
                case "FortPlaysetItemDefinition":
                case "FortGiftBoxItemDefinition":
                case "FortSpyTechItemDefinition":
                case "FortOutpostItemDefinition":
                case "FortAccoladeItemDefinition":
                case "FortCardPackItemDefinition":
                case "FortDefenderItemDefinition":
                case "FortCurrencyItemDefinition":
                case "FortResourceItemDefinition":
                case "FortCodeTokenItemDefinition":
                case "FortSchematicItemDefinition":
                case "FortExpeditionItemDefinition":
                case "FortIngredientItemDefinition":
                case "FortAccountBuffItemDefinition":
                case "FortWeaponMeleeItemDefinition":
                case "FortContextTrapItemDefinition":
                case "FortPlayerPerksItemDefinition":
                case "FortPlaysetPropItemDefinition":
                case "FortHomebaseNodeItemDefinition":
                case "FortWeaponRangedItemDefinition":
                case "FortNeverPersistItemDefinition":
                case "RadioContentSourceItemDefinition":
                case "FortPlaysetGrenadeItemDefinition":
                case "FortPersonalVehicleItemDefinition":
                case "FortGameplayModifierItemDefinition":
                case "FortHardcoreModifierItemDefinition":
                case "FortConsumableAccountItemDefinition":
                case "FortConversionControlItemDefinition":
                case "FortAccountBuffCreditItemDefinition":
                case "FortEventCurrencyItemDefinitionRedir":
                case "FortPersistentResourceItemDefinition":
                case "FortHomebaseBannerIconItemDefinition":
                case "FortCampaignHeroLoadoutItemDefinition":
                case "FortConditionalResourceItemDefinition":
                case "FortChallengeBundleScheduleDefinition":
                case "FortWeaponMeleeDualWieldItemDefinition":
                case "FortDailyRewardScheduleTokenDefinition":
                case "FortCreativeRealEstatePlotItemDefinition":
                {
                    BaseIcon icon = new BaseIcon(exports[index], exportType, ref assetName);
                    int height = icon.Size + icon.AdditionalSize;
                    using (var ret = new SKBitmap(icon.Size, height, SKColorType.Rgba8888, SKAlphaType.Premul))
                    using (var c = new SKCanvas(ret))
                    {
                        if ((EIconDesign) Properties.Settings.Default.AssetsIconDesign != EIconDesign.NoBackground)
                        {
                            Rarity.DrawRarity(c, icon);
                        }

                        LargeSmallImage.DrawPreviewImage(c, icon);

                        if ((EIconDesign) Properties.Settings.Default.AssetsIconDesign != EIconDesign.NoBackground)
                        {
                            if ((EIconDesign) Properties.Settings.Default.AssetsIconDesign != EIconDesign.NoText)
                            {
                                Text.DrawBackground(c, icon);
                                Text.DrawDisplayName(c, icon);
                                Text.DrawDescription(c, icon);
                                if ((EIconDesign) Properties.Settings.Default.AssetsIconDesign != EIconDesign.Mini)
                                {
                                    if (!icon.ShortDescription.Equals(icon.DisplayName) &&
                                        !icon.ShortDescription.Equals(icon.Description))
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
                case "FortPlaylistAthena":
                {
                    BasePlaylist icon = new BasePlaylist(exports[index]);
                    using (var ret = new SKBitmap(icon.Width, icon.Height, SKColorType.Rgba8888, SKAlphaType.Premul))
                    using (var c = new SKCanvas(ret))
                    {
                        if ((EIconDesign) Properties.Settings.Default.AssetsIconDesign != EIconDesign.NoBackground)
                        {
                            Rarity.DrawRarity(c, icon);
                        }

                        LargeSmallImage.DrawNotStretchedPreviewImage(c, icon);

                        if ((EIconDesign) Properties.Settings.Default.AssetsIconDesign != EIconDesign.NoBackground)
                        {
                            if ((EIconDesign) Properties.Settings.Default.AssetsIconDesign != EIconDesign.NoText)
                            {
                                Text.DrawBackground(c, icon);
                                Text.DrawDisplayName(c, icon);
                                Text.DrawDescription(c, icon);
                            }
                        }

                        // Watermark.DrawWatermark(c); // boi why would you watermark something you don't own ¯\_(ツ)_/¯
                        ImageBoxVm.imageBoxViewModel.Set(ret, assetName);
                    }

                    return true;
                }
                case "AthenaSeasonItemDefinition":
                {
                    BaseSeason icon = new BaseSeason(exports[index], assetFolder);
                    using (var ret = new SKBitmap(icon.Width, icon.HeaderHeight + icon.AdditionalSize,
                        SKColorType.Rgba8888, SKAlphaType.Opaque))
                    using (var c = new SKCanvas(ret))
                    {
                        icon.Draw(c);

                        ImageBoxVm.imageBoxViewModel.Set(ret, assetName);
                    }

                    return true;
                }
                case "FortMtxOfferData":
                {
                    BaseOffer icon = new BaseOffer(exports[index]);
                    using (var ret = new SKBitmap(icon.Size, icon.Size, SKColorType.Rgba8888, SKAlphaType.Premul))
                    using (var c = new SKCanvas(ret))
                    {
                        if ((EIconDesign) Properties.Settings.Default.AssetsIconDesign != EIconDesign.NoBackground)
                        {
                            icon.DrawBackground(c);
                        }

                        icon.DrawImage(c);

                        Watermark.DrawWatermark(c); // watermark should only be applied on icons with width = 512
                        ImageBoxVm.imageBoxViewModel.Set(ret, assetName);
                    }

                    return true;
                }
                case "MaterialInstanceConstant":
                {
                    if (assetFolder.Equals("MI_OfferImages") || assetFolder.Equals("RenderSwitch_Materials"))
                    {
                        BaseOfferMaterial icon = new BaseOfferMaterial(exports[index]);
                        using (var ret = new SKBitmap(icon.Size, icon.Size, SKColorType.Rgba8888, SKAlphaType.Premul))
                        using (var c = new SKCanvas(ret))
                        {
                            if ((EIconDesign) Properties.Settings.Default.AssetsIconDesign != EIconDesign.NoBackground)
                            {
                                icon.DrawBackground(c);
                            }

                            icon.DrawImage(c);

                            Watermark.DrawWatermark(c); // watermark should only be applied on icons with width = 512
                            ImageBoxVm.imageBoxViewModel.Set(ret, assetName);
                        }

                        return true;
                    }

                    return false;
                }
                case "FortItemSeriesDefinition":
                {
                    BaseIcon icon = new BaseIcon();
                    using (var ret = new SKBitmap(icon.Size, icon.Size, SKColorType.Rgba8888, SKAlphaType.Opaque))
                    using (var c = new SKCanvas(ret))
                    {
                        Serie.GetRarity(icon, exports[index]);
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
                    BaseUserOption icon = new BaseUserOption(exports[index]);
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
                    BaseBundle icon = new BaseBundle(exports[index], assetFolder);
                    using (var ret = new SKBitmap(icon.Width, icon.HeaderHeight + icon.AdditionalSize,
                        SKColorType.Rgba8888, SKAlphaType.Opaque))
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
                case "FortItemAccessTokenType":
                {
                    BaseItemAccess icon = new BaseItemAccess(exports[index]);
                    using (var ret = new SKBitmap(icon.Size, icon.Size, SKColorType.Rgba8888, SKAlphaType.Opaque))
                    using (var c = new SKCanvas(ret))
                    {
                        icon.Draw(c);

                        Watermark.DrawWatermark(c); // watermark should only be applied on icons with width = 512
                        ImageBoxVm.imageBoxViewModel.Set(ret, assetName);
                    }

                    return true;
                }
                case "MapUIData":
                {
                    BaseMapUIData icon = new BaseMapUIData(exports[index]);
                    using (var ret = new SKBitmap(icon.Width, icon.Height, SKColorType.Rgba8888, SKAlphaType.Premul))
                    using (var c = new SKCanvas(ret))
                    {
                        icon.Draw(c);
                        ImageBoxVm.imageBoxViewModel.Set(ret, assetName);
                    }

                    return true;
                }
                case "ArmorUIData":
                case "SprayUIData":
                case "ThemeUIData":
                case "ContractUIData":
                case "CurrencyUIData":
                case "GameModeUIData":
                case "CharacterUIData":
                case "SprayLevelUIData":
                case "EquippableUIData":
                case "PlayerCardUIData":
                case "Gun_UIData_Base_C":
                case "CharacterRoleUIData":
                case "EquippableSkinUIData":
                case "EquippableCharmUIData":
                case "EquippableSkinLevelUIData":
                case "EquippableSkinChromaUIData":
                case "EquippableCharmLevelUIData":
                {
                    BaseUIData icon = new BaseUIData(exports, index);
                    using (var ret = new SKBitmap(icon.Width + icon.AdditionalWidth, icon.Height, SKColorType.Rgba8888,
                        SKAlphaType.Premul))
                    using (var c = new SKCanvas(ret))
                    {
                        icon.Draw(c);

                        Watermark.DrawWatermark(c); // watermark should only be applied on icons with width = 512
                        ImageBoxVm.imageBoxViewModel.Set(ret, assetName);
                    }

                    return true;
                }
                //case "StreamedVideoDataAsset": // must find a way to automatically gets the right version in the url
                //    {
                //        if (Globals.Game.ActualGame == EGame.Valorant && exports[index].GetExport<StructProperty>("Uuid") is StructProperty s && s.Value is FGuid uuid)
                //        {
                //            Process.Start(new ProcessStartInfo
                //            {
                //                FileName = string.Format(
                //                    "http://valorant.dyn.riotcdn.net/x/videos/release-01.05/{0}_default_universal.mp4",
                //                    $"{uuid.A:x8}-{uuid.B >> 16:x4}-{uuid.B & 0xFFFF:x4}-{uuid.C >> 16:x4}-{uuid.C & 0xFFFF:x4}{uuid.D:x8}"),
                //                UseShellExecute = true
                //            });
                //        }
                //        return false;
                //    }
                case "GQuest":
                case "GAccolade":
                case "GCosmeticSkin":
                case "GCharacterPerk":
                case "GCosmeticTitle":
                case "GCosmeticBadge":
                case "GCosmeticEmote":
                case "GCosmeticTriumph":
                case "GCosmeticRunTrail":
                case "GCosmeticArtifact":
                case "GCosmeticDropTrail":
                {
                    BaseGCosmetic icon = new BaseGCosmetic(exports[index], exportType);
                    using (var ret = new SKBitmap(icon.Width, icon.Height, SKColorType.Rgba8888, SKAlphaType.Premul))
                    using (var c = new SKCanvas(ret))
                    {
                        if ((EIconDesign) Properties.Settings.Default.AssetsIconDesign == EIconDesign.Flat)
                        {
                            icon.Draw(c);
                        }
                        else if ((EIconDesign) Properties.Settings.Default.AssetsIconDesign != EIconDesign.NoBackground)
                        { 
                            Rarity.DrawRarity(c, icon);
                        }

                        LargeSmallImage.DrawPreviewImage(c, icon);

                        if ((EIconDesign) Properties.Settings.Default.AssetsIconDesign != EIconDesign.NoBackground &&
                            (EIconDesign)Properties.Settings.Default.AssetsIconDesign != EIconDesign.NoText)
                        {
                            Text.DrawBackground(c, icon);
                            Text.DrawDisplayName(c, icon);
                            Text.DrawDescription(c, icon);
                        }

                        Watermark.DrawWatermark(c); // watermark should only be applied on icons with width = 512
                        ImageBoxVm.imageBoxViewModel.Set(ret, assetName);
                    }

                    return true;
                }
                case "GCosmeticCard":
                {
                    BaseGCosmetic icon = new BaseGCosmetic(exports[index], exportType);
                    using (var ret = new SKBitmap(icon.Width, icon.Height, SKColorType.Rgba8888, SKAlphaType.Premul))
                    using (var c = new SKCanvas(ret))
                    {
                        if ((EIconDesign) Properties.Settings.Default.AssetsIconDesign == EIconDesign.Flat)
                        {
                            icon.Draw(c);
                        }
                        else
                        {
                            if ((EIconDesign) Properties.Settings.Default.AssetsIconDesign != EIconDesign.NoBackground)
                            {
                                Rarity.DrawRarity(c, icon);
                            }
                        }

                        LargeSmallImage.DrawPreviewImage(c, icon);

                        if ((EIconDesign) Properties.Settings.Default.AssetsIconDesign != EIconDesign.NoBackground)
                        {
                            if ((EIconDesign) Properties.Settings.Default.AssetsIconDesign != EIconDesign.NoText)
                            {
                                Text.DrawBackground(c, icon);
                                Text.DrawDisplayName(c, icon);
                                Text.DrawDescription(c, icon);
                            }
                        }

                        ImageBoxVm.imageBoxViewModel.Set(ret, assetName);
                    }

                    return true;
                }
                // Battle Breakers
                case "WExpGenericAccountItemDefinition":
                {
                    BaseBBDefinition icon = new BaseBBDefinition(exports[index], exportType);
                    using (var ret = new SKBitmap(icon.Width, icon.Height, SKColorType.Rgba8888, SKAlphaType.Premul))
                    using (var c = new SKCanvas(ret))
                    {
                        if ((EIconDesign) Properties.Settings.Default.AssetsIconDesign != EIconDesign.NoBackground)
                        {
                            if (icon.RarityBackgroundImage != null)
                            {
                                c.DrawBitmap(icon.RarityBackgroundImage, new SKRect(icon.Margin, icon.Margin, icon.Width - icon.Margin, icon.Height - icon.Margin),
                                    new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true });
                            }
                            else
                            {
                                Rarity.DrawRarity(c, icon);
                            }
                        }

                        LargeSmallImage.DrawPreviewImage(c, icon);

                        if ((EIconDesign) Properties.Settings.Default.AssetsIconDesign != EIconDesign.NoBackground)
                        {
                            if ((EIconDesign) Properties.Settings.Default.AssetsIconDesign != EIconDesign.NoText)
                            {
                                Text.DrawBackground(c, icon);
                                Text.DrawDisplayName(c, icon);
                                Text.DrawDescription(c, icon);
                            }
                        }

                        Watermark.DrawWatermark(c); // watermark should only be applied on icons with width = 512
                        ImageBoxVm.imageBoxViewModel.Set(ret, assetName);
                    }

                    return true;
                }
            }

            return false;
        }
    }
}
