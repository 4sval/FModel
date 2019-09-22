using FModel.Methods.Utilities;
using Newtonsoft.Json.Linq;
using SkiaSharp;
using System.IO;

namespace FModel.Methods.Assets.IconCreator
{
    class IconCreator
    {
        public static SKCanvas ICCanvas { get; set; }

        public static void DrawTest(JArray AssetProperties)
        {
            FWindow.FMain.Dispatcher.InvokeAsync(() =>
            {
                SKImageInfo imageInfo = new SKImageInfo(518, 518);
                using (SKSurface surface = SKSurface.Create(imageInfo))
                {
                    ICCanvas = surface.Canvas;

                    Rarity.DrawRarityBackground(AssetProperties);

                    using (SKImage image = surface.Snapshot())
                    using (SKData data = image.Encode(SKEncodedImageFormat.Png, 100))
                    using (MemoryStream mStream = new MemoryStream(data.ToArray()))
                    {
                        FWindow.FMain.ImageBox_Main.Source = ImagesUtility.GetImageSource(mStream);
                    }
                }
            });
        }
    }
}
