using System.ComponentModel;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Extensions;

namespace FModel.Creator.Bases.MV;

public class BaseQuest : BasePandaIcon
{
    public BaseQuest(UObject uObject, EIconStyle style) : base(uObject, style)
    {
    }

    public override void ParseForInfo()
    {
        if (Object.TryGetValue(out FStructFallback[] questCompletionRewards, "QuestCompletionRewards") &&
                 questCompletionRewards.Length > 0 && questCompletionRewards[0] is { } actualReward)
        {
            var rewardType = actualReward.GetOrDefault("RewardType", EQuestRewardType.Inventory);
            var count = actualReward.GetOrDefault("Count", 0);
            Pictos.Add((Utils.GetBitmap("/Game/Panda_Main/UI/Assets/Icons/ui_icons_plus.ui_icons_plus"), count.ToString()));

            base.ParseForInfo();

            if (actualReward.TryGetValue(out FPackageIndex assetReward, "AssetReward") &&
                Utils.TryGetPackageIndexExport(assetReward, out UObject export))
            {
                var item = new BasePandaIcon(export, Style);
                item.ParseForInfo();
                Preview = item.Preview;
            }
            else if (rewardType != EQuestRewardType.Inventory)
            {
                Preview = Utils.GetBitmap(rewardType.GetDescription());
            }
        }
        else
        {
            base.ParseForInfo();
        }
    }
}

public enum EQuestRewardType : byte
{
    Inventory = 0, // Default

    [Description("/Game/Panda_Main/UI/Assets/Icons/UI_CharacterTicket.UI_CharacterTicket")]
    AccountXP = 1,

    [Description("/Game/Panda_Main/UI/Assets/Icons/UI_BattlepassToken.UI_BattlepassToken")]
    BattlepassXP = 2
}
