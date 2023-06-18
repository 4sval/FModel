using System;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets.Exports;
using FModel.Creator.Bases;
using FModel.Creator.Bases.BB;
using FModel.Creator.Bases.FN;
using FModel.Creator.Bases.MV;
using FModel.Creator.Bases.SB;

namespace FModel.Creator;

public class CreatorPackage : IDisposable
{
    private UObject _object;
    private EIconStyle _style;

    public CreatorPackage(UObject uObject, EIconStyle style)
    {
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
    public bool TryConstructCreator(out UCreator creator)
    {
        switch (_object.ExportType)
        {
            // Fortnite
            case "FortCreativeWeaponMeleeItemDefinition":
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
            case "FortBannerTokenType":
            case "FortVariantTokenType":
            case "FortDecoItemDefinition":
            case "FortStatItemDefinition":
            case "FortAmmoItemDefinition":
            case "FortEmoteItemDefinition":
            case "FortBadgeItemDefinition":
            case "FortAwardItemDefinition":
            case "FortGadgetItemDefinition":
            case "AthenaCharmItemDefinition":
            case "FortPlaysetItemDefinition":
            case "FortGiftBoxItemDefinition":
            case "FortOutpostItemDefinition":
            case "FortVehicleItemDefinition":
            case "FortCardPackItemDefinition":
            case "FortDefenderItemDefinition":
            case "FortCurrencyItemDefinition":
            case "FortResourceItemDefinition":
            case "FortBackpackItemDefinition":
            case "FortEventQuestMapDataAsset":
            case "FortWeaponModItemDefinition":
            case "FortCodeTokenItemDefinition":
            case "FortSchematicItemDefinition":
            case "FortWorldMultiItemDefinition":
            case "FortAlterationItemDefinition":
            case "FortExpeditionItemDefinition":
            case "FortIngredientItemDefinition":
            case "FortAccountBuffItemDefinition":
            case "FortWeaponMeleeItemDefinition":
            case "FortPlayerPerksItemDefinition":
            case "FortPlaysetPropItemDefinition":
            case "FortHomebaseNodeItemDefinition":
            case "FortNeverPersistItemDefinition":
            case "FortPlayerAugmentItemDefinition":
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
            case "FortCreativeWeaponRangedItemDefinition":
            case "FortCreativeRealEstatePlotItemDefinition":
            case "AthenaDanceItemDefinition_AdHocSquadsJoin_C":
            case "StWFortAccoladeItemDefinition":
            case "FortSmartBuildingItemDefinition":
                creator = _style switch
                {
                    EIconStyle.Cataba => new BaseCommunity(_object, _style, "Cataba"),
                    _ => new BaseIcon(_object, _style)
                };
                return true;
            case "FortTandemCharacterData":
                creator = new BaseTandem(_object, _style);
                return true;
            case "FortTrapItemDefinition":
            case "FortSpyTechItemDefinition":
            case "FortAccoladeItemDefinition":
            case "FortContextTrapItemDefinition":
            case "FortWeaponRangedItemDefinition":
            case "Daybreak_LevelExitVehicle_PartItemDefinition_C":
                creator = new BaseIconStats(_object, _style);
                return true;
            case "FortItemSeriesDefinition":
                creator = new BaseSeries(_object, _style);
                return true;
            case "MaterialInstanceConstant"
                when _object.Owner != null &&
                     (_object.Owner.Name.EndsWith($"/MI_OfferImages/{_object.Name}", StringComparison.OrdinalIgnoreCase) ||
                      _object.Owner.Name.EndsWith($"/RenderSwitch_Materials/{_object.Name}", StringComparison.OrdinalIgnoreCase) ||
                      _object.Owner.Name.EndsWith($"/MI_BPTile/{_object.Name}", StringComparison.OrdinalIgnoreCase)):
                creator = new BaseMaterialInstance(_object, _style);
                return true;
            case "AthenaItemShopOfferDisplayData":
                creator = new BaseOfferDisplayData(_object, _style);
                return true;
            case "FortMtxOfferData":
                creator = new BaseMtxOffer(_object, _style);
                return true;
            case "FortPlaylistAthena":
                creator = new BasePlaylist(_object, _style);
                return true;
            case "FortFeatItemDefinition":
            case "FortQuestItemDefinition":
            case "FortQuestItemDefinition_Athena":
            case "FortQuestItemDefinition_Campaign":
            case "AthenaDailyQuestDefinition":
            case "FortUrgentQuestItemDefinition":
                creator = new Bases.FN.BaseQuest(_object, _style);
                return true;
            case "FortCompendiumItemDefinition":
            case "FortChallengeBundleItemDefinition":
                creator = new BaseBundle(_object, _style);
                return true;
            case "AthenaSeasonItemDefinition":
                creator = new BaseSeason(_object, _style);
                return true;
            case "FortItemAccessTokenType":
                creator = new BaseItemAccessToken(_object, _style);
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
                creator = new BaseUserControl(_object, _style);
                return true;
            // PandaGame
            case "CharacterData":
                creator = new BaseFighter(_object, _style);
                return true;
            case "PerkGroup":
                creator = new BasePerkGroup(_object, _style);
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
                creator = new BasePandaIcon(_object, _style);
                return true;
            case "QuestData":
                creator = new Bases.MV.BaseQuest(_object, _style);
                return true;
            default:
                creator = null;
                return false;
        }
    }

    public override string ToString() => $"{_object.ExportType} | {_style}";

    public void Dispose()
    {
        _object = null;
    }
}
