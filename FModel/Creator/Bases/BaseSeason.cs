using FModel.Creator.Bundles;
using FModel.Creator.Texts;
using SkiaSharp;
using SkiaSharp.HarfBuzz;
using System;
using System.Collections.Generic;
using System.Linq;
using FModel.PakReader.Parsers.Class;
using FModel.PakReader.Parsers.PropertyTagData;

namespace FModel.Creator.Bases
{
    public class BaseSeason
    {
        public string DisplayName;
        public string FolderName;
        public string Watermark;
        public Reward FirstWinReward;
        public Dictionary<int, List<Reward>> BookXpSchedule;
        public Header DisplayStyle;
        public int Width = 1024;
        public int HeaderHeight = 261; // height is the header basically
        public int AdditionalSize = 50; // must be increased depending on the number of quests to draw

        public BaseSeason()
        {
            DisplayName = "";
            FolderName = "";
            Watermark = Properties.Settings.Default.ChallengeBannerWatermark;
            FirstWinReward = null;
            BookXpSchedule = new Dictionary<int, List<Reward>>();
            DisplayStyle = new Header();
        }

        public BaseSeason(IUExport export, string assetFolder) : this()
        {
            if (export.GetExport<TextProperty>("DisplayName") is TextProperty displayName)
                DisplayName = Text.GetTextPropertyBase(displayName);

            if (export.GetExport<StructProperty>("SeasonFirstWinRewards") is StructProperty s && s.Value is UObject seasonFirstWinRewards &&
                seasonFirstWinRewards.GetExport<ArrayProperty>("Rewards") is ArrayProperty rewards)
            {
                foreach (StructProperty reward in rewards.Value)
                {
                    if (reward.Value is UObject o &&
                        o.GetExport<SoftObjectProperty>("ItemDefinition") is SoftObjectProperty itemDefinition &&
                        o.GetExport<IntProperty>("Quantity") is IntProperty quantity)
                    {
                        FirstWinReward = new Reward(quantity, itemDefinition.Value);
                    }
                }
            }

            if (export.GetExport<StructProperty>("BookXpScheduleFree") is StructProperty r2 && r2.Value is UObject bookXpScheduleFree &&
                bookXpScheduleFree.GetExport<ArrayProperty>("Levels") is ArrayProperty levels2)
            {
                for (int i = 0; i < levels2.Value.Length; i++)
                {
                    BookXpSchedule[i] = new List<Reward>(); // init list for all reward index and once
                    if (levels2.Value[i] is StructProperty level && level.Value is UObject l &&
                        l.GetExport<ArrayProperty>("Rewards") is ArrayProperty elRewards && elRewards.Value.Length > 0)
                    {
                        foreach (StructProperty reward in elRewards.Value)
                        {
                            if (reward.Value is UObject o &&
                                o.GetExport<SoftObjectProperty>("ItemDefinition") is SoftObjectProperty itemDefinition &&
                                !itemDefinition.Value.AssetPathName.String.StartsWith("/Game/Items/Tokens/") &&
                                !itemDefinition.Value.AssetPathName.String.StartsWith("/BattlepassS15/Items/Tokens/") &&
                                o.GetExport<IntProperty>("Quantity") is IntProperty quantity)
                            {
                                BookXpSchedule[i].Add(new Reward(quantity, itemDefinition.Value));
                            }
                        }
                    }
                }
            }

            if (export.GetExport<StructProperty>("BookXpSchedulePaid") is StructProperty r1 && r1.Value is UObject bookXpSchedulePaid &&
                bookXpSchedulePaid.GetExport<ArrayProperty>("Levels") is ArrayProperty levels1)
            {
                for (int i = 0; i < levels1.Value.Length; i++)
                {
                    if (levels1.Value[i] is StructProperty level && level.Value is UObject l &&
                        l.GetExport<ArrayProperty>("Rewards") is ArrayProperty elRewards && elRewards.Value.Length > 0)
                    {
                        foreach (StructProperty reward in elRewards.Value)
                        {
                            if (reward.Value is UObject o &&
                                o.GetExport<SoftObjectProperty>("ItemDefinition") is SoftObjectProperty itemDefinition &&
                                //!itemDefinition.Value.AssetPathName.String.StartsWith("/Game/Items/Tokens/") &&
                                o.GetExport<IntProperty>("Quantity") is IntProperty quantity)
                            {
                                BookXpSchedule[i].Add(new Reward(quantity, itemDefinition.Value));
                            }
                        }
                    }
                }
            }

            FolderName = assetFolder;
            AdditionalSize += 100 * (BookXpSchedule.Count / 10);
        }

