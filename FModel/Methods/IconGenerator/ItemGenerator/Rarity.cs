using FModel.Properties;
using Newtonsoft.Json.Linq;
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
        private static Image GetRarityImage(JToken theItem)
        {
            JToken raritiesToken = theItem["Rarity"];
            switch (raritiesToken != null ? raritiesToken.Value<string>() : "")
            {
                case "EFortRarity::Transcendent":
                    return string.Equals(Settings.Default.rarityDesign, "Flat") ? Resources.T512 : Resources.T512v1;
                case "EFortRarity::Mythic":
                    return string.Equals(Settings.Default.rarityDesign, "Flat") ? Resources.M512 : Resources.M512v1;
                case "EFortRarity::Legendary":
                    return string.Equals(Settings.Default.rarityDesign, "Flat") ? Resources.L512 : Resources.L512v1;
                case "EFortRarity::Epic":
                case "EFortRarity::Quality":
                    return string.Equals(Settings.Default.rarityDesign, "Flat") ? Resources.E512 : Resources.E512v1;
                case "EFortRarity::Rare":
                    return string.Equals(Settings.Default.rarityDesign, "Flat") ? Resources.R512 : Resources.R512v1;
                case "EFortRarity::Common":
                    return string.Equals(Settings.Default.rarityDesign, "Flat") ? Resources.C512 : Resources.C512v1;
                default:
                    return string.Equals(Settings.Default.rarityDesign, "Flat") ? Resources.U512 : Resources.U512v1;
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
                return string.Equals(Settings.Default.rarityDesign, "Flat") ? Resources.Marvel512 : Resources.Marvel512v1;
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
                return string.Equals(Settings.Default.rarityDesign, "Flat") ? Resources.C512 : Resources.C512v1;
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
        public static void DrawRarity(JToken theItem, Graphics toDrawOn, string specialMode = null)
        {
            Image rarityBg;
            JToken seriesToken = theItem["Series"];
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
        }
    }
}
