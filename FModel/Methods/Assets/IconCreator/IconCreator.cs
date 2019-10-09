using Newtonsoft.Json.Linq;
using System;
using System.Windows;
using System.Windows.Media;
using FProp = FModel.Properties.Settings;

namespace FModel.Methods.Assets.IconCreator
{
    static class IconCreator
    {
        public static DrawingContext ICDrawingContext { get; set; }
        public static double PPD { get; set; }

        public static DrawingVisual DrawTest(JArray AssetProperties)
        {
            DrawingVisual drawingVisual = new DrawingVisual();
            PPD = VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip;
            using (ICDrawingContext = drawingVisual.RenderOpen())
            {
                //INITIALIZATION
                ICDrawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(new Point(0, 0), new Size(515, 515)));

                Rarity.DrawRarityBackground(AssetProperties);
                IconImage.DrawIconImage(AssetProperties, FProp.Default.FIsFeatured);
                IconText.DrawIconText(AssetProperties);
                IconWatermark.DrawIconWatermark();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            return drawingVisual;
        }
    }
}
