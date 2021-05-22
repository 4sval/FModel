using System;
using System.Linq;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Extensions;
using FModel.Framework;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace FModel.Creator.Bases.FN
{
    public class BaseQuest : BaseIcon
    {
        private int _count;
        private Reward _reward;
        private readonly bool _screenLayer;
        private readonly string[] _unauthorizedReward = {"Token", "ChallengeBundle", "GiftBox"};

        public string NextQuestName { get; private set; }

        public BaseQuest(UObject uObject, EIconStyle style) : base(uObject, style)
        {
            Margin = 0;
            Width = 1024;
            Height = 256;
            DefaultPreview = Utils.GetBitmap("FortniteGame/Content/Athena/HUD/Quests/Art/T_NPC_Default.T_NPC_Default");
            if (uObject != null)
            {
                _screenLayer = uObject.ExportType.Equals("FortFeatItemDefinition", StringComparison.OrdinalIgnoreCase);
            }
        }

        private BaseQuest(int completionCount, EIconStyle style) : this(null, style) // completion
        {
            var description = completionCount < 0 ?
                Utils.GetLocalizedResource("AthenaChallengeDetailsEntry", "CompletionRewardFormat_All", "Complete <text color=\"FFF\" case=\"upper\" fontface=\"black\">all {0} challenges</> to earn the reward item") :
                Utils.GetLocalizedResource("AthenaChallengeDetailsEntry", "CompletionRewardFormat", "Complete <text color=\"FFF\" case=\"upper\" fontface=\"black\">any {0} challenges</> to earn the reward item");

            DisplayName = ReformatString(description, completionCount.ToString(), completionCount < 0);
        }

        public BaseQuest(int completionCount, FSoftObjectPath itemDefinition, EIconStyle style) : this(completionCount, style) // completion
        {
            _reward = Utils.TryLoadObject(itemDefinition.AssetPathName.Text, out UObject uObject) ? new Reward(uObject) : new Reward();
        }

        public BaseQuest(int completionCount, int quantity, string reward, EIconStyle style) : this(completionCount, style) // completion
        {
            _reward = new Reward(quantity, reward);
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
                Description = ShortDescription;
                if (Object.TryGetValue(out FText completionText, "CompletionText"))
                    Description += "\n" + completionText.Text;
                if (Object.TryGetValue(out FSoftObjectPath tandemCharacterData, "TandemCharacterData") &&
                    Utils.TryLoadObject(tandemCharacterData.AssetPathName.Text, out UObject uObject) &&
                    uObject.TryGetValue(out FSoftObjectPath tandemIcon, "SidePanelIcon", "EntryListIcon", "ToastIcon"))
                {
                    Preview = Utils.GetBitmap(tandemIcon);
                }
            }

            if (Object.TryGetValue(out int objectiveCompletionCount, "ObjectiveCompletionCount"))
                _count = objectiveCompletionCount;

            if (Object.TryGetValue(out FStructFallback[] objectives, "Objectives") && objectives.Length > 0)
            {
                // actual description doesn't exist
                if (string.IsNullOrEmpty(Description) && objectives[0].TryGetValue(out FText description, "Description"))
                    Description = description.Text;

                // ObjectiveCompletionCount doesn't exist
                if (_count == 0)
                {
                    if (objectives[0].TryGetValue(out int count, "Count") && count > 1)
                        _count = count;
                    else
                        _count = objectives.Length;
                }
            }

            if (Object.TryGetValue(out FStructFallback[] rewards, "Rewards"))
            {
                foreach (var reward in rewards)
                {
                    if (!reward.TryGetValue(out FStructFallback itemPrimaryAssetId, "ItemPrimaryAssetId") ||
                        !reward.TryGetValue(out int quantity, "Quantity")) continue;

                    if (!itemPrimaryAssetId.TryGetValue(out FStructFallback primaryAssetType, "PrimaryAssetType") ||
                        !itemPrimaryAssetId.TryGetValue(out FName primaryAssetName, "PrimaryAssetName") ||
                        !primaryAssetType.TryGetValue(out FName name, "Name")) continue;

                    if (name.Text.Equals("Quest", StringComparison.OrdinalIgnoreCase))
                    {
                        NextQuestName = primaryAssetName.Text;
                    }
                    else if (!_unauthorizedReward.Contains(name.Text))
                    {
                        _reward = new Reward(quantity, primaryAssetName);
                    }
                }
            }

            if (_reward == null && Object.TryGetValue(out UDataTable rewardsTable, "RewardsTable"))
            {
                if (rewardsTable.TryGetDataTableRow("Default", StringComparison.InvariantCulture, out var row))
                {
                    if (row.TryGetValue(out FName templateId, "TemplateId") &&
                        row.TryGetValue(out int quantity, "Quantity"))
                    {
                        _reward = new Reward(quantity, templateId);
                    }
                }
            }

            if (_reward == null && Object.TryGetValue(out FStructFallback[] hiddenRewards, "HiddenRewards"))
            {
                foreach (var hiddenReward in hiddenRewards)
                {
                    if (!hiddenReward.TryGetValue(out FName templateId, "TemplateId") ||
                        !hiddenReward.TryGetValue(out int quantity, "Quantity")) continue;

                    _reward = new Reward(quantity, templateId);
                    break;
                }
            }

            _reward ??= new Reward();
        }

        public void DrawQuest(SKCanvas c, int y)
        {
            DrawBackground(c, y);
            DrawPreview(c, y);
            DrawTexts(c, y);
        }

        public override SKImage Draw()
        {
            using var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Opaque);
            using var c = new SKCanvas(ret);

            DrawQuest(c, 0);

            return SKImage.FromBitmap(ret);
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
            Color = SKColor.Parse("#262630")
        };

        private void DrawBackground(SKCanvas c, int y)
        {
            c.DrawRect(new SKRect(Margin, y, Width, y + Height), _informationPaint);

            _informationPaint.Shader = SKShader.CreateRadialGradient(new SKPoint(Width / 2, y + Height / 2), Width / 5 * 4,
                new[] {Background[0].WithAlpha(50), Background[1].WithAlpha(50)}, SKShaderTileMode.Clamp);
            c.DrawRect(new SKRect(Height / 2, y, Width, y + Height), _informationPaint);

            _informationPaint.Shader = SKShader.CreateLinearGradient(new SKPoint(Width / 2, y + Height), new SKPoint(Width / 2, 75),
                new[] {SKColors.Black.WithAlpha(25), Background[1].WithAlpha(0)}, SKShaderTileMode.Clamp);
            c.DrawRect(new SKRect(0, y + 75, Width, y + Height), _informationPaint);

            _informationPaint.Shader = SKShader.CreateRadialGradient(new SKPoint(Width / 2, y + Height / 2), Width / 5 * 4, Background, SKShaderTileMode.Clamp);
            c.DrawRect(new SKRect(Margin, y, Height / 2, y + Height), _informationPaint);

            _informationPaint.Shader = SKShader.CreateLinearGradient(new SKPoint(Height / 2, y + Height / 2), new SKPoint(Height / 2 + 100, y + Height / 2),
                new[] {SKColors.Black.WithAlpha(25), Background[1].WithAlpha(0)}, SKShaderTileMode.Clamp);
            c.DrawRect(new SKRect(Height / 2, y, Height / 2 + 100, y + Height), _informationPaint);
        }

        private void DrawPreview(SKCanvas c, int y)
        {
            ImagePaint.BlendMode = _screenLayer ? SKBlendMode.Screen : Preview == null ? SKBlendMode.ColorBurn : SKBlendMode.SrcOver;
            c.DrawBitmap(Preview ?? DefaultPreview, new SKRect(Margin, y, Height - Margin, y + Height), ImagePaint);
        }

        private void DrawTexts(SKCanvas c, int y)
        {
            _informationPaint.Shader = null;

            if (!string.IsNullOrWhiteSpace(DisplayName))
            {
                _informationPaint.TextSize = 40;
                _informationPaint.Color = SKColors.White;
                _informationPaint.Typeface = Utils.Typefaces.Bundle;
                while (_informationPaint.MeasureText(DisplayName) > Width - Height - 10)
                {
                    _informationPaint.TextSize -= 1;
                }

                var shaper = new CustomSKShaper(_informationPaint.Typeface);
                shaper.Shape(DisplayName, _informationPaint);
                c.DrawShapedText(shaper, DisplayName, Height, y + 50, _informationPaint);
            }

            var outY = y + 75f;
            if (!string.IsNullOrWhiteSpace(Description))
            {
                _informationPaint.TextSize = 16;
                _informationPaint.Color = SKColors.White.WithAlpha(175);
                _informationPaint.Typeface = Utils.Typefaces.Description;
                Utils.DrawMultilineText(c, Description, Width - Height, y, SKTextAlign.Left,
                    new SKRect(Height, outY, Width - 10, y + Height), _informationPaint, out outY);
            }

            _informationPaint.Color = Border[0].WithAlpha(100);
            c.DrawRect(new SKRect(Height, outY, Width - 150, outY + 5), _informationPaint);

            if (_count > 0)
            {
                _informationPaint.TextSize = 25;
                _informationPaint.Color = SKColors.White;
                _informationPaint.Typeface = Utils.Typefaces.BundleNumber;
                c.DrawText("0 / ", new SKPoint(Width - 130, outY + 10), _informationPaint);

                _informationPaint.Color = Border[0];
                c.DrawText(_count.ToString(), new SKPoint(Width - 95, outY + 10), _informationPaint);
            }

            _reward.DrawQuest(c, new SKRect(Height, outY + 25, Width - 20, y + Height - 25));
        }
    }
}