using System;
using System.Collections.Generic;
using CUE4Parse.GameTypes.FN.Enums;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Engine.Curves;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Extensions;
using FModel.Framework;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace FModel.Creator.Bases.FN;

public class BaseIconStats : UStatCreator
{
    public BaseIconStats(UObject uObject, EIconStyle style) : base(uObject, style)
    {
        Width = 1024;
        Height = _headerHeight;
        Margin = 0;
        _statistics = new List<IconStat>();
        _screenLayer = uObject.ExportType.Equals("FortAccoladeItemDefinition", StringComparison.OrdinalIgnoreCase);
        DefaultPreview = Utils.GetBitmap("FortniteGame/Content/Athena/HUD/Quests/Art/T_NPC_Default.T_NPC_Default");
    }

    public override void ParseForInfo()
    {
        DisplayName = DisplayName.ToUpperInvariant();

        if (Object.TryGetValue(out FName accoladeType, "AccoladeType") &&
            accoladeType.Text.Equals("EFortAccoladeType::Medal", StringComparison.OrdinalIgnoreCase))
        {
            _screenLayer = false;
        }

        if (Object.TryGetValue(out FGameplayTagContainer poiLocations, "POILocations") &&
            Utils.TryLoadObject("FortniteGame/Content/Quests/QuestIndicatorData.QuestIndicatorData", out UObject uObject) &&
            uObject.TryGetValue(out FStructFallback[] challengeMapPoiData, "ChallengeMapPoiData"))
        {
            foreach (var location in poiLocations)
            {
                var locationName = "Unknown";
                foreach (var poi in challengeMapPoiData)
                {
                    if (!poi.TryGetValue(out FStructFallback locationTag, "LocationTag") || !locationTag.TryGetValue(out FName tagName, "TagName") ||
                        tagName != location.TagName || !poi.TryGetValue(out FText text, "Text")) continue;

                    locationName = text.Text;
                    break;
                }

                _statistics.Add(new IconStat(Utils.GetLocalizedResource("", "B0C091D7409B1657423C5F97E9CF4C77", "LOCATION NAME"), locationName.ToUpper()));
            }
        }

        if (Object.TryGetValue(out FStructFallback maxStackSize, "MaxStackSize"))
        {
            if (maxStackSize.TryGetValue(out float v, "Value") && v > 0)
            {
                _statistics.Add(new IconStat("Max Stack", v, 15));
            }
            else if (TryGetCurveTableStat(maxStackSize, out var s))
            {
                _statistics.Add(new IconStat("Max Stack", s, 15));
            }
        }

        if (Object.TryGetValue(out FStructFallback xpRewardAmount, "XpRewardAmount") && TryGetCurveTableStat(xpRewardAmount, out var x))
        {
            _statistics.Add(new IconStat("XP Amount", x));
        }

        if (Object.TryGetValue(out FStructFallback weaponStatHandle, "WeaponStatHandle") &&
            weaponStatHandle.TryGetValue(out FName weaponRowName, "RowName") &&
            weaponStatHandle.TryGetValue(out UDataTable dataTable, "DataTable") &&
            dataTable.TryGetDataTableRow(weaponRowName.Text, StringComparison.OrdinalIgnoreCase, out var weaponRowValue))
        {
            if (weaponRowValue.TryGetValue(out int bpc, "BulletsPerCartridge"))
            {
                var multiplier = bpc != 0f ? bpc : 1;
                if (weaponRowValue.TryGetValue(out float dmgPb, "DmgPB") && dmgPb != 0f)
                {
                    _statistics.Add(new IconStat(Utils.GetLocalizedResource("", "35D04D1B45737BEA25B69686D9E085B9", "Damage"), dmgPb * multiplier, 200));
                }

                if (weaponRowValue.TryGetValue(out float mdpc, "MaxDamagePerCartridge") && mdpc >= 0f)
                {
                    _statistics.Add(new IconStat(Utils.GetLocalizedResource("", "0DEF2455463B008C4499FEA03D149EDF", "Headshot Damage"), mdpc, 200));
                }
                else if (weaponRowValue.TryGetValue(out float dmgCritical, "DamageZone_Critical"))
                {
                    _statistics.Add(new IconStat(Utils.GetLocalizedResource("", "0DEF2455463B008C4499FEA03D149EDF", "Headshot Damage"), dmgPb * dmgCritical * multiplier, 200));
                }
            }

            if (weaponRowValue.TryGetValue(out int clipSize, "ClipSize") && clipSize != 0)
            {
                _statistics.Add(new IconStat(Utils.GetLocalizedResource("", "068239DD4327B36124498C9C5F61C038", "Magazine Size"), clipSize, 50));
            }

            if (weaponRowValue.TryGetValue(out float firingRate, "FiringRate") && firingRate != 0f)
            {
                _statistics.Add(new IconStat(Utils.GetLocalizedResource("", "27B80BA44805ABD5A2D2BAB2902B250C", "Fire Rate"), firingRate, 15));
            }

            if (weaponRowValue.TryGetValue(out float armTime, "ArmTime") && armTime != 0f)
            {
                _statistics.Add(new IconStat(Utils.GetLocalizedResource("", "3BFEB8BD41A677CC5F45B9A90D6EAD6F", "Arming Delay"), armTime, 125));
            }

            if (weaponRowValue.TryGetValue(out float reloadTime, "ReloadTime") && reloadTime != 0f)
            {
                _statistics.Add(new IconStat(Utils.GetLocalizedResource("", "6EA26D1A4252034FBD869A90F9A6E49A", "Reload Time"), reloadTime, 15));
            }

            if ((Object.ExportType.Equals("FortContextTrapItemDefinition", StringComparison.OrdinalIgnoreCase) ||
                 Object.ExportType.Equals("FortTrapItemDefinition", StringComparison.OrdinalIgnoreCase)) &&
                weaponRowValue.TryGetValue(out UDataTable durabilityTable, "Durability") &&
                weaponRowValue.TryGetValue(out FName durabilityRowName, "DurabilityRowName") &&
                durabilityTable.TryGetDataTableRow(durabilityRowName.Text, StringComparison.OrdinalIgnoreCase, out var durability) &&
                durability.TryGetValue(out int duraByRarity, Object.GetOrDefault("Rarity", EFortRarity.Uncommon).GetDescription()))
            {
                _statistics.Add(new IconStat(Utils.GetLocalizedResource("", "6FA2882140CB69DE32FD73A392F0585B", "Durability"), duraByRarity, 20));
            }
        }

        base.ParseForInfo();
    }
}
