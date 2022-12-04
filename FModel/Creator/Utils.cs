using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using CUE4Parse_Conversion.Textures;
using FModel.Framework;
using FModel.Extensions;
using FModel.Services;
using FModel.Settings;
using FModel.ViewModels;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace FModel.Creator;

public static class Utils
{
    private static ApplicationViewModel _applicationView => ApplicationService.ApplicationView;
    private static readonly Regex _htmlRegex = new("<.*?>");
    public static Typefaces Typefaces;

    public static string RemoveHtmlTags(string s)
    {
        var match = _htmlRegex.Match(s);
        while (match.Success)
        {
            s = s.Replace(match.Value, string.Empty);
            match = match.NextMatch();
        }

        return s;
    }

    public static bool TryGetDisplayAsset(UObject uObject, out SKBitmap preview)
    {
        if (uObject.TryGetValue(out FSoftObjectPath sidePanelIcon, "SidePanelIcon"))
        {
            preview = GetBitmap(sidePanelIcon);
            return preview != null;
        }

        var path = $"/Game/Catalog/MI_OfferImages/MI_{uObject.Name.Replace("Athena_Commando_", string.Empty)}";
        if (!TryLoadObject(path, out UMaterialInstanceConstant material)) // non-obfuscated item definition
        {
            if (!TryLoadObject($"{path[..path.LastIndexOf('_')]}_{path.SubstringAfterLast('_').ToLower()}", out material)) // Try to get MI with lowercase obfuscation
                TryLoadObject(path[..path.LastIndexOf('_')], out material); // hopefully gets obfuscated item definition
        }

        preview = GetBitmap(material);
        return preview != null;
    }

    public static SKBitmap GetBitmap(FPackageIndex packageIndex)
    {
        while (true)
        {
            if (!TryGetPackageIndexExport(packageIndex, out UObject export)) return null;
            switch (export)
            {
                case UTexture2D texture:
                    return GetBitmap(texture);
                case UMaterialInstanceConstant material:
                    return GetBitmap(material);
                default:
                {
                    if (export.TryGetValue(out FSoftObjectPath previewImage, "LargePreviewImage", "SmallPreviewImage")) return GetBitmap(previewImage);
                    if (export.TryGetValue(out string largePreview, "LargePreviewImage")) return GetBitmap(largePreview);
                    if (export.TryGetValue(out FPackageIndex smallPreview, "SmallPreviewImage"))
                    {
                        packageIndex = smallPreview;
                        continue;
                    }

                    return null;
                }
            }
        }
    }

    public static SKBitmap GetBitmap(UMaterialInstanceConstant material)
    {
        if (material == null) return null;
        foreach (var textureParameter in material.TextureParameterValues)
        {
            if (!textureParameter.ParameterValue.TryLoad<UTexture2D>(out var texture)) continue;
            switch (textureParameter.ParameterInfo.Name.Text)
            {
                case "MainTex":
                case "Texture":
                case "TextureA":
                case "TextureB":
                case "OfferImage":
                case "KeyArtTexture":
                case "NPC-Portrait":
                {
                    return GetBitmap(texture);
                }
            }
        }

        return null;
    }

    public static SKBitmap GetB64Bitmap(string b64) => SKBitmap.Decode(new MemoryStream(Convert.FromBase64String(b64)) { Position = 0 });
    public static SKBitmap GetBitmap(FSoftObjectPath softObjectPath) => GetBitmap(softObjectPath.AssetPathName.Text);
    public static SKBitmap GetBitmap(string fullPath) => TryLoadObject(fullPath, out UTexture2D texture) ? GetBitmap(texture) : null;
    public static SKBitmap GetBitmap(UTexture2D texture) => texture.IsVirtual ? null : texture.Decode(UserSettings.Default.OverridedPlatform);
    public static SKBitmap GetBitmap(byte[] data) => SKBitmap.Decode(data);

    public static SKBitmap ResizeWithRatio(this SKBitmap me, double width, double height)
    {
        var ratioX = width / me.Width;
        var ratioY = height / me.Height;
        return ResizeWithRatio(me, ratioX < ratioY ? ratioX : ratioY);
    }
    public static SKBitmap ResizeWithRatio(this SKBitmap me, double ratio)
    {
        return me.Resize(Convert.ToInt32(me.Width * ratio), Convert.ToInt32(me.Height * ratio));
    }

    public static SKBitmap Resize(this SKBitmap me, int size) => me.Resize(size, size);
    public static SKBitmap Resize(this SKBitmap me, int width, int height)
    {
        var bmp = new SKBitmap(new SKImageInfo(width, height), SKBitmapAllocFlags.ZeroPixels);
        using var pixmap = bmp.PeekPixels();
        me.ScalePixels(pixmap, SKFilterQuality.Medium);
        return bmp;
    }

