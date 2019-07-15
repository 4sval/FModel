using FModel.Parser.Items;
using FModel.Properties;
using System.Drawing;

namespace FModel
{
    static class Rarity
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
                    return Settings.Default.rarityNew ? Resources.T512 : Resources.T512v1;
                case "EFortRarity::Mythic":
                    return Settings.Default.rarityNew ? Resources.M512 : Resources.M512v1;
                case "EFortRarity::Legendary":
                    return Settings.Default.rarityNew ? Resources.L512 : Resources.L512v1;
                case "EFortRarity::Epic":
                case "EFortRarity::Quality":
                    return Settings.Default.rarityNew ? Resources.E512 : Resources.E512v1;
                case "EFortRarity::Rare":
                    return Settings.Default.rarityNew ? Resources.R512 : Resources.R512v1;
                case "EFortRarity::Common":
                    return Settings.Default.rarityNew ? Resources.C512 : Resources.C512v1;
                default:
                    return Settings.Default.rarityNew ? Resources.U512 : Resources.U512v1;
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
                return Settings.Default.rarityNew ? Resources.Marvel512 : Resources.Marvel512v1;
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
                return Settings.Default.rarityNew ? Resources.C512 : Resources.C512v1;
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