        public void Draw(SKCanvas c)
        {
            DrawHeaderPaint(c);
            DrawHeaderText(c);

            using SKPaint paint = new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High,
                TextSize = 15,
                Color = SKColors.White,
                TextAlign = SKTextAlign.Center,
                Typeface = Text.TypeFaces.BottomDefaultTypeface ?? Text.TypeFaces.DisplayNameTypeface
            };
            using SKPaint bg = new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High,
                Color = SKColor.Parse("#0F5CAF"),
            };
            using SKPaint rarity = new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High,
                Color = SKColors.White,
            };
            using SKPaint icon = new SKPaint
            {
                FilterQuality = SKFilterQuality.High,
                IsAntialias = true
            };

            int y = HeaderHeight + 50;
            int defaultSize = 80;
            int x = 20;
            foreach (var (index, reward) in BookXpSchedule)
            {
                if (index == 0 || reward.Count == 0)
                    continue;

                c.DrawText(index.ToString(), new SKPoint(x + (defaultSize / 2), y - 5), paint);

                var theReward = reward[0].TheReward;
                if (theReward == null)
                    continue;

                rarity.Color = theReward.RarityBackgroundColors[0];
                c.DrawRect(new SKRect(x, y, x + defaultSize, y + defaultSize), bg);
                c.DrawBitmap(reward[0].RewardIcon, new SKPoint(x, y), icon);
                var pathBottom = new SKPath { FillType = SKPathFillType.EvenOdd };
                pathBottom.MoveTo(x, y + defaultSize);
                pathBottom.LineTo(x, y + defaultSize - (defaultSize / 25 * 2.5f));
                pathBottom.LineTo(x + defaultSize, y + defaultSize - (defaultSize / 25 * 4.5f));
                pathBottom.LineTo(x + defaultSize, y + defaultSize);
                pathBottom.Close();
                c.DrawPath(pathBottom, rarity);

                if (index != 1 && index % 10 == 0)
                {
                    y += defaultSize + 20;
                    x = 20;
                }
                else
                {
                    x += defaultSize + 20;
                }
            }
        }

        private void DrawHeaderText(SKCanvas c)
        {
            using SKPaint paint = new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High,
                Typeface = Text.TypeFaces.BundleDisplayNameTypeface,
                TextSize = 50,
                Color = SKColors.White,
                TextAlign = SKTextAlign.Left,
            };

            string text = DisplayName.ToUpper();
            int x = 300;
            if ((ELanguage)Properties.Settings.Default.AssetsLanguage == ELanguage.Arabic)
            {
                
                SKShaper shaper = new SKShaper(paint.Typeface);
                float shapedTextWidth;

                while (true)
                {
                    SKShaper.Result shapedText = shaper.Shape(text, paint);
                    shapedTextWidth = shapedText.Points[^1].X + paint.TextSize / 2f;

                    if (shapedTextWidth > Width)
                    {
                        paint.TextSize -= 2;
                    }
                    else
                    {
                        break;
                    }
                }
                //only trigger the fix if The Last char is a digit
               if (char.IsDigit(text[text.Length - 1]))
                {
                    int s = text.Count(k => Char.IsDigit(k));
                    float numberwidth = paint.MeasureText(text.Substring(text.Length - s));

                    //Draw Number Separately 
                    c.DrawShapedText(shaper, text.Substring(text.Length - s), x, 155, paint);

                    c.DrawShapedText(shaper, text.Substring(0, text.Length - s), x + numberwidth, 155, paint);
                }  
               else
                {
                    //feels bad man 
                    c.DrawShapedText(shaper, text, x, 155, paint);

                }

            }
            else
            {
                while (paint.MeasureText(text) > (Width - x))
                {
                    paint.TextSize -= 2;
                }
                c.DrawText(text, x, 155, paint);
            }

            paint.Color = SKColors.White.WithAlpha(150);
            paint.TextAlign = SKTextAlign.Right;
            paint.TextSize = 23;
            paint.Typeface = Text.TypeFaces.DefaultTypeface;
            c.DrawText(Watermark
                .Replace("{BundleName}", text)
                .Replace("{Date}", DateTime.Now.ToString("dd/MM/yyyy")),
                Width - 25, HeaderHeight - 40, paint);

            paint.Typeface = Text.TypeFaces.BundleDefaultTypeface;
            paint.Color = DisplayStyle.SecondaryColor;
            paint.TextAlign = SKTextAlign.Left;
            paint.TextSize = 30;
            c.DrawText(FolderName.ToUpper(), x, 95, paint);
        }

        private void DrawHeaderPaint(SKCanvas c)
        {
            c.DrawRect(new SKRect(0, 0, Width, HeaderHeight), new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High,
                Color = DisplayStyle.PrimaryColor
            });

            if (DisplayStyle.CustomBackground != null && DisplayStyle.CustomBackground.Height != DisplayStyle.CustomBackground.Width)
            {
                var bgPaint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High, BlendMode = SKBlendMode.Screen };
                if (Properties.Settings.Default.UseChallengeBanner) bgPaint.Color = SKColors.Transparent.WithAlpha((byte)Properties.Settings.Default.ChallengeBannerOpacity);
                c.DrawBitmap(DisplayStyle.CustomBackground, new SKRect(0, 0, 1024, 256), bgPaint);
            }
            else if (DisplayStyle.DisplayImage != null)
            {
                if (DisplayStyle.CustomBackground != null && DisplayStyle.CustomBackground.Height == DisplayStyle.CustomBackground.Width)
                    c.DrawBitmap(DisplayStyle.CustomBackground, new SKRect(0, 0, HeaderHeight, HeaderHeight),
                        new SKPaint
                        {
                            IsAntialias = true,
                            FilterQuality = SKFilterQuality.High,
                            BlendMode = SKBlendMode.Screen,
                            ImageFilter = SKImageFilter.CreateDropShadow(2.5F, 0, 20, 0, DisplayStyle.SecondaryColor.WithAlpha(25))
                        });

                c.DrawBitmap(DisplayStyle.DisplayImage, new SKRect(0, 0, HeaderHeight, HeaderHeight),
                    new SKPaint
                    {
                        IsAntialias = true,
                        FilterQuality = SKFilterQuality.High,
                        ImageFilter = SKImageFilter.CreateDropShadow(-2.5F, 0, 20, 0, DisplayStyle.SecondaryColor.WithAlpha(50))
                    });
            }

            if (FirstWinReward != null)
            {
                c.DrawBitmap(FirstWinReward.TheReward.IconImage.Resize(HeaderHeight, HeaderHeight), new SKPoint(0, 0), new SKPaint
                {
                    FilterQuality = SKFilterQuality.High,
                    IsAntialias = true
                });
            }

            SKPath pathTop = new SKPath { FillType = SKPathFillType.EvenOdd };
            pathTop.MoveTo(0, HeaderHeight);
            pathTop.LineTo(Width, HeaderHeight);
            pathTop.LineTo(Width, HeaderHeight - 19);
            pathTop.LineTo(Width / 2 + 7, HeaderHeight - 23);
            pathTop.LineTo(Width / 2 + 13, HeaderHeight - 7);
            pathTop.LineTo(0, HeaderHeight - 19);
            pathTop.Close();
            c.DrawPath(pathTop, new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High,
                Color = DisplayStyle.SecondaryColor,
                ImageFilter = SKImageFilter.CreateDropShadow(-5, -5, 0, 0, DisplayStyle.AccentColor.WithAlpha(75))
            });

            c.DrawRect(new SKRect(0, HeaderHeight, Width, HeaderHeight + AdditionalSize), new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High,
                Color = DisplayStyle.PrimaryColor.WithAlpha(200) // default background is black, so i'm kinda lowering the brightness here and that's what i want
            });
        }
    }
}
