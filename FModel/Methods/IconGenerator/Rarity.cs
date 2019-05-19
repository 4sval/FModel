using FModel.Parser.Items;
using FModel.Properties;
using System.Drawing;

namespace FModel
{
    class Rarity
    {
        public static Image GetRarityImage(ItemsIdParser theItem)
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
                    return Resources.E512;
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
        public static Image GetSeriesImage(ItemsIdParser theItem)
        {
            switch (theItem.Series)
            {
                case "MarvelSeries":
                    return Resources.Marvel512;
                default:
                    return GetRarityImage(theItem);
            }
        }
        public static Image GetSpecialModeImage(ItemsIdParser theItem, string SpecialMode)
        {
            switch (SpecialMode)
            {
                case "ammo":
                    return Resources.C512;
                default:
                    return GetRarityImage(theItem);
            }
        }
        public static void DrawRarity(ItemsIdParser theItem, Graphics toDrawOn, string SpecialMode = null)
        {
            Image rarityBg;

            if (theItem.Series != null)
                rarityBg = GetSeriesImage(theItem);
            else if (SpecialMode != null)
                rarityBg = GetSpecialModeImage(theItem, SpecialMode);
            else
                rarityBg = GetRarityImage(theItem);

            toDrawOn.DrawImage(rarityBg, new Point(0, 0));
        }
    }
}
