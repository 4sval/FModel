using FModel.Properties;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace FModel
{
    static class Rarity
    {
        #region flat design
        private static void DrawFlatBackground(Graphics toDrawOn, Color background, Color backgroundUpDown, Color border)
        {
            toDrawOn.FillRectangle(new SolidBrush(background), new Rectangle(-1, -1, 522, 522));

            GraphicsPath p = new GraphicsPath();
            p.StartFigure();
            p.AddLine(4, 438, 517, 383);
            p.AddLine(517, 383, 517, 383 + 134);
            p.AddLine(4, 383 + 134, 4, 383 + 134);
            p.AddLine(4, 383 + 134, 4, 438);
            p.CloseFigure();
            toDrawOn.FillPath(new SolidBrush(backgroundUpDown), p);

            p.StartFigure();
            p.AddLine(4, 4, 4, 38);
            p.AddLine(4, 38, 325, 4);
            p.CloseFigure();
            toDrawOn.FillPath(new SolidBrush(backgroundUpDown), p);

            Pen pen = new Pen(border, 5);
            pen.Alignment = PenAlignment.Inset;
            toDrawOn.DrawRectangle(pen, new Rectangle(-1, -1, 522, 522));
        }

        private static void DrawSeriesBackground(JToken theItem, Graphics toDrawOn, string theSeries)
        {
            if (theSeries.Equals("MarvelSeries"))
            {
                DrawFlatBackground(toDrawOn, Color.FromArgb(255, 165, 29, 30), Color.FromArgb(255, 128, 22, 31), Color.FromArgb(255, 237, 52, 52));
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
                DrawFlatBackground(toDrawOn, Color.FromArgb(255, 109, 109, 109), Color.FromArgb(255, 84, 84, 84), Color.FromArgb(255, 144, 144, 144));
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
                    DrawFlatBackground(toDrawOn, Color.FromArgb(255, 155, 39, 69), Color.FromArgb(255, 120, 30, 60), Color.FromArgb(255, 223, 65, 104));
                    break;
                case "EFortRarity::Mythic":
                    DrawFlatBackground(toDrawOn, Color.FromArgb(255, 170, 143, 47), Color.FromArgb(255, 132, 105, 37), Color.FromArgb(255, 217, 192, 72));
                    break;
                case "EFortRarity::Legendary":
                    DrawFlatBackground(toDrawOn, Color.FromArgb(255, 170, 96, 47), Color.FromArgb(255, 132, 69, 37), Color.FromArgb(255, 217, 132, 72));
                    break;
                case "EFortRarity::Epic":
                case "EFortRarity::Quality":
                    DrawFlatBackground(toDrawOn, Color.FromArgb(255, 96, 47, 170), Color.FromArgb(255, 69, 37, 132), Color.FromArgb(255, 141, 67, 195));
                    break;
                case "EFortRarity::Rare":
                    DrawFlatBackground(toDrawOn, Color.FromArgb(255, 55, 92, 163), Color.FromArgb(255, 42, 77, 126), Color.FromArgb(255, 72, 121, 217));
                    break;
                case "EFortRarity::Common":
                    DrawFlatBackground(toDrawOn, Color.FromArgb(255, 109, 109, 109), Color.FromArgb(255, 84, 84, 84), Color.FromArgb(255, 144, 144, 144));
                    break;
                default:
                    DrawFlatBackground(toDrawOn, Color.FromArgb(255, 87, 155, 39), Color.FromArgb(255, 75, 120, 30), Color.FromArgb(255, 109, 219, 73));
                    break;
            }
        }
        #endregion

        #region default image
        /// <summary>
        /// check the rarity and return the right image
        /// </summary>
        /// <param name="theItem"></param>
        /// <returns> the resource image depending on the rarity </returns>
        private static Image GetRarityImage(JToken theItem)
        {
            JToken raritiesToken = theItem["Rarity"];
            switch (raritiesToken != null ? raritiesToken.Value<string>() : "")
            {
                case "EFortRarity::Transcendent":
                    return Resources.T512v1;
                case "EFortRarity::Mythic":
                    return Resources.M512v1;
                case "EFortRarity::Legendary":
                    return Resources.L512v1;
                case "EFortRarity::Epic":
                case "EFortRarity::Quality":
                    return Resources.E512v1;
                case "EFortRarity::Rare":
                    return Resources.R512v1;
                case "EFortRarity::Common":
                    return Resources.C512v1;
                default:
                    return Resources.U512v1;
            }
        }

        /// <summary>
        /// check the series and return the right image
        /// if no series known, return the normal item rarity image with GetRarityImage
        /// </summary>
        /// <param name="theItem"></param>
        /// <returns> the resource image depending on the series </returns>
        private static Image GetSeriesImage(JToken theItem, string theSeries)
        {
            if (theSeries.Equals("MarvelSeries"))
            {
                return Resources.Marvel512v1;
            }
            else
            {
                return GetRarityImage(theItem);
            }
        }

        /// <summary>
        /// if specialMode isn't null and is known return the right image
        /// if specialMode is unknown, return the normal item rarity image with GetRarityImage
        /// </summary>
        /// <param name="theItem"></param>
        /// <param name="specialMode"></param>
        /// <returns> the resource image depending on specialMode </returns>
        private static Image GetSpecialModeImage(JToken theItem, string specialMode)
        {
            if (specialMode == "ammo")
            {
                return Resources.C512v1;
            }
            else
            {
                return GetRarityImage(theItem);
            }
        }
        #endregion

        /// <summary>
        /// just draw the rarity
        /// </summary>
        /// <param name="theItem"></param>
        /// <param name="toDrawOn"></param>
        /// <param name="specialMode"></param>
        public static void DrawRarity(JToken theItem, Graphics toDrawOn, string specialMode = null)
        {
            JToken seriesToken = theItem["Series"];
            switch (Settings.Default.rarityDesign)
            {
                case "Flat":
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
                    break;
                default:
                    Image rarityBg;
                    if (!string.IsNullOrEmpty(seriesToken != null ? seriesToken.Value<string>() : ""))
                    {
                        rarityBg = GetSeriesImage(theItem, seriesToken.Value<string>());
                    }
                    else if (specialMode != null)
                    {
                        rarityBg = GetSpecialModeImage(theItem, specialMode);
                    }
                    else
                    {
                        rarityBg = GetRarityImage(theItem);
                    }
                    toDrawOn.DrawImage(rarityBg, new Point(0, 0));
                    break;

            }
        }
    }
}
