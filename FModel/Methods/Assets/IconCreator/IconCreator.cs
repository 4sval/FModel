using FModel.Methods.Utilities;
using Newtonsoft.Json.Linq;
using SkiaSharp;
using System.IO;
using System.Windows.Media;

namespace FModel.Methods.Assets.IconCreator
{
    static class IconCreator
    {
        public static SKCanvas ICCanvas { get; set; }

        public static ImageSource DrawTest(JArray AssetProperties)
        {
            SKImageInfo imageInfo = new SKImageInfo(515, 515);
            using (SKSurface surface = SKSurface.Create(imageInfo))
            {
                ICCanvas = surface.Canvas;

                Rarity.DrawRarityBackground(AssetProperties);
                IconImage.DrawIconImage(AssetProperties);
                IconText.DrawIconText(AssetProperties);

                using (SKImage image = surface.Snapshot())
                using (SKData data = image.Encode(SKEncodedImageFormat.Png, 100))
                using (MemoryStream mStream = new MemoryStream(data.ToArray()))
                {
                    return ImagesUtility.GetImageSource(mStream);
                }
            }
        }
    }
}
