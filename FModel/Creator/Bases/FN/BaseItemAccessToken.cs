using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Framework;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace FModel.Creator.Bases.FN;

public class BaseItemAccessToken : UCreator
{
    private readonly SKBitmap _locked, _unlocked;
    private string _unlockedDescription, _exportName;
    private BaseIcon _icon;

    public BaseItemAccessToken(UObject uObject, EIconStyle style) : base(uObject, style)
    {
        _unlocked = Utils.GetBitmap("FortniteGame/Content/UI/Foundation/Textures/Icons/Locks/T-Icon-Unlocked-128.T-Icon-Unlocked-128").Resize(24);
        _locked = Utils.GetBitmap("FortniteGame/Content/UI/Foundation/Textures/Icons/Locks/T-Icon-Lock-128.T-Icon-Lock-128").Resize(24);
    }

    public override void ParseForInfo()
    {
        if (Object.TryGetValue(out FPackageIndex accessItem, "access_item") &&
            Utils.TryGetPackageIndexExport(accessItem, out UObject uObject))
        {
            _exportName = uObject.Name;
            _icon = new BaseIcon(uObject, EIconStyle.Default);
            _icon.ParseForReward(false);
        }

        if (Object.TryGetValue(out FText displayName, "DisplayName") && displayName.Text != "TBD")
            DisplayName = displayName.Text;
        else
            DisplayName = _icon?.DisplayName;

        Description = Object.TryGetValue(out FText description, "Description") ? description.Text : _icon?.Description;
        if (Object.TryGetValue(out FText unlockDescription, "UnlockDescription")) _unlockedDescription = unlockDescription.Text;
    }

    public override SKBitmap[] Draw()
    {
        var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var c = new SKCanvas(ret);

        switch (Style)
        {
            case EIconStyle.NoBackground:
                Preview = _icon.Preview;
                DrawPreview(c);
                break;
            case EIconStyle.NoText:
                Preview = _icon.Preview;
                _icon.DrawBackground(c);
                DrawPreview(c);
                break;
            default:
                _icon.DrawBackground(c);
                DrawInformation(c);
                DrawToBottom(c, SKTextAlign.Right, _exportName);
                break;
        }

        return new[] { ret };
    }

    private void DrawInformation(SKCanvas c)
    {
        var size = 45;
        var left = Width / 2;

        while (DisplayNamePaint.MeasureText(DisplayName) > Width - _icon.Margin * 2)
        {
            DisplayNamePaint.TextSize = size -= 2;
        }

        var shaper = new CustomSKShaper(DisplayNamePaint.Typeface);
        var shapedText = shaper.Shape(DisplayName, DisplayNamePaint);
        c.DrawShapedText(shaper, DisplayName, left - shapedText.Points[^1].X / 2, _icon.Margin * 8 + size, DisplayNamePaint);

        float topBase = _icon.Margin + size * 2;
        if (!string.IsNullOrEmpty(_unlockedDescription))
        {
            c.DrawBitmap(_locked, new SKRect(50, topBase, 50 + _locked.Width, topBase + _locked.Height), ImagePaint);
            Utils.DrawMultilineText(c, _unlockedDescription, Width, _icon.Margin, SKTextAlign.Left,
                new SKRect(70 + _locked.Width, topBase + 10, Width - 50, 256), DescriptionPaint, out topBase);
        }

        if (!string.IsNullOrEmpty(Description))
        {
            c.DrawBitmap(_unlocked, new SKRect(50, topBase, 50 + _unlocked.Width, topBase + _unlocked.Height), ImagePaint);
            Utils.DrawMultilineText(c, Description, Width, _icon.Margin, SKTextAlign.Left,
                new SKRect(70 + _unlocked.Width, topBase + 10, Width - 50, 256), DescriptionPaint, out topBase);
        }

        var h = Width - _icon.Margin - topBase;
        c.DrawBitmap(_icon.Preview ?? _icon.DefaultPreview, new SKRect(left - h / 2, topBase, left + h / 2, Width - _icon.Margin), ImagePaint);
    }
}