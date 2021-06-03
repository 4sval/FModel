using CUE4Parse.UE4.Assets.Exports;
using FModel.Creator.Bases.FN;
using SkiaSharp;

namespace FModel.Creator.Bases.SOD2
{
    public class BaseDecayIcon : BaseIcon
    {
        public BaseDecayIcon(UObject uObject, EIconStyle style) : base(uObject, style)
        {
        }
        
        // TODO
        //
        // ExtraLargeBackpackBase - for extra large
        // SmallBackpackBase - for small
        // ExtraSmallBackpackBase - for extra small
        // MediumBackpackBase - for medium
        // LargeBackpackBase - for large

        public override SKImage Draw()
        {
            using var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var c = new SKCanvas(ret);

            var bit = SKBitmap.Decode(@"C:\Users\GMatrixGames\Downloads\443546551013343242.png");
            c.DrawBitmap(bit, 0, 0);

            return SKImage.FromBitmap(ret);
        }
    }
}