using FModel.Items;
using FModel.Challenges;
using FModel.Quest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Text.RegularExpressions;
using System.Text;

namespace FModel
{
    public partial class PAKWindow : Form
    {
        public static string docPath;
        private static string[] PAKFileAsTXT;
        private static string[] AllPAKs;
        private static bool isAllPAKs;
        private static string ItemName;
        private static List<string> afterItems;
        public static string[] SelectedArray;
        public static string[] challengesArray;
        public static Dictionary<string, string> AllPAKsDict;
        public static Dictionary<string, long> questStageDict;
        public static Dictionary<string, long> questDictFinal;

        PrivateFontCollection pfc = new PrivateFontCollection();
        StringFormat centeredString = new StringFormat();
        StringFormat rightString = new StringFormat();
        StringFormat centeredStringLine = new StringFormat();
        private int fontLength;
        private byte[] fontdata;
        private int fontLength2;
        private byte[] fontdata2;

        [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string pszSubIdList);
        public static void SetTreeViewTheme(IntPtr treeHandle)
        {
            SetWindowTheme(treeHandle, "explorer", null);
        }

        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int description, int reservedValue);
        public static bool IsInternetAvailable()
        {
            int description;
            return InternetGetConnectedState(out description, 0);
        }

        public PAKWindow()
        {
            InitializeComponent();
        }

        /*****************************
         USEFUL STUFF FOR THIS PROJECT
         *****************************/
        private void AppendText(string text, Color color, bool addNewLine = false, HorizontalAlignment align = HorizontalAlignment.Left)
        {
            ConsoleRichTextBox.SuspendLayout();
            ConsoleRichTextBox.SelectionColor = color;
            ConsoleRichTextBox.SelectionAlignment = align;
            ConsoleRichTextBox.AppendText(addNewLine
                ? $"{text}{Environment.NewLine}"
                : text);
            ConsoleRichTextBox.ScrollToCaret();
            ConsoleRichTextBox.ResumeLayout();
        }
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
        public static void OpenWithDefaultProgramAndNoFocus(string path)
        {
            Process fileopener = new Process();
            fileopener.StartInfo.FileName = "explorer";
            fileopener.StartInfo.Arguments = "\"" + path + "\"";
            fileopener.Start();
        }

        private void PAKWindow_Load(object sender, EventArgs e)
        {
            bool connection = IsInternetAvailable();

            SetTreeViewTheme(PAKTreeView.Handle);
            Properties.Settings.Default.ExtractAndSerialize = true; //SERIALIZE BY DEFAULT

            docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).ToString() + "\\FModel";
            if (string.IsNullOrEmpty(Properties.Settings.Default.ExtractOutput))
            {
                Properties.Settings.Default.ExtractOutput = docPath;
                Properties.Settings.Default.Save();
            }
            else
            {
                docPath = Properties.Settings.Default.ExtractOutput;
            }

            if (!Directory.Exists(Properties.Settings.Default.FortnitePAKs))
            {
                AppendText("[PathNotFoundException] ", Color.Red);
                AppendText(" Please go to the ", Color.Black);
                AppendText("Load button drop down menu ", Color.SteelBlue);
                AppendText(", ", Color.Black);
                AppendText("click on Options ", Color.SteelBlue);
                AppendText("and enter your Fortnite .PAK files path", Color.Black, true);
            }
            else
            {
                IEnumerable<string> yourPAKs = Directory.GetFiles(Properties.Settings.Default.FortnitePAKs).Where(x => x.EndsWith(".pak"));
                AllPAKs = new string[yourPAKs.Count()];
                for (int i = 0; i < yourPAKs.Count(); i++)
                {
                    PAKsComboBox.Items.Add(Path.GetFileName(yourPAKs.ElementAt(i)));
                    AllPAKs[i] = Path.GetFileName(yourPAKs.ElementAt(i));
                }
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

            FileInfo info;
            if (!File.Exists(docPath + "\\john-wick-parse-modded.exe") && connection == true)
            {
                WebClient Client = new WebClient();
                Client.DownloadFile("https://dl.dropbox.com/s/9y5rv3hycin3w8r/john-wick-parse-modded.exe?dl=0", docPath + "\\john-wick-parse-modded.exe");
                info = new FileInfo(docPath + "\\john-wick-parse-modded.exe");

                AppendText("[FileNotFoundException] ", Color.Red);
                AppendText("File ", Color.Black);
                AppendText("john-wick-parse-modded.exe ", Color.SteelBlue);
                AppendText("downloaded successfully", Color.Black, true);
            }
            else if (!File.Exists(docPath + "\\john-wick-parse-modded.exe") && connection == false)
            {
                AppendText("Can't download ", Color.Black);
                AppendText("john-wick-parse-modded.exe", Color.SteelBlue);
                AppendText(", no internet connection", Color.Black, true);
            }

            if (File.Exists(docPath + "\\john-wick-parse-modded.exe"))
            {
                info = new FileInfo(docPath + "\\john-wick-parse-modded.exe");

                string url = "https://pastebin.com/raw/0fbB05hc";
                long fileSize = 0;
                if (connection == true)
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (Stream stream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        fileSize = Convert.ToInt64(reader.ReadToEnd());
                    }
                    if (info.Length != fileSize)
                    {
                        WebClient Client = new WebClient();
                        Client.DownloadFile("https://dl.dropbox.com/s/9y5rv3hycin3w8r/john-wick-parse-modded.exe?dl=0", docPath + "\\john-wick-parse-modded.exe");

                        AppendText("[FileNeedUpdateException] ", Color.Red);
                        AppendText("john-wick-parse-modded.exe ", Color.SteelBlue);
                        AppendText("updated successfully", Color.Black, true);
                    }
                }
                else
                {
                    AppendText("Can't check if ", Color.Black);
                    AppendText("john-wick-parse-modded.exe ", Color.SteelBlue);
                    AppendText("needs to be updated", Color.Black, true);
                }
            }

            ExtractAssetButton.Enabled = false;
            SaveImageButton.Enabled = false;

            fontLength = Properties.Resources.BurbankBigCondensed_Bold.Length;
            fontdata = Properties.Resources.BurbankBigCondensed_Bold;
            System.IntPtr weirdData = Marshal.AllocCoTaskMem(fontLength);
            Marshal.Copy(fontdata, 0, weirdData, fontLength);
            pfc.AddMemoryFont(weirdData, fontLength);

            fontLength2 = Properties.Resources.BurbankBigCondensed_Black.Length;
            fontdata2 = Properties.Resources.BurbankBigCondensed_Black;
            System.IntPtr weirdData2 = Marshal.AllocCoTaskMem(fontLength2);
            Marshal.Copy(fontdata2, 0, weirdData2, fontLength2);
            pfc.AddMemoryFont(weirdData2, fontLength2);

            centeredString.Alignment = StringAlignment.Center;
            rightString.Alignment = StringAlignment.Far;
            centeredStringLine.LineAlignment = StringAlignment.Center;
            centeredStringLine.Alignment = StringAlignment.Center;

