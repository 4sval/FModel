using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FModel
{
    public partial class PAKWindow : Form
    {
        public static ConfigFile conf;
        private static string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).ToString() + "\\FModel";
        private static string[] PAKFileAsTXT;
        private static string ItemName;

        PrivateFontCollection pfc = new PrivateFontCollection();
        StringFormat centeredString = new StringFormat();
        StringFormat rightString = new StringFormat();
        StringFormat centeredStringLine = new StringFormat();
        private int fontLength;
        private byte[] fontdata;

        public PAKWindow()
        {
            InitializeComponent();
        }

        private void PAKWindow_Load(object sender, EventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.White; //DEFAULT CONSOLE COLOR
            conf = new ConfigFile(); //CREATE CONFIG FILE
            foreach (var file in Directory.GetFiles(Config.conf.pathToFortnitePAKs).Where(x => x.EndsWith(".pak"))) //GET EACH PAKs NAME IN COMBO BOX
            {
                PAKsComboBox.Items.Add(Path.GetFileName(file));
            }

            if (!File.Exists("key.txt"))
            {
                AppendText("[FileNotFoundException] ", Color.Red);
                AppendText("File ", Color.Black);
                AppendText("key.txt ", Color.SteelBlue);
                AppendText("created", Color.Black, true);

                File.Create("key.txt").Close();
            }
            AESKeyTextBox.Text = "0x" + File.ReadAllText("key.txt").ToUpper();

            if (!File.Exists(docPath + "\\john-wick-parse-modded.exe"))
            {
                WebClient Client = new WebClient();
                Client.DownloadFile("https://www53.zippyshare.com/d/m6LyNUXB/936989/john-wick-parse-modded.exe", docPath + "\\john-wick-parse-modded.exe");

                AppendText("[FileNotFoundException] ", Color.Red);
                AppendText("File ", Color.Black);
                AppendText("john-wick-parse-modded.exe ", Color.SteelBlue);
                AppendText("downloaded successfully", Color.Black, true);
            }

            ExtractButton.Enabled = false;
            SaveImageButton.Enabled = false;
            SaveImageCheckBox.Enabled = false;

            fontLength = Properties.Resources.BurbankBigCondensed_Bold.Length;
            fontdata = Properties.Resources.BurbankBigCondensed_Bold;
            System.IntPtr weirdData = Marshal.AllocCoTaskMem(fontLength);
            Marshal.Copy(fontdata, 0, weirdData, fontLength);
            pfc.AddMemoryFont(weirdData, fontLength);

            centeredString.Alignment = StringAlignment.Center;
            rightString.Alignment = StringAlignment.Far;
            centeredStringLine.LineAlignment = StringAlignment.Center;
            centeredStringLine.Alignment = StringAlignment.Center;
        }

        private void CreatePath(TreeNodeCollection nodeList, string path)
        {
            TreeNode node = null;
            string folder = string.Empty;
            int p = path.IndexOf('/');

            if (p == -1)
            {
                folder = path;
                path = "";
            }
            else
            {
                folder = path.Substring(0, p);
                path = path.Substring(p + 1, path.Length - (p + 1));
            }

            node = null;
            foreach (TreeNode item in nodeList)
            {
                if (item.Text == folder)
                {
                    node = item;
                }
            }
            if (node == null)
            {
                node = new TreeNode(folder);
                nodeList.Add(node);
            }
            if (path != "")
            {
                CreatePath(node.Nodes, path);
            }
        }
        private void jwpmProcess(string args)
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
        private static string readPAKGuid(string pakPath)
        {
            using (BinaryReader reader = new BinaryReader(File.Open(pakPath, FileMode.Open)))
            {
                reader.BaseStream.Seek(reader.BaseStream.Length - 61, SeekOrigin.Begin);
                uint g1 = reader.ReadUInt32();
                reader.BaseStream.Seek(reader.BaseStream.Length - 57, SeekOrigin.Begin);
                uint g2 = reader.ReadUInt32();
                reader.BaseStream.Seek(reader.BaseStream.Length - 53, SeekOrigin.Begin);
                uint g3 = reader.ReadUInt32();
                reader.BaseStream.Seek(reader.BaseStream.Length - 49, SeekOrigin.Begin);
                uint g4 = reader.ReadUInt32();

                var guid = g1 + "-" + g2 + "-" + g3 + "-" + g4;
                return guid;
            }
        }

        private static string currentGUID;
        private void PAKsLoad_Click(object sender, EventArgs e)
        {
            PAKTreeView.Nodes.Clear();
            ItemsListBox.Items.Clear();
            File.WriteAllText("key.txt", AESKeyTextBox.Text.Substring(2));

            jwpmProcess("filelist \"" + Config.conf.pathToFortnitePAKs + "\\" + PAKsComboBox.SelectedItem + "\" \"" + docPath + "\""); //JWP FILELIST
            currentGUID = readPAKGuid(Config.conf.pathToFortnitePAKs + "\\" + PAKsComboBox.SelectedItem);

            if (!File.Exists(docPath + "\\" + PAKsComboBox.SelectedItem + ".txt"))
            {
                AppendText("✗ ", Color.Red);
                AppendText(" Can't read ", Color.Black);
                AppendText(PAKsComboBox.SelectedItem.ToString(), Color.SteelBlue);
                AppendText(" with this key", Color.Black, true);
            }
            else
            {
                PAKFileAsTXT = File.ReadAllLines(docPath + "\\" + PAKsComboBox.SelectedItem + ".txt");
                File.Delete(docPath + "\\" + PAKsComboBox.SelectedItem + ".txt");

                foreach (var i in PAKFileAsTXT)
                {
                    CreatePath(PAKTreeView.Nodes, i.Replace(i.Split('/').Last(), ""));
                }
            }
        }

        public static class TreeHelpers
        {
            public static IEnumerable<TItem> GetAncestors<TItem>(TItem item, Func<TItem, TItem> getParentFunc)
            {
                if (getParentFunc == null)
                {
                    throw new ArgumentNullException("getParentFunc");
                }
                if (ReferenceEquals(item, null)) yield break;
                for (TItem curItem = getParentFunc(item); !ReferenceEquals(curItem, null); curItem = getParentFunc(curItem))
                {
                    yield return curItem;
                }
            }
        }
        private void PAKTreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            List<string> beforeItems = new List<string>();
            List<string> afterItems = new List<string>();

            ItemsListBox.Items.Clear();

            var all = TreeHelpers.GetAncestors(e.Node, x => x.Parent).ToList();
            all.Reverse();
            var full = string.Join("/", all.Select(x => x.Text)) + "/" + e.Node.Text + "/";
            if (string.IsNullOrEmpty(full))
            {
                return;
            }
            var dircount = full.Count(f => f == '/');
            var dirfiles = PAKFileAsTXT.Where(x => x.StartsWith(full) && !x.Replace(full, "").Contains("/"));
            if (dirfiles.Count() == 0)
            {
                return;
            }

            ItemsListBox.Items.Clear();

            foreach (var i in dirfiles)
            {
                string v = string.Empty;
                if (i.Contains(".uasset") || i.Contains(".uexp") || i.Contains(".ubulk"))
                {
                    v = i.Substring(0, i.LastIndexOf('.'));
                }
                else
                {
                    v = i.Replace(full, "");
                }
                beforeItems.Add(v.Replace(full, ""));
            }
            afterItems = beforeItems.Distinct().ToList(); //NO DUPLICATION + NO EXTENSION = EASY TO FIND WHAT WE WANT
            foreach (var b in afterItems)
            {
                ItemsListBox.Items.Add(b);
            }

            ExtractButton.Enabled = ItemsListBox.SelectedIndex >= 0;
        }
        private void ItemsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ItemsListBox.SelectedItem != null)
            {
                ExtractButton.Enabled = true;
            }
        } //NO EXTRACT IF NOTHING SELECTED

        public void AppendText(string text, Color color, bool addNewLine = false)
        {
            ConsoleRichTextBox.SuspendLayout();
            ConsoleRichTextBox.SelectionColor = color;
            ConsoleRichTextBox.AppendText(addNewLine
                ? $"{text}{Environment.NewLine}"
                : text);
            ConsoleRichTextBox.ScrollToCaret();
            ConsoleRichTextBox.ResumeLayout();
        }

        byte[] oggFind = { 0x4F, 0x67, 0x67, 0x53 };
        byte[] oggNoHeader = { 0x4F, 0x67, 0x67, 0x53 };
        byte[] uexpToDelete = { 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x04, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x05, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x04, 0x00 };
        byte[] oggOutNewArray = null;
        static public List<int> SearchBytePattern(byte[] pattern, byte[] bytes)
        {
            List<int> positions = new List<int>();
            int patternLength = pattern.Length;
            int totalLength = bytes.Length;
            byte firstMatchByte = pattern[0];
            for (int i = 0; i < totalLength; i++)
            {
                if (firstMatchByte == bytes[i] && totalLength - i >= patternLength)
                {
                    byte[] match = new byte[patternLength];
                    Array.Copy(bytes, i, match, 0, patternLength);
                    if (match.SequenceEqual<byte>(pattern))
                    {
                        positions.Add(i);
                        i += patternLength - 1;
                    }
                }
            }
            return positions;
        }
        public static bool TryFindAndReplace<T>(T[] source, T[] pattern, T[] replacement, out T[] newArray)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern));
            if (replacement == null)
                throw new ArgumentNullException(nameof(replacement));

            newArray = null;
            if (pattern.Length > source.Length)
                return false;

            for (var start = 0; start < source.Length - pattern.Length + 1; start += 1)
            {
                var segment = new ArraySegment<T>(source, start, pattern.Length);
                if (Enumerable.SequenceEqual(segment, pattern))
                {
                    newArray = replacement.Concat(source.Skip(start + pattern.Length)).ToArray();
                    return true;
                }
            }
            return false;
        }
        private void convertToOGG(string file, string item)
        {
            var isUBULKFound = new DirectoryInfo(System.IO.Path.GetDirectoryName(file)).GetFiles(Path.GetFileNameWithoutExtension(file) + "*.ubulk", SearchOption.AllDirectories).FirstOrDefault();
            if (isUBULKFound == null)
            {
                // Handle the file not being found
                AppendText("✔ ", Color.Green);
                AppendText(item, Color.DarkRed);
                AppendText(" has no ", Color.Black);
                AppendText("UBULK file", Color.SteelBlue, true);

                string oggPattern = "OggS";
                if (File.ReadAllText(file).Contains(oggPattern))
                {
                    byte[] src = File.ReadAllBytes(file);
                    TryFindAndReplace<byte>(src, oggFind, oggNoHeader, out oggOutNewArray);
                    File.WriteAllBytes(Path.GetFileNameWithoutExtension(file) + ".temp", oggOutNewArray);

                    FileInfo fi = new FileInfo(Path.GetFileNameWithoutExtension(file) + ".temp");
                    FileStream fs = fi.Open(FileMode.Open);
                    long bytesToDelete = 4;
                    fs.SetLength(Math.Max(0, fi.Length - bytesToDelete));
                    fs.Close();

                    byte[] srcFinal = File.ReadAllBytes(Path.GetFileNameWithoutExtension(file) + ".temp");
                    int i = srcFinal.Length - 7;
                    while (srcFinal[i] == 0)
                        --i;
                    byte[] bar = new byte[i + 1];
                    Array.Copy(srcFinal, bar, i + 1);
                    AppendText("✔ ", Color.Green);
                    AppendText("Empty bytes", Color.DarkRed);
                    AppendText(" deleted successfully", Color.Black, true);

                    File.WriteAllBytes(docPath + "\\Extracted Sounds\\" + Path.GetFileNameWithoutExtension(file) + ".ogg", bar);
                    File.Delete(Path.GetFileNameWithoutExtension(file) + ".temp");
                }
                else
                {
                    AppendText("✗ ", Color.Red);
                    AppendText("No Sound Pattern Found", Color.Black, true);
                }
            }
            else
            {
                // The file variable has the *first* occurrence of that filename
                AppendText("✔ ", Color.Green);
                AppendText(item, Color.DarkRed);
                AppendText(" extracted with an ", Color.Black);
                AppendText("UBULK file", Color.SteelBlue, true);

                string oggPattern = "OggS";
                if (File.ReadAllText(file).Contains(oggPattern))
                {
                    byte[] src = File.ReadAllBytes(file);
                    List<int> positions = SearchBytePattern(uexpToDelete, src);

                    AppendText("UBULK Footer Index: ", Color.Black);
                    AppendText(positions[0].ToString("X2"), Color.Green, true);
                    AppendText("Source Last Index: ", Color.Black);
                    AppendText(src.Length.ToString("X2"), Color.Green, true);

                    TryFindAndReplace<byte>(src, oggFind, oggNoHeader, out oggOutNewArray);
                    File.WriteAllBytes(Path.GetFileNameWithoutExtension(file) + ".temp", oggOutNewArray);

                    int lengthToDelete = src.Length - positions[0];

                    FileInfo fi = new FileInfo(Path.GetFileNameWithoutExtension(file) + ".temp");
                    FileStream fs = fi.Open(FileMode.Open);
                    long bytesToDelete = lengthToDelete;
                    fs.SetLength(Math.Max(0, fi.Length - bytesToDelete));
                    fs.Close();

                    byte[] src44 = File.ReadAllBytes(Path.GetFileNameWithoutExtension(file) + ".temp");
                    byte[] srcUBULK = File.ReadAllBytes(Path.GetDirectoryName(file) + "\\" + isUBULKFound.ToString());
                    byte[] buffer = new byte[srcUBULK.Length];
                    using (FileStream fs1 = new FileStream(Path.GetDirectoryName(file) + "\\" + isUBULKFound.ToString(), FileMode.Open, FileAccess.ReadWrite))
                    {
                        AppendText("✔ ", Color.Green);
                        AppendText("Writing ", Color.Black);
                        AppendText("UBULK Data", Color.DarkRed, true);

                        fs1.Read(buffer, 0, buffer.Length);

                        FileStream fs2 = new FileStream(Path.GetFileNameWithoutExtension(file) + ".temp", FileMode.Open, FileAccess.ReadWrite);
                        fs2.Position = src44.Length;
                        fs2.Write(buffer, 0, buffer.Length);
                        fs2.Close();
                        fs1.Close();
                    }

                    byte[] srcFinal = File.ReadAllBytes(Path.GetFileNameWithoutExtension(file) + ".temp");
                    int i = srcFinal.Length - 1;
                    while (srcFinal[i] == 0)
                        --i;
                    byte[] bar = new byte[i + 1];
                    Array.Copy(srcFinal, bar, i + 1);
                    AppendText("✔ ", Color.Green);
                    AppendText("Empty bytes", Color.DarkRed);
                    AppendText(" deleted successfully", Color.Black, true);

                    File.WriteAllBytes(docPath + "\\Extracted Sounds\\" + Path.GetFileNameWithoutExtension(file) + ".ogg", bar);
                    File.Delete(Path.GetFileNameWithoutExtension(file) + ".temp");
                }
                else
                {
                    AppendText("✗ ", Color.Red);
                    AppendText("No Sound Pattern Found", Color.Black, true);
                }
            }
        }

        private void ExtractButton_Click(object sender, EventArgs e)
        {
            ItemRichTextBox.Text = "";
            ItemIconPictureBox.Image = null;

            if (!Directory.Exists(docPath + "\\Generated Icons\\")) //Create Generated Icons Subfolder
                Directory.CreateDirectory(docPath + "\\Generated Icons\\");
            if (!Directory.Exists(docPath + "\\Extracted Sounds\\")) //Create Generated Icons Subfolder
                Directory.CreateDirectory(docPath + "\\Extracted Sounds\\");

            foreach (var sItems in ItemsListBox.SelectedItems)
            {
                var files = Directory.GetFiles(docPath + "\\Extracted", sItems + ".*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".png")).FirstOrDefault();
                if (!File.Exists(files))
                {
                    jwpmProcess("extract \"" + Config.conf.pathToFortnitePAKs + "\\" + PAKsComboBox.SelectedItem + "\" \"" + sItems + "\" \"" + docPath + "\"");
                    files = Directory.GetFiles(docPath + "\\Extracted", sItems + ".*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".png")).FirstOrDefault();
                }
                if (files != null)
                {
                    AppendText("", Color.Black, true);
                    AppendText("✔ ", Color.Green);
                    AppendText(sItems.ToString(), Color.DarkRed);
                    AppendText(" successfully extracted to ", Color.Black);
                    AppendText(files.Substring(0, files.LastIndexOf('.')), Color.SteelBlue, true);

                    if (files.Contains(".uasset") || files.Contains(".uexp") || files.Contains(".ubulk"))
                    {
                        AppendText("✔ ", Color.Green);
                        AppendText(sItems.ToString(), Color.DarkRed);
                        AppendText(" is an ", Color.Black);
                        AppendText("asset", Color.SteelBlue, true);

                        jwpmProcess("serialize \"" + files.Substring(0, files.LastIndexOf('.')) + "\"");
                        var filesJSON = Directory.GetFiles(docPath, sItems + ".json", SearchOption.AllDirectories).FirstOrDefault();
                        if (filesJSON != null)
                        {
                            var json = JToken.Parse(File.ReadAllText(filesJSON)).ToString();
                            File.Delete(filesJSON);
                            AppendText("✔ ", Color.Green);
                            AppendText(sItems.ToString(), Color.DarkRed);
                            AppendText(" successfully serialized", Color.Black, true);
                            ItemRichTextBox.Text = json;

                            var IDParser = ItemsIdParser.FromJson(json);

                            if (LoadDataCheckBox.Checked == true)
                            {
                                AppendText("Auto loading data set to ", Color.Black);
                                AppendText("True", Color.Green, true);

                                if (filesJSON.Contains("Athena\\Items\\Cosmetics"))
                                {
                                    AppendText("✔ ", Color.Green);
                                    AppendText(sItems.ToString(), Color.DarkRed);
                                    AppendText(" is an ", Color.Black);
                                    AppendText("ID file", Color.SteelBlue, true);
                                    AppendText("Parsing...", Color.Black, true);
                                    foreach (var data in IDParser)
                                    {
                                        if (data.ExportType.Contains("Item") && data.ExportType.Contains("Definition"))
                                        {
                                            ItemName = data.DisplayName;
                                            Bitmap bmp = new Bitmap(522, 522);
                                            Graphics g = Graphics.FromImage(bmp);
                                            if (data.Rarity == "EFortRarity::Legendary")
                                            {
                                                Image RarityBG = Properties.Resources.I512;
                                                g.DrawImage(RarityBG, new Point(0, 0));
                                                AppendText("Item Rarity: ", Color.Black);
                                                AppendText("IMPOSSIBLE (T9)", Color.DarkOrange, true);
                                            }
                                            if (data.Rarity == "EFortRarity::Masterwork")
                                            {
                                                Image RarityBG = Properties.Resources.T512;
                                                g.DrawImage(RarityBG, new Point(0, 0));
                                                AppendText("Item Rarity: ", Color.Black);
                                                AppendText("TRANSCENDENT", Color.OrangeRed, true);
                                            }
                                            if (data.Rarity == "EFortRarity::Elegant")
                                            {
                                                Image RarityBG = Properties.Resources.M512;
                                                g.DrawImage(RarityBG, new Point(0, 0));
                                                AppendText("Item Rarity: ", Color.Black);
                                                AppendText("MYTHIC", Color.Yellow, true);
                                            }
                                            if (data.Rarity == "EFortRarity::Fine")
                                            {
                                                Image RarityBG = Properties.Resources.L512;
                                                g.DrawImage(RarityBG, new Point(0, 0));
                                                AppendText("Item Rarity: ", Color.Black);
                                                AppendText("LEGENDARY", Color.Orange, true);
                                            }
                                            if (data.Rarity == "EFortRarity::Quality")
                                            {
                                                Image RarityBG = Properties.Resources.E512;
                                                g.DrawImage(RarityBG, new Point(0, 0));
                                                AppendText("Item Rarity: ", Color.Black);
                                                AppendText("EPIC", Color.Purple, true);
                                            }
                                            if (data.Rarity == "EFortRarity::Sturdy")
                                            {
                                                Image RarityBG = Properties.Resources.R512;
                                                g.DrawImage(RarityBG, new Point(0, 0));
                                                AppendText("Item Rarity: ", Color.Black);
                                                AppendText("RARE", Color.Blue, true);
                                            }
                                            if (data.Rarity == "EFortRarity::Handmade")
                                            {
                                                Image RarityBG = Properties.Resources.C512;
                                                g.DrawImage(RarityBG, new Point(0, 0));
                                                AppendText("Item Rarity: ", Color.Black);
                                                AppendText("COMMON", Color.DarkGray, true);
                                            }
                                            if (data.Rarity == null)
                                            {
                                                Image RarityBG = Properties.Resources.U512;
                                                g.DrawImage(RarityBG, new Point(0, 0));
                                                AppendText("Item Rarity: ", Color.Black);
                                                AppendText("UNCOMMON", Color.Green, true);
                                            }

                                            string IMGPath = string.Empty;
                                            if (data.LargePreviewImage != null)
                                            {
                                                string textureFile = Path.GetFileName(data.LargePreviewImage.AssetPathName).Substring(0, Path.GetFileName(data.LargePreviewImage.AssetPathName).LastIndexOf('.'));
                                                AppendText("✔ ", Color.Green);
                                                AppendText(textureFile, Color.DarkRed);
                                                AppendText(" detected as a ", Color.Black);
                                                AppendText("Texture2D file", Color.SteelBlue, true);

                                                var filesPath = Directory.GetFiles(docPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                                if (!File.Exists(filesPath))
                                                {
                                                    if (currentGUID != "0-0-0-0")
                                                    {
                                                        jwpmProcess("extract \"" + Config.conf.pathToFortnitePAKs + "\\" + PAKsComboBox.SelectedItem + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                        filesPath = Directory.GetFiles(docPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                                    }
                                                    else
                                                    {
                                                        if (data.LargePreviewImage.AssetPathName.Contains("/Game/2dAssets/"))
                                                        {
                                                            jwpmProcess("extract \"" + Config.conf.pathToFortnitePAKs + "\\pakchunk0-WindowsClient.pak" + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                        }
                                                        else if (data.LargePreviewImage.AssetPathName.Contains("/Game/Athena/TestAssets/") || data.LargePreviewImage.AssetPathName.Contains("/Game/Athena/Prototype/"))
                                                        {
                                                            jwpmProcess("extract \"" + Config.conf.pathToFortnitePAKs + "\\" + PAKsComboBox.SelectedItem + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                        }
                                                        else
                                                        {
                                                            jwpmProcess("extract \"" + Config.conf.pathToFortnitePAKs + "\\pakchunk0_s7-WindowsClient.pak" + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                        }
                                                        filesPath = Directory.GetFiles(docPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                                    }
                                                }
                                                try
                                                {
                                                    if (filesPath != null)
                                                    {
                                                        AppendText("✔ ", Color.Green);
                                                        AppendText(textureFile, Color.DarkRed);
                                                        AppendText(" successfully extracted to ", Color.Black);
                                                        AppendText(filesPath.Substring(0, filesPath.LastIndexOf('.')), Color.SteelBlue, true);

                                                        IMGPath = filesPath.Substring(0, filesPath.LastIndexOf('.')) + ".png";
                                                        if (!File.Exists(IMGPath))
                                                        {
                                                            jwpmProcess("texture \"" + filesPath.Substring(0, filesPath.LastIndexOf('.')) + "\"");
                                                            IMGPath = filesPath.Substring(0, filesPath.LastIndexOf('.')) + ".png";
                                                        }

                                                        AppendText("✔ ", Color.Green);
                                                        AppendText(textureFile, Color.DarkRed);
                                                        AppendText(" successfully converted to a PNG image with path ", Color.Black);
                                                        AppendText(IMGPath, Color.SteelBlue, true);
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
                                            else if (data.SmallPreviewImage != null)
                                            {
                                                string textureFile = Path.GetFileName(data.SmallPreviewImage.AssetPathName).Substring(0, Path.GetFileName(data.SmallPreviewImage.AssetPathName).LastIndexOf('.'));
                                                AppendText("✔ ", Color.Green);
                                                AppendText(textureFile, Color.DarkRed);
                                                AppendText(" detected as a ", Color.Black);
                                                AppendText("Texture2D file", Color.SteelBlue, true);

                                                var filesPath = Directory.GetFiles(docPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                                if (!File.Exists(filesPath))
                                                {
                                                    if (currentGUID != "0-0-0-0")
                                                    {
                                                        jwpmProcess("extract \"" + Config.conf.pathToFortnitePAKs + "\\" + PAKsComboBox.SelectedItem + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                        filesPath = Directory.GetFiles(docPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                                    }
                                                    else
                                                    {
                                                        if (data.SmallPreviewImage.AssetPathName.Contains("/Game/2dAssets/"))
                                                        {
                                                            jwpmProcess("extract \"" + Config.conf.pathToFortnitePAKs + "\\pakchunk0-WindowsClient.pak" + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                        }
                                                        else if (data.SmallPreviewImage.AssetPathName.Contains("/Game/Athena/TestAssets/"))
                                                        {
                                                            jwpmProcess("extract \"" + Config.conf.pathToFortnitePAKs + "\\" + PAKsComboBox.SelectedItem + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                        }
                                                        else
                                                        {
                                                            jwpmProcess("extract \"" + Config.conf.pathToFortnitePAKs + "\\pakchunk0_s7-WindowsClient.pak" + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                        }
                                                        filesPath = Directory.GetFiles(docPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                                    }
                                                }
                                                try
                                                {
                                                    if (filesPath != null)
                                                    {
                                                        AppendText("✔ ", Color.Green);
                                                        AppendText(textureFile, Color.DarkRed);
                                                        AppendText(" successfully extracted to ", Color.Black);
                                                        AppendText(filesPath.Substring(0, filesPath.LastIndexOf('.')), Color.SteelBlue, true);

                                                        IMGPath = filesPath.Substring(0, filesPath.LastIndexOf('.')) + ".png";
                                                        if (!File.Exists(IMGPath))
                                                        {
                                                            jwpmProcess("texture \"" + filesPath.Substring(0, filesPath.LastIndexOf('.')) + "\"");
                                                            IMGPath = filesPath.Substring(0, filesPath.LastIndexOf('.')) + ".png";
                                                        }

                                                        AppendText("✔ ", Color.Green);
                                                        AppendText(textureFile, Color.DarkRed);
                                                        AppendText(" successfully converted to a PNG image with path ", Color.Black);
                                                        AppendText(IMGPath, Color.SteelBlue, true);
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
                                            if (data.HeroDefinition != null)
                                            {
                                                var filesPath = Directory.GetFiles(docPath + "\\Extracted", data.HeroDefinition + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                                if (!File.Exists(filesPath))
                                                {
                                                    AppendText("✔ ", Color.Green);
                                                    AppendText("Extracting ", Color.Black);
                                                    AppendText(data.HeroDefinition, Color.DarkRed, true);

                                                    jwpmProcess("extract \"" + Config.conf.pathToFortnitePAKs + "\\" + PAKsComboBox.SelectedItem + "\" \"" + data.HeroDefinition + "\" \"" + docPath + "\"");
                                                    filesPath = Directory.GetFiles(docPath + "\\Extracted", data.HeroDefinition + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                                }
                                                try
                                                {
                                                    if (filesPath != null)
                                                    {
                                                        AppendText("✔ ", Color.Green);
                                                        AppendText(data.HeroDefinition, Color.DarkRed);
                                                        AppendText(" successfully extracted to ", Color.Black);
                                                        AppendText(filesPath.Substring(0, filesPath.LastIndexOf('.')), Color.SteelBlue, true);
                                                        try
                                                        {
                                                            jwpmProcess("serialize \"" + filesPath.Substring(0, filesPath.LastIndexOf('.')) + "\"");
                                                            var filesJSON2 = Directory.GetFiles(docPath, data.HeroDefinition + ".json", SearchOption.AllDirectories).FirstOrDefault();
                                                            var json2 = JToken.Parse(File.ReadAllText(filesJSON2)).ToString();
                                                            File.Delete(filesJSON2);
                                                            AppendText("✔ ", Color.Green);
                                                            AppendText(data.HeroDefinition, Color.DarkRed);
                                                            AppendText(" successfully serialized", Color.Black, true);

                                                            var IDParser2 = ItemsIdParser.FromJson(json2);
                                                            foreach (var data2 in IDParser2)
                                                            {
                                                                if (data2.LargePreviewImage != null)
                                                                {
                                                                    string textureFile = Path.GetFileName(data2.LargePreviewImage.AssetPathName).Substring(0, Path.GetFileName(data2.LargePreviewImage.AssetPathName).LastIndexOf('.'));
                                                                    AppendText("✔ ", Color.Green);
                                                                    AppendText(textureFile, Color.DarkRed);
                                                                    AppendText(" detected as a ", Color.Black);
                                                                    AppendText("Texture2D file", Color.SteelBlue, true);

                                                                    var filesPath2 = Directory.GetFiles(docPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                                                    if (!File.Exists(filesPath2))
                                                                    {
                                                                        if (currentGUID != "0-0-0-0")
                                                                        {
                                                                            jwpmProcess("extract \"" + Config.conf.pathToFortnitePAKs + "\\" + PAKsComboBox.SelectedItem + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                            filesPath2 = Directory.GetFiles(docPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                                                        }
                                                                        else
                                                                        {
                                                                            jwpmProcess("extract \"" + Config.conf.pathToFortnitePAKs + "\\pakchunk0_s7-WindowsClient.pak" + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                            filesPath2 = Directory.GetFiles(docPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                                                        }
                                                                    }
                                                                    try
                                                                    {
                                                                        if (filesPath2 != null)
                                                                        {
                                                                            AppendText("✔ ", Color.Green);
                                                                            AppendText(textureFile, Color.DarkRed);
                                                                            AppendText(" successfully extracted to ", Color.Black);
                                                                            AppendText(filesPath2.Substring(0, filesPath2.LastIndexOf('.')), Color.SteelBlue, true);

                                                                            IMGPath = filesPath2.Substring(0, filesPath2.LastIndexOf('.')) + ".png";
                                                                            if (!File.Exists(IMGPath))
                                                                            {
                                                                                jwpmProcess("texture \"" + filesPath2.Substring(0, filesPath2.LastIndexOf('.')) + "\"");
                                                                                IMGPath = filesPath2.Substring(0, filesPath2.LastIndexOf('.')) + ".png";
                                                                            }
                                                                            
                                                                            AppendText("✔ ", Color.Green);
                                                                            AppendText(textureFile, Color.DarkRed);
                                                                            AppendText(" successfully converted to a PNG image with path ", Color.Black);
                                                                            AppendText(IMGPath, Color.SteelBlue, true);
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
                                                    AppendText(data.HeroDefinition, Color.SteelBlue);
                                                    AppendText(" in ", Color.Black);
                                                    AppendText(PAKsComboBox.SelectedItem.ToString(), Color.DarkRed, true);
                                                }
                                            }

                                            if (File.Exists(IMGPath))
                                            {
                                                Image ItemIcon = Image.FromFile(IMGPath);
                                                g.DrawImage(ItemIcon, new Point(5, 5));
                                            }
                                            else
                                            {
                                                Image ItemIcon = Properties.Resources.unknown512;
                                                g.DrawImage(ItemIcon, new Point(0, 0));
                                            }

                                            Image bg512 = Properties.Resources.BG512;
                                            g.DrawImage(bg512, new Point(5, 383));

                                            try
                                            {
                                                g.DrawString(ItemName, new Font(pfc.Families[0], 40), new SolidBrush(Color.White), new Point(522 / 2, 390), centeredString);
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
                                                g.DrawString(data.Description, new Font("Arial", 10), new SolidBrush(Color.White), new Point(522 / 2, 465), centeredStringLine);
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
                                                g.DrawString(data.ShortDescription, new Font(pfc.Families[0], 13), new SolidBrush(Color.White), new Point(5, 498));
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
                                                g.DrawString(data.GameplayTags.GameplayTagsGameplayTags[Array.FindIndex(data.GameplayTags.GameplayTagsGameplayTags, x => x.StartsWith("Cosmetics.Source."))].Substring(17), new Font(pfc.Families[0], 13), new SolidBrush(Color.White), new Point(522 - 5, 498), rightString);
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
                                                AppendText("GameplayTags as Cosmetics.Source ", Color.SteelBlue);
                                                AppendText("found", Color.Black, true);
                                            } //COSMETIC SOURCE

                                            ItemIconPictureBox.Image = bmp;
                                            if (SaveImageCheckBox.Checked == true)
                                            {
                                                AppendText("Auto saving icon set to ", Color.Black);
                                                AppendText("True", Color.Green, true);
                                                ItemIconPictureBox.Image.Save(docPath + "\\Generated Icons\\" + ItemName + ".png", ImageFormat.Png);

                                                AppendText("✔ ", Color.Green);
                                                AppendText(ItemName, Color.DarkRed);
                                                AppendText(" successfully saved to ", Color.Black);
                                                AppendText(docPath + "\\Generated Icons\\" + ItemName + ".png", Color.SteelBlue, true);
                                            }
                                        }
                                    }
                                }
                                foreach (var data in IDParser)
                                {
                                    if (data.ExportType == "Texture2D")
                                    {
                                        AppendText("Parsing...", Color.Black, true);
                                        ItemName = sItems.ToString();

                                        AppendText("✔ ", Color.Green);
                                        AppendText(sItems.ToString(), Color.DarkRed);
                                        AppendText(" detected as a ", Color.Black);
                                        AppendText("Texture2D file", Color.SteelBlue, true);

                                        string IMGPath = string.Empty;

                                        var filesPath = Directory.GetFiles(docPath + "\\Extracted", sItems + ".*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".png")).FirstOrDefault();
                                        if (!File.Exists(filesPath))
                                        {
                                            jwpmProcess("extract \"" + Config.conf.pathToFortnitePAKs + "\\" + PAKsComboBox.SelectedItem + "\" \"" + sItems + "\" \"" + docPath + "\"");
                                            filesPath = Directory.GetFiles(docPath + "\\Extracted", sItems + ".*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".png")).FirstOrDefault();
                                        }
                                        try
                                        {
                                            if (filesPath != null)
                                            {
                                                AppendText("✔ ", Color.Green);
                                                AppendText(sItems.ToString(), Color.DarkRed);
                                                AppendText(" successfully extracted to ", Color.Black);
                                                AppendText(filesPath.Substring(0, filesPath.LastIndexOf('.')), Color.SteelBlue, true);

                                                IMGPath = filesPath.Substring(0, filesPath.LastIndexOf('.')) + ".png";
                                                if (!File.Exists(IMGPath))
                                                {
                                                    jwpmProcess("texture \"" + filesPath.Substring(0, filesPath.LastIndexOf('.')) + "\"");
                                                    IMGPath = filesPath.Substring(0, filesPath.LastIndexOf('.')) + ".png";
                                                }
                                                
                                                AppendText("✔ ", Color.Green);
                                                AppendText(sItems.ToString(), Color.DarkRed);
                                                AppendText(" successfully converted to a PNG image with path ", Color.Black);
                                                AppendText(IMGPath, Color.SteelBlue, true);
                                            }
                                        }
                                        catch (IndexOutOfRangeException)
                                        {
                                            AppendText("[IndexOutOfRangeException] ", Color.Red);
                                            AppendText("Can't extract ", Color.Black);
                                            AppendText(sItems.ToString(), Color.SteelBlue);
                                            AppendText(" in ", Color.Black);
                                            AppendText(PAKsComboBox.SelectedItem.ToString(), Color.DarkRed, true);
                                        }

                                        if (File.Exists(IMGPath))
                                        {
                                            ItemIconPictureBox.Image = Image.FromFile(IMGPath);
                                        }
                                        else
                                        {
                                            ItemIconPictureBox.Image = Properties.Resources.unknown512;
                                        }

                                        if (SaveImageCheckBox.Checked == true)
                                        {
                                            AppendText("Auto saving icon set to ", Color.Black);
                                            AppendText("True", Color.Green, true);
                                            ItemIconPictureBox.Image.Save(docPath + "\\Generated Icons\\" + ItemName + ".png", ImageFormat.Png);

                                            AppendText("✔ ", Color.Green);
                                            AppendText(ItemName, Color.DarkRed);
                                            AppendText(" successfully saved to ", Color.Black);
                                            AppendText(docPath + "\\Generated Icons\\" + ItemName + ".png", Color.SteelBlue, true);
                                        }
                                    }
                                    if (data.ExportType == "SoundWave")
                                    {
                                        AppendText("Parsing...", Color.Black, true);
                                        ItemName = sItems.ToString();

                                        AppendText("✔ ", Color.Green);
                                        AppendText(sItems.ToString(), Color.DarkRed);
                                        AppendText(" detected as a ", Color.Black);
                                        AppendText("SoundWave file", Color.SteelBlue, true);

                                        string MusicPath = Directory.GetFiles(docPath + "\\Extracted Sounds", sItems + ".ogg", SearchOption.AllDirectories).FirstOrDefault();
                                        if (!File.Exists(MusicPath))
                                        {
                                            var filesPath = Directory.GetFiles(docPath + "\\Extracted", sItems + ".uexp", SearchOption.AllDirectories).FirstOrDefault();
                                            if (!File.Exists(filesPath))
                                            {
                                                jwpmProcess("extract \"" + Config.conf.pathToFortnitePAKs + "\\" + PAKsComboBox.SelectedItem + "\" \"" + sItems + "\" \"" + docPath + "\"");
                                                filesPath = Directory.GetFiles(docPath + "\\Extracted", sItems + ".uexp", SearchOption.AllDirectories).FirstOrDefault();
                                            }
                                            try
                                            {
                                                if (filesPath != null)
                                                {
                                                    AppendText("✔ ", Color.Green);
                                                    AppendText(sItems.ToString(), Color.DarkRed);
                                                    AppendText(" successfully extracted to ", Color.Black);
                                                    AppendText(filesPath.Substring(0, filesPath.LastIndexOf('.')), Color.SteelBlue, true);
                                                    try
                                                    {
                                                        convertToOGG(filesPath, sItems.ToString());
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Console.WriteLine(ex.Message);
                                                    }

                                                    MusicPath = docPath + "\\Extracted Sounds\\" + Path.GetFileNameWithoutExtension(filesPath) + ".ogg";
                                                    AppendText("✔ ", Color.Green);
                                                    AppendText(sItems.ToString(), Color.DarkRed);
                                                    AppendText(" successfully converted to an OGG sound with path ", Color.Black);
                                                    AppendText(MusicPath, Color.SteelBlue, true);
                                                }
                                            }
                                            catch (IndexOutOfRangeException)
                                            {
                                                AppendText("[IndexOutOfRangeException] ", Color.Red);
                                                AppendText("Can't extract ", Color.Black);
                                                AppendText(sItems.ToString(), Color.SteelBlue);
                                                AppendText(" in ", Color.Black);
                                                AppendText(PAKsComboBox.SelectedItem.ToString(), Color.DarkRed, true);
                                            }
                                        }
                                        OpenWithDefaultProgramAndNoFocus(MusicPath);
                                    }
                                }
                            }
                        }
                        else
                        {
                            AppendText("✗ ", Color.Red);
                            AppendText("No serialized file found", Color.Black, true);
                        }
                    }
                    if (files.Contains(".ini"))
                    {
                        ItemRichTextBox.Text = File.ReadAllText(files);
                    }
                }
                else
                {
                    AppendText("", Color.Black, true);
                    AppendText("✗ ", Color.Red);
                    AppendText(" Error while extracting ", Color.Black);
                    AppendText(sItems.ToString(), Color.SteelBlue, true);
                }
            }
            AppendText("\nDone", Color.Green, true);
        }

        private void LoadImageCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (LoadDataCheckBox.Checked == true)
            {
                SaveImageButton.Enabled = true;
                SaveImageCheckBox.Enabled = true;
            }
            if (LoadDataCheckBox.Checked == false)
            {
                SaveImageButton.Enabled = false;
                SaveImageCheckBox.Enabled = false;
            }
        }
        private void SaveImageCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (SaveImageCheckBox.Checked == true)
            {
                SaveImageButton.Enabled = false;
            }
            if (SaveImageCheckBox.Checked == false)
            {
                SaveImageButton.Enabled = true;
            }
        }

        public static void OpenWithDefaultProgramAndNoFocus(string path)
        {
            Process fileopener = new Process();
            fileopener.StartInfo.FileName = "explorer";
            fileopener.StartInfo.Arguments = "\"" + path + "\"";
            fileopener.Start();
        }

        private void SaveImageButton_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveTheDialog = new SaveFileDialog();
            saveTheDialog.Title = "Save Icon";
            saveTheDialog.Filter = "PNG Files (*.png)|*.png";
            saveTheDialog.InitialDirectory = docPath + "\\Generated Icons\\";
            saveTheDialog.FileName = ItemName;
            if (saveTheDialog.ShowDialog() == DialogResult.OK)
            {
                ItemIconPictureBox.Image.Save(saveTheDialog.FileName, ImageFormat.Png);
                AppendText("✔ ", Color.Green);
                AppendText(ItemName, Color.DarkRed);
                AppendText(" successfully saved to ", Color.Black);
                AppendText(saveTheDialog.FileName, Color.SteelBlue, true);
            }
        }
    }
}
