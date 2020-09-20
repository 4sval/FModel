using SkiaSharp;

namespace FModel.Creator.Bases
{
    interface IBase
    {
        SKBitmap FallbackImage { get; }
        SKBitmap IconImage { get; }
        SKColor[] RarityBackgroundColors { get; }
        SKColor[] RarityBorderColor { get; }
        string DisplayName { get; }
        string Description { get; }
        int Width { get; }
        int Height { get; }
        int Margin { get; }
    }
}