    public static bool TryGetPackageIndexExport<T>(FPackageIndex packageIndex, out T export) where T : UObject
    {
        return packageIndex.TryLoad(out export);
    }

    // fullpath must be either without any extension or with the export objectname
    public static bool TryLoadObject<T>(string fullPath, out T export) where T : UObject
    {
        return _applicationView.CUE4Parse.Provider.TryLoadObject(fullPath, out export);
    }

    public static IEnumerable<UObject> LoadExports(string fullPath)
    {
        return _applicationView.CUE4Parse.Provider.LoadObjectExports(fullPath);
    }

    public static float GetMaxFontSize(double sectorSize, SKTypeface typeface, string text, float degreeOfCertainty = 1f, float maxFont = 100f)
    {
        var max = maxFont;
        var min = 0f;
        var last = -1f;
        float value;
        while (true)
        {
            value = min + ((max - min) / 2);
            using (SKFont ft = new SKFont(typeface, value))
            using (SKPaint paint = new SKPaint(ft))
            {
                if (paint.MeasureText(text) > sectorSize)
                {
                    last = value;
                    max = value;
                }
                else
                {
                    min = value;
                    if (Math.Abs(last - value) <= degreeOfCertainty)
                        return last;

                    last = value;
                }
            }
        }
    }

    public static string GetLocalizedResource(string @namespace, string key, string defaultValue)
    {
        return _applicationView.CUE4Parse.Provider.GetLocalizedString(@namespace, key, defaultValue);
    }
    public static string GetLocalizedResource<T>(T @enum) where T : Enum
    {
        var resource = _applicationView.CUE4Parse.Provider.GetLocalizedString("", @enum.GetDescription(), @enum.ToString());
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(resource.ToLower());
    }

    public static string GetFullPath(string partialPath)
    {
        var regex = new Regex(partialPath, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        foreach (var path in _applicationView.CUE4Parse.Provider.Files.Keys)
        {
            if (regex.IsMatch(path))
            {
                return path;
            }
        }

        return string.Empty;
    }

    public static string FixPath(string weirdPath) =>
        _applicationView.CUE4Parse.Provider.FixPath(weirdPath, StringComparison.Ordinal);

    public static void DrawCenteredMultilineText(SKCanvas c, string text, int maxCount, int size, int margin, SKTextAlign side, SKRect area, SKPaint paint)
    {
        var lineHeight = paint.TextSize * 1.2f;
        var lines = SplitLines(text, paint, area.Width - margin);

#if DEBUG
        c.DrawRect(new SKRect(area.Left, area.Top, area.Right, area.Bottom), new SKPaint { Color = SKColors.Red, IsStroke = true });
#endif

        if (lines == null) return;
        if (lines.Count <= maxCount) maxCount = lines.Count;
        var height = maxCount * lineHeight;
        var y = area.MidY - height / 2;

        var shaper = new CustomSKShaper(paint.Typeface);
        for (var i = 0; i < maxCount; i++)
        {
            var line = lines[i];
            if (line == null) continue;

            var lineText = line.Trim();
            var shapedText = shaper.Shape(lineText, paint);

            y += lineHeight;
            var x = side switch
            {
                SKTextAlign.Center => area.MidX - shapedText.Points[^1].X / 2,
                SKTextAlign.Right => size - margin - shapedText.Points[^1].X,
                SKTextAlign.Left => margin,
                _ => throw new NotImplementedException()
            };

            c.DrawShapedText(shaper, lineText, x, y, paint);
        }
    }

    public static void DrawMultilineText(SKCanvas c, string text, int size, int margin, SKTextAlign side, SKRect area, SKPaint paint, out float yPos)
    {
        yPos = area.Top;
        var lineHeight = paint.TextSize * 1.2f;
        var lines = SplitLines(text, paint, area.Width);
        if (lines == null) return;

        foreach (var line in lines)
        {
            var fontSize = GetMaxFontSize(area.Width, paint.Typeface, line);
            if (paint.TextSize > fontSize) // if the text is not fitting in the line decrease the font size (CKJ languages)
            {
                paint.TextSize = fontSize;
                lineHeight = paint.TextSize * 1.2f;
            }

            if (line == null) continue;
            var lineText = line.Trim();
            var shaper = new CustomSKShaper(paint.Typeface);
            var shapedText = shaper.Shape(lineText, paint);

            var x = side switch
            {
                SKTextAlign.Center => area.MidX - shapedText.Points[^1].X / 2,
                SKTextAlign.Right => size - margin - shapedText.Points[^1].X,
                SKTextAlign.Left => area.Left,
                _ => throw new NotImplementedException()
            };

            c.DrawShapedText(shaper, lineText, x, yPos, paint);
            yPos += lineHeight;
        }

#if DEBUG
        c.DrawRect(new SKRect(area.Left, area.Top - paint.TextSize, area.Right, yPos), new SKPaint { Color = SKColors.Red, IsStroke = true });
#endif
    }

    #region Chinese, Korean and Japanese text split

    // https://github.com/YoungjaeKim/mikan.sharp/blob/master/MikanSharp/Mikan/Mikan.cs

    static string joshi = @"(でなければ|について|かしら|くらい|けれど|なのか|ばかり|ながら|ことよ|こそ|こと|さえ|しか|した|たり|だけ|だに|だの|つつ|ても|てよ|でも|とも|から|など|なり|ので|のに|ほど|まで|もの|やら|より|って|で|と|な|に|ね|の|も|は|ば|へ|や|わ|を|か|が|さ|し|ぞ|て)";
    static string keywords = @"(\&nbsp;|[a-zA-Z0-9]+\.[a-z]{2,}|[一-龠々〆ヵヶゝ]+|[ぁ-んゝ]+|[ァ-ヴー]+|[a-zA-Z0-9]+|[ａ-ｚＡ-Ｚ０-９]+)";
    static string periods = @"([\.\,。、！\!？\?]+)$";
    static string bracketsBegin = @"([〈《「『｢（(\[【〔〚〖〘❮❬❪❨(<{❲❰｛❴])";
    static string bracketsEnd = @"([〉》」』｣)）\]】〕〗〙〛}>\)❩❫❭❯❱❳❵｝])";

    public static string[] SplitCKJText(string str)
    {
        var line1 = Regex.Split(str, keywords).ToList();
        var line2 = line1.SelectMany((o, _) => Regex.Split(o, joshi)).ToList();
        var line3 = line2.SelectMany((o, _) => Regex.Split(o, bracketsBegin)).ToList();
        var line4 = line3.SelectMany((o, _) => Regex.Split(o, bracketsEnd)).ToList();
        var words = line4.Where(o => !string.IsNullOrEmpty(o)).ToList();

        var prevType = string.Empty;
        var prevWord = string.Empty;
        List<string> result = new List<string>();

        words.ForEach(word =>
        {
            var token = Regex.IsMatch(word, periods) || Regex.IsMatch(word, joshi);

            if (Regex.IsMatch(word, bracketsBegin))
            {
                prevType = "braketBegin";
                prevWord = word;
                return;
            }

            if (Regex.IsMatch(word, bracketsEnd))
            {
                result[result.Count - 1] += word;
                prevType = "braketEnd";
                prevWord = word;
                return;
            }

            if (prevType == "braketBegin")
            {
                word = prevWord + word;
                prevWord = string.Empty;
                prevType = string.Empty;
            }

            // すでに文字が入っている上で助詞が続く場合は結合する
            if (result.Count > 0 && token && prevType == string.Empty)
            {
                result[result.Count - 1] += word;
                prevType = "keyword";
                prevWord = word;
                return;
            }

            // 単語のあとの文字がひらがななら結合する
            if (result.Count > 1 && token || (prevType == "keyword" && Regex.IsMatch(word, @"[ぁ-んゝ]+")))
            {
                result[result.Count - 1] += word;
                prevType = string.Empty;
                prevWord = word;
                return;
            }

            result.Add(word);
            prevType = "keyword";
            prevWord = word;
        });
        return result.ToArray();
    }

    #endregion

    public static List<string> SplitLines(string text, SKPaint paint, float maxWidth)
    {
        if (string.IsNullOrEmpty(text)) return null;

        var spaceWidth = paint.MeasureText(" ");
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var ret = new List<string>(lines.Length);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            float width = 0;
            var isCJK = false;
            var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (words.Length <= 1 && UserSettings.Default.AssetLanguage is ELanguage.Japanese or ELanguage.Korean or ELanguage.Chinese or ELanguage.TraditionalChinese)
            {
                words = SplitCKJText(line);
                isCJK = true;
            }

            var lineResult = new StringBuilder();
            foreach (var word in words)
            {
                var wordWidth = paint.MeasureText(word);
                var wordWithSpaceWidth = wordWidth + spaceWidth;
                var wordWithSpace = isCJK ? word : word + " ";

                if (width + wordWidth > maxWidth)
                {
                    ret.Add(lineResult.ToString());
                    lineResult = new StringBuilder(wordWithSpace);
                    width = wordWithSpaceWidth;
                }
                else
                {
                    lineResult.Append(wordWithSpace);
                    width += wordWithSpaceWidth;
                }
            }

            ret.Add(lineResult.ToString());
        }

        return ret;
    }
}
