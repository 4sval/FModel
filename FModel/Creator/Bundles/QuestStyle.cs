using FModel.Creator.Bases;
using FModel.Creator.Texts;
using SkiaSharp;

namespace FModel.Creator.Bundles
{
    static class QuestStyle
    {
        public static void DrawQuests(SKCanvas c, BaseBundle icon)
        {
            using SKPaint paint = new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High,
                TextSize = 27,
                Color = SKColors.White,
                TextAlign = SKTextAlign.Left,
                Typeface = Text.TypeFaces.BundleDisplayNameTypeface
            };

            int y = icon.HeaderHeight + 50;
            foreach (Quest q in icon.Quests)
            {
                DrawQuestBackground(c, icon, y, true);

                paint.TextSize = 27;
                paint.ImageFilter = null;
                paint.Color = SKColors.White;
                paint.TextAlign = SKTextAlign.Left;
                paint.Typeface = Text.TypeFaces.BundleDisplayNameTypeface;
                while (paint.MeasureText(q.Description) > icon.Width - 65 - 165)
                {
                    paint.TextSize -= 1;
                }
                c.DrawText(q.Description, new SKPoint(65, y + paint.TextSize + 11), paint);

                paint.TextSize = 16;
                paint.Color = SKColors.White.WithAlpha(200);
                paint.Typeface = Text.TypeFaces.BundleDefaultTypeface;
                c.DrawText(q.Count.ToString(), new SKPoint(93 + icon.Width - (175 * 3), y + 60), paint);

                if (q.Reward?.RewardIcon != null)
                {
                    if (q.Reward.IsCountShifted)
                    {
                        int l = q.Reward.RewardQuantity.ToString().Length;
                        paint.TextSize = l >= 5 ? 30 : 35;
                        paint.TextAlign = SKTextAlign.Right;
                        paint.Color = SKColor.Parse(q.Reward.RewardFillColor);
                        paint.ImageFilter = SKImageFilter.CreateDropShadow(0, 0, 5, 5, SKColor.Parse(q.Reward.RewardBorderColor).WithAlpha(200));
                        c.DrawText(q.Reward.RewardQuantity.ToString(), new SKPoint(icon.Width - 85, y + 47.5F), paint);
                        c.DrawBitmap(q.Reward.RewardIcon, new SKPoint(icon.Width - 30 - q.Reward.RewardIcon.Width, y + 12.5F),
                            new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High });
                    }
                    else
                        c.DrawBitmap(q.Reward.RewardIcon, new SKPoint(icon.Width - 125, y + 5),
                            new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High });
                }

                y += 95;
            }
        }

        public static void DrawCompletionRewards(SKCanvas c, BaseBundle icon)
        {
            using SKPaint paint = new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High,
                TextSize = 35,
                Color = SKColors.White,
                TextAlign = SKTextAlign.Left,
                Typeface = Text.TypeFaces.BundleDisplayNameTypeface
            };

            int y = icon.HeaderHeight + (50 * 2) + (95 * icon.Quests.Count);
            foreach (CompletionReward r in icon.CompletionRewards)
            {
                DrawQuestBackground(c, icon, y, false);

                paint.TextSize = 35;
                paint.ImageFilter = null;
                paint.Color = SKColors.White;
                paint.TextAlign = SKTextAlign.Left;
                paint.Typeface = Text.TypeFaces.BundleDisplayNameTypeface;
                while (paint.MeasureText(r.CompletionText) > icon.Width - 65 - 165)
                {
                    paint.TextSize -= 1;
                }
                c.DrawText(r.CompletionText, new SKPoint(65, y + paint.TextSize + 15), paint);

                if (r.Reward?.RewardIcon != null)
                {
                    if (r.Reward.IsCountShifted)
                    {
                        int l = r.Reward.RewardQuantity.ToString().Length;
                        paint.TextSize = l >= 5 ? 30 : 35;
                        paint.TextAlign = SKTextAlign.Right;
                        paint.Color = SKColor.Parse(r.Reward.RewardFillColor);
                        paint.Typeface = Text.TypeFaces.BundleDefaultTypeface;
                        paint.ImageFilter = SKImageFilter.CreateDropShadow(0, 0, 5, 5, SKColor.Parse(r.Reward.RewardBorderColor).WithAlpha(200));
                        c.DrawText(r.Reward.RewardQuantity.ToString(), new SKPoint(icon.Width - 85, y + 47.5F), paint);
                        c.DrawBitmap(r.Reward.RewardIcon, new SKPoint(icon.Width - 30 - r.Reward.RewardIcon.Width, y + 12.5F),
                            new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High });
                    }
                    else
                        c.DrawBitmap(r.Reward.RewardIcon, new SKPoint(icon.Width - 125, y + 5),
                            new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High });
                }

                y += 95;
            }
        }

        private static void DrawQuestBackground(SKCanvas c, BaseBundle icon, int y, bool hasSlider)
        {
            SKColor baseColor = icon.DisplayStyle.PrimaryColor;
            using SKPaint paint = new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High,
                Color = baseColor
            };
            using SKPath secondaryRect = new SKPath
            {
                FillType = SKPathFillType.EvenOdd
            };
            using SKPath selector = new SKPath
            {
                FillType = SKPathFillType.EvenOdd
            };
            using SKPath slider = new SKPath
            {
                FillType = SKPathFillType.EvenOdd
            };

            c.DrawRect(new SKRect(25, y, icon.Width - 25, y + 75), paint);

            baseColor.ToHsl(out float h, out float s, out float l);
            baseColor = SKColor.FromHsl(h, s, l + 5);
            paint.Color = baseColor;

            secondaryRect.MoveTo(32, y + 5);
            secondaryRect.LineTo(icon.Width - 155, y + 4);
            secondaryRect.LineTo(icon.Width - 175, y + 68);
            secondaryRect.LineTo(29, y + 71);
            secondaryRect.Close();
            c.DrawPath(secondaryRect, paint);

            paint.Color = icon.DisplayStyle.SecondaryColor;
            selector.MoveTo(41, y + 38);
            selector.LineTo(48, y + 34);
            selector.LineTo(52, y + 39);
            selector.LineTo(46, y + 44);
            selector.Close();
            c.DrawPath(selector, paint);

            if (hasSlider)
            {
                slider.MoveTo(65, y + 53);
                slider.LineTo(65 + icon.Width - (175 * 3), y + 53);
                slider.LineTo(65 + icon.Width - (175 * 3), y + 58);
                slider.LineTo(65, y + 58);
                slider.Close();
                c.DrawPath(slider, paint);

                paint.TextSize = 14;
                paint.Color = SKColors.White;
                paint.TextAlign = SKTextAlign.Left;
                paint.Typeface = Text.TypeFaces.BundleDefaultTypeface;
                c.DrawText("0 / ", new SKPoint(75 + icon.Width - (175 * 3), y + 59), paint);
            }
        }
    }
}
