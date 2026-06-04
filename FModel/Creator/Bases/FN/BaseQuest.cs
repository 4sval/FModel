using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FModel.Framework;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace FModel.Creator.Bases.FN;

public class BaseQuest : BaseIcon
{
    private int _count;
    private readonly List<Reward> _rewards;
    private readonly bool _screenLayer;

    public string NextQuestName { get; private set; }

    public BaseQuest(UObject uObject, EIconStyle style) : base(uObject, style)
    {
        Margin = 0;
        Width = 1024;
        Height = 200;
        _rewards = [];
        if (uObject != null)
        {
            _screenLayer = uObject.ExportType.Equals("FortFeatItemDefinition", StringComparison.OrdinalIgnoreCase);
        }
    }

    public BaseQuest(int completionCount, EIconStyle style) : this(null, style) // completion
    {
        var description = completionCount < 0 ?
            Utils.GetLocalizedResource("AthenaChallengeDetailsEntry", "CompletionRewardFormat_All", "Complete <text color=\"FFF\" case=\"upper\" fontface=\"black\">all {0} challenges</> to earn the reward item") :
            Utils.GetLocalizedResource("AthenaChallengeDetailsEntry", "CompletionRewardFormat", "Complete <text color=\"FFF\" case=\"upper\" fontface=\"black\">any {0} challenges</> to earn the reward item");

        DisplayName = ReformatString(description, completionCount.ToString(), completionCount < 0);
    }

    public void AddCompletionReward(FSoftObjectPath itemDefinition)
    {
        _rewards.Add(itemDefinition.TryLoad(out UObject uObject) ? new Reward(uObject) : new Reward());
    }

    public void AddCompletionReward(int quantity, string reward)
    {
        _rewards.Add(new Reward(quantity, reward));
    }

    public override void ParseForInfo()
    {
        ParseForReward(false);

        if (Object.TryGetValue(out FStructFallback urgentQuestData, "UrgentQuestData"))
        {
            if (urgentQuestData.TryGetValue(out FText eventTitle, "EventTitle"))
                DisplayName = eventTitle.Text;
            if (urgentQuestData.TryGetValue(out FText eventDescription, "EventDescription"))
                Description = eventDescription.Text;
            if (urgentQuestData.TryGetValue(out FPackageIndex alertIcon, "AlertIcon", "BountyPriceImage"))
                Preview = Utils.GetBitmap(alertIcon);
        }
        else
        {
            Description = string.Empty;

            if ((Object.TryGetValue(out FSoftObjectPath icon, "QuestGiverWidgetIcon", "NotificationIconOverride") &&
                Utils.TryLoadObject(icon.AssetPathName.Text, out UObject iconObject)) ||
                (Object.TryGetValue(out FSoftObjectPath tandemCharacterData, "TandemCharacterData") &&
                Utils.TryLoadObject(tandemCharacterData.AssetPathName.Text, out UObject uObject) &&
                uObject.TryGetValue(out FSoftObjectPath tandemIcon, "EntryListIcon", "ToastIcon") &&
                Utils.TryLoadObject(tandemIcon.AssetPathName.Text, out iconObject)))
            {
                Preview = iconObject switch
                {
                    UTexture2D text => Utils.GetBitmap(text),
                    UMaterialInstanceConstant mat => Utils.GetBitmap(mat),
                    _ => Preview
                };
            }
        }

        if (Object.TryGetValue(out int objectiveCompletionCount, "ObjectiveCompletionCount"))
            _count = objectiveCompletionCount;

        if (Object.TryGetValue(out FStructFallback[] objectives, "Objectives") && objectives.Length > 0)
        {
            // actual description doesn't exist
            if (string.IsNullOrEmpty(DisplayName) && objectives[0].TryGetValue(out FText description, "Description"))
                DisplayName = description.Text;

            // ObjectiveCompletionCount doesn't exist
            if (_count == 0)
            {
                if (objectives[0].TryGetValue(out int count, "Count") && count > 1)
                    _count = count;
                else
                    _count = objectives.Length;
            }
        }

        if (Object.TryGetValue(out FPackageIndex[] questDefinitionComponents, "QuestDefinitionComponents"))
        {
            foreach (var questDefinitionComponent in questDefinitionComponents)
            {
                if (!questDefinitionComponent.Name.StartsWith("FortQuestDefinitionComponent_Rewards") ||
                    !questDefinitionComponent.TryLoad(out var rewardComponent) ||
                    !rewardComponent.TryGetValue(out FInstancedStruct[] questRewardsArray, "QuestRewardsArray")) continue;

                foreach (var questReward in questRewardsArray)
                {
                    if (questReward.NonConstStruct.TryGetValue(out FStructFallback[] resourceDataTableRewards, "ResourceDataTableRewards") &&
                        resourceDataTableRewards.Length > 0 && resourceDataTableRewards[0].TryGetValue(out FStructFallback tableRowEntry, "TableRowEntry") &&
                        tableRowEntry.TryGetValue(out UDataTable rewardsTable, "DataTable") &&
                        tableRowEntry.TryGetValue(out FName rowName, "RowName") &&
                        rewardsTable.TryGetDataTableRow(rowName.Text, StringComparison.InvariantCulture, out var row) &&
                        row.TryGetValue(out FSoftObjectPath resourceDefinition, "ResourceDefinition") &&
                        row.TryGetValue(out int quantity, "Quantity"))
                    {
                        _rewards.Add(new Reward(quantity, resourceDefinition));
                    }
                    else if (questReward.NonConstStruct.TryGetValue(out FInstancedStruct[] rewards, "CosmeticRewards", "CurrencyRewards", "VariantTokenRewards", "ResourceRewards"))
                    {
                        foreach (var reward in rewards)
                        {
                            if (reward.NonConstStruct.TryGetValue(out FSoftObjectPath cosmeticDefinition, "CosmeticDefinition", "CurrencyDefinition", "VariantTokenDefinition", "ResourceDefinition"))
                            {
                                if (reward.NonConstStruct.TryGetValue(out int count, "CurrencyCount", "ResourceCount"))
                                {
                                    _rewards.Add(new Reward(count, cosmeticDefinition));
                                }
                                else if (cosmeticDefinition.TryLoad(out var cosmetic))
                                {
                                    _rewards.Add(new Reward(cosmetic));
                                }
                            }
                        }
                    }
                }
                break;
            }
        }
    }

