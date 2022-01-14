using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Extensions;
using FModel.Framework;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace FModel.Creator.Bases.FN
{
    public class BaseBundle : UCreator
    {
        private IList<BaseQuest> _quests;
        private const int _headerHeight = 100;

        public BaseBundle(UObject uObject, EIconStyle style) : base(uObject, style)
        {
            Width = 1024;
            Height = _headerHeight;
            Margin = 0;
        }

        public override void ParseForInfo()
        {
            _quests = new List<BaseQuest>();

            if (Object.TryGetValue(out FText displayName, "DisplayName"))
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

                    foreach (var reward in rewards)
                    {
                        if (!reward.TryGetValue(out int quantity, "Quantity") ||
                            !reward.TryGetValue(out string templateId, "TemplateId") ||
                            !reward.TryGetValue(out FSoftObjectPath itemDefinition, "ItemDefinition")) continue;

                        if (!itemDefinition.AssetPathName.IsNone &&
                            !itemDefinition.AssetPathName.Text.Contains("/Items/Tokens/") &&
                            !itemDefinition.AssetPathName.Text.Contains("/Items/Quests"))
                        {
                            _quests.Add(new BaseQuest(completionCount, itemDefinition, Style));
                        }
                        else if (!string.IsNullOrWhiteSpace(templateId))
                        {
                            _quests.Add(new BaseQuest(completionCount, quantity, templateId, Style));
                        }
                    }
                }
            }

            Height += 256 * _quests.Count;
        }

        public override SKBitmap Draw()
        {
            var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Opaque);
            using var c = new SKCanvas(ret);

            DrawHeader(c);
            DrawDisplayName(c);
            DrawQuests(c);

            return ret;
        }

        private readonly SKPaint _headerPaint = new()
        {
            IsAntialias = true, FilterQuality = SKFilterQuality.High,
            Typeface = Utils.Typefaces.Bundle, TextSize = 50,
            TextAlign = SKTextAlign.Center, Color = SKColor.Parse("#262630")
        };

        private void DrawHeader(SKCanvas c)
        {
            c.DrawRect(new SKRect(0, 0, Width, _headerHeight), _headerPaint);

            var background = _quests.Count > 0 ? _quests[0].Background : Background;
            _headerPaint.Shader = SKShader.CreateRadialGradient(new SKPoint(Width / 2, _headerHeight / 2), Width / 5 * 4,
                new[] {background[0].WithAlpha(50), background[1].WithAlpha(50)}, SKShaderTileMode.Clamp);
            c.DrawRect(new SKRect(0, 0, Width, _headerHeight), _headerPaint);

            _headerPaint.Shader = SKShader.CreateLinearGradient(new SKPoint(Width / 2, _headerHeight), new SKPoint(Width / 2, 75),
                new[] {SKColors.Black.WithAlpha(25), background[1].WithAlpha(0)}, SKShaderTileMode.Clamp);
            c.DrawRect(new SKRect(0, 75, Width, _headerHeight), _headerPaint);
        }

        private new void DrawDisplayName(SKCanvas c)
        {
            if (string.IsNullOrEmpty(DisplayName)) return;

            _headerPaint.Shader = null;
            _headerPaint.Color = SKColors.White;
            while (_headerPaint.MeasureText(DisplayName) > Width)
            {
                _headerPaint.TextSize -= 1;
            }

            var shaper = new CustomSKShaper(_headerPaint.Typeface);
            var shapedText = shaper.Shape(DisplayName, _headerPaint);
            c.DrawShapedText(shaper, DisplayName, (Width - shapedText.Points[^1].X) / 2, _headerHeight / 2 + _headerPaint.TextSize / 2 - 10, _headerPaint);
        }

        private void DrawQuests(SKCanvas c)
        {
            var y = _headerHeight;
            foreach (var quest in _quests)
            {
                quest.DrawQuest(c, y);
                y += quest.Height;
            }
        }
    }
}
