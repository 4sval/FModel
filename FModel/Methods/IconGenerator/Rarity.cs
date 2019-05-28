using FModel.Parser.Items;
using FModel.Properties;
using System.Drawing;

namespace FModel
{
    class Rarity
    {
        /// <summary>
        /// check the rarity and return the right image
        /// </summary>
        /// <param name="theItem"></param>
        /// <returns> the resource image depending on the rarity </returns>
        private static Image GetRarityImage(ItemsIdParser theItem)
        {
            switch (theItem.Rarity)
            {
                case "EFortRarity::Transcendent":
                    return Resources.T512;
                case "EFortRarity::Mythic":
                    return Resources.M512;
                case "EFortRarity::Legendary":
                    return Resources.L512;
                case "EFortRarity::Epic":
                case "EFortRarity::Quality":
                    return Resources.E512;
                case "EFortRarity::Rare":
                    return Resources.R512;
                case "EFortRarity::Common":
                    return Resources.C512;
                default:
                    return Resources.U512;
            }
        }

        /// <summary>
        /// check the series and return the right image
        /// if no series known, return the normal item rarity image with GetRarityImage
        /// </summary>
        /// <param name="theItem"></param>
        /// <returns> the resource image depending on the series </returns>
        private static Image GetSeriesImage(ItemsIdParser theItem)
        {
            if (theItem.Series == "MarvelSeries")
            {
                return Resources.Marvel512;
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
        private static Image GetSpecialModeImage(ItemsIdParser theItem, string specialMode)
        {
            if (specialMode == "ammo")
            {
                return Resources.C512;
            }
            else
            {
                return GetRarityImage(theItem);
            }
        }

        /// <summary>
        /// just draw the rarity
        /// </summary>
        /// <param name="theItem"></param>
        /// <param name="toDrawOn"></param>
        /// <param name="specialMode"></param>
        public static void DrawRarity(ItemsIdParser theItem, Graphics toDrawOn, string specialMode = null)
        {
            Image rarityBg;

            if (theItem.Series != null)
            {
                rarityBg = GetSeriesImage(theItem);
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
        }
    }
}
