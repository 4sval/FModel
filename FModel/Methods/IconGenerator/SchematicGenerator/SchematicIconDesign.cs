using csharp_wick;
using FModel.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace FModel
{
    static class SchematicIconDesign
    {
        public static Graphics toDrawOn { get; set; }
        public static Bitmap schematicBitmap { get; set; }

        public static Graphics createGraphic(int x, int y)
        {
            schematicBitmap = new Bitmap(x, y);
            Graphics g = Graphics.FromImage(schematicBitmap);
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            g.SmoothingMode = SmoothingMode.HighQuality;
            return g;
        }

        public static void createItemDefinitionIcon(JToken theItem)
        {
            JToken craftedItem = SchematicItemInfos.setSchematicData(theItem);
            toDrawOn = createGraphic(522, 522 + (75 * SchematicItemInfos.schematicInfoList.Count));
            Rarity.DrawRarity(craftedItem, toDrawOn);

            ItemIcon.ItemIconPath = string.Empty;
            ItemIcon.GetItemIcon(craftedItem, Settings.Default.loadFeaturedImage);
            if (File.Exists(ItemIcon.ItemIconPath))
            {
                Image itemIcon;
                using (var bmpTemp = new Bitmap(ItemIcon.ItemIconPath))
                {
                    itemIcon = new Bitmap(bmpTemp);
                }
                toDrawOn.DrawImage(ImageUtilities.ResizeImage(itemIcon, 512, 512), new Point(5, 5));
            }
            else
            {
                Image itemIcon = Resources.unknown512;
                toDrawOn.DrawImage(itemIcon, new Point(0, 0));
            }

            if (Settings.Default.rarityNew)
            {
                GraphicsPath p = new GraphicsPath();
                p.StartFigure();
                p.AddLine(4, 438, 517, 383);
                p.AddLine(517, 383, 517, 383 + 134);
                p.AddLine(4, 383 + 134, 4, 383 + 134);
                p.AddLine(4, 383 + 134, 4, 438);
                p.CloseFigure();
                toDrawOn.FillPath(new SolidBrush(Color.FromArgb(70, 0, 0, 50)), p);
            }
            else { toDrawOn.FillRectangle(new SolidBrush(Color.FromArgb(70, 0, 0, 50)), new Rectangle(5, 383, 512, 134)); }

            DrawText.DrawTexts(craftedItem, toDrawOn, "");
        }

        public static void createIngredientIcon()
        {
            for (int i = 0; i < SchematicItemInfos.schematicInfoList.Count; i++)
            {
                string ingredientsFileName = ThePak.AllpaksDictionary.Where(x => string.Equals(x.Key, SchematicItemInfos.schematicInfoList[i].theIngredientItemDefinition, StringComparison.CurrentCultureIgnoreCase)).Select(d => d.Key).FirstOrDefault();
                if (!string.IsNullOrEmpty(ingredientsFileName))
                {
                    string extractedIconPath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary[ingredientsFileName], ingredientsFileName);
                    if (extractedIconPath != null)
                    {
                        if (extractedIconPath.Contains(".uasset") || extractedIconPath.Contains(".uexp") || extractedIconPath.Contains(".ubulk"))
                        {
                            JohnWick.MyAsset = new PakAsset(extractedIconPath.Substring(0, extractedIconPath.LastIndexOf('.')));
                            try
                            {
                                if (JohnWick.MyAsset.GetSerialized() != null)
                                {
                                    new UpdateMyState("Drawing " + ingredientsFileName + "...", "Waiting").ChangeProcessState();

                                    dynamic AssetData = JsonConvert.DeserializeObject(JohnWick.MyAsset.GetSerialized());
                                    JArray AssetArray = JArray.FromObject(AssetData);

                                    fillSchematicIcon(AssetData[0], i);
                                    drawIngredientIcon(AssetData[0], i);
                                    drawDisplayNameAndQuantity(AssetData[0], SchematicItemInfos.schematicInfoList[i].theIngredientQuantity, i);
                                }
                            }
                            catch (JsonSerializationException)
                            {
                                //do not crash when JsonSerialization does weird stuff
                            }
                        }
                    }
                }
            }
        }

        private static void fillSchematicIcon(JToken AssetArray, int i)
        {
            Color rarityColor = new Color();
            JToken rarity = AssetArray["Rarity"];
            if (rarity != null)
            {
                switch (rarity.Value<string>())
                {
                    case "EFortRarity::Transcendent":
                        rarityColor = Color.FromArgb(255, 155, 39, 69);
                        break;
                    case "EFortRarity::Mythic":
                        rarityColor = Color.FromArgb(255, 170, 143, 47);
                        break;
                    case "EFortRarity::Legendary":
                        rarityColor = Color.FromArgb(255, 170, 96, 47);
                        break;
                    case "EFortRarity::Epic":
                    case "EFortRarity::Quality":
                        rarityColor = Color.FromArgb(255, 96, 47, 170);
                        break;
                    case "EFortRarity::Rare":
                        rarityColor = Color.FromArgb(255, 55, 92, 163);
                        break;
                    case "EFortRarity::Common":
                        rarityColor = Color.FromArgb(255, 109, 109, 109);
                        break;
                }
            }
            else { rarityColor = Color.FromArgb(255, 87, 155, 39); }

            toDrawOn.FillRectangle(new SolidBrush(rarityColor), new Rectangle(0, 522 + (75 * i), schematicBitmap.Width, 75));
            Pen pen = new Pen(ControlPaint.Light(rarityColor), 4);
            pen.Alignment = PenAlignment.Inset; //<-- this
            toDrawOn.DrawRectangle(pen, new Rectangle(0, 522 + (75 * i), schematicBitmap.Width, 75));
        }

        private static void drawIngredientIcon(JToken AssetArray, int i)
        {
            ItemIcon.ItemIconPath = string.Empty;
            ItemIcon.SearchAthIteDefIcon(AssetArray);
            if (File.Exists(ItemIcon.ItemIconPath))
            {
                Image itemIcon;
                using (var bmpTemp = new Bitmap(ItemIcon.ItemIconPath))
                {
                    itemIcon = new Bitmap(bmpTemp);
                }
                toDrawOn.DrawImage(ImageUtilities.ResizeImage(itemIcon, 50, 50), new Point(10, 536 + (75 * i)));
            }
            else
            {
                Image itemIcon = Resources.unknown512;
                toDrawOn.DrawImage(itemIcon, new Point(0, 0));
            }
        }

        private static void drawDisplayNameAndQuantity(JToken AssetArray, string quantity, int i)
        {
            JToken displayName = AssetArray["DisplayName"];
            if (displayName != null)
            {
                JToken key = displayName["key"];
                JToken sourceString = displayName["source_string"];
                if (key != null && sourceString != null)
                {
                    string text = SearchResource.getTextByKey(key.Value<string>(), sourceString.Value<string>());

                    toDrawOn.DrawString(
                        text,
                        new Font(Settings.Default.IconLanguage == "Japanese" ? FontUtilities.pfc.Families[2] : FontUtilities.pfc.Families[0], 25),
                        new SolidBrush(Color.White),
                        new Point(70, 545 + (75 * i)),
                        FontUtilities.leftString
                        );
                }
            }

            toDrawOn.DrawString(
                        quantity,
                        new Font(Settings.Default.IconLanguage == "Japanese" ? FontUtilities.pfc.Families[2] : FontUtilities.pfc.Families[0], 25),
                        new SolidBrush(Color.White),
                        new Point(502, 545 + (75 * i)),
                        FontUtilities.rightString
                        );
        }
    }
}
