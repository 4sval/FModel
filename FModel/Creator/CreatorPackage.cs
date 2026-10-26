using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets.Exports;
using FModel.Creator.Bases;
using FModel.Creator.Bases.FN;
using FModel.Creator.Bases.MV;

namespace FModel.Creator;

public class CreatorPackage : IDisposable
{
    private string _pkgName;
    private string _exportType;
    private Lazy<UObject> _object;
    private EIconStyle _style;

    public CreatorPackage(string packageName, string exportType, Lazy<UObject> uObject, EIconStyle style)
    {
        _pkgName = packageName;
        _exportType = exportType;
        _object = uObject;
        _style = style;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UCreator ConstructCreator()
    {
        TryConstructCreator(out var creator);
        return creator;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryConstructCreator([MaybeNullWhen(false)] out UCreator creator)
    {
        // TODO: convert to a type based system
        switch (_exportType)
        {
            // Fortnite
            case "FortHackData":
            case "FortHeroType":
            case "FortTokenType":
            case "FortAbilityKit":
            case "FortWorkerType":
            case "FortMissionInfo":
            case "RewardGraphToken":
            case "FortBannerTokenType":
            case "FortHackDataMinimal":
            case "FortHackKeyCoreData":
            case "JunoKnowledgeBundle":
            case "FortVariantTokenType":
            case "FortAmmoItemDefinition":
            case "FortDecoItemDefinition":
            case "FortStatItemDefinition":
            case "AthenaHatItemDefinition":
            case "AthenaToyItemDefinition":
            case "FortAwardItemDefinition":
            case "FortBadgeItemDefinition":
            case "FortEmoteItemDefinition":
            case "FortStackItemDefinition":
            case "FortWorldItemDefinition":
            case "SparksMicItemDefinition":
            case "AthenaItemWrapDefinition":
            case "FortGadgetItemDefinition":
            case "SparksAuraItemDefinition":
            case "SparksBassItemDefinition":
            case "SparksDrumItemDefinition":
            case "AthenaCharmItemDefinition":
            case "AthenaDanceItemDefinition":
            case "AthenaEmojiItemDefinition":
            case "AthenaSprayItemDefinition":
            case "ExtractableItemDefinition":
            case "FortAccountItemDefinition":
            case "FortGiftBoxItemDefinition":
            case "FortHackKeyItemDefinition":
            case "FortMissionItemDefinition":
            case "FortOutpostItemDefinition":
            case "FortPlaysetItemDefinition":
            case "FortVehicleItemDefinition":
            case "AthenaGadgetItemDefinition":
            case "AthenaGliderItemDefinition":
            case "FortBackpackItemDefinition":
            case "FortBuildingItemDefinition":
            case "FortCardPackItemDefinition":
            case "FortCurrencyItemDefinition":
            case "FortDefenderItemDefinition":
            case "FortEventQuestMapDataAsset":
            case "FortResourceItemDefinition":
            case "SparksGuitarItemDefinition":
            case "AthenaPickaxeItemDefinition":
            case "CosmeticShoesItemDefinition":
            case "FortAlterableItemDefinition":
            case "FortCodeTokenItemDefinition":
            case "FortItemCacheItemDefinition":
            case "FortSchematicItemDefinition":
            case "FortWeaponModItemDefinition":
            case "SproutHousingItemDefinition":
            case "AthenaBackpackItemDefinition":
            case "FortAlterationItemDefinition":
            case "FortConsumableItemDefinition":
            case "FortExpeditionItemDefinition":
            case "FortIngredientItemDefinition":
            case "FortWorldMultiItemDefinition":
            case "MyTownBuildingDefinitionData":
            case "PiggybackDanceItemDefinition":
            case "SparksKeyboardItemDefinition":
            case "SproutBuildingItemDefinition":
            case "AthenaBattleBusItemDefinition":
            case "AthenaCharacterItemDefinition":
            case "AthenaMapMarkerItemDefinition":
            case "AthenaMusicPackItemDefinition":
            case "BattleLabDeviceItemDefinition":
            case "FortAccountBuffItemDefinition":
            case "FortFOBCoreDecoItemDefinition":
            case "FortPlayerPerksItemDefinition":
            case "FortPlaysetPropItemDefinition":
            case "FortPrerollDataItemDefinition":
            case "StWFortAccoladeItemDefinition":
            case "AthenaPetCarrierItemDefinition":
            case "FortHomebaseNodeItemDefinition":
            case "FortNeverPersistItemDefinition":
            case "JunoRecipeBundleItemDefinition":
            case "AthenaVictoryPoseItemDefinition":
            case "CosmeticCompanionItemDefinition":
            case "FortDeferredItemGrantDefinition":
            case "FortGiftBoxUnlockItemDefinition":
            case "FortHackCartridgeItemDefinition":
            case "FortPlayerAugmentItemDefinition":
            case "FortSmartBuildingItemDefinition":
            case "FortCreativeGadgetItemDefinition":
            case "FortEventDependentItemDefinition":
            case "FortPlaysetGrenadeItemDefinition":
            case "FortWeaponModItemDefinitionOptic":
            case "JunoWeaponCreatureItemDefinition":
            case "RadioContentSourceItemDefinition":
            case "AthenaLoadingScreenItemDefinition":
            case "FortPersonalVehicleItemDefinition":
            case "MagpieEntitlementRewardDefinition":
            case "FortGameplayModifierItemDefinition":
            case "FortHardcoreModifierItemDefinition":
            case "AthenaConsumableEmoteItemDefinition":
            case "AthenaSkyDiveContrailItemDefinition":
            case "FortAccountBuffCreditItemDefinition":
            case "FortCharacterCosmeticItemDefinition":
            case "FortConsumableAccountItemDefinition":
            case "FortConversionControlItemDefinition":
            case "FortWeaponModItemDefinitionMagazine":
            case "JunoBuildInstructionsItemDefinition":
            case "FortEventCurrencyItemDefinitionRedir":
            case "FortHomebaseBannerIconItemDefinition":
            case "FortPersistentResourceItemDefinition":
            case "FortVehicleCosmeticsVariantTokenType":
            case "FortWeaponMeleeOffhandItemDefinition":
            case "JunoBuildingSetAccountItemDefinition":
            case "FortCampaignHeroLoadoutItemDefinition":
            case "FortChallengeBundleScheduleDefinition":
            case "FortConditionalResourceItemDefinition":
            case "BattleLabDeviceAccountItemDefinition":
            case "JunoBuildingPropAccountItemDefinition":
            case "CosmeticCompanionReactFXItemDefinition":
            case "FortDailyRewardScheduleTokenDefinition":
            case "FortVehicleCosmeticsItemDefinition_Body":
            case "FortVehicleCosmeticsItemDefinition_Skin":
            case "FortCreativeRealEstatePlotItemDefinition":
            case "FortVehicleCosmeticsItemDefinition_Wheel":
            case "FortDeployableBaseCloudSaveItemDefinition":
            case "FortVehicleCosmeticsItemDefinition_Booster":
            case "AthenaDanceItemDefinition_AdHocSquadsJoin_C":
            case "FortVehicleCosmeticsItemDefinition_DriftSmoke":
            case "FortVehicleCosmeticsItemDefinition_EngineAudio":
                creator = _style switch
                {
                    EIconStyle.Cataba => new BaseCommunity(_object.Value, _style, "Cataba"),
                    _ => new BaseIcon(_object.Value, _style)
                };
                return true;
            case "JunoAthenaCharacterItemOverrideDefinition":
            case "JunoAthenaDanceItemOverrideDefinition":
                creator = new BaseJuno(_object.Value, _style);
                return true;
            case "AssembledMeshSchema":
                creator = new BaseAssembledMesh(_object.Value, _style);
                return true;
            case "FortTandemCharacterData":
                creator = new BaseTandem(_object.Value, _style);
                return true;
            case "FortTrapItemDefinition":
            case "FortSpyTechItemDefinition":
            case "FortAccoladeItemDefinition":
            case "FortContextTrapItemDefinition":
            case "FortWeaponMeleeItemDefinition":
            case "FortWeaponRangedItemDefinition":
            case "FortCreativeWeaponMeleeItemDefinition":
            case "FortWeaponMeleeDualWieldItemDefinition":
            case "FortCreativeWeaponRangedItemDefinition":
            case "Daybreak_LevelExitVehicle_PartItemDefinition_C":
                creator = new BaseIconStats(_object.Value, _style);
                return true;
            case "FortItemSeriesDefinition":
                creator = new BaseSeries(_object.Value, _style);
                return true;
            case "MaterialInstanceConstant"
                when _pkgName.Contains("/MI_OfferImages/", StringComparison.OrdinalIgnoreCase) ||
                     _pkgName.Contains("/RenderSwitch_Materials/", StringComparison.OrdinalIgnoreCase) ||
                     _pkgName.Contains("/MI_BPTile/", StringComparison.OrdinalIgnoreCase):
                creator = new BaseMaterialInstance(_object.Value, _style);
                return true;
            case "AthenaItemShopOfferDisplayData":
                creator = new BaseOfferDisplayData(_object.Value, _style);
                return true;
            case "FortMtxOfferData":
                creator = new BaseMtxOffer(_object.Value, _style);
                return true;
            case "FortPlaylistAthena":
                creator = new BasePlaylist(_object.Value, _style);
                return true;
            case "FortFeatItemDefinition":
            case "FortQuestItemDefinition":
            case "FortQuestItemDefinition_Athena":
            case "FortQuestItemDefinition_Campaign":
            case "AthenaDailyQuestDefinition":
            case "FortUrgentQuestItemDefinition":
                creator = new Bases.FN.BaseQuest(_object.Value, _style);
                return true;
            case "FortCompendiumItemDefinition":
            case "FortCompendiumBundleDefinition":
            case "FortChallengeBundleItemDefinition":
                creator = new BaseBundle(_object.Value, _style);
                return true;
            // case "AthenaSeasonItemDefinition":
            //     creator = new BaseSeason(_object, _style);
            //     return true;
            case "FortItemAccessTokenType":
                creator = new BaseItemAccessToken(_object.Value, _style);
                return true;
            case "FortCreativeOption":
            case "PlaylistUserOptionEnum":
            case "PlaylistUserOptionBool":
            case "PlaylistUserOptionString":
            case "PlaylistUserOptionIntEnum":
            case "PlaylistUserOptionIntRange":
            case "PlaylistUserOptionColorEnum":
            case "PlaylistUserOptionFloatEnum":
            case "PlaylistUserOptionFloatRange":
            case "PlaylistUserTintedIconIntEnum":
            case "PlaylistUserOptionPrimaryAsset":
            case "PlaylistUserOptionCollisionProfileEnum":
                creator = new BaseUserControl(_object.Value, _style);
                return true;
            // PandaGame
            case "CharacterData":
                creator = new BaseFighter(_object.Value, _style);
                return true;
            case "PerkGroup":
                creator = new BasePerkGroup(_object.Value, _style);
                return true;
            case "StatTrackingBundleData":
            case "HydraSyncedDataAsset":
            case "AnnouncerPackData":
            case "CharacterGiftData":
            case "ProfileIconData":
            case "RingOutVfxData":
            case "BannerData":
            case "EmoteData":
            case "TauntData":
            case "SkinData":
            case "PerkData":
                creator = new BasePandaIcon(_object.Value, _style);
                return true;
            case "QuestData":
                creator = new Bases.MV.BaseQuest(_object.Value, _style);
                return true;
            default:
                creator = null;
                return false;
                            // Spyjinx
            case "OcnPlaylist":
            case "OcnItemReward":
            case "OcnMtxOfferData":
            case "OcnSetDefinition":
            case "OcnElimDefinition":
            case "OcnBombDefinition":
            case "OcnLogoDefinition":
            case "OcnRoomDefinition":
            case "OcnRaceDefinition":
            case "OcnEmoteDefinition":
            case "OcnTokenDefinition":
            case "OcnDailyLeadQuests":
            case "OcnGadgetDefinition":
            case "OcnGenderDefinition":
            case "OcnDailyLeadRewards":
            case "OcnMissionDefinition":
            case "OcnDefenseDefinition":
            case "OcnHideoutDefinition":
            case "OcnArtifactDefinition":
            case "OcnCampaignDefinition":
            case "OcnCoolDownDefinition":
            case "OcnCurrencyDefinition":
            case "OcnStatPerkDefinition":
            case "OcnSwapPerkDefinition":
            case "OcnOperativeDefinition":
            case "OcnConcealedDefinition":
            case "OcnQuestListDefinition":
            case "OcnNpcAgencyDefinition":
            case "OcnDailyGoalDefinition":
            case "OcnDecorCustomizerData":
            case "OcnNpcHideoutDefinition":
            case "OcnContentPackDefinition":
            case "OcnArtifactPerkDefinition":
            case "OcnRelationshipDefinition":
            case "OcnNpcOperativeDefinition":
            case "OcnMastermindPerkDefinition":
            case "OcnMastermindChairDefinition":
            case "OcnOperativeGadgetDefinition":
            case "OcnScriptedTournamentDefinition":
                creator = new BaseSpyjinxIcon(_object.Value, _style);
                return true;
            // Battle Breakers
            case "WExpHelpData":
            case "WExpGearAffix":
            case "WExpItemDefinition":
            case "WExpZoneDefinition":
            case "WExpEventDefinition":
            case "WExpTokenDefinition":
            case "WExpHQMineDefinition":
            case "WExpRegionDefinition":
            case "WExpXpBookDefinition":
            case "WExpGiftboxDefinition":
            case "WExpStandInDefinition":
            case "WExpHQMarketDefinition":
            case "WExpLevelArtDefinition":
            case "WExpCharacterDefinition":
            case "WExpContainerDefinition":
            case "WExpHQSmelterDefinition":
            case "WExpUnlockableDefinition":
            case "WExpHQWorkshopDefinition":
            case "WExpHammerChestDefinition":
            case "WExpHQHeroTowerDefinition":
            case "WExpTreasureMapDefinition":
            case "WExpVoucherItemDefinition":
            case "WExpHQBlacksmithDefinition":
            case "WExpHQMonsterPitDefinition":
            case "WExpHQSecretShopDefinition":
            case "WExpAccountRewardDefinition":
            case "WExpPersonalEventDefinition":
            case "WExpUpgradePotionDefinition":
            case "WExpHQWorkerLodgesDefinition":
            case "WExpGearAccountItemDefinition":
            case "WExpGenericAccountItemDefinition":
                creator = new BaseBreakersIcon(_object.Value, _style);
                return true;
        }
    }

    public override string ToString() => $"{_exportType} | {_style}";

    public void Dispose()
    {
        _object = null;
    }
}