            // Configure the JSON lexer styles
            scintilla1.Styles[ScintillaNET.Style.Json.Default].ForeColor = Color.Silver;
            scintilla1.Styles[ScintillaNET.Style.Json.BlockComment].ForeColor = Color.FromArgb(0, 128, 0);
            scintilla1.Styles[ScintillaNET.Style.Json.LineComment].ForeColor = Color.FromArgb(0, 128, 0);
            scintilla1.Styles[ScintillaNET.Style.Json.Number].ForeColor = Color.Green;
            scintilla1.Styles[ScintillaNET.Style.Json.PropertyName].ForeColor = Color.SteelBlue; ;
            scintilla1.Styles[ScintillaNET.Style.Json.String].ForeColor = Color.OrangeRed;
            scintilla1.Styles[ScintillaNET.Style.Json.StringEol].BackColor = Color.OrangeRed;
            scintilla1.Styles[ScintillaNET.Style.Json.Operator].ForeColor = Color.Black;
            scintilla1.Lexer = ScintillaNET.Lexer.Json;
        } //EVERYTHING TO SET WHEN APP IS STARTING
        private void PAKWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            Environment.Exit(Environment.ExitCode);
        } //STOP EVERYTHING WHEN FORM IS CLOSING

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

        private static string currentPAK;
        private static string currentGUID;
        private async void LoadButton_Click(object sender, EventArgs e)
        {
            isAllPAKs = false;
            if (PAKsComboBox.SelectedItem == null)
            {
                AppendText("Please, select one of your ", Color.Black);
                AppendText("Fortnite .PAK files ", Color.SteelBlue);
                AppendText("to load.", Color.Black, true);
            }
            else
            {
                PAKTreeView.Nodes.Clear();
                ItemsListBox.Items.Clear();
                File.WriteAllText("key.txt", AESKeyTextBox.Text.Substring(2));

                currentPAK = PAKsComboBox.SelectedItem.ToString();
                LoadButton.Enabled = false;
                await Task.Run(() => {
                    jwpmProcess("filelist \"" + Properties.Settings.Default.FortnitePAKs + "\\" + currentPAK + "\" \"" + docPath + "\"");
                });
                LoadButton.Enabled = true;
                currentGUID = readPAKGuid(Properties.Settings.Default.FortnitePAKs + "\\" + PAKsComboBox.SelectedItem);

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

                    for (int i = 0; i < PAKFileAsTXT.Length; i++)
                    {
                        CreatePath(PAKTreeView.Nodes, PAKFileAsTXT[i].Replace(PAKFileAsTXT[i].Split('/').Last(), ""));
                    }
                }
            }
        }
        private async void loadAllPAKsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            isAllPAKs = true;
            AllPAKsDict = new Dictionary<string, string>();
            PAKTreeView.Nodes.Clear();
            ItemsListBox.Items.Clear();
            File.WriteAllText("key.txt", AESKeyTextBox.Text.Substring(2));

            LoadButton.Enabled = false;
            for (int i = 0; i < AllPAKs.Count(); i++)
            {
                currentPAK = AllPAKs[i];
                currentGUID = readPAKGuid(Properties.Settings.Default.FortnitePAKs + "\\" + currentPAK);
                if (currentGUID == "0-0-0-0")
                {
                    AppendText("✔ ", Color.Green);
                    AppendText("Loading ", Color.Black);
                    AppendText(currentPAK, Color.DarkRed, true);
                    await Task.Run(() => {
                        jwpmProcess("filelist \"" + Properties.Settings.Default.FortnitePAKs + "\\" + currentPAK + "\" \"" + docPath + "\"");
                    });
                    if (!File.Exists(docPath + "\\" + currentPAK + ".txt"))
                    {
                        AppendText("✗ ", Color.Red);
                        AppendText(" Can't read ", Color.Black);
                        AppendText(currentPAK, Color.SteelBlue);
                        AppendText(" with this key", Color.Black, true);
                    }
                    else
                    {
                        if (!File.Exists(docPath + "\\FortnitePAKs_Temp.txt"))
                        {
                            File.Create(docPath + "\\FortnitePAKs_Temp.txt").Dispose();
                        }

                        string[] arr = File.ReadAllLines(docPath + "\\" + currentPAK + ".txt");
                        for (int ii = 0; ii < arr.Length; ii++)
                        {
                            string filename = arr[ii].Substring(arr[ii].LastIndexOf("/") + 1);
                            if (filename.Contains(".uasset") || filename.Contains(".uexp") || filename.Contains(".ubulk"))
                            {
                                if (!AllPAKsDict.ContainsKey(filename.Substring(0, filename.LastIndexOf("."))))
                                {
                                    AllPAKsDict.Add(filename.Substring(0, filename.LastIndexOf(".")), currentPAK);
                                }
                            }
                            else
                            {
                                if (!AllPAKsDict.ContainsKey(filename))
                                {
                                    AllPAKsDict.Add(filename, currentPAK);
                                }
                            }
                        }
                        File.AppendAllText(docPath + "\\FortnitePAKs_Temp.txt", File.ReadAllText(docPath + "\\" + currentPAK + ".txt"));
                        File.Delete(docPath + "\\" + currentPAK + ".txt");
                    }
                }
            }
            if (File.Exists(docPath + "\\FortnitePAKs_Temp.txt"))
            {
                string[] arr = File.ReadAllLines(docPath + "\\FortnitePAKs_Temp.txt");
                File.Delete(docPath + "\\FortnitePAKs_Temp.txt");
                AppendText("Fixing paths... Please wait ", Color.Black, true);
                await Task.Run(() => {
                    for (int i = 0; i < arr.Length; i++)
                    {
                        if (arr[i].StartsWith("Athena") ||
                                arr[i].StartsWith("Balance") ||
                                arr[i].StartsWith("Characters") ||
                                arr[i].StartsWith("Banners") ||
                                arr[i].StartsWith("Building") ||
                                arr[i].StartsWith("Blueprints") ||
                                arr[i].StartsWith("ArtTools") ||
                                arr[i].StartsWith("Catalog") ||
                                arr[i].StartsWith("Animation") ||
                                arr[i].StartsWith("Effects") ||
                                arr[i].StartsWith("Environments") ||
                                arr[i].StartsWith("Characters") ||
                                arr[i].StartsWith("CharClasses") ||
                                arr[i].StartsWith("ContentCreationTools") ||
                                arr[i].StartsWith("DeathPenalty") ||
                                arr[i].StartsWith("CollectionBook") ||
                                arr[i].StartsWith("CommandConsole") ||
                                arr[i].StartsWith("Creative") ||
                                arr[i].StartsWith("DeployableBases") ||
                                arr[i].StartsWith("GameplayCueNotifies") ||
                                arr[i].StartsWith("Items") ||
                                arr[i].StartsWith("Missions") ||
                                arr[i].StartsWith("GameplayEffectTemplates") ||
                                arr[i].StartsWith("Heroes") ||
                                arr[i].StartsWith("GameplayCurves") ||
                                arr[i].StartsWith("Packages") ||
                                arr[i].StartsWith("FortressPhysicalMaterials") ||
                                arr[i].StartsWith("Expeditions") ||
                                arr[i].StartsWith("Gamepad") ||
                                arr[i].StartsWith("Gadgets") ||
                                arr[i].StartsWith("Macros") ||
                                arr[i].StartsWith("Maps") ||
                                arr[i].StartsWith("Frontend") ||
                                arr[i].StartsWith("Playgrounds") ||
                                arr[i].StartsWith("Playsets") ||
                                arr[i].StartsWith("Movies") ||
                                arr[i].StartsWith("L10N") ||
                                arr[i].StartsWith("ImpostorBaker") ||
                                arr[i].StartsWith("Marketing") ||
                                arr[i].StartsWith("Marketing_Screenshots") ||
                                arr[i].StartsWith("Sounds") ||
                                arr[i].StartsWith("UI") ||
                                arr[i].StartsWith("Quests") ||
                                arr[i].StartsWith("VisualThreatManager") ||
                                arr[i].StartsWith("Weapons") ||
                                arr[i].StartsWith("WaterAndWind") ||
                                arr[i].StartsWith("Tools") ||
                                arr[i].StartsWith("Vehicles") ||
                                arr[i].StartsWith("PvP") ||
                                arr[i].StartsWith("Spectating") ||
                                arr[i].StartsWith("TheOutpost") ||
                                arr[i].StartsWith("TimeOfDay") ||
                                arr[i].StartsWith("Research") ||
                                arr[i].StartsWith("Relics") ||
                                arr[i].StartsWith("Slate") ||
                                arr[i].StartsWith("TheKeep") ||
                                arr[i].StartsWith("FrontEnd") ||
                                arr[i].StartsWith("AIDirector") ||
                                arr[i].StartsWith("AI") ||
                                arr[i].StartsWith("Abilities") ||
                                arr[i].StartsWith("Accessories") ||
                                arr[i].StartsWith("WorldTiles") ||
                                arr[i].StartsWith("Widgets"))
                        {
                            arr[i] = "FortniteGame/Content/" + arr[i];
                        }
                        if (arr[i].StartsWith("Content") || arr[i].StartsWith("Plugins"))
                        {
                            arr[i] = "FortniteGame/" + arr[i];
                        }
                        if (arr[i].StartsWith("Female") || arr[i].StartsWith("Male"))
                        {
                            arr[i] = "FortniteGame/Content/Characters/Player/" + arr[i];
                        }
                        if (arr[i].StartsWith("Male_Avg_Base") || arr[i].StartsWith("Medium") || arr[i].StartsWith("LegacyAssets") || arr[i].StartsWith("Large"))
                        {
                            arr[i] = "FortniteGame/Content/Characters/Player/Male/" + arr[i];
                        }
                        if (arr[i].StartsWith("Props") || arr[i].StartsWith("Sets") || arr[i].StartsWith("Prototype"))
                        {
                            arr[i] = "FortniteGame/Content/Environments/" + arr[i];
                        }
                    }
                });
                File.WriteAllLines(docPath + "\\FortnitePAKs.txt", arr);
                PAKFileAsTXT = File.ReadAllLines(docPath + "\\FortnitePAKs.txt");
                File.Delete(docPath + "\\FortnitePAKs.txt");

                for (int ii = 0; ii < PAKFileAsTXT.Length; ii++)
                {
                    CreatePath(PAKTreeView.Nodes, PAKFileAsTXT[ii].Replace(PAKFileAsTXT[ii].Split('/').Last(), ""));
                }
            }
            LoadButton.Enabled = true;

            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            AppendText("\nTime elapsed: " + elapsedTime, Color.Green, true, HorizontalAlignment.Right);
        }

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
        private void PAKTreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            List<string> beforeItems = new List<string>();
            afterItems = new List<string>();

            ItemsListBox.Items.Clear();
            FilterTextBox.Text = string.Empty;

            var all = GetAncestors(e.Node, x => x.Parent).ToList();
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
            for (int i = 0; i < afterItems.Count; i++)
            {
                ItemsListBox.Items.Add(afterItems[i]);
            }

            ExtractAssetButton.Enabled = ItemsListBox.SelectedIndex >= 0;
        }
        private void ItemsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ItemsListBox.SelectedItem != null && SelectedArray == null)
            {
                ExtractAssetButton.Enabled = true;
            }
        } //NO EXTRACT BUTTON IF NOTHING SELECTED

        private static bool CaseInsensitiveContains(string text, string value, StringComparison stringComparison = StringComparison.CurrentCultureIgnoreCase)
        {
            return text.IndexOf(value, stringComparison) >= 0;
        } //FILTER INSENSITIVE
        private void FilterTextBox_TextChanged(object sender, EventArgs e)
        {
            ItemsListBox.BeginUpdate();
            ItemsListBox.Items.Clear();
            if (!string.IsNullOrEmpty(FilterTextBox.Text))
            {
                for (int i = 0; i < afterItems.Count; i++)
                {
                    if (CaseInsensitiveContains(afterItems[i], FilterTextBox.Text))
                    {
                        ItemsListBox.Items.Add(afterItems[i]);
                    }
                }
            }
            else
            {
                for (int i = 0; i < afterItems.Count; i++)
                {
                    ItemsListBox.Items.Add(afterItems[i]);
                }
            }
            ItemsListBox.EndUpdate();
        } //FILTER METHOD

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
                        fs1.Read(buffer, 0, buffer.Length);

                        FileStream fs2 = new FileStream(Path.GetFileNameWithoutExtension(file) + ".temp", FileMode.Open, FileAccess.ReadWrite);
                        fs2.Position = src44.Length;
                        fs2.Write(buffer, 0, buffer.Length);
                        fs2.Close();
                        fs1.Close();

                        AppendText("✔ ", Color.Green);
                        AppendText("Writing ", Color.Black);
                        AppendText("UBULK Data", Color.DarkRed, true);
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

        private void convertToOTF(string file, string item)
        {
            AppendText("✔ ", Color.Green);
            AppendText(item, Color.DarkRed);
            AppendText(" is a ", Color.Black);
            AppendText("font", Color.SteelBlue, true);

            File.Move(file, Path.ChangeExtension(file, ".otf"));

            AppendText("✔ ", Color.Green);
            AppendText(item, Color.DarkRed);
            AppendText(" successfully converter to a ", Color.Black);
            AppendText("font", Color.SteelBlue, true);
        }

        private void getItemRarity(ItemsIdParser parser, Graphics toDrawOn)
        {
            if (parser.Rarity == "EFortRarity::Legendary")
            {
                Image RarityBG = Properties.Resources.I512;
                toDrawOn.DrawImage(RarityBG, new Point(0, 0));
                AppendText("Item Rarity: ", Color.Black);
                AppendText("IMPOSSIBLE (T9)", Color.DarkOrange, true);
            }
            if (parser.Rarity == "EFortRarity::Masterwork")
            {
                Image RarityBG = Properties.Resources.T512;
                toDrawOn.DrawImage(RarityBG, new Point(0, 0));
                AppendText("Item Rarity: ", Color.Black);
                AppendText("TRANSCENDENT", Color.OrangeRed, true);
            }
            if (parser.Rarity == "EFortRarity::Elegant")
            {
                Image RarityBG = Properties.Resources.M512;
                toDrawOn.DrawImage(RarityBG, new Point(0, 0));
                AppendText("Item Rarity: ", Color.Black);
                AppendText("MYTHIC", Color.Yellow, true);
            }
            if (parser.Rarity == "EFortRarity::Fine")
            {
                Image RarityBG = Properties.Resources.L512;
                toDrawOn.DrawImage(RarityBG, new Point(0, 0));
                AppendText("Item Rarity: ", Color.Black);
                AppendText("LEGENDARY", Color.Orange, true);
            }
            if (parser.Rarity == "EFortRarity::Quality")
            {
                Image RarityBG = Properties.Resources.E512;
                toDrawOn.DrawImage(RarityBG, new Point(0, 0));
                AppendText("Item Rarity: ", Color.Black);
                AppendText("EPIC", Color.Purple, true);
            }
            if (parser.Rarity == "EFortRarity::Sturdy")
            {
                Image RarityBG = Properties.Resources.R512;
                toDrawOn.DrawImage(RarityBG, new Point(0, 0));
                AppendText("Item Rarity: ", Color.Black);
                AppendText("RARE", Color.Blue, true);
            }
            if (parser.Rarity == "EFortRarity::Handmade")
            {
                Image RarityBG = Properties.Resources.C512;
                toDrawOn.DrawImage(RarityBG, new Point(0, 0));
                AppendText("Item Rarity: ", Color.Black);
                AppendText("COMMON", Color.DarkGray, true);
            }
            if (parser.Rarity == null)
            {
                Image RarityBG = Properties.Resources.U512;
                toDrawOn.DrawImage(RarityBG, new Point(0, 0));
                AppendText("Item Rarity: ", Color.Black);
                AppendText("UNCOMMON", Color.Green, true);
            }
        }

        private void loopStageQuest(string qAssetType, string qAssetName)
        {
            if (qAssetType == "Quest")
            {
                var filesPath = Directory.GetFiles(docPath + "\\Extracted", qAssetName + ".*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".png")).FirstOrDefault();
                if (!File.Exists(filesPath))
                {
                    if (isAllPAKs == false)
                    {
                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + currentPAK + "\" \"" + qAssetName + "\" \"" + docPath + "\"");
                    }
                    if (isAllPAKs == true)
                    {
                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + AllPAKsDict[qAssetName] + "\" \"" + qAssetName + "\" \"" + docPath + "\"");
                    }
                    filesPath = Directory.GetFiles(docPath + "\\Extracted", qAssetName + ".*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".png")).FirstOrDefault();
                }
                try
                {
                    if (filesPath != null)
                    {
                        jwpmProcess("serialize \"" + filesPath.Substring(0, filesPath.LastIndexOf('.')) + "\"");
                        var filesJSON3 = Directory.GetFiles(docPath, qAssetName + ".json", SearchOption.AllDirectories).FirstOrDefault();
                        if (filesJSON3 != null)
                        {
                            var json3 = JToken.Parse(File.ReadAllText(filesJSON3)).ToString();
                            File.Delete(filesJSON3);

                            var questParser2 = QuestParser.FromJson(json3);
                            for (int p2 = 0; p2 < questParser2.Length; p2++)
                            {
                                for (int pp = 0; pp < questParser2[p2].Objectives.Length; pp++)
                                {
                                    if (!questStageDict.ContainsKey(questParser2[p2].Objectives[pp].Description)) //AVOID DUPLICATA, THANKS EPIC...
                                    {
                                        questStageDict.Add(questParser2[p2].Objectives[pp].Description, questParser2[p2].Objectives[pp].Count);
                                        questDictFinal.Add(questParser2[p2].Objectives[pp].Description, questParser2[p2].Objectives[pp].Count);

                                        AppendText(questParser2[p2].Objectives[pp].Description, Color.SteelBlue);
                                        AppendText("\t\tCount: " + questParser2[p2].Objectives[pp].Count, Color.DarkRed, true);
                                    }
                                    for (int ppp = 0; ppp < questParser2[p2].Rewards.Length; ppp++)
                                    {
                                        loopStageQuest(questParser2[p2].Rewards[ppp].ItemPrimaryAssetId.PrimaryAssetType.Name, questParser2[p2].Rewards[ppp].ItemPrimaryAssetId.PrimaryAssetName);
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
                }
                catch (IndexOutOfRangeException)
                {
                    AppendText("[IndexOutOfRangeException] ", Color.Red);
                    AppendText("Can't extract ", Color.Black);
                    AppendText(qAssetName, Color.SteelBlue);
                    AppendText(" in ", Color.Black);
                    AppendText(PAKsComboBox.SelectedItem.ToString(), Color.DarkRed, true);
                }
            }
        }

        public static string currentItem;
        private async void ExtractAssetButton_Click(object sender, EventArgs e)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            scintilla1.Text = "";
            ItemIconPictureBox.Image = null;
            questStageDict = new Dictionary<string, long>();
            questDictFinal = new Dictionary<string, long>();

            if (!Directory.Exists(docPath + "\\Extracted\\")) //Create Extracted Subfolder
                Directory.CreateDirectory(docPath + "\\Extracted\\");
            if (!Directory.Exists(docPath + "\\Generated Icons\\")) //Create Generated Icons Subfolder
                Directory.CreateDirectory(docPath + "\\Generated Icons\\");
            if (!Directory.Exists(docPath + "\\Extracted Sounds\\")) //Create Generated Icons Subfolder
                Directory.CreateDirectory(docPath + "\\Extracted Sounds\\");

            SelectedArray = new string[ItemsListBox.SelectedItems.Count];
            for (int i = 0; i < ItemsListBox.SelectedItems.Count; i++) //ADD SELECTED ITEM TO ARRAY
            {
                SelectedArray[i] = ItemsListBox.SelectedItems[i].ToString();
            }

            ExtractAssetButton.Enabled = false;
            SaveImageButton.Enabled = false;
            for (int i = 0; i < SelectedArray.Length; i++)
            {
                currentItem = SelectedArray[i].ToString();

                var files = Directory.GetFiles(docPath + "\\Extracted", currentItem + ".*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".png")).FirstOrDefault();
                if (!File.Exists(files))
                {
                    if (isAllPAKs == false)
                    {
                        await Task.Run(() => {
                            jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + currentPAK + "\" \"" + currentItem + "\" \"" + docPath + "\"");
                        });
                    }
                    if (isAllPAKs == true)
                    {
                        await Task.Run(() => {
                            jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + AllPAKsDict[currentItem] + "\" \"" + currentItem + "\" \"" + docPath + "\"");
                        });
                    }
                    files = Directory.GetFiles(docPath + "\\Extracted", currentItem + ".*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".png")).FirstOrDefault();
                }
                if (files != null)
                {
                    AppendText("", Color.Black, true);
                    AppendText("✔ ", Color.Green);
                    AppendText(currentItem, Color.DarkRed);
                    AppendText(" successfully extracted to ", Color.Black);
                    AppendText(files.Substring(0, files.LastIndexOf('.')), Color.SteelBlue, true);

                    if (files.Contains(".uasset") || files.Contains(".uexp") || files.Contains(".ubulk"))
                    {
                        AppendText("✔ ", Color.Green);
                        AppendText(currentItem, Color.DarkRed);
                        AppendText(" is an ", Color.Black);
                        AppendText("asset", Color.SteelBlue, true);

                        if (Properties.Settings.Default.ExtractAndSerialize == true)
                        {
                            await Task.Run(() =>
                            {
                                jwpmProcess("serialize \"" + files.Substring(0, files.LastIndexOf('.')) + "\"");
                            });
                        }
                        try
                        {
                            var filesJSON = Directory.GetFiles(docPath, currentItem + ".json", SearchOption.AllDirectories).FirstOrDefault();
                            if (filesJSON != null)
                            {
                                var json = JToken.Parse(File.ReadAllText(filesJSON)).ToString();
                                File.Delete(filesJSON);
                                AppendText("✔ ", Color.Green);
                                AppendText(currentItem, Color.DarkRed);
                                AppendText(" successfully serialized", Color.Black, true);
                                scintilla1.Text = json;

                                var IDParser = ItemsIdParser.FromJson(json);

                                if (((ToolStripMenuItem)ExtractAsset.Items[0]).Checked == true)
                                {
                                    AppendText("Auto loading data set to ", Color.Black);
                                    AppendText("True", Color.Green, true);

                                    if (filesJSON.Contains("Athena\\Items\\") && (filesJSON.Contains("Cosmetics") || filesJSON.Contains("Consumables") || filesJSON.Contains("Traps") || filesJSON.Contains("Gameplay") || filesJSON.Contains("Weapons") || filesJSON.Contains("CosmeticVariantTokens"))) //ASSET IS AN ID => CREATE ICON
                                    {
                                        AppendText("Parsing...", Color.Black, true);
                                        for (int iii = 0; iii < IDParser.Length; iii++)
                                        {
                                            if (Properties.Settings.Default.createIconForCosmetics == true && (IDParser[iii].ExportType.Contains("Athena") && IDParser[iii].ExportType.Contains("Item") && IDParser[iii].ExportType.Contains("Definition")))
                                            {
                                                AppendText("✔ ", Color.Green);
                                                AppendText(currentItem, Color.DarkRed);
                                                AppendText(" is a ", Color.Black);
                                                AppendText("Cosmetic ID", Color.SteelBlue);
                                                AppendText(" file", Color.Black, true);

                                                ItemName = IDParser[iii].DisplayName;
                                                Bitmap bmp = new Bitmap(522, 522);
                                                Graphics g = Graphics.FromImage(bmp);
                                                g.TextRenderingHint = TextRenderingHint.AntiAlias;

                                                getItemRarity(IDParser[iii], g);

                                                string itemIconPath = string.Empty;

                                                if (IDParser[iii].HeroDefinition != null)
                                                {
                                                    var filesPath = Directory.GetFiles(docPath + "\\Extracted", IDParser[iii].HeroDefinition + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                                    if (!File.Exists(filesPath))
                                                    {
                                                        AppendText("✔ ", Color.Green);
                                                        AppendText("Extracting ", Color.Black);
                                                        AppendText(IDParser[iii].HeroDefinition, Color.DarkRed, true);

                                                        if (isAllPAKs == false)
                                                        {
                                                            await Task.Run(() =>
                                                            {
                                                                jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + currentPAK + "\" \"" + IDParser[iii].HeroDefinition + "\" \"" + docPath + "\"");
                                                            });
                                                        }
                                                        if (isAllPAKs == true)
                                                        {
                                                            await Task.Run(() => {
                                                                jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + AllPAKsDict[IDParser[iii].HeroDefinition] + "\" \"" + IDParser[iii].HeroDefinition + "\" \"" + docPath + "\"");
                                                            });
                                                        }
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

                                                                var IDParser2 = ItemsIdParser.FromJson(json2);
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
                                                                            if (currentGUID != "0-0-0-0")
                                                                            {
                                                                                await Task.Run(() =>
                                                                                {
                                                                                    jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + currentPAK + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                                });
                                                                                filesPath2 = Directory.GetFiles(docPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                                                            }
                                                                            else
                                                                            {
                                                                                if (isAllPAKs == false)
                                                                                {
                                                                                    await Task.Run(() =>
                                                                                    {
                                                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\pakchunk0_s7-WindowsClient.pak" + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                                    });
                                                                                }
                                                                                if (isAllPAKs == true)
                                                                                {
                                                                                    await Task.Run(() => {
                                                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + AllPAKsDict[textureFile] + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                                    });
                                                                                }
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
                                                        AppendText(PAKsComboBox.SelectedItem.ToString(), Color.DarkRed, true);
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

                                                        if (isAllPAKs == false)
                                                        {
                                                            await Task.Run(() =>
                                                            {
                                                                jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + currentPAK + "\" \"" + IDParser[iii].WeaponDefinition + "\" \"" + docPath + "\"");
                                                            });
                                                        }
                                                        if (isAllPAKs == true)
                                                        {
                                                            await Task.Run(() => {
                                                                jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + AllPAKsDict[IDParser[iii].WeaponDefinition] + "\" \"" + IDParser[iii].WeaponDefinition + "\" \"" + docPath + "\"");
                                                            });
                                                        }
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

                                                                var IDParser2 = ItemsIdParser.FromJson(json2);
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
                                                                            if (currentGUID != "0-0-0-0")
                                                                            {
                                                                                await Task.Run(() =>
                                                                                {
                                                                                    jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + currentPAK + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                                });
                                                                                filesPath2 = Directory.GetFiles(docPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                                                            }
                                                                            else
                                                                            {
                                                                                if (isAllPAKs == false)
                                                                                {
                                                                                    await Task.Run(() =>
                                                                                    {
                                                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\pakchunk0_s7-WindowsClient.pak" + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                                    });
                                                                                }
                                                                                if (isAllPAKs == true)
                                                                                {
                                                                                    await Task.Run(() => {
                                                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + AllPAKsDict[textureFile] + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                                    });
                                                                                }
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
                                                        AppendText(PAKsComboBox.SelectedItem.ToString(), Color.DarkRed, true);
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
                                                        if (currentGUID != "0-0-0-0") //DYNAMIC PAK
                                                        {
                                                            await Task.Run(() =>
                                                            {
                                                                jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + currentPAK + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                            });
                                                            filesPath = Directory.GetFiles(docPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                                        }
                                                        else //NORMAL PAK
                                                        {
                                                            if (isAllPAKs == false)
                                                            {
                                                                await Task.Run(() =>
                                                                {
                                                                    if (IDParser[iii].LargePreviewImage.AssetPathName.Contains("/Game/2dAssets/"))
                                                                    {
                                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\pakchunk0-WindowsClient.pak" + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                    }
                                                                    else if (IDParser[iii].LargePreviewImage.AssetPathName.Contains("/Game/Athena/TestAssets/") || IDParser[iii].LargePreviewImage.AssetPathName.Contains("/Game/Athena/Prototype/") || IDParser[iii].LargePreviewImage.AssetPathName.Contains("/Game/Athena/Items/"))
                                                                    {
                                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + currentPAK + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                    }
                                                                    else
                                                                    {
                                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\pakchunk0_s7-WindowsClient.pak" + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                    }
                                                                });
                                                            }
                                                            if (isAllPAKs == true)
                                                            {
                                                                await Task.Run(() => {
                                                                    jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + AllPAKsDict[textureFile] + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                });
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
                                                        if (currentGUID != "0-0-0-0")
                                                        {
                                                            await Task.Run(() =>
                                                            {
                                                                jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + currentPAK + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                            });
                                                            filesPath = Directory.GetFiles(docPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                                        }
                                                        else
                                                        {
                                                            if (isAllPAKs == false)
                                                            {
                                                                await Task.Run(() =>
                                                                {
                                                                    if (IDParser[iii].SmallPreviewImage.AssetPathName.Contains("/Game/2dAssets/"))
                                                                    {
                                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\pakchunk0-WindowsClient.pak" + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                    }
                                                                    else if (IDParser[iii].SmallPreviewImage.AssetPathName.Contains("/Game/Athena/TestAssets/") || IDParser[iii].SmallPreviewImage.AssetPathName.Contains("/Game/Athena/Prototype/") || IDParser[iii].LargePreviewImage.AssetPathName.Contains("/Game/Athena/Items/"))
                                                                    {
                                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + currentPAK + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                    }
                                                                    else
                                                                    {
                                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\pakchunk0_s7-WindowsClient.pak" + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                    }
                                                                });
                                                            }
                                                            if (isAllPAKs == true)
                                                            {
                                                                await Task.Run(() => {
                                                                    jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + AllPAKsDict[textureFile] + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                });
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
                                                    g.DrawString(ItemName, new Font(pfc.Families[0], 35), new SolidBrush(Color.White), new Point(522 / 2, 395), centeredString);
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
                                                    g.DrawString(IDParser[iii].Description, new Font("Arial", 10), new SolidBrush(Color.White), new Point(522 / 2, 465), centeredStringLine);
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
                                                    g.DrawString(IDParser[iii].ShortDescription, new Font(pfc.Families[0], 13), new SolidBrush(Color.White), new Point(5, 500));
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
                                                    g.DrawString(IDParser[iii].GameplayTags.GameplayTagsGameplayTags[Array.FindIndex(IDParser[iii].GameplayTags.GameplayTagsGameplayTags, x => x.StartsWith("Cosmetics.Source."))].Substring(17), new Font(pfc.Families[0], 13), new SolidBrush(Color.White), new Point(522 - 5, 500), rightString);
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

                                                ItemIconPictureBox.Image = bmp;
                                                if (((ToolStripMenuItem)ExtractAsset.Items[1]).Checked == true)
                                                {
                                                    string nameToSave;
                                                    if (Properties.Settings.Default.IconName == null || Properties.Settings.Default.IconName == "Selected Item Name (i.e. CID_001_Athena_Commando_F_Default)")
                                                    {
                                                        nameToSave = currentItem;
                                                    }
                                                    else if (Properties.Settings.Default.IconName == "displayName (i.e. Recruit)")
                                                    {
                                                        nameToSave = ItemName;
                                                    }
                                                    else
                                                    {
                                                        nameToSave = currentItem;
                                                    }

                                                    AppendText("Auto saving icons set to ", Color.Black);
                                                    AppendText("True", Color.Green, true);
                                                    ItemIconPictureBox.Image.Save(docPath + "\\Generated Icons\\" + nameToSave + ".png", ImageFormat.Png);

                                                    AppendText("✔ ", Color.Green);
                                                    AppendText(nameToSave, Color.DarkRed);
                                                    AppendText(" successfully saved to ", Color.Black);
                                                    AppendText(docPath + "\\Generated Icons\\" + nameToSave + ".png", Color.SteelBlue, true);
                                                }
                                            } //Cosmetics
                                            if (Properties.Settings.Default.createIconForConsumablesWeapons == true && (IDParser[iii].ExportType.Contains("FortWeaponRangedItemDefinition") || IDParser[iii].ExportType.Contains("FortWeaponMeleeItemDefinition")))
                                            {
                                                AppendText("✔ ", Color.Green);
                                                AppendText(currentItem, Color.DarkRed);
                                                AppendText(" is an ", Color.Black);
                                                AppendText("ID", Color.SteelBlue);
                                                AppendText(" file", Color.Black, true);

                                                ItemName = IDParser[iii].DisplayName;
                                                Bitmap bmp = new Bitmap(522, 522);
                                                Graphics g = Graphics.FromImage(bmp);
                                                g.TextRenderingHint = TextRenderingHint.AntiAlias;

                                                getItemRarity(IDParser[iii], g);

                                                string itemIconPath = string.Empty;

                                                if (IDParser[iii].LargePreviewImage != null)
                                                {
                                                    string textureFile = Path.GetFileName(IDParser[iii].LargePreviewImage.AssetPathName).Substring(0, Path.GetFileName(IDParser[iii].LargePreviewImage.AssetPathName).LastIndexOf('.'));
                                                    AppendText("✔ ", Color.Green);
                                                    AppendText(textureFile, Color.DarkRed);
                                                    AppendText(" detected as a ", Color.Black);
                                                    AppendText("Texture2D file", Color.SteelBlue, true);

                                                    var filesPath = Directory.GetFiles(docPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                                    if (!File.Exists(filesPath))
                                                    {
                                                        if (currentGUID != "0-0-0-0") //DYNAMIC PAK
                                                        {
                                                            await Task.Run(() =>
                                                            {
                                                                jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + currentPAK + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                            });
                                                            filesPath = Directory.GetFiles(docPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                                        }
                                                        else //NORMAL PAK
                                                        {
                                                            if (isAllPAKs == false)
                                                            {
                                                                await Task.Run(() =>
                                                                {
                                                                    if (IDParser[iii].LargePreviewImage.AssetPathName.Contains("/Game/2dAssets/"))
                                                                    {
                                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\pakchunk0-WindowsClient.pak" + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                    }
                                                                    else if (IDParser[iii].LargePreviewImage.AssetPathName.Contains("/Game/Athena/TestAssets/") || IDParser[iii].LargePreviewImage.AssetPathName.Contains("/Game/Athena/Prototype/") || IDParser[iii].LargePreviewImage.AssetPathName.Contains("/Game/Athena/Items/"))
                                                                    {
                                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + currentPAK + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                    }
                                                                    else
                                                                    {
                                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\pakchunk0_s7-WindowsClient.pak" + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                    }
                                                                });
                                                            }
                                                            if (isAllPAKs == true)
                                                            {
                                                                await Task.Run(() => {
                                                                    jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + AllPAKsDict[textureFile] + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                });
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
                                                        if (currentGUID != "0-0-0-0")
                                                        {
                                                            await Task.Run(() =>
                                                            {
                                                                jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + currentPAK + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                            });
                                                            filesPath = Directory.GetFiles(docPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                                        }
                                                        else
                                                        {
                                                            if (isAllPAKs == false)
                                                            {
                                                                await Task.Run(() =>
                                                                {
                                                                    if (IDParser[iii].SmallPreviewImage.AssetPathName.Contains("/Game/2dAssets/"))
                                                                    {
                                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\pakchunk0-WindowsClient.pak" + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                    }
                                                                    else if (IDParser[iii].SmallPreviewImage.AssetPathName.Contains("/Game/Athena/TestAssets/") || IDParser[iii].SmallPreviewImage.AssetPathName.Contains("/Game/Athena/Prototype/") || IDParser[iii].LargePreviewImage.AssetPathName.Contains("/Game/Athena/Items/"))
                                                                    {
                                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + currentPAK + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                    }
                                                                    else
                                                                    {
                                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\pakchunk0_s7-WindowsClient.pak" + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                    }
                                                                });
                                                            }
                                                            if (isAllPAKs == true)
                                                            {
                                                                await Task.Run(() => {
                                                                    jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + AllPAKsDict[textureFile] + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                });
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
                                                    g.DrawString(ItemName, new Font(pfc.Families[0], 35), new SolidBrush(Color.White), new Point(522 / 2, 395), centeredString);
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
                                                    g.DrawString(IDParser[iii].Description, new Font("Arial", 10), new SolidBrush(Color.White), new Point(522 / 2, 465), centeredStringLine);
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
                                                    g.DrawString(IDParser[iii].GameplayTags.GameplayTagsGameplayTags[Array.FindIndex(IDParser[iii].GameplayTags.GameplayTagsGameplayTags, x => x.StartsWith("Athena.ItemAction."))].Substring(18), new Font(pfc.Families[0], 13), new SolidBrush(Color.White), new Point(522 - 5, 500), rightString);
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
                                                    try
                                                    {
                                                        g.DrawString(IDParser[iii].GameplayTags.GameplayTagsGameplayTags[Array.FindIndex(IDParser[iii].GameplayTags.GameplayTagsGameplayTags, x => x.StartsWith("Weapon."))].Substring(7), new Font(pfc.Families[0], 13), new SolidBrush(Color.White), new Point(522 - 5, 500), rightString);
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
                                                        AppendText("Athena.ItemAction ", Color.SteelBlue);
                                                        AppendText("or ", Color.Black);
                                                        AppendText("Weapon ", Color.SteelBlue);
                                                        AppendText("found", Color.Black, true);
                                                    }
                                                } //ACTION

                                                ItemIconPictureBox.Image = bmp;
                                                if (((ToolStripMenuItem)ExtractAsset.Items[1]).Checked == true)
                                                {
                                                    string nameToSave;
                                                    if (Properties.Settings.Default.IconName == null || Properties.Settings.Default.IconName == "Selected Item Name (i.e. CID_001_Athena_Commando_F_Default)")
                                                    {
                                                        nameToSave = currentItem;
                                                    }
                                                    else if (Properties.Settings.Default.IconName == "displayName (i.e. Recruit)")
                                                    {
                                                        nameToSave = ItemName;
                                                    }
                                                    else
                                                    {
                                                        nameToSave = currentItem;
                                                    }

                                                    AppendText("Auto saving icons set to ", Color.Black);
                                                    AppendText("True", Color.Green, true);
                                                    ItemIconPictureBox.Image.Save(docPath + "\\Generated Icons\\" + nameToSave + ".png", ImageFormat.Png);

                                                    AppendText("✔ ", Color.Green);
                                                    AppendText(nameToSave, Color.DarkRed);
                                                    AppendText(" successfully saved to ", Color.Black);
                                                    AppendText(docPath + "\\Generated Icons\\" + nameToSave + ".png", Color.SteelBlue, true);
                                                }
                                            } //Consumables & Weapons
                                            if (Properties.Settings.Default.createIconForTraps == true && (IDParser[iii].ExportType.Contains("FortTrapItemDefinition") || IDParser[iii].ExportType.Contains("FortContextTrapItemDefinition")))
                                            {
                                                AppendText("✔ ", Color.Green);
                                                AppendText(currentItem, Color.DarkRed);
                                                AppendText(" is a ", Color.Black);
                                                AppendText("Trap ID", Color.SteelBlue);
                                                AppendText(" file", Color.Black, true);

                                                ItemName = IDParser[iii].DisplayName;
                                                Bitmap bmp = new Bitmap(522, 522);
                                                Graphics g = Graphics.FromImage(bmp);
                                                g.TextRenderingHint = TextRenderingHint.AntiAlias;

                                                getItemRarity(IDParser[iii], g);

                                                string itemIconPath = string.Empty;

                                                if (IDParser[iii].LargePreviewImage != null)
                                                {
                                                    string textureFile = Path.GetFileName(IDParser[iii].LargePreviewImage.AssetPathName).Substring(0, Path.GetFileName(IDParser[iii].LargePreviewImage.AssetPathName).LastIndexOf('.'));
                                                    AppendText("✔ ", Color.Green);
                                                    AppendText(textureFile, Color.DarkRed);
                                                    AppendText(" detected as a ", Color.Black);
                                                    AppendText("Texture2D file", Color.SteelBlue, true);

                                                    var filesPath = Directory.GetFiles(docPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                                    if (!File.Exists(filesPath))
                                                    {
                                                        if (currentGUID != "0-0-0-0") //DYNAMIC PAK
                                                        {
                                                            await Task.Run(() =>
                                                            {
                                                                jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + currentPAK + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                            });
                                                            filesPath = Directory.GetFiles(docPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                                        }
                                                        else //NORMAL PAK
                                                        {
                                                            if (isAllPAKs == false)
                                                            {
                                                                await Task.Run(() =>
                                                                {
                                                                    if (IDParser[iii].LargePreviewImage.AssetPathName.Contains("/Game/2dAssets/"))
                                                                    {
                                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\pakchunk0-WindowsClient.pak" + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                    }
                                                                    else if (IDParser[iii].LargePreviewImage.AssetPathName.Contains("/Game/Athena/TestAssets/") || IDParser[iii].LargePreviewImage.AssetPathName.Contains("/Game/Athena/Prototype/") || IDParser[iii].LargePreviewImage.AssetPathName.Contains("/Game/Athena/Items/"))
                                                                    {
                                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + currentPAK + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                    }
                                                                    else
                                                                    {
                                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\pakchunk0_s7-WindowsClient.pak" + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                    }
                                                                });
                                                            }
                                                            if (isAllPAKs == true)
                                                            {
                                                                await Task.Run(() => {
                                                                    jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + AllPAKsDict[textureFile] + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                });
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
                                                        if (currentGUID != "0-0-0-0")
                                                        {
                                                            await Task.Run(() =>
                                                            {
                                                                jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + currentPAK + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                            });
                                                            filesPath = Directory.GetFiles(docPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                                        }
                                                        else
                                                        {
                                                            if (isAllPAKs == false)
                                                            {
                                                                await Task.Run(() =>
                                                                {
                                                                    if (IDParser[iii].SmallPreviewImage.AssetPathName.Contains("/Game/2dAssets/"))
                                                                    {
                                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\pakchunk0-WindowsClient.pak" + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                    }
                                                                    else if (IDParser[iii].SmallPreviewImage.AssetPathName.Contains("/Game/Athena/TestAssets/") || IDParser[iii].SmallPreviewImage.AssetPathName.Contains("/Game/Athena/Prototype/") || IDParser[iii].LargePreviewImage.AssetPathName.Contains("/Game/Athena/Items/"))
                                                                    {
                                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + currentPAK + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                    }
                                                                    else
                                                                    {
                                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\pakchunk0_s7-WindowsClient.pak" + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                    }
                                                                });
                                                            }
                                                            if (isAllPAKs == true)
                                                            {
                                                                await Task.Run(() => {
                                                                    jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + AllPAKsDict[textureFile] + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                });
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
                                                    g.DrawString(ItemName, new Font(pfc.Families[0], 35), new SolidBrush(Color.White), new Point(522 / 2, 395), centeredString);
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
                                                    g.DrawString(IDParser[iii].Description, new Font("Arial", 10), new SolidBrush(Color.White), new Point(522 / 2, 465), centeredStringLine);
                                                }
                                                catch (NullReferenceException)
                                                {
                                                    AppendText("[NullReferenceException] ", Color.Red);
                                                    AppendText("No ", Color.Black);
                                                    AppendText("Description ", Color.SteelBlue);
                                                    AppendText("found", Color.Black, true);
                                                } //DESCRIPTION

                                                ItemIconPictureBox.Image = bmp;
                                                if (((ToolStripMenuItem)ExtractAsset.Items[1]).Checked == true)
                                                {
                                                    string nameToSave;
                                                    if (Properties.Settings.Default.IconName == null || Properties.Settings.Default.IconName == "Selected Item Name (i.e. CID_001_Athena_Commando_F_Default)")
                                                    {
                                                        nameToSave = currentItem;
                                                    }
                                                    else if (Properties.Settings.Default.IconName == "displayName (i.e. Recruit)")
                                                    {
                                                        nameToSave = ItemName;
                                                    }
                                                    else
                                                    {
                                                        nameToSave = currentItem;
                                                    }

                                                    AppendText("Auto saving icons set to ", Color.Black);
                                                    AppendText("True", Color.Green, true);
                                                    ItemIconPictureBox.Image.Save(docPath + "\\Generated Icons\\" + nameToSave + ".png", ImageFormat.Png);

                                                    AppendText("✔ ", Color.Green);
                                                    AppendText(nameToSave, Color.DarkRed);
                                                    AppendText(" successfully saved to ", Color.Black);
                                                    AppendText(docPath + "\\Generated Icons\\" + nameToSave + ".png", Color.SteelBlue, true);
                                                }
                                            } //Traps
                                            if (Properties.Settings.Default.createIconForVariants == true && (IDParser[iii].ExportType == "FortVariantTokenType"))
                                            {
                                                AppendText("✔ ", Color.Green);
                                                AppendText(currentItem, Color.DarkRed);
                                                AppendText(" is a ", Color.Black);
                                                AppendText("Cosmetic Variant", Color.SteelBlue);
                                                AppendText(" file", Color.Black, true);

                                                ItemName = IDParser[iii].DisplayName;
                                                Bitmap bmp = new Bitmap(522, 522);
                                                Graphics g = Graphics.FromImage(bmp);
                                                g.TextRenderingHint = TextRenderingHint.AntiAlias;

                                                getItemRarity(IDParser[iii], g);

                                                string itemIconPath = string.Empty;

                                                if (IDParser[iii].LargePreviewImage != null)
                                                {
                                                    string textureFile = Path.GetFileName(IDParser[iii].LargePreviewImage.AssetPathName).Substring(0, Path.GetFileName(IDParser[iii].LargePreviewImage.AssetPathName).LastIndexOf('.'));
                                                    AppendText("✔ ", Color.Green);
                                                    AppendText(textureFile, Color.DarkRed);
                                                    AppendText(" detected as a ", Color.Black);
                                                    AppendText("Texture2D file", Color.SteelBlue, true);

                                                    var filesPath = Directory.GetFiles(docPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                                    if (!File.Exists(filesPath))
                                                    {
                                                        if (currentGUID != "0-0-0-0") //DYNAMIC PAK
                                                        {
                                                            await Task.Run(() =>
                                                            {
                                                                jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + currentPAK + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                            });
                                                            filesPath = Directory.GetFiles(docPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                                        }
                                                        else //NORMAL PAK
                                                        {
                                                            if (isAllPAKs == false)
                                                            {
                                                                await Task.Run(() =>
                                                                {
                                                                    if (IDParser[iii].LargePreviewImage.AssetPathName.Contains("/Game/2dAssets/"))
                                                                    {
                                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\pakchunk0-WindowsClient.pak" + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                    }
                                                                    else if (IDParser[iii].LargePreviewImage.AssetPathName.Contains("/Game/Athena/TestAssets/") || IDParser[iii].LargePreviewImage.AssetPathName.Contains("/Game/Athena/Prototype/") || IDParser[iii].LargePreviewImage.AssetPathName.Contains("/Game/Athena/Items/"))
                                                                    {
                                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + currentPAK + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                    }
                                                                    else
                                                                    {
                                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\pakchunk0_s7-WindowsClient.pak" + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                    }
                                                                });
                                                            }
                                                            if (isAllPAKs == true)
                                                            {
                                                                await Task.Run(() => {
                                                                    jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + AllPAKsDict[textureFile] + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                });
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
                                                        if (currentGUID != "0-0-0-0")
                                                        {
                                                            await Task.Run(() =>
                                                            {
                                                                jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + currentPAK + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                            });
                                                            filesPath = Directory.GetFiles(docPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).FirstOrDefault();
                                                        }
                                                        else
                                                        {
                                                            if (isAllPAKs == false)
                                                            {
                                                                await Task.Run(() =>
                                                                {
                                                                    if (IDParser[iii].SmallPreviewImage.AssetPathName.Contains("/Game/2dAssets/"))
                                                                    {
                                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\pakchunk0-WindowsClient.pak" + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                    }
                                                                    else if (IDParser[iii].SmallPreviewImage.AssetPathName.Contains("/Game/Athena/TestAssets/") || IDParser[iii].SmallPreviewImage.AssetPathName.Contains("/Game/Athena/Prototype/") || IDParser[iii].LargePreviewImage.AssetPathName.Contains("/Game/Athena/Items/"))
                                                                    {
                                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + currentPAK + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                    }
                                                                    else
                                                                    {
                                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\pakchunk0_s7-WindowsClient.pak" + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                    }
                                                                });
                                                            }
                                                            if (isAllPAKs == true)
                                                            {
                                                                await Task.Run(() => {
                                                                    jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + AllPAKsDict[textureFile] + "\" \"" + textureFile + "\" \"" + docPath + "\"");
                                                                });
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
                                                    g.DrawString(ItemName, new Font(pfc.Families[0], 35), new SolidBrush(Color.White), new Point(522 / 2, 395), centeredString);
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
                                                    g.DrawString(IDParser[iii].Description, new Font("Arial", 10), new SolidBrush(Color.White), new Point(522 / 2, 465), centeredStringLine);
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
                                                    g.DrawString(IDParser[iii].ShortDescription, new Font(pfc.Families[0], 13), new SolidBrush(Color.White), new Point(5, 500));
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
                                                    g.DrawString(IDParser[iii].cosmetic_item, new Font(pfc.Families[0], 13), new SolidBrush(Color.White), new Point(522 - 5, 500), rightString);
                                                }
                                                catch (NullReferenceException)
                                                {
                                                    AppendText("[NullReferenceException] ", Color.Red);
                                                    AppendText("No ", Color.Black);
                                                    AppendText("Cosmetic Item ", Color.SteelBlue);
                                                    AppendText("found", Color.Black, true);
                                                } //COSMETIC ITEM

                                                ItemIconPictureBox.Image = bmp;
                                                if (((ToolStripMenuItem)ExtractAsset.Items[1]).Checked == true)
                                                {
                                                    string nameToSave;
                                                    if (Properties.Settings.Default.IconName == null || Properties.Settings.Default.IconName == "Selected Item Name (i.e. CID_001_Athena_Commando_F_Default)")
                                                    {
                                                        nameToSave = currentItem;
                                                    }
                                                    else if (Properties.Settings.Default.IconName == "displayName (i.e. Recruit)")
                                                    {
                                                        nameToSave = ItemName;
                                                    }
                                                    else
                                                    {
                                                        nameToSave = currentItem;
                                                    }

                                                    AppendText("Auto saving icons set to ", Color.Black);
                                                    AppendText("True", Color.Green, true);
                                                    ItemIconPictureBox.Image.Save(docPath + "\\Generated Icons\\" + nameToSave + ".png", ImageFormat.Png);

                                                    AppendText("✔ ", Color.Green);
                                                    AppendText(nameToSave, Color.DarkRed);
                                                    AppendText(" successfully saved to ", Color.Black);
                                                    AppendText(docPath + "\\Generated Icons\\" + nameToSave + ".png", Color.SteelBlue, true);
                                                }
                                            } //CosmeticVariantTokens
                                        }
                                    }
                                    for (int ii = 0; ii < IDParser.Length; ii++)
                                    {
                                        if (IDParser[ii].ExportType == "FortChallengeBundleItemDefinition")
                                        {
                                            var ChallengeParser = ChallengeBundleIdParser.FromJson(json);
                                            AppendText("Parsing...Please wait while extracting all challenges\n", Color.Black, true);

                                            for (int iii = 0; iii < ChallengeParser.Length; iii++)
                                            {
                                                ItemName = ChallengeParser[iii].DisplayName.ToUpper();
                                                challengesArray = new string[ChallengeParser[iii].QuestInfos.Length];
                                                for (int y = 0; y < ChallengeParser[iii].QuestInfos.Length; y++)
                                                {
                                                    string cName = Path.GetFileName(ChallengeParser[iii].QuestInfos[y].QuestDefinition.AssetPathName);
                                                    challengesArray[y] = cName.Substring(cName.LastIndexOf('.') + 1);
                                                }

                                                for (int w = 0; w < challengesArray.Length; w++)
                                                {
                                                    var filesPath = Directory.GetFiles(docPath + "\\Extracted", challengesArray[w] + ".*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".png")).FirstOrDefault();
                                                    if (!File.Exists(filesPath))
                                                    {
                                                        if (isAllPAKs == false)
                                                        {
                                                            await Task.Run(() =>
                                                            {
                                                                jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + currentPAK + "\" \"" + challengesArray[w] + "\" \"" + docPath + "\"");
                                                            });
                                                        }
                                                        if (isAllPAKs == true)
                                                        {
                                                            await Task.Run(() => {
                                                                jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + AllPAKsDict[challengesArray[w]] + "\" \"" + challengesArray[w] + "\" \"" + docPath + "\"");
                                                            });
                                                        }
                                                        filesPath = Directory.GetFiles(docPath + "\\Extracted", challengesArray[w] + ".*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".png")).FirstOrDefault();
                                                    }
                                                    try
                                                    {
                                                        if (filesPath != null)
                                                        {
                                                            await Task.Run(() =>
                                                            {
                                                                jwpmProcess("serialize \"" + filesPath.Substring(0, filesPath.LastIndexOf('.')) + "\"");
                                                            });
                                                            var filesJSON2 = Directory.GetFiles(docPath, challengesArray[w] + ".json", SearchOption.AllDirectories).FirstOrDefault();
                                                            if (filesJSON2 != null)
                                                            {
                                                                var json2 = JToken.Parse(File.ReadAllText(filesJSON2)).ToString();
                                                                File.Delete(filesJSON2);

                                                                var questParser = QuestParser.FromJson(json2);
                                                                for (int p = 0; p < questParser.Length; p++)
                                                                {
                                                                    string oldQuest = null;
                                                                    for (int pp = 0; pp < questParser[p].Objectives.Length; pp++)
                                                                    {
                                                                        string newQuest = questParser[p].Objectives[pp].Description;
                                                                        if (newQuest != oldQuest)
                                                                        {
                                                                            if (!questDictFinal.ContainsKey(questParser[p].Objectives[pp].Description))
                                                                            {
                                                                                questDictFinal.Add(questParser[p].Objectives[pp].Description, questParser[p].Objectives[pp].Count);
                                                                            }
                                                                            AppendText(questParser[p].Objectives[pp].Description, Color.SteelBlue);
                                                                            AppendText("\t\tCount: " + questParser[p].Objectives[pp].Count, Color.DarkRed, true);

                                                                            oldQuest = questParser[p].Objectives[pp].Description;
                                                                        }
                                                                        try
                                                                        {
                                                                            for (int ppp = 0; ppp < questParser[p].Rewards.Length; ppp++)
                                                                            {
                                                                                loopStageQuest(questParser[p].Rewards[ppp].ItemPrimaryAssetId.PrimaryAssetType.Name, questParser[p].Rewards[ppp].ItemPrimaryAssetId.PrimaryAssetName);
                                                                            }
                                                                        }
                                                                        catch (Exception ex)
                                                                        {
                                                                            Console.WriteLine(ex.Message);
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
                                                    }
                                                    catch (IndexOutOfRangeException)
                                                    {
                                                        AppendText("[IndexOutOfRangeException] ", Color.Red);
                                                        AppendText("Can't extract ", Color.Black);
                                                        AppendText(challengesArray[w], Color.SteelBlue);
                                                        AppendText(" in ", Color.Black);
                                                        AppendText(PAKsComboBox.SelectedItem.ToString(), Color.DarkRed, true);
                                                    }
                                                }
                                            }

                                            if (Properties.Settings.Default.createIconForChallenges == true)
                                            {
                                                Bitmap bmp = new Bitmap(Properties.Resources.Quest);
                                                Graphics g = Graphics.FromImage(bmp);
                                                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                                                int iamY = 400;
                                                int justSkip = 0;
                                                foreach (var d in questDictFinal)
                                                {
                                                    justSkip += 1;
                                                    iamY += 100;
                                                    g.DrawString(d.Key, new Font(pfc.Families[1], 50), new SolidBrush(Color.White), new Point(100, iamY));
                                                    g.DrawString("/" + d.Value, new Font(pfc.Families[1], 50), new SolidBrush(Color.FromArgb(255, 149, 213, 255)), new Point(2410, iamY), rightString);
                                                    if (justSkip != 1)
                                                    {
                                                        g.DrawLine(new Pen(Color.FromArgb(30, 255, 255, 255)), 100, iamY - 10, 2410, iamY - 10);
                                                    }
                                                }
                                                try
                                                {
                                                    string seasonFolder = filesJSON.Substring(filesJSON.Substring(0, filesJSON.LastIndexOf("\\")).LastIndexOf("\\") + 1).ToUpper();
                                                    g.DrawString(seasonFolder.Substring(0, seasonFolder.LastIndexOf("\\")), new Font(pfc.Families[1], 42), new SolidBrush(Color.FromArgb(255, 149, 213, 255)), new Point(340, 40));
                                                }
                                                catch (NullReferenceException)
                                                {
                                                    AppendText("[NullReferenceException] ", Color.Red);
                                                    AppendText("No ", Color.Black);
                                                    AppendText("Season ", Color.SteelBlue);
                                                    AppendText("found", Color.Black, true);
                                                } //LAST SUBFOLDER
                                                try
                                                {
                                                    g.DrawString(ItemName, new Font(pfc.Families[1], 115), new SolidBrush(Color.White), new Point(325, 70));
                                                }
                                                catch (NullReferenceException)
                                                {
                                                    AppendText("[NullReferenceException] ", Color.Red);
                                                    AppendText("No ", Color.Black);
                                                    AppendText("DisplayName ", Color.SteelBlue);
                                                    AppendText("found", Color.Black, true);
                                                } //NAME
                                                using (Bitmap bmp2 = bmp)
                                                {
                                                    var newImg = bmp2.Clone(
                                                        new Rectangle { X = 0, Y = 0, Width = bmp.Width, Height = iamY + 280 },
                                                        bmp2.PixelFormat);
                                                    ItemIconPictureBox.Image = newImg;
                                                } //CUT
                                            }

                                            if (((ToolStripMenuItem)ExtractAsset.Items[1]).Checked == true)
                                            {
                                                string nameToSave;
                                                if (Properties.Settings.Default.IconName == null || Properties.Settings.Default.IconName == "Selected Item Name (i.e. CID_001_Athena_Commando_F_Default)")
                                                {
                                                    nameToSave = currentItem;
                                                }
                                                else if (Properties.Settings.Default.IconName == "displayName (i.e. Recruit)")
                                                {
                                                    nameToSave = ItemName;
                                                }
                                                else
                                                {
                                                    nameToSave = currentItem;
                                                }

                                                AppendText("Auto saving icons set to ", Color.Black);
                                                AppendText("True", Color.Green, true);
                                                ItemIconPictureBox.Image.Save(docPath + "\\Generated Icons\\" + nameToSave + ".png", ImageFormat.Png);

                                                AppendText("✔ ", Color.Green);
                                                AppendText(nameToSave, Color.DarkRed);
                                                AppendText(" successfully saved to ", Color.Black);
                                                AppendText(docPath + "\\Generated Icons\\" + nameToSave + ".png", Color.SteelBlue, true);
                                            }
                                        } //ASSET IS A CHALLENGE => 
                                        if (IDParser[ii].ExportType == "Texture2D")
                                        {
                                            AppendText("Parsing...", Color.Black, true);
                                            ItemName = currentItem;

                                            AppendText("✔ ", Color.Green);
                                            AppendText(currentItem, Color.DarkRed);
                                            AppendText(" detected as a ", Color.Black);
                                            AppendText("Texture2D file", Color.SteelBlue, true);

                                            string IMGPath = string.Empty;

                                            var filesPath = Directory.GetFiles(docPath + "\\Extracted", currentItem + ".*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".png")).FirstOrDefault();
                                            if (!File.Exists(filesPath))
                                            {
                                                if (isAllPAKs == false)
                                                {
                                                    await Task.Run(() =>
                                                    {
                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + currentPAK + "\" \"" + currentItem + "\" \"" + docPath + "\"");
                                                    });
                                                }
                                                if (isAllPAKs == true)
                                                {
                                                    await Task.Run(() => {
                                                        jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + AllPAKsDict[currentItem] + "\" \"" + currentItem + "\" \"" + docPath + "\"");
                                                    });
                                                }
                                                filesPath = Directory.GetFiles(docPath + "\\Extracted", currentItem + ".*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".png")).FirstOrDefault();
                                            }
                                            try
                                            {
                                                if (filesPath != null)
                                                {
                                                    AppendText("✔ ", Color.Green);
                                                    AppendText(currentItem, Color.DarkRed);
                                                    AppendText(" successfully extracted to ", Color.Black);
                                                    AppendText(filesPath.Substring(0, filesPath.LastIndexOf('.')), Color.SteelBlue, true);

                                                    IMGPath = filesPath.Substring(0, filesPath.LastIndexOf('.')) + ".png";
                                                    if (!File.Exists(IMGPath))
                                                    {
                                                        await Task.Run(() =>
                                                        {
                                                            jwpmProcess("texture \"" + filesPath.Substring(0, filesPath.LastIndexOf('.')) + "\"");
                                                        });
                                                        IMGPath = filesPath.Substring(0, filesPath.LastIndexOf('.')) + ".png";
                                                    }

                                                    AppendText("✔ ", Color.Green);
                                                    AppendText(currentItem, Color.DarkRed);
                                                    AppendText(" successfully converted to a PNG image with path ", Color.Black);
                                                    AppendText(IMGPath, Color.SteelBlue, true);
                                                }
                                            }
                                            catch (IndexOutOfRangeException)
                                            {
                                                AppendText("[IndexOutOfRangeException] ", Color.Red);
                                                AppendText("Can't extract ", Color.Black);
                                                AppendText(currentItem, Color.SteelBlue);
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

                                            if (((ToolStripMenuItem)ExtractAsset.Items[1]).Checked == true)
                                            {
                                                string nameToSave;
                                                if (Properties.Settings.Default.IconName == null || Properties.Settings.Default.IconName == "Selected Item Name (i.e. CID_001_Athena_Commando_F_Default)")
                                                {
                                                    nameToSave = currentItem;
                                                }
                                                else if (Properties.Settings.Default.IconName == "displayName (i.e. Recruit)")
                                                {
                                                    nameToSave = ItemName;
                                                }
                                                else
                                                {
                                                    nameToSave = currentItem;
                                                }

                                                AppendText("Auto saving images set to ", Color.Black);
                                                AppendText("True", Color.Green, true);
                                                ItemIconPictureBox.Image.Save(docPath + "\\Generated Icons\\" + nameToSave + ".png", ImageFormat.Png);

                                                AppendText("✔ ", Color.Green);
                                                AppendText(nameToSave, Color.DarkRed);
                                                AppendText(" successfully saved to ", Color.Black);
                                                AppendText(docPath + "\\Generated Icons\\" + nameToSave + ".png", Color.SteelBlue, true);
                                            }
                                        } //ASSET IS A TEXTURE => LOAD TEXTURE
                                        if (IDParser[ii].ExportType == "SoundWave")
                                        {
                                            AppendText("Parsing...", Color.Black, true);
                                            ItemName = currentItem;

                                            AppendText("✔ ", Color.Green);
                                            AppendText(currentItem, Color.DarkRed);
                                            AppendText(" detected as a ", Color.Black);
                                            AppendText("SoundWave file", Color.SteelBlue, true);

                                            string MusicPath = Directory.GetFiles(docPath + "\\Extracted Sounds", currentItem + ".ogg", SearchOption.AllDirectories).FirstOrDefault();
                                            if (!File.Exists(MusicPath))
                                            {
                                                var filesPath = Directory.GetFiles(docPath + "\\Extracted", currentItem + ".uexp", SearchOption.AllDirectories).FirstOrDefault();
                                                if (!File.Exists(filesPath))
                                                {
                                                    if (isAllPAKs == false)
                                                    {
                                                        await Task.Run(() =>
                                                        {
                                                            jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + currentPAK + "\" \"" + currentItem + "\" \"" + docPath + "\"");
                                                        });
                                                    }
                                                    if (isAllPAKs == true)
                                                    {
                                                        await Task.Run(() => {
                                                            jwpmProcess("extract \"" + Properties.Settings.Default.FortnitePAKs + "\\" + AllPAKsDict[currentItem] + "\" \"" + currentItem + "\" \"" + docPath + "\"");
                                                        });
                                                    }
                                                    filesPath = Directory.GetFiles(docPath + "\\Extracted", currentItem + ".uexp", SearchOption.AllDirectories).FirstOrDefault();
                                                }
                                                try
                                                {
                                                    if (filesPath != null)
                                                    {
                                                        AppendText("✔ ", Color.Green);
                                                        AppendText(currentItem, Color.DarkRed);
                                                        AppendText(" successfully extracted to ", Color.Black);
                                                        AppendText(filesPath.Substring(0, filesPath.LastIndexOf('.')), Color.SteelBlue, true);
                                                        try
                                                        {
                                                            convertToOGG(filesPath, currentItem);
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            Console.WriteLine(ex.Message);
                                                        }

                                                        MusicPath = docPath + "\\Extracted Sounds\\" + Path.GetFileNameWithoutExtension(filesPath) + ".ogg";
                                                        AppendText("✔ ", Color.Green);
                                                        AppendText(currentItem, Color.DarkRed);
                                                        AppendText(" successfully converted to an OGG sound with path ", Color.Black);
                                                        AppendText(MusicPath, Color.SteelBlue, true);
                                                    }
                                                }
                                                catch (IndexOutOfRangeException)
                                                {
                                                    AppendText("[IndexOutOfRangeException] ", Color.Red);
                                                    AppendText("Can't extract ", Color.Black);
                                                    AppendText(currentItem, Color.SteelBlue);
                                                    AppendText(" in ", Color.Black);
                                                    AppendText(PAKsComboBox.SelectedItem.ToString(), Color.DarkRed, true);
                                                }
                                            }
                                            OpenWithDefaultProgramAndNoFocus(MusicPath);
                                        } //ASSET IS A SOUND => CONVERT AND LOAD SOUND
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
                    if (files.Contains(".ufont")) //ASSET IS A FONT => CONVERT TO FONT
                    {
                        convertToOTF(files, currentItem);
                    }
                    if (files.Contains(".ini"))
                    {
                        scintilla1.Text = File.ReadAllText(files);
                    }
                }
                else
                {
                    AppendText("", Color.Black, true);
                    AppendText("✗ ", Color.Red);
                    AppendText("Error while extracting ", Color.Black);
                    AppendText(currentItem, Color.SteelBlue, true);
                }
            }
            ExtractAssetButton.Enabled = true;
            SaveImageButton.Enabled = true;
            SelectedArray = null;

            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            AppendText("\nTime elapsed: " + elapsedTime, Color.Green, true, HorizontalAlignment.Right);
        }

        private void SaveImageButton_Click(object sender, EventArgs e)
        {
            if (ItemIconPictureBox.Image != null)
            {
                string nameToSave;
                if (Properties.Settings.Default.IconName == null || Properties.Settings.Default.IconName == "Selected Item Name (i.e. CID_001_Athena_Commando_F_Default)")
                {
                    nameToSave = currentItem;
                }
                else if (Properties.Settings.Default.IconName == "displayName (i.e. Recruit)")
                {
                    nameToSave = ItemName;
                }
                else
                {
                    nameToSave = currentItem;
                }

                SaveFileDialog saveTheDialog = new SaveFileDialog();
                saveTheDialog.Title = "Save Icon";
                saveTheDialog.Filter = "PNG Files (*.png)|*.png";
                saveTheDialog.InitialDirectory = docPath + "\\Generated Icons\\";
                saveTheDialog.FileName = nameToSave;
                if (saveTheDialog.ShowDialog() == DialogResult.OK)
                {
                    ItemIconPictureBox.Image.Save(saveTheDialog.FileName, ImageFormat.Png);
                    AppendText("✔ ", Color.Green);
                    AppendText(nameToSave, Color.DarkRed);
                    AppendText(" successfully saved to ", Color.Black);
                    AppendText(saveTheDialog.FileName, Color.SteelBlue, true);
                }
            }
        }
        private void OpenImageTS_Click(object sender, EventArgs e)
        {
            if (ItemIconPictureBox.Image != null)
            {
                var newForm = new Form();

                PictureBox pb = new PictureBox();
                pb.Dock = DockStyle.Fill;
                pb.Image = ItemIconPictureBox.Image;
                pb.SizeMode = PictureBoxSizeMode.Zoom;

                newForm.Size = ItemIconPictureBox.Image.Size;
                newForm.Icon = Properties.Resources.FNTools_Logo_Icon;
                newForm.Text = currentItem;
                newForm.StartPosition = FormStartPosition.CenterScreen;
                newForm.Controls.Add(pb);
                newForm.Show();
            }
        }

        private void mergeGeneratedImagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Properties.Settings.Default.mergerFileName))
            {
                MessageBox.Show("Please, set a name to your file before trying to merge images\n\nSteps:\n\t- Load button drop down menu\n\t- Options", "FModel Merger File Name Missing", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                OpenFileDialog theDialog = new OpenFileDialog();
                theDialog.Multiselect = true;
                theDialog.InitialDirectory = docPath + "\\Generated Icons\\";
                theDialog.Title = "Choose your images";
                theDialog.Filter = "PNG Files (*.png)|*.png|JPEG Files (*.jpg)|*.jpg|BMP Files (*.bmp)|*.bmp|All Files (*.*)|*.*";

                if (theDialog.ShowDialog() == DialogResult.OK)
                {
                    List<Image> selectedImages = new List<Image>();
                    foreach (var files in theDialog.FileNames)
                    {
                        selectedImages.Add(Image.FromFile(files));
                    }

                    if (Properties.Settings.Default.mergerImagesRow == 0)
                    {
                        Properties.Settings.Default.mergerImagesRow = 7;
                        Properties.Settings.Default.Save();
                    }
                    int numperrow = Properties.Settings.Default.mergerImagesRow;
                    var w = 530 * numperrow;
                    if (selectedImages.Count * 530 < 530 * numperrow)
                    {
                        w = selectedImages.Count * 530;
                    }

                    int h = int.Parse(Math.Ceiling(double.Parse(selectedImages.Count.ToString()) / numperrow).ToString()) * 530;
                    Bitmap bmp = new Bitmap(w - 8, h - 8);

                    var num = 1;
                    var cur_w = 0;
                    var cur_h = 0;

                    for (int i = 0; i < selectedImages.Count; i++)
                    {
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            g.DrawImage(selectedImages[i], new PointF(cur_w, cur_h));
                            if (num % numperrow == 0)
                            {
                                cur_w = 0;
                                cur_h += 530;
                                num += 1;
                            }
                            else
                            {
                                cur_w += 530;
                                num += 1;
                            }
                        }
                    }
                    bmp.Save(docPath + "\\" + Properties.Settings.Default.mergerFileName + ".png", ImageFormat.Png);
                    var newForm = new Form();

                    PictureBox pb = new PictureBox();
                    pb.Dock = DockStyle.Fill;
                    pb.Image = bmp;
                    pb.SizeMode = PictureBoxSizeMode.Zoom;

                    newForm.WindowState = FormWindowState.Maximized;
                    newForm.Size = bmp.Size;
                    newForm.Icon = Properties.Resources.FNTools_Logo_Icon;
                    newForm.Text = docPath + "\\" + Properties.Settings.Default.mergerFileName + ".png";
                    newForm.StartPosition = FormStartPosition.CenterScreen;
                    newForm.Controls.Add(pb);
                    newForm.Show();
                }
            }
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if ((Application.OpenForms["OptionsWindow"] as OptionsWindow) != null)
            {
                Application.OpenForms["OptionsWindow"].Focus();
            }
            else
            {
                var optionForm = new OptionsWindow();
                optionForm.Show();
            }
        }
    }
}