    public void DrawQuest(SKCanvas c, int y)
    {
        var x = Preview is null ? 0 : Height + 10;
        DrawBackground(c, x, y);
        DrawPreview(c, y);
        DrawTexts(c, x + 50, y, 50);
    }

    public override SKBitmap[] Draw()
    {
        var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var c = new SKCanvas(ret);

        DrawQuest(c, 0);

        return [ret];
    }

    private string ReformatString(string s, string completionCount, bool isAll)
    {
        s = s.Replace("({0})", "{0}").Replace("{QuestNumber}", "<text color=\"FFF\" case=\"upper\" fontface=\"black\">{0}</>");
        var index = s.IndexOf("{0}|plural(", StringComparison.OrdinalIgnoreCase);
        if (index > -1)
        {
            var p = s.Substring(index, s[index..].IndexOf(')') + 1);
            s = s.Replace(p, string.Empty);
            s = s.Insert(s.IndexOf("</>", StringComparison.OrdinalIgnoreCase), p.SubstringAfter("(").SubstringAfter("=").SubstringBefore(","));
        }

        var upper = s.SubstringAfter(">").SubstringBefore("</>");
        return string.Format(Utils.RemoveHtmlTags(s.Replace(upper, upper.ToUpper())), isAll ? string.Empty : completionCount).Replace("  ", " ");
    }

    private readonly SKPaint _informationPaint = new()
    {
        IsAntialias = true, FilterQuality = SKFilterQuality.High,
        Color = SKColor.Parse("#183E94"),
    };

    private void DrawBackground(SKCanvas c, int x, int y)
    {
        _informationPaint.Shader = SKShader.CreateRadialGradient(new SKPoint(Width * 0.75f, y + Height * 0.5f), Width * 0.75f,
            [SKColor.Parse("#1565D0"), SKColor.Parse("#1B1150")], SKShaderTileMode.Clamp);
        c.DrawRoundRect(new SKRect(x, y, Width, y + Height), 25, 25, _informationPaint);
    }

    private void DrawPreview(SKCanvas c, int y)
    {
        if (Preview is null) return;
        ImagePaint.BlendMode = _screenLayer ? SKBlendMode.Screen : Preview == null ? SKBlendMode.ColorBurn : SKBlendMode.SrcOver;

        var rect = new SKRect(0, y, Height, y + Height);

        c.Save();
        using (var roundRectPath = new SKPath())
        {
            roundRectPath.AddRoundRect(rect, 15, 15);
            c.ClipPath(roundRectPath, antialias: true);
        }

        _informationPaint.Shader = SKShader.CreateLinearGradient(
            new SKPoint(rect.Left, rect.Bottom),
            new SKPoint(rect.Left, rect.Top),
            [
                _informationPaint.Color,
                _informationPaint.Color.WithAlpha(0)
            ],
            [0, 1],
            SKShaderTileMode.Clamp
        );
        c.DrawRect(rect, _informationPaint);
        c.DrawBitmap(Preview, rect, ImagePaint);
        c.Restore();
    }

    private void DrawTexts(SKCanvas c, int x, int y, int padding)
    {
        _informationPaint.Shader = null;
        _informationPaint.Color = SKColors.White;

        float maxX = Width - padding;
        float steps = Height * 0.5f;
        foreach (var reward in _rewards)
        {
            reward.DrawQuest(c, new SKRect(maxX - steps, y + padding, maxX, y + padding + steps));
            maxX -= steps;
        }
        maxX -= steps * 0.5f;

        if (!string.IsNullOrWhiteSpace(DisplayName))
        {
            _informationPaint.TextSize = 25;
            _informationPaint.Typeface = Utils.Typefaces.Bundle;

            Utils.DrawMultilineText(c, Utils.RemoveHtmlTags(DisplayName).Replace("  ", " "), Width - padding, 0, SKTextAlign.Left,
                new SKRect(x, y + padding, maxX, Height - padding * 1.5f), _informationPaint, out _);
        }

        maxX -= steps * 0.5f;
        if (maxX > Width * 0.7f) maxX = Width * 0.7f;
        if (_count > 0)
        {
            _informationPaint.TextSize = 20;
            _informationPaint.Typeface = Utils.Typefaces.BundleNumber;
            c.DrawText($"0/{_count}", new SKPoint(maxX, y + Height - padding), _informationPaint);
        }

        _informationPaint.Color = SKColor.Parse("#121A45").WithAlpha(200);
        c.DrawRoundRect(new SKRect(x, y + Height - padding - 12.5f, maxX - 12.5f, y + Height - padding), 5, 5, _informationPaint);
    }
}
