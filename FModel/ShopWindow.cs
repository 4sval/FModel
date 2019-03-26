using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FModel
{
    public partial class ShopWindow : Form
    {
        private static string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).ToString() + "\\FModel";
        private static string exchangeToken;
        private static List<string> EncryptedItemsID;
        private static List<string> ItemsID;
        private static string ItemName;

        PrivateFontCollection pfc = new PrivateFontCollection();
        StringFormat centeredString = new StringFormat();
        StringFormat rightString = new StringFormat();
        StringFormat centeredStringLine = new StringFormat();
        private int fontLength;
        private byte[] fontdata;

        private static void jwpmProcess(string args)
        {
            using (Process p = new Process())
            {
                p.StartInfo.FileName = docPath + "/john-wick-parse-modded.exe";
                p.StartInfo.Arguments = args;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.UseShellExecute = false;
                p.Start();
                p.WaitForExit();
            }
        }
        private void AppendText(string text, Color color, bool addNewLine = false, HorizontalAlignment align = HorizontalAlignment.Left)
        {
            richTextBox1.SuspendLayout();
            richTextBox1.SelectionColor = color;
            richTextBox1.SelectionAlignment = align;
            richTextBox1.AppendText(addNewLine
                ? $"{text}{Environment.NewLine}"
                : text);
            richTextBox1.ScrollToCaret();
            richTextBox1.ResumeLayout();
        }
        private void getItemRarity(FModel.Items.ItemsIdParser parser, Graphics toDrawOn, int x, int y)
        {
            if (parser.Rarity == "EFortRarity::Legendary")
            {
                Image RarityBG = Properties.Resources.I512;
                toDrawOn.DrawImage(RarityBG, new Point(x, y));
                AppendText("Item Rarity: ", Color.Black);
                AppendText("IMPOSSIBLE (T9)", Color.DarkOrange, true);
            }
            if (parser.Rarity == "EFortRarity::Masterwork")
            {
                Image RarityBG = Properties.Resources.T512;
                toDrawOn.DrawImage(RarityBG, new Point(x, y));
                AppendText("Item Rarity: ", Color.Black);
                AppendText("TRANSCENDENT", Color.OrangeRed, true);
            }
            if (parser.Rarity == "EFortRarity::Elegant")
            {
                Image RarityBG = Properties.Resources.M512;
                toDrawOn.DrawImage(RarityBG, new Point(x, y));
                AppendText("Item Rarity: ", Color.Black);
                AppendText("MYTHIC", Color.Yellow, true);
            }
            if (parser.Rarity == "EFortRarity::Fine")
            {
                Image RarityBG = Properties.Resources.L512;
                toDrawOn.DrawImage(RarityBG, new Point(x, y));
                AppendText("Item Rarity: ", Color.Black);
                AppendText("LEGENDARY", Color.Orange, true);
            }
            if (parser.Rarity == "EFortRarity::Quality")
            {
                Image RarityBG = Properties.Resources.E512;
                toDrawOn.DrawImage(RarityBG, new Point(x, y));
                AppendText("Item Rarity: ", Color.Black);
                AppendText("EPIC", Color.Purple, true);
            }
            if (parser.Rarity == "EFortRarity::Sturdy")
            {
                Image RarityBG = Properties.Resources.R512;
                toDrawOn.DrawImage(RarityBG, new Point(x, y));
                AppendText("Item Rarity: ", Color.Black);
                AppendText("RARE", Color.Blue, true);
            }
            if (parser.Rarity == "EFortRarity::Handmade")
            {
                Image RarityBG = Properties.Resources.C512;
                toDrawOn.DrawImage(RarityBG, new Point(x, y));
                AppendText("Item Rarity: ", Color.Black);
                AppendText("COMMON", Color.DarkGray, true);
            }
            if (parser.Rarity == null)
            {
                Image RarityBG = Properties.Resources.U512;
                toDrawOn.DrawImage(RarityBG, new Point(x, y));
                AppendText("Item Rarity: ", Color.Black);
                AppendText("UNCOMMON", Color.Green, true);
            }
        }

        public ShopWindow(string exToken)
        {
            InitializeComponent();
            exchangeToken = exToken;
        }

        private async void ShopWindow_Load(object sender, EventArgs e)
        {
            fontLength = Properties.Resources.BurbankBigCondensed_Bold.Length;
            fontdata = Properties.Resources.BurbankBigCondensed_Bold;
            System.IntPtr weirdData = Marshal.AllocCoTaskMem(fontLength);
            Marshal.Copy(fontdata, 0, weirdData, fontLength);
            pfc.AddMemoryFont(weirdData, fontLength);

            EncryptedItemsID = new List<string>();
            ItemsID = new List<string>();

            FModel.Shop.ShopParser shop = new Shop.ShopParser();
            await Task.Run(() =>
            {
                var client = new RestClient("https://fortnite-public-service-prod11.ol.epicgames.com/fortnite/api/storefront/v2/catalog");
                var req = new RestRequest(Method.GET);
                req.AddHeader("X-EpicGames-Language", "en");
                req.AddHeader("Authorization", "bearer " + exchangeToken);
                var response = client.Execute(req).Content;
                shop = FModel.Shop.ShopParser.FromJson(response);
                //File.WriteAllText(docPath + "\\shop.json", JToken.Parse(content).ToString(Newtonsoft.Json.Formatting.Indented));
            });

            for (int i = 0; i < shop.Storefronts.Length; i++)
            {
                if (shop.Storefronts[i].Name == "BRDailyStorefront")
                {
                    for (int i2 = 0; i2 < shop.Storefronts[i].CatalogEntries.Length; i2++)
                    {
                        for (int i3 = 0; i3 < shop.Storefronts[i].CatalogEntries[i2].ItemGrants.Length; i3++)
                        {
                            var parts = shop.Storefronts[i].CatalogEntries[i2].ItemGrants[i3].TemplateId.Split(':');
                            ItemsID.Add(parts[1]);
                        }
                    }
                }
                if (shop.Storefronts[i].Name == "BRWeeklyStorefront")
                {
                    for (int i2 = 0; i2 < shop.Storefronts[i].CatalogEntries.Length; i2++)
                    {
                        if (shop.Storefronts[i].CatalogEntries[i2].MetaInfo != null)
                        {
                            for (int ii = 0; ii < shop.Storefronts[i].CatalogEntries[i2].MetaInfo.Length; ii++)
                            {
                                if (shop.Storefronts[i].CatalogEntries[i2].MetaInfo[ii].Key == "EncryptionKey")
                                {
                                    for (int i3 = 0; i3 < shop.Storefronts[i].CatalogEntries[i2].ItemGrants.Length; i3++)
                                    {
                                        var parts = shop.Storefronts[i].CatalogEntries[i2].ItemGrants[i3].TemplateId.Split(':');
                                        EncryptedItemsID.Add(parts[1]);
                                    }
                                }
                            }
                        }
                        else
                        {
                            for (int i3 = 0; i3 < shop.Storefronts[i].CatalogEntries[i2].ItemGrants.Length; i3++)
                            {
                                var parts = shop.Storefronts[i].CatalogEntries[i2].ItemGrants[i3].TemplateId.Split(':');
                                ItemsID.Add(parts[1]);
                            }
                        }
                    }
                }
            }

            Bitmap bmp = new Bitmap(ItemsID.Count * 522 + (ItemsID.Count - 1) * 10, (522 * 2) + 10);
            int xPoint = 0;
            int yPoint = 532;
            for (int i = 0; i < ItemsID.Count; i++)
            {
                var files = Directory.GetFiles(docPath + "\\Extracted", ItemsID[i] + ".*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".png")).FirstOrDefault();
                if (!File.Exists(files))
                {
                    await Task.Run(() =>
                    {
                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\pakchunk0_s1-WindowsClient.pak\" \"" + ItemsID[i] + "\" \"" + docPath + "\"");
                    });
                    files = Directory.GetFiles(docPath + "\\Extracted", ItemsID[i] + ".*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".png")).FirstOrDefault();
                }
                if (files != null)
                {
                    AppendText("", Color.Black, true);
                    AppendText("✔ ", Color.Green);
                    AppendText(ItemsID[i], Color.DarkRed);
                    AppendText(" successfully extracted to ", Color.Black);
                    AppendText(files.Substring(0, files.LastIndexOf('.')), Color.SteelBlue, true);

                    if (files.Contains(".uasset") || files.Contains(".uexp") || files.Contains(".ubulk"))
                    {
                        AppendText("✔ ", Color.Green);
                        AppendText(ItemsID[i], Color.DarkRed);
                        AppendText(" is an ", Color.Black);
                        AppendText("asset", Color.SteelBlue, true);
                        await Task.Run(() =>
                        {
                            jwpmProcess("serialize \"" + files.Substring(0, files.LastIndexOf('.')) + "\"");
                        });
                    }
                    try
                    {
                        var filesJSON = Directory.GetFiles(docPath, ItemsID[i] + ".json", SearchOption.AllDirectories).FirstOrDefault();
                        if (filesJSON != null)
                        {
                            var json = JToken.Parse(File.ReadAllText(filesJSON)).ToString();
                            File.Delete(filesJSON);
                            AppendText("✔ ", Color.Green);
                            AppendText(ItemsID[i], Color.DarkRed);
                            AppendText(" successfully serialized", Color.Black, true);

                            var IDParser = FModel.Items.ItemsIdParser.FromJson(json);

                            if (filesJSON.Contains("Athena\\Items\\") && (filesJSON.Contains("Cosmetics"))) //ASSET IS AN ID => CREATE ICON
                            {
                                AppendText("Parsing...", Color.Black, true);
                                for (int iii = 0; iii < IDParser.Length; iii++)
                                {
                                    if (IDParser[iii].ExportType.Contains("Athena") && IDParser[iii].ExportType.Contains("Item") && IDParser[iii].ExportType.Contains("Definition"))
                                    {
                                        AppendText("✔ ", Color.Green);
                                        AppendText(ItemsID[i], Color.DarkRed);
                                        AppendText(" is a ", Color.Black);
                                        AppendText("Cosmetic ID", Color.SteelBlue);
                                        AppendText(" file", Color.Black, true);

                                        ItemName = IDParser[iii].DisplayName;
                                        Graphics g = Graphics.FromImage(bmp);
                                        g.TextRenderingHint = TextRenderingHint.AntiAlias;

                                        getItemRarity(IDParser[iii], g, xPoint, yPoint);

                                        string itemIconPath = string.Empty;

                                        if (IDParser[iii].HeroDefinition != null)
                                        {
                                            var filesPath = Directory.GetFiles(docPath + "\\Extracted", IDParser[iii].HeroDefinition + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                            if (!File.Exists(filesPath))
                                            {
                                                AppendText("✔ ", Color.Green);
                                                AppendText("Extracting ", Color.Black);
                                                AppendText(IDParser[iii].HeroDefinition, Color.DarkRed, true);

                                                await Task.Run(() =>
                                                {
                                                    jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\pakchunk0_s1-WindowsClient.pak\" \"" + IDParser[iii].HeroDefinition + "\" \"" + docPath + "\"");
                                                });
                                                filesPath = Directory.GetFiles(docPath + "\\Extracted", IDParser[iii].HeroDefinition + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                            }
                                            try
                                            {
                                                if (filesPath != null)
                                                {
                                                    AppendText("✔ ", Color.Green);
                                                    AppendText(IDParser[iii].HeroDefinition, Color.DarkRed);
                                                    AppendText(" successfully extracted to ", Color.Black);
                                                    AppendText(filesPath.Substring(0, filesPath.LastIndexOf('.')), Color.SteelBlue, true);
                                                    try
                                                    {
                                                        await Task.Run(() =>
                                                        {
                                                            jwpmProcess("serialize \"" + filesPath.Substring(0, filesPath.LastIndexOf('.')) + "\"");
                                                        });
                                                        var filesJSON2 = Directory.GetFiles(docPath, IDParser[iii].HeroDefinition + ".json", SearchOption.AllDirectories).FirstOrDefault();
                                                        var json2 = JToken.Parse(File.ReadAllText(filesJSON2)).ToString();
                                                        File.Delete(filesJSON2);
                                                        AppendText("✔ ", Color.Green);
                                                        AppendText(IDParser[iii].HeroDefinition, Color.DarkRed);
                                                        AppendText(" successfully serialized", Color.Black, true);

                                                        var IDParser2 = FModel.Items.ItemsIdParser.FromJson(json2);
                                                        for (int i1 = 0; i1 < IDParser2.Length; i1++)
                                                        {
                                                            if (IDParser2[i1].LargePreviewImage != null)
                                                            {
                                                                string textureFile = Path.GetFileName(IDParser2[i1].LargePreviewImage.AssetPathName).Substring(0, Path.GetFileName(IDParser2[i1].LargePreviewImage.AssetPathName).LastIndexOf('.'));
                                                                AppendText("✔ ", Color.Green);
                                                                AppendText(textureFile, Color.DarkRed);
                                                                AppendText(" detected as a ", Color.Black);
                                                                AppendText("Texture2D file", Color.SteelBlue, true);

                                                                var filesPath2 = Directory.GetFiles(docPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                                                if (!File.Exists(filesPath2))
                                                                {
                                                                    await Task.Run(() =>
                                                                    {
                                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\pakchunk0_s7-WindowsClient.pak" + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                    });
                                                                    filesPath2 = Directory.GetFiles(docPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                                                }
                                                                try
                                                                {
                                                                    if (filesPath2 != null)
                                                                    {
                                                                        AppendText("✔ ", Color.Green);
                                                                        AppendText(textureFile, Color.DarkRed);
                                                                        AppendText(" successfully extracted to ", Color.Black);
                                                                        AppendText(filesPath2.Substring(0, filesPath2.LastIndexOf('.')), Color.SteelBlue, true);

                                                                        itemIconPath = filesPath2.Substring(0, filesPath2.LastIndexOf('.')) + ".png";
                                                                        if (!File.Exists(itemIconPath))
                                                                        {
                                                                            await Task.Run(() =>
                                                                            {
                                                                                jwpmProcess("texture \"" + filesPath2.Substring(0, filesPath2.LastIndexOf('.')) + "\"");
                                                                            });
                                                                            itemIconPath = filesPath2.Substring(0, filesPath2.LastIndexOf('.')) + ".png";
                                                                        }

                                                                        AppendText("✔ ", Color.Green);
                                                                        AppendText(textureFile, Color.DarkRed);
                                                                        AppendText(" successfully converted to a PNG image with path ", Color.Black);
                                                                        AppendText(itemIconPath, Color.SteelBlue, true);
                                                                    }
                                                                }
                                                                catch (IndexOutOfRangeException)
                                                                {
                                                                    AppendText("[IndexOutOfRangeException] ", Color.Red);
                                                                    AppendText("Can't extract ", Color.Black);
                                                                    AppendText(textureFile, Color.SteelBlue);
                                                                    AppendText(" in ", Color.Black);
                                                                    AppendText("pakchunk0_s7-WindowsClient.pak", Color.DarkRed, true);
                                                                }
                                                            }
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Console.WriteLine(ex.Message);
                                                    }
                                                }
                                            }
                                            catch (IndexOutOfRangeException)
                                            {
                                                AppendText("[IndexOutOfRangeException] ", Color.Red);
                                                AppendText("Can't extract ", Color.Black);
                                                AppendText(IDParser[iii].HeroDefinition, Color.SteelBlue);
                                                AppendText(" in ", Color.Black);
                                                AppendText("pakchunk0_s1-WindowsClient.pak", Color.DarkRed, true);
                                            }
                                        }
                                        else if (IDParser[iii].WeaponDefinition != null)
                                        {
                                            var filesPath = Directory.GetFiles(docPath + "\\Extracted", IDParser[iii].WeaponDefinition + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                            if (!File.Exists(filesPath))
                                            {
                                                AppendText("✔ ", Color.Green);
                                                AppendText("Extracting ", Color.Black);
                                                AppendText(IDParser[iii].WeaponDefinition, Color.DarkRed, true);

                                                await Task.Run(() =>
                                                {
                                                    jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\pakchunk0_s1-WindowsClient.pak\" \"" + IDParser[iii].WeaponDefinition + "\" \"" + docPath + "\"");
                                                });
                                                filesPath = Directory.GetFiles(docPath + "\\Extracted", IDParser[iii].WeaponDefinition + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                            }
                                            try
                                            {
                                                if (filesPath != null)
                                                {
                                                    AppendText("✔ ", Color.Green);
                                                    AppendText(IDParser[iii].WeaponDefinition, Color.DarkRed);
                                                    AppendText(" successfully extracted to ", Color.Black);
                                                    AppendText(filesPath.Substring(0, filesPath.LastIndexOf('.')), Color.SteelBlue, true);
                                                    try
                                                    {
                                                        await Task.Run(() =>
                                                        {
                                                            jwpmProcess("serialize \"" + filesPath.Substring(0, filesPath.LastIndexOf('.')) + "\"");
                                                        });
                                                        var filesJSON2 = Directory.GetFiles(docPath, IDParser[iii].WeaponDefinition + ".json", SearchOption.AllDirectories).FirstOrDefault();
                                                        var json2 = JToken.Parse(File.ReadAllText(filesJSON2)).ToString();
                                                        File.Delete(filesJSON2);
                                                        AppendText("✔ ", Color.Green);
                                                        AppendText(IDParser[iii].WeaponDefinition, Color.DarkRed);
                                                        AppendText(" successfully serialized", Color.Black, true);

                                                        var IDParser2 = FModel.Items.ItemsIdParser.FromJson(json2);
                                                        for (int i2 = 0; i2 < IDParser2.Length; i2++)
                                                        {
                                                            if (IDParser2[i2].LargePreviewImage != null)
                                                            {
                                                                string textureFile = Path.GetFileName(IDParser2[i2].LargePreviewImage.AssetPathName).Substring(0, Path.GetFileName(IDParser2[i2].LargePreviewImage.AssetPathName).LastIndexOf('.'));
                                                                AppendText("✔ ", Color.Green);
                                                                AppendText(textureFile, Color.DarkRed);
                                                                AppendText(" detected as a ", Color.Black);
                                                                AppendText("Texture2D file", Color.SteelBlue, true);

                                                                var filesPath2 = Directory.GetFiles(docPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                                                if (!File.Exists(filesPath2))
                                                                {
                                                                    await Task.Run(() =>
                                                                    {
                                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\pakchunk0_s7-WindowsClient.pak" + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                    });
                                                                    filesPath2 = Directory.GetFiles(docPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                                                }
                                                                try
                                                                {
                                                                    if (filesPath2 != null)
                                                                    {
                                                                        AppendText("✔ ", Color.Green);
                                                                        AppendText(textureFile, Color.DarkRed);
                                                                        AppendText(" successfully extracted to ", Color.Black);
                                                                        AppendText(filesPath2.Substring(0, filesPath2.LastIndexOf('.')), Color.SteelBlue, true);

                                                                        itemIconPath = filesPath2.Substring(0, filesPath2.LastIndexOf('.')) + ".png";
                                                                        if (!File.Exists(itemIconPath))
                                                                        {
                                                                            await Task.Run(() =>
                                                                            {
                                                                                jwpmProcess("texture \"" + filesPath2.Substring(0, filesPath2.LastIndexOf('.')) + "\"");
                                                                            });
                                                                            itemIconPath = filesPath2.Substring(0, filesPath2.LastIndexOf('.')) + ".png";
                                                                        }

                                                                        AppendText("✔ ", Color.Green);
                                                                        AppendText(textureFile, Color.DarkRed);
                                                                        AppendText(" successfully converted to a PNG image with path ", Color.Black);
                                                                        AppendText(itemIconPath, Color.SteelBlue, true);
                                                                    }
                                                                }
                                                                catch (IndexOutOfRangeException)
                                                                {
                                                                    AppendText("[IndexOutOfRangeException] ", Color.Red);
                                                                    AppendText("Can't extract ", Color.Black);
                                                                    AppendText(textureFile, Color.SteelBlue);
                                                                    AppendText(" in ", Color.Black);
                                                                    AppendText("pakchunk0_s7-WindowsClient.pak", Color.DarkRed, true);
                                                                }
                                                            }
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Console.WriteLine(ex.Message);
                                                    }
                                                }
                                            }
                                            catch (IndexOutOfRangeException)
                                            {
                                                AppendText("[IndexOutOfRangeException] ", Color.Red);
                                                AppendText("Can't extract ", Color.Black);
                                                AppendText(IDParser[iii].WeaponDefinition, Color.SteelBlue);
                                                AppendText(" in ", Color.Black);
                                                AppendText("pakchunk0_s1-WindowsClient.pak", Color.DarkRed, true);
                                            }
                                        }
                                        else if (IDParser[iii].LargePreviewImage != null)
                                        {
                                            string textureFile = Path.GetFileName(IDParser[iii].LargePreviewImage.AssetPathName).Substring(0, Path.GetFileName(IDParser[iii].LargePreviewImage.AssetPathName).LastIndexOf('.'));
                                            AppendText("✔ ", Color.Green);
                                            AppendText(textureFile, Color.DarkRed);
                                            AppendText(" detected as a ", Color.Black);
                                            AppendText("Texture2D file", Color.SteelBlue, true);

                                            var filesPath = Directory.GetFiles(docPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                            if (!File.Exists(filesPath))
                                            {
                                                await Task.Run(() =>
                                                {
                                                    if (IDParser[iii].LargePreviewImage.AssetPathName.Contains("/Game/2dAssets/"))
                                                    {
                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\pakchunk0-WindowsClient.pak" + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                    }
                                                    else if (IDParser[iii].LargePreviewImage.AssetPathName.Contains("/Game/Athena/TestAssets/") || IDParser[iii].LargePreviewImage.AssetPathName.Contains("/Game/Athena/Prototype/") || IDParser[iii].LargePreviewImage.AssetPathName.Contains("/Game/Athena/Items/"))
                                                    {
                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\pakchunk0_s1-WindowsClient.pak\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                    }
                                                    else
                                                    {
                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\pakchunk0_s7-WindowsClient.pak" + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                    }
                                                });
                                                filesPath = Directory.GetFiles(docPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                            }
                                            try
                                            {
                                                if (filesPath != null)
                                                {
                                                    AppendText("✔ ", Color.Green);
                                                    AppendText(textureFile, Color.DarkRed);
                                                    AppendText(" successfully extracted to ", Color.Black);
                                                    AppendText(filesPath.Substring(0, filesPath.LastIndexOf('.')), Color.SteelBlue, true);

                                                    itemIconPath = filesPath.Substring(0, filesPath.LastIndexOf('.')) + ".png";
                                                    if (!File.Exists(itemIconPath))
                                                    {
                                                        await Task.Run(() =>
                                                        {
                                                            jwpmProcess("texture \"" + filesPath.Substring(0, filesPath.LastIndexOf('.')) + "\"");
                                                        });
                                                        itemIconPath = filesPath.Substring(0, filesPath.LastIndexOf('.')) + ".png";
                                                    }

                                                    AppendText("✔ ", Color.Green);
                                                    AppendText(textureFile, Color.DarkRed);
                                                    AppendText(" successfully converted to a PNG image with path ", Color.Black);
                                                    AppendText(itemIconPath, Color.SteelBlue, true);
                                                }
                                            }
                                            catch (IndexOutOfRangeException)
                                            {
                                                AppendText("[IndexOutOfRangeException] ", Color.Red);
                                                AppendText("Can't extract ", Color.Black);
                                                AppendText(textureFile, Color.SteelBlue);
                                                AppendText(" in ", Color.Black);
                                                AppendText("pakchunk0_s7-WindowsClient.pak", Color.DarkRed, true);
                                            }
                                        }
                                        else if (IDParser[iii].SmallPreviewImage != null)
                                        {
                                            string textureFile = Path.GetFileName(IDParser[iii].SmallPreviewImage.AssetPathName).Substring(0, Path.GetFileName(IDParser[iii].SmallPreviewImage.AssetPathName).LastIndexOf('.'));
                                            AppendText("✔ ", Color.Green);
                                            AppendText(textureFile, Color.DarkRed);
                                            AppendText(" detected as a ", Color.Black);
                                            AppendText("Texture2D file", Color.SteelBlue, true);

                                            var filesPath = Directory.GetFiles(docPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                            if (!File.Exists(filesPath))
                                            {
                                                await Task.Run(() =>
                                                {
                                                    if (IDParser[iii].SmallPreviewImage.AssetPathName.Contains("/Game/2dAssets/"))
                                                    {
                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\pakchunk0-WindowsClient.pak" + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                    }
                                                    else if (IDParser[iii].SmallPreviewImage.AssetPathName.Contains("/Game/Athena/TestAssets/") || IDParser[iii].SmallPreviewImage.AssetPathName.Contains("/Game/Athena/Prototype/") || IDParser[iii].LargePreviewImage.AssetPathName.Contains("/Game/Athena/Items/"))
                                                    {
                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\pakchunk0_s1-WindowsClient.pak\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                    }
                                                    else
                                                    {
                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\pakchunk0_s7-WindowsClient.pak" + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                    }
                                                });
                                                filesPath = Directory.GetFiles(docPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                            }
                                            try
                                            {
                                                if (filesPath != null)
                                                {
                                                    AppendText("✔ ", Color.Green);
                                                    AppendText(textureFile, Color.DarkRed);
                                                    AppendText(" successfully extracted to ", Color.Black);
                                                    AppendText(filesPath.Substring(0, filesPath.LastIndexOf('.')), Color.SteelBlue, true);

                                                    itemIconPath = filesPath.Substring(0, filesPath.LastIndexOf('.')) + ".png";
                                                    if (!File.Exists(itemIconPath))
                                                    {
                                                        await Task.Run(() =>
                                                        {
                                                            jwpmProcess("texture \"" + filesPath.Substring(0, filesPath.LastIndexOf('.')) + "\"");
                                                        });
                                                        itemIconPath = filesPath.Substring(0, filesPath.LastIndexOf('.')) + ".png";
                                                    }

                                                    AppendText("✔ ", Color.Green);
                                                    AppendText(textureFile, Color.DarkRed);
                                                    AppendText(" successfully converted to a PNG image with path ", Color.Black);
                                                    AppendText(itemIconPath, Color.SteelBlue, true);
                                                }
                                            }
                                            catch (IndexOutOfRangeException)
                                            {
                                                AppendText("[IndexOutOfRangeException] ", Color.Red);
                                                AppendText("Can't extract ", Color.Black);
                                                AppendText(textureFile, Color.SteelBlue);
                                                AppendText(" in ", Color.Black);
                                                AppendText("pakchunk0_s7-WindowsClient.pak", Color.DarkRed, true);
                                            }
                                        }

                                        if (File.Exists(itemIconPath))
                                        {
                                            Image ItemIcon = Image.FromFile(itemIconPath);
                                            g.DrawImage(ItemIcon, new Point(xPoint + 5, yPoint + 5));
                                        }
                                        else
                                        {
                                            Image ItemIcon = Properties.Resources.unknown512;
                                            g.DrawImage(ItemIcon, new Point(xPoint, yPoint));
                                        }

                                        Image bg512 = Properties.Resources.BG512;
                                        g.DrawImage(bg512, new Point(xPoint + 5, yPoint + 383));

                                        try
                                        {
                                            g.DrawString(ItemName, new Font(pfc.Families[0], 35), new SolidBrush(Color.White), new Point(xPoint + (522 / 2), yPoint + 395), centeredString);
                                        }
                                        catch (NullReferenceException)
                                        {
                                            AppendText("[NullReferenceException] ", Color.Red);
                                            AppendText("No ", Color.Black);
                                            AppendText("DisplayName ", Color.SteelBlue);
                                            AppendText("found", Color.Black, true);
                                        } //NAME
                                        try
                                        {
                                            g.DrawString(IDParser[iii].Description, new Font("Arial", 10), new SolidBrush(Color.White), new Point(xPoint + (522 / 2), yPoint + 465), centeredStringLine);
                                        }
                                        catch (NullReferenceException)
                                        {
                                            AppendText("[NullReferenceException] ", Color.Red);
                                            AppendText("No ", Color.Black);
                                            AppendText("Description ", Color.SteelBlue);
                                            AppendText("found", Color.Black, true);
                                        } //DESCRIPTION
                                        try
                                        {
                                            g.DrawString(IDParser[iii].ShortDescription, new Font(pfc.Families[0], 13), new SolidBrush(Color.White), new Point(xPoint + 5, yPoint + 500));
                                        }
                                        catch (NullReferenceException)
                                        {
                                            AppendText("[NullReferenceException] ", Color.Red);
                                            AppendText("No ", Color.Black);
                                            AppendText("ShortDescription ", Color.SteelBlue);
                                            AppendText("found", Color.Black, true);
                                        } //TYPE
                                        try
                                        {
                                            g.DrawString(IDParser[iii].GameplayTags.GameplayTagsGameplayTags[Array.FindIndex(IDParser[iii].GameplayTags.GameplayTagsGameplayTags, x => x.StartsWith("Cosmetics.Source."))].Substring(17), new Font(pfc.Families[0], 13), new SolidBrush(Color.White), new Point(xPoint + (522 - 5), yPoint + 500), rightString);
                                        }
                                        catch (NullReferenceException)
                                        {
                                            AppendText("[NullReferenceException] ", Color.Red);
                                            AppendText("No ", Color.Black);
                                            AppendText("GameplayTags ", Color.SteelBlue);
                                            AppendText("found", Color.Black, true);
                                        }
                                        catch (IndexOutOfRangeException)
                                        {
                                            AppendText("[IndexOutOfRangeException] ", Color.Red);
                                            AppendText("No ", Color.Black);
                                            AppendText("GameplayTags ", Color.SteelBlue);
                                            AppendText("as ", Color.Black);
                                            AppendText("Cosmetics.Source ", Color.SteelBlue);
                                            AppendText("found", Color.Black, true);
                                        } //COSMETIC SOURCE
                                    } //Cosmetics
                                }
                            }
                        }
                        else
                        {
                            AppendText("✗ ", Color.Red);
                            AppendText("No serialized file found", Color.Black, true);
                        }
                    }
                    catch (JsonSerializationException)
                    {
                        AppendText("", Color.Black, true);
                        AppendText("✗ ", Color.Red);
                        AppendText("Error, json file too large to be fully displayed", Color.Black, true);
                    }
                }
                else
                {
                    AppendText("", Color.Black, true);
                    AppendText("✗ ", Color.Red);
                    AppendText("Error while extracting ", Color.Black);
                    AppendText(ItemsID[i], Color.SteelBlue, true);
                }
                xPoint += 522;
            }
            pictureBox1.Image = bmp;
        }
    }
}
