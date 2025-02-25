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
        switch (_exportType)
        {
            // Fortnite
            case "AthenaConsumableEmoteItemDefinition":
            case "AthenaSkyDiveContrailItemDefinition":
            case "AthenaLoadingScreenItemDefinition":
            case "AthenaVictoryPoseItemDefinition":
            case "AthenaPetCarrierItemDefinition":
            case "AthenaMusicPackItemDefinition":
            case "AthenaBattleBusItemDefinition":
            case "AthenaCharacterItemDefinition":
            case "AthenaMapMarkerItemDefinition":
            case "AthenaBackpackItemDefinition":
            case "CosmeticShoesItemDefinition":
            case "AthenaPickaxeItemDefinition":
            case "AthenaGadgetItemDefinition":
            case "AthenaGliderItemDefinition":
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
            case "JunoKnowledgeBundle":
            case "FortBannerTokenType":
            case "FortVariantTokenType":
            case "FortDecoItemDefinition":
            case "FortStatItemDefinition":
            case "FortAmmoItemDefinition":
            case "FortEmoteItemDefinition":
            case "FortBadgeItemDefinition":
            case "SparksMicItemDefinition":
            case "FortAwardItemDefinition":
            case "FortStackItemDefinition":
            case "FortWorldItemDefinition":
            case "SparksAuraItemDefinition":
            case "SparksDrumItemDefinition":
            case "SparksBassItemDefinition":
            case "FortGadgetItemDefinition":
            case "AthenaCharmItemDefinition":
            case "FortPlaysetItemDefinition":
            case "FortGiftBoxItemDefinition":
            case "FortOutpostItemDefinition":
            case "FortVehicleItemDefinition":
            case "FortMissionItemDefinition":
            case "FortAccountItemDefinition":
            case "SparksGuitarItemDefinition":
            case "FortCardPackItemDefinition":
            case "FortDefenderItemDefinition":
            case "FortCurrencyItemDefinition":
            case "FortResourceItemDefinition":
            case "FortBackpackItemDefinition":
            case "FortEventQuestMapDataAsset":
            case "FortBuildingItemDefinition":
            case "FortItemCacheItemDefinition":
            case "FortWeaponModItemDefinition":
            case "FortCodeTokenItemDefinition":
            case "FortSchematicItemDefinition":
            case "FortAlterableItemDefinition":
            case "SparksKeyboardItemDefinition":
            case "FortWorldMultiItemDefinition":
            case "FortAlterationItemDefinition":
            case "FortExpeditionItemDefinition":
            case "FortIngredientItemDefinition":
            case "FortConsumableItemDefinition":
            case "StWFortAccoladeItemDefinition":
            case "FortAccountBuffItemDefinition":
            case "FortFOBCoreDecoItemDefinition":
            case "FortPlayerPerksItemDefinition":
            case "FortPlaysetPropItemDefinition":
            case "FortPrerollDataItemDefinition":
            case "JunoRecipeBundleItemDefinition":
            case "FortHomebaseNodeItemDefinition":
            case "FortNeverPersistItemDefinition":
            case "FortPlayerAugmentItemDefinition":
            case "FortSmartBuildingItemDefinition":
            case "FortGiftBoxUnlockItemDefinition":
            case "FortCreativeGadgetItemDefinition":
            case "FortWeaponModItemDefinitionOptic":
            case "RadioContentSourceItemDefinition":
            case "FortPlaysetGrenadeItemDefinition":
            case "JunoWeaponCreatureItemDefinition":
            case "FortEventDependentItemDefinition":
            case "FortPersonalVehicleItemDefinition":
            case "FortGameplayModifierItemDefinition":
            case "FortHardcoreModifierItemDefinition":
            case "FortWeaponModItemDefinitionMagazine":
            case "FortConsumableAccountItemDefinition":
            case "FortConversionControlItemDefinition":
            case "FortAccountBuffCreditItemDefinition":
            case "JunoBuildInstructionsItemDefinition":
            case "FortCharacterCosmeticItemDefinition":
            case "JunoBuildingSetAccountItemDefinition":
            case "FortEventCurrencyItemDefinitionRedir":
            case "FortPersistentResourceItemDefinition":
            case "FortWeaponMeleeOffhandItemDefinition":
            case "FortHomebaseBannerIconItemDefinition":
            case "FortVehicleCosmeticsVariantTokenType":
            case "JunoBuildingPropAccountItemDefinition":
            case "FortCampaignHeroLoadoutItemDefinition":
            case "FortConditionalResourceItemDefinition":
            case "FortChallengeBundleScheduleDefinition":
            case "FortDailyRewardScheduleTokenDefinition":
            case "FortVehicleCosmeticsItemDefinition_Body":
            case "FortVehicleCosmeticsItemDefinition_Skin":
            case "FortVehicleCosmeticsItemDefinition_Wheel":
            case "FortCreativeRealEstatePlotItemDefinition":
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
        }
    }

    public override string ToString() => $"{_exportType} | {_style}";

    public void Dispose()
    {
        _object = null;
    }
}
