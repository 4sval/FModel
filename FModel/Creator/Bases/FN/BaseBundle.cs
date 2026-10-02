using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using SkiaSharp;

namespace FModel.Creator.Bases.FN;

public class BaseBundle : UCreator
{
    private IList<BaseQuest> _quests;

    public BaseBundle(UObject uObject, EIconStyle style) : base(uObject, style)
    {
        Width = 1024;
        Height = 0;
        Margin = 0;
    }

    public override void ParseForInfo()
    {
        _quests = new List<BaseQuest>();

        if (Object.TryGetValue(out FText displayName, "DisplayName", "ItemName"))
            DisplayName = displayName.Text.ToUpperInvariant();

        if (Object.TryGetValue(out FStructFallback[] quests, "QuestInfos")) // prout :)
        {
            foreach (var quest in quests)
            {
                if (!quest.TryGetValue(out FSoftObjectPath questDefinition, "QuestDefinition")) continue;

                BaseQuest q;
                var path = questDefinition.AssetPathName.Text;
                do
                {
                    if (!Utils.TryLoadObject(path, out UObject uObject)) break;

                    q = new BaseQuest(uObject, Style);
                    q.ParseForInfo();
                    _quests.Add(q);
                    path = path.SubstringBeforeWithLast('/') + q.NextQuestName + "." + q.NextQuestName;
                } while (!string.IsNullOrEmpty(q.NextQuestName));
            }
        }

        if (Object.TryGetValue(out FStructFallback[] completionRewards, "BundleCompletionRewards"))
        {
            foreach (var completionReward in completionRewards)
            {
                if (!completionReward.TryGetValue(out int completionCount, "CompletionCount") ||
                    !completionReward.TryGetValue(out FStructFallback[] rewards, "Rewards")) continue;

                var quest = new BaseQuest(completionCount, Style);
                foreach (var reward in rewards)
                {
                    if (!reward.TryGetValue(out FSoftObjectPath itemDefinition, "ItemDefinition")) continue;
                    quest.AddCompletionReward(itemDefinition);
                }
                _quests.Add(quest);
            }
        }

        Height += 200 * _quests.Count;
    }

    public override SKBitmap[] Draw()
    {
        var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var c = new SKCanvas(ret);

        var y = 0;
        foreach (var quest in _quests)
        {
            quest.DrawQuest(c, y);
            y += quest.Height;
        }

        return [ret];
    }
}
