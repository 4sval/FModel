using FModel.Parser.Items;
using FModel.Properties;
using System.Drawing;

namespace FModel
{
    class Rarity
    {
        public static void GetItemRarity(ItemsIdParser theItem, Graphics toDrawOn, string SpecialMode = null)
        {
            if (theItem.Rarity == "EFortRarity::Transcendent")
            {
                Image rarityBg = Resources.T512;
                toDrawOn.DrawImage(rarityBg, new Point(0, 0));
            }
            else if (theItem.Rarity == "EFortRarity::Mythic")
            {
                Image rarityBg = Resources.M512;
                toDrawOn.DrawImage(rarityBg, new Point(0, 0));
            }
            else if (theItem.Rarity == "EFortRarity::Legendary")
            {
                Image rarityBg = Resources.L512;
                toDrawOn.DrawImage(rarityBg, new Point(0, 0));
            }
            else if (theItem.Rarity == "EFortRarity::Epic" || theItem.Rarity == "EFortRarity::Quality")
            {
                Image rarityBg = Resources.E512;
                toDrawOn.DrawImage(rarityBg, new Point(0, 0));
            }
            else if (theItem.Rarity == "EFortRarity::Rare")
            {
                Image rarityBg = Resources.R512;
                toDrawOn.DrawImage(rarityBg, new Point(0, 0));
            }
            else if (theItem.Rarity == "EFortRarity::Common" || SpecialMode == "ammo") // Force common rarity if ammo, as ammo is always common in FN
            {
                Image rarityBg = Resources.C512;
                toDrawOn.DrawImage(rarityBg, new Point(0, 0));
            }
            else
            {
                Image rarityBg = Resources.U512;
                toDrawOn.DrawImage(rarityBg, new Point(0, 0));
            }
        }

        public static void GetSeriesRarity(ItemsIdParser theItem, Graphics toDrawOn)
        {
            if (theItem.Series == "MarvelSeries")
            {
                Image rarityBg = Resources.Marvel512;
                toDrawOn.DrawImage(rarityBg, new Point(0, 0));
            }
        }
    }
}
