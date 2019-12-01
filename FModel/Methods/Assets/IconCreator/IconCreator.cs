using FModel.Methods.Assets.IconCreator.ChallengeID;
using FModel.Methods.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using FProp = FModel.Properties.Settings;

namespace FModel.Methods.Assets.IconCreator
{
    static class IconCreator
    {
        public static DrawingContext ICDrawingContext { get; set; }
        public static double PPD { get; set; }

        public static DrawingVisual DrawNormalIconKThx(JArray AssetProperties)
        {
            FoldersUtility.CheckWatermark();

            new UpdateMyProcessEvents("Creating Icon...", "Waiting").Update();

            DrawingVisual drawingVisual = new DrawingVisual();
            PPD = VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip;
            using (ICDrawingContext = drawingVisual.RenderOpen())
            {
                //INITIALIZATION
                ICDrawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(new Point(0, 0), new Size(515, 515)));

                Rarity.DrawRarityBackground(AssetProperties);
                DebugHelper.WriteLine("DefaultIconCreation: Icon rarity done for {0}", FWindow.FCurrentAsset);

                IconImage.DrawIconImage(AssetProperties, FProp.Default.FIsFeatured);
                DebugHelper.WriteLine("DefaultIconCreation: Icon image done for {0}", FWindow.FCurrentAsset);

                IconText.DrawIconText(AssetProperties);
                DebugHelper.WriteLine("DefaultIconCreation: Icon text done for {0}", FWindow.FCurrentAsset);

                IconWatermark.DrawIconWatermark();
            }

            new UpdateMyProcessEvents("Done", "Success").Update();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            return drawingVisual;
        }

        public static DrawingVisual DrawChallengeKThx(JArray AssetProperties, string path)
        {
            FoldersUtility.CheckWatermark();
            new UpdateMyProcessEvents("Creating Challenges Icon...", "Waiting").Update();

            DrawingVisual drawingVisual = new DrawingVisual();
            PPD = VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip;
            using (ICDrawingContext = drawingVisual.RenderOpen())
            {
                //INITIALIZATION
                ICDrawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(new Point(0, 0), new Size(1024, 300)));

                ChallengeBundleInfos.GetBundleData(AssetProperties);
                DebugHelper.WriteLine("BChallengesIconCreation: Bundle data has been gathered for {0}", FWindow.FCurrentAsset);

                new UpdateMyProcessEvents("Drawing Quests Informations...", "Waiting").Update();
                ChallengeIconDesign.DrawChallenge(AssetProperties, new DirectoryInfo(path).Parent.Name.ToUpperInvariant());
                DebugHelper.WriteLine("BChallengesIconCreation: Bundle image has been drawn for {0}", FWindow.FCurrentAsset);
            }

            new UpdateMyProcessEvents("Done", "Success").Update();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            return drawingVisual;
        }
    }
}
