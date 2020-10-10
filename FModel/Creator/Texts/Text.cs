using System.Collections.Generic;
using FModel.Creator.Bases;
using PakReader.Pak;
using PakReader.Parsers.Class;
using PakReader.Parsers.Objects;
using PakReader.Parsers.PropertyTagData;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace FModel.Creator.Texts
{
    static class Text
    {
        public static Typefaces TypeFaces = new Typefaces();
        private const int _STARTER_TEXT_POSITION = 380;
        private static int _BOTTOM_TEXT_SIZE = 15;
        private static int _NAME_TEXT_SIZE = 45;

        public static string GetTextPropertyBase(TextProperty t)
        {
            if (t.Value is { } text)
                if (text.Text is FTextHistory.None n)
                    return n.CultureInvariantString;
                else if (text.Text is FTextHistory.Base b)
                    return b.SourceString.Replace("<Emphasized>", string.Empty).Replace("</>", string.Empty);
                else if (text.Text is FTextHistory.StringTableEntry s)
                {
                    PakPackage p = Utils.GetPropertyPakPackage(s.TableId.String);
                    if (p.HasExport() && !p.Equals(default))
                    {
                        var table = p.GetExport<UStringTable>();
                        if (table != null)
                        {
                            if (table.TryGetValue("StringTable", out var v1) && v1 is FStringTable stringTable &&
                                stringTable.KeysToMetadata.TryGetValue(stringTable.TableNamespace, out var v2) && v2 is Dictionary<string, string> dico &&
                                dico.TryGetValue(s.Key, out var ret))
                            {
                                return ret;
                            }
                        }
                    }
                }

            return string.Empty;
        }
        public static string GetTextPropertyBase(ArrayProperty a)
        {
            if (a.Value.Length > 0 && a.Value[0] is TextProperty t)
                return GetTextPropertyBase(t);
            return string.Empty;
        }

        public static (string, string, string) GetTextPropertyBases(TextProperty t)
        {
            if (t.Value is { } text && text.Text is FTextHistory.Base b)
                return (b.Namespace, b.Key, b.SourceString);
            return (string.Empty, string.Empty, string.Empty);
        }

        public static string GetMaxStackSize(StructProperty maxStackSize)
        {
            if (maxStackSize.Value is UObject o1)
            {
                if (o1.TryGetValue("Value", out var c) && c is FloatProperty value && value.Value != -1) // old way
                    return $"MaxStackSize : {value.Value}";

                if (
                    o1.TryGetValue("Curve", out var c1) && c1 is StructProperty curve && curve.Value is UObject o2 &&
                    o2.TryGetValue("CurveTable", out var c2) && c2 is ObjectProperty curveTable &&
                    o2.TryGetValue("RowName", out var c3) && c3 is NameProperty rowName) // new way
                {
                    PakPackage p = Utils.GetPropertyPakPackage(curveTable.Value.Resource.OuterIndex.Resource.ObjectName.String);
                    if (p.HasExport() && !p.Equals(default))
                    {
                        var table = p.GetExport<UCurveTable>();
                        if (table != null)
                        {
                            if (table.TryGetValue(rowName.Value.String, out var v1) && v1 is UObject maxStackAmount &&
                                maxStackAmount.TryGetValue("Keys", out var v2) && v2 is ArrayProperty keys &&
                                keys.Value.Length > 0 && (keys.Value[0] as StructProperty)?.Value is FSimpleCurveKey amount &&
                                amount.KeyValue != -1)
                            {
                                return $"MaxStackSize : {amount.KeyValue}";
                            }
                        }
                    }
                }
            }
            return string.Empty;
        }

        public static string GetXpRewardAmount(StructProperty xpRewardAmount)
        {
            if (xpRewardAmount.Value is UObject o1)
            {
                if (
                    o1.TryGetValue("Curve", out var c1) && c1 is StructProperty curve && curve.Value is UObject o2 &&
                    o2.TryGetValue("CurveTable", out var c2) && c2 is ObjectProperty curveTable &&
                    o2.TryGetValue("RowName", out var c3) && c3 is NameProperty rowName) // new way
                {
                    PakPackage p = Utils.GetPropertyPakPackage(curveTable.Value.Resource.OuterIndex.Resource.ObjectName.String);
                    if (p.HasExport() && !p.Equals(default))
                    {
                        var table = p.GetExport<UCurveTable>();
                        if (table != null)
                        {
                            if (table.TryGetValue(rowName.Value.String, out var v1) && v1 is UObject maxStackAmount &&
                                maxStackAmount.TryGetValue("Keys", out var v2) && v2 is ArrayProperty keys &&
                                keys.Value.Length > 0 && (keys.Value[0] as StructProperty)?.Value is FSimpleCurveKey amount &&
                                amount.KeyValue != -1)
                            {
                                return $"{amount.KeyValue} Xp";
                            }
                        }
                    }
                }
            }
            return string.Empty;
        }

        public static void DrawBackground(SKCanvas c, IBase icon)
        {
            switch ((EIconDesign)Properties.Settings.Default.AssetsIconDesign)
            {
                case EIconDesign.Flat:
                    {
                        var pathBottom = new SKPath { FillType = SKPathFillType.EvenOdd };
                        pathBottom.MoveTo(icon.Margin, icon.Height - icon.Margin);
                        pathBottom.LineTo(icon.Margin, icon.Height - icon.Margin - icon.Height / 17 * 2.5f);
                        pathBottom.LineTo(icon.Width - icon.Margin, icon.Height - icon.Margin - icon.Height / 17 * 4.5f);
                        pathBottom.LineTo(icon.Width - icon.Margin, icon.Height - icon.Margin);
                        pathBottom.Close();
                        c.DrawPath(pathBottom, new SKPaint
                        {
                            IsAntialias = true,
                            FilterQuality = SKFilterQuality.High,
                            Color = new SKColor(0, 0, 50, 75),
                        });
                        break;
                    }
                default:
                    {
                        c.DrawRect(
                            new SKRect(icon.Margin, _STARTER_TEXT_POSITION, icon.Width - icon.Margin, icon.Height - icon.Margin),
                            new SKPaint
                            {
                                IsAntialias = true,
                                FilterQuality = SKFilterQuality.High,
                                Color = new SKColor(0, 0, 50, 75),
                            });
                        break;
                    }
            }
        }

        public static void DrawDisplayName(SKCanvas c, IBase icon)
        {
            _NAME_TEXT_SIZE = 45;
            string text = icon.DisplayName;
            if (!string.IsNullOrEmpty(text))
            {
                SKTextAlign side = SKTextAlign.Center;
                int x = icon.Width / 2;
                int y = _STARTER_TEXT_POSITION + _NAME_TEXT_SIZE;
                switch ((EIconDesign)Properties.Settings.Default.AssetsIconDesign)
                {
                    case EIconDesign.Mini:
                        {
                            _NAME_TEXT_SIZE = 47;
                            text = text.ToUpperInvariant();
                            break;
                        }
                    case EIconDesign.Flat:
                        {
                            _NAME_TEXT_SIZE = 47;
                            side = SKTextAlign.Right;
                            x = icon.Width - icon.Margin * 2;
                            break;
                        }
                }

                SKPaint namePaint = new SKPaint
                {
                    IsAntialias = true,
                    FilterQuality = SKFilterQuality.High,
                    Typeface = TypeFaces.DisplayNameTypeface,
                    TextSize = _NAME_TEXT_SIZE,
                    Color = SKColors.White,
                    TextAlign = side
                };

                if ((ELanguage)Properties.Settings.Default.AssetsLanguage == ELanguage.Arabic)
                {
                    SKShaper shaper = new SKShaper(namePaint.Typeface);
                    float shapedTextWidth;

                    while (true)
                    {
                        SKShaper.Result shapedText = shaper.Shape(text, namePaint);
                        shapedTextWidth = shapedText.Points[^1].X + namePaint.TextSize / 2f;

                        if (shapedTextWidth > icon.Width - icon.Margin * 2)
                        {
                            namePaint.TextSize = _NAME_TEXT_SIZE -= 1;
                        }
                        else
                        {
                            break;
                        }
                    }

                    c.DrawShapedText(shaper, text, (icon.Width - shapedTextWidth) / 2f, y, namePaint);
                }
                else
                {
                    // resize if too long
                    while (namePaint.MeasureText(text) > icon.Width - icon.Margin * 2)
                    {
                        namePaint.TextSize = _NAME_TEXT_SIZE -= 1;
                    }

                    c.DrawText(text, x, y, namePaint);
                }
            }
        }

        public static void DrawDescription(SKCanvas c, IBase icon)
        {
            int maxLine = 4;
            _BOTTOM_TEXT_SIZE = 15;
            string text = icon.Description;
            ETextSide side = ETextSide.Center;
            switch ((EIconDesign)Properties.Settings.Default.AssetsIconDesign)
            {
                case EIconDesign.Mini:
                    {
                        maxLine = 5;
                        _BOTTOM_TEXT_SIZE = icon.Margin;
                        text = text.ToUpper();
                        break;
                    }
                case EIconDesign.Flat:
                    {
                        side = ETextSide.Right;
                        break;
                    }
            }

            SKPaint descriptionPaint = new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High,
                Typeface = TypeFaces.DescriptionTypeface,
                TextSize = 13,
                Color = SKColors.White,
            };
            
            // wrap if too long
            Helper.DrawCenteredMultilineText(c, text, maxLine, icon, side,
                new SKRect(icon.Margin, _STARTER_TEXT_POSITION + _NAME_TEXT_SIZE, icon.Width - icon.Margin, icon.Height - _BOTTOM_TEXT_SIZE),
                descriptionPaint);
        }

        public static void DrawToBottom(SKCanvas c, BaseIcon icon, ETextSide side, string text)
        {
            SKPaint shortDescriptionPaint = new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High,
                Typeface = side == ETextSide.Left ? TypeFaces.BottomDefaultTypeface ?? TypeFaces.DisplayNameTypeface : TypeFaces.BottomDefaultTypeface ?? TypeFaces.DefaultTypeface,
                TextSize = TypeFaces.BottomDefaultTypeface == null ? 15 : 13,
                Color = SKColors.White,
                TextAlign = side == ETextSide.Left ? SKTextAlign.Left : SKTextAlign.Right,
            };

            if (side == ETextSide.Left)
            {
                if ((ELanguage)Properties.Settings.Default.AssetsLanguage == ELanguage.Arabic)
                {
                    shortDescriptionPaint.TextSize -= 4f;
                    SKShaper shaper = new SKShaper(shortDescriptionPaint.Typeface);
                    c.DrawShapedText(shaper, text, icon.Margin * 2.5f, icon.Size - icon.Margin * 2.5f - shortDescriptionPaint.TextSize * .5f /* ¯\_(ツ)_/¯ */, shortDescriptionPaint);
                }
                else
                {
                    c.DrawText(text, icon.Margin * 2.5f, icon.Size - icon.Margin * 2.5f, shortDescriptionPaint);
                }
            }
            else
            {
                c.DrawText(text, icon.Size - icon.Margin * 2.5f, icon.Size - icon.Margin * 2.5f, shortDescriptionPaint);
            }
        }
    }
}
