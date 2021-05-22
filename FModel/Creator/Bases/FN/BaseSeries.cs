using CUE4Parse.UE4.Assets.Exports;
using SkiaSharp;

namespace FModel.Creator.Bases.FN
{
    public class BaseSeries : BaseIcon
    {
        public BaseSeries(UObject uObject, EIconStyle style) : base(uObject, style)
        {
        }

        public override void ParseForInfo()
        {
            GetSeries(Object);
        }

        public override SKImage Draw()
        {
            using var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var c = new SKCanvas(ret);

            DrawBackground(c);

            return SKImage.FromBitmap(ret);
        }
    }
}