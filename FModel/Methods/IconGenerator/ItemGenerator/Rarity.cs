using FModel.Properties;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace FModel
{
    static class Rarity
    {
        private static void DrawBackground(Graphics toDrawOn, Color background, Color backgroundUpDown, Color border)
        {
            Pen pen = null;
            GraphicsPath p = null;
            PathGradientBrush pthGrBrush = null;
            Color[] colors = { backgroundUpDown };

            switch (Settings.Default.rarityDesign)
            {
                case "Flat":
                    toDrawOn.FillRectangle(new SolidBrush(background), new Rectangle(-1, -1, 522, 522));

                    p = new GraphicsPath();
                    p.StartFigure();
                    p.AddLine(4, 438, 517, 383);
                    p.AddLine(517, 383, 517, 383 + 134);
                    p.AddLine(4, 383 + 134, 4, 383 + 134);
                    p.AddLine(4, 383 + 134, 4, 438);
                    p.CloseFigure();
                    toDrawOn.FillPath(new SolidBrush(Utilities.ChangeLightness(background, (float) 0.8)), p);

                    p.StartFigure();
                    p.AddLine(4, 4, 4, 38);
                    p.AddLine(4, 38, 325, 4);
                    p.CloseFigure();
                    toDrawOn.FillPath(new SolidBrush(Utilities.ChangeLightness(background, (float) 0.8)), p);

                    pen = new Pen(border, 5);
                    pen.Alignment = PenAlignment.Inset;
                    toDrawOn.DrawRectangle(pen, new Rectangle(-1, -1, 522, 522));
                    break;
                case "Default":
                    p = new GraphicsPath();
                    p.AddEllipse(-266, -266, 1044, 1044);

                    pthGrBrush = new PathGradientBrush(p);
                    pthGrBrush.CenterColor = background;
                    pthGrBrush.SurroundColors = colors;

                    toDrawOn.FillEllipse(pthGrBrush, -266, -266, 1044, 1044);

                    pen = new Pen(border, 5);
                    pen.Alignment = PenAlignment.Inset;
                    toDrawOn.DrawRectangle(pen, new Rectangle(-1, -1, 522, 522));
                    break;
            }
        }

        private static void DrawSeriesBackground(JToken theItem, Graphics toDrawOn, string theSeries)
        {
            if (theSeries.Equals("MarvelSeries"))
            {
                DrawBackground(toDrawOn, Color.FromArgb(255, 203, 35, 45), Color.FromArgb(255, 127, 14, 29), Color.FromArgb(255, 255, 67, 61));
            }
            else
            {
                DrawRarityBackground(theItem, toDrawOn);
            }
        }

        private static void DrawSpecialModeBackground(JToken theItem, Graphics toDrawOn, string specialMode)
        {
            if (specialMode == "ammo")
            {
                DrawBackground(toDrawOn, Color.FromArgb(255, 109, 109, 109), Color.FromArgb(255, 70, 70, 70), Color.FromArgb(255, 158, 158, 158));
            }
            else
            {
                DrawRarityBackground(theItem, toDrawOn);
            }
        }

        /// <summary>
        /// check the rarity and draw the right colors
        /// </summary>
        /// <param name="theItem"></param>
        /// <param name="toDrawOn"></param>
        /// <param name="specialMode"></param>
        private static void DrawRarityBackground(JToken theItem, Graphics toDrawOn)
        {
            JToken raritiesToken = theItem["Rarity"];
            switch (raritiesToken != null ? raritiesToken.Value<string>() : "")
            {
                case "EFortRarity::Transcendent":
                    DrawBackground(toDrawOn, Color.FromArgb(255, 213, 25, 68), Color.FromArgb(255, 134, 7, 45), Color.FromArgb(255, 255, 63, 88));
                    break;
                case "EFortRarity::Mythic":
                    DrawBackground(toDrawOn, Color.FromArgb(255, 186, 156, 54), Color.FromArgb(255, 115, 88, 26), Color.FromArgb(255, 238, 217, 81));
                    break;
                case "EFortRarity::Legendary":
                    DrawBackground(toDrawOn, Color.FromArgb(255, 192, 106, 56), Color.FromArgb(255, 115, 51, 26), Color.FromArgb(255, 236, 150, 80));
                    break;
                case "EFortRarity::Epic":
                case "EFortRarity::Quality":
                    DrawBackground(toDrawOn, Color.FromArgb(255, 129, 56, 194), Color.FromArgb(255, 66, 26, 115), Color.FromArgb(255, 178, 81, 237));
                    break;
                case "EFortRarity::Rare":
                    DrawBackground(toDrawOn, Color.FromArgb(255, 54, 105, 187), Color.FromArgb(255, 26, 68, 115), Color.FromArgb(255, 81, 128, 238));
                    break;
                case "EFortRarity::Common":
                    DrawBackground(toDrawOn, Color.FromArgb(255, 109, 109, 109), Color.FromArgb(255, 70, 70, 70), Color.FromArgb(255, 158, 158, 158));
                    break;
                default:
                    DrawBackground(toDrawOn, Color.FromArgb(255, 94, 188, 54), Color.FromArgb(255, 60, 115, 26), Color.FromArgb(255, 116, 239, 82));
                    break;
            }
        }

        /// <summary>
        /// just draw the rarity
        /// </summary>
        /// <param name="theItem"></param>
        /// <param name="toDrawOn"></param>
        /// <param name="specialMode"></param>
        public static void DrawRarity(JToken theItem, Graphics toDrawOn, string specialMode = null)
        {
            JToken seriesToken = theItem["Series"];
            if (!string.IsNullOrEmpty(seriesToken != null ? seriesToken.Value<string>() : ""))
            {
                DrawSeriesBackground(theItem, toDrawOn, seriesToken.Value<string>());
            }
            else if (specialMode != null)
            {
                DrawSpecialModeBackground(theItem, toDrawOn, specialMode);
            }
            else
            {
                DrawRarityBackground(theItem, toDrawOn);
            }
        }
    }
}
