using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;
using AutoUpdaterDotNET;
using csharp_wick;
using FModel.Converter;
using FModel.Forms;
using FModel.Parser.Banners;
using FModel.Parser.Challenges;
using FModel.Parser.Featured;
using FModel.Parser.Items;
using FModel.Parser.Quests;
using FModel.Parser.RenderMat;
using FModel.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScintillaNET;
using ScintillaNET_FindReplaceDialog;
using Image = System.Drawing.Image;
using Settings = FModel.Properties.Settings;

namespace FModel
{
    public partial class MainWindow : Form
    {
        #region EVERYTHING WE NEED
        FindReplace _myFindReplace;
        public Stopwatch StopWatch;
        public PakAsset MyAsset;
        public PakExtractor MyExtractor;
        private static string[] _paksArray;
        public static string[] pakAsTxt;
        public static Dictionary<string, string> AllpaksDictionary;
        private static Dictionary<string, long> _questStageDict;
        private static Dictionary<string, string> _diffToExtract;
        private static Dictionary<string, string> _paksMountPoint;
        private static string _backupFileName;
        private static string _backupDynamicKeys;
        private static List<string> _itemsToDisplay;
        public static string DefaultOutputPath;
        public static string CurrentUsedPak;
        public static string CurrentUsedPakGuid;
        public static string CurrentUsedItem;
        public static string ExtractedFilePath;
        public static string[] SelectedItemsArray;
        public static string[] SelectedChallengesArray;
        public static bool WasFeatured;
        public static string ItemIconPath;
        public static int YAfterLoop;
        public static bool UmWorking;
        #endregion

        #region FONTS
        PrivateFontCollection _pfc = new PrivateFontCollection();
        StringFormat _centeredString = new StringFormat();
        StringFormat _rightString = new StringFormat();
        StringFormat _centeredStringLine = new StringFormat();
        private int _fontLength;
        private byte[] _fontdata;
        private int _fontLength2;
        private byte[] _fontdata2;
        #endregion

        #region DLLIMPORT
        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int description, int reservedValue);
        public static bool IsInternetAvailable()
        {
            return InternetGetConnectedState(description: out _, reservedValue: 0);
        }
        [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string pszSubIdList);
        public static void SetTreeViewTheme(IntPtr treeHandle)
        {
            SetWindowTheme(treeHandle, "explorer", null);
        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            toolStripStatusLabel1.Text += @" " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString().Substring(0, 5);

            treeView1.Sort();
            //REMOVE SPACE CAUSED BY SIZING GRIP
            statusStrip1.Padding = new Padding(statusStrip1.Padding.Left, statusStrip1.Padding.Top, statusStrip1.Padding.Left, statusStrip1.Padding.Bottom);

            // Create instance of FindReplace with reference to a ScintillaNET control.
            _myFindReplace = new FindReplace(scintilla1); // For WinForms
            _myFindReplace.Window.StartPosition = FormStartPosition.CenterScreen;
            // Tie in FindReplace event
            _myFindReplace.KeyPressed += MyFindReplace_KeyPressed;
            // Tie in Scintilla event
            scintilla1.KeyDown += scintilla1_KeyDown;
        }

        #region USEFUL METHODS
        private void UpdateConsole(string textToDisplay, Color seColor, string seText)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, Color, string>(UpdateConsole), textToDisplay, seColor, seText);
                return;
            }

            toolStripStatusLabel2.Text = textToDisplay;
            toolStripStatusLabel3.BackColor = seColor;
            toolStripStatusLabel3.Text = seText;
        }
        private void AppendText(string text, Color color, bool addNewLine = false, HorizontalAlignment align = HorizontalAlignment.Left)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, Color, bool, HorizontalAlignment>(AppendText), text, color, addNewLine, align);
                return;
            }
            richTextBox1.SuspendLayout();
            richTextBox1.SelectionColor = color;
            richTextBox1.SelectionAlignment = align;
            richTextBox1.AppendText(addNewLine
                ? $"{text}{Environment.NewLine}"
                : text);
            richTextBox1.ScrollToCaret();
            richTextBox1.ResumeLayout();
        }
        private void OpenWithDefaultProgramAndNoFocus(string path)
        {
            Process fileopener = new Process();
            fileopener.StartInfo.FileName = "explorer";
            fileopener.StartInfo.Arguments = "\"" + path + "\"";
            fileopener.Start();
        }
        public Image SetImageOpacity(Image image, float opacity)
        {
            try
            {
                //create a Bitmap the size of the image provided  
                Bitmap bmp = new Bitmap(image.Width, image.Height);

                //create a graphics object from the image  
                using (Graphics gfx = Graphics.FromImage(bmp))
                {

                    //create a color matrix object  
                    ColorMatrix matrix = new ColorMatrix();

                    //set the opacity  
                    matrix.Matrix33 = opacity;

                    //create image attributes  
                    ImageAttributes attributes = new ImageAttributes();

                    //set the color(opacity) of the image  
                    attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                    //now draw the image  
                    gfx.DrawImage(image, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
                }
                return bmp;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
        }
        private void CreateDir()
        {
            if (!Directory.Exists(DefaultOutputPath + "\\Backup\\"))
                Directory.CreateDirectory(DefaultOutputPath + "\\Backup\\");
            if (!Directory.Exists(DefaultOutputPath + "\\Extracted\\"))
                Directory.CreateDirectory(DefaultOutputPath + "\\Extracted\\");
            if (!Directory.Exists(DefaultOutputPath + "\\Icons\\"))
                Directory.CreateDirectory(DefaultOutputPath + "\\Icons\\");
            if (!Directory.Exists(DefaultOutputPath + "\\Sounds\\"))
                Directory.CreateDirectory(DefaultOutputPath + "\\Sounds\\");
        }
        public static void SetFolderPermission(string folderPath)
        {
            var directoryInfo = new DirectoryInfo(folderPath);
            var directorySecurity = directoryInfo.GetAccessControl();
            var currentUserIdentity = WindowsIdentity.GetCurrent();
            var fileSystemRule = new FileSystemAccessRule(currentUserIdentity.Name,
                                                          FileSystemRights.Read,
                                                          InheritanceFlags.ObjectInherit |
                                                          InheritanceFlags.ContainerInherit,
                                                          PropagationFlags.None,
                                                          AccessControlType.Allow);

            directorySecurity.AddAccessRule(fileSystemRule);
            directoryInfo.SetAccessControl(directorySecurity);
        }
        #endregion

        #region LOAD & LEAVE
        //METHODS
        private void AddPaKs(IEnumerable<string> thePaks, int index)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<IEnumerable<string>, int>(AddPaKs), thePaks, index);
                return;
            }
            loadOneToolStripMenuItem.DropDownItems.Add(Path.GetFileName(thePaks.ElementAt(index)));
        }
        private void FillWithPaKs()
        {
            if (!Directory.Exists(Settings.Default.PAKsPath))
            {
                loadOneToolStripMenuItem.Enabled = false;
                loadAllToolStripMenuItem.Enabled = false;
                backupPAKsToolStripMenuItem.Enabled = false;

                UpdateConsole(".PAK Files Path is missing", Color.FromArgb(255, 244, 66, 66), "Error");
            }
            else
            {
                IEnumerable<string> yourPaKs = Directory.GetFiles(Settings.Default.PAKsPath).Where(x => x.EndsWith(".pak"));
                int count = 0;
                var thePaks = yourPaKs as string[] ?? yourPaKs.ToArray();
                foreach (var dummy in thePaks) count++;
                _paksArray = new string[count];
                for (int i = 0; i < thePaks.Count(); i++)
                {
                    AddPaKs(thePaks, i);
                    _paksArray[i] = Path.GetFileName(thePaks.ElementAt(i));
                }
            }
        }
        private void SetOutput()
        {
            DefaultOutputPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\FModel"; //DOCUMENTS FOLDER BY DEFAULT
            if (string.IsNullOrEmpty(Settings.Default.ExtractOutput))
            {
                Settings.Default.ExtractOutput = DefaultOutputPath;
                Settings.Default.Save();
            }
            else
            {
                DefaultOutputPath = Settings.Default.ExtractOutput;
            }

            if (!Directory.Exists(DefaultOutputPath))
                Directory.CreateDirectory(DefaultOutputPath);
        }
        private void JohnWickCheck()
        {
            if (File.Exists(DefaultOutputPath + "\\john-wick-parse-modded.exe"))
            {
                File.Delete(DefaultOutputPath + "\\john-wick-parse-modded.exe");
            }
            if (File.Exists(DefaultOutputPath + "\\john-wick-parse_custom.exe"))
            {
                File.Delete(DefaultOutputPath + "\\john-wick-parse_custom.exe");
            }
        }
        private void KeyCheck()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(KeyCheck));
                return;
            }
            AESKeyTextBox.Text = @"0x" + Settings.Default.AESKey;
        }
        private void SetScintillaStyle()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(SetScintillaStyle));
                return;
            }

            scintilla1.Styles[Style.Json.Default].ForeColor = Color.Silver;
            scintilla1.Styles[Style.Json.BlockComment].ForeColor = Color.FromArgb(0, 128, 0);
            scintilla1.Styles[Style.Json.LineComment].ForeColor = Color.FromArgb(0, 128, 0);
            scintilla1.Styles[Style.Json.Number].ForeColor = Color.Green;
            scintilla1.Styles[Style.Json.PropertyName].ForeColor = Color.SteelBlue;
            scintilla1.Styles[Style.Json.String].ForeColor = Color.OrangeRed;
            scintilla1.Styles[Style.Json.StringEol].BackColor = Color.OrangeRed;
            scintilla1.Styles[Style.Json.Operator].ForeColor = Color.Black;
            scintilla1.Styles[Style.LineNumber].ForeColor = Color.DarkGray;
            var nums = scintilla1.Margins[1];
            nums.Width = 30;
            nums.Type = MarginType.Number;
            nums.Sensitive = true;
            nums.Mask = 0;

            scintilla1.ClearCmdKey(Keys.Control | Keys.F);
            scintilla1.ClearCmdKey(Keys.Control | Keys.Z);
            scintilla1.Lexer = Lexer.Json;
        }
        private void SetFont()
        {
            _fontLength = Resources.BurbankBigCondensed_Bold.Length;
            _fontdata = Resources.BurbankBigCondensed_Bold;
            IntPtr weirdData = Marshal.AllocCoTaskMem(_fontLength);
            Marshal.Copy(_fontdata, 0, weirdData, _fontLength);
            _pfc.AddMemoryFont(weirdData, _fontLength);

            _fontLength2 = Resources.BurbankBigCondensed_Black.Length;
            _fontdata2 = Resources.BurbankBigCondensed_Black;
            IntPtr weirdData2 = Marshal.AllocCoTaskMem(_fontLength2);
            Marshal.Copy(_fontdata2, 0, weirdData2, _fontLength2);
            _pfc.AddMemoryFont(weirdData2, _fontLength2);

            _centeredString.Alignment = StringAlignment.Center;
            _rightString.Alignment = StringAlignment.Far;
            _centeredStringLine.LineAlignment = StringAlignment.Center;
            _centeredStringLine.Alignment = StringAlignment.Center;
        }

        //EVENTS
        private async void MainWindow_Load(object sender, EventArgs e)
        {
            AutoUpdater.Start("https://dl.dropbox.com/s/3kv2pukqu6tj1r0/FModel.xml?dl=0");

            SetTreeViewTheme(treeView1.Handle);
            _backupFileName = "\\FortniteGame_" + DateTime.Now.ToString("MMddyyyy") + ".txt";

            // Copy user settings from previous application version if necessary
            if (Settings.Default.UpdateSettings)
            {
                Settings.Default.Upgrade();
                Settings.Default.UpdateSettings = false;
                Settings.Default.Save();
            }

            await Task.Run(() => {
                FillWithPaKs();
                KeyCheck();
                SetOutput();
                SetFolderPermission(DefaultOutputPath);
                JohnWickCheck();
                CreateDir();
                SetScintillaStyle();
                SetFont();
            });
        }
        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            Environment.Exit(Environment.ExitCode);
        }
        private void differenceModeToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (differenceModeToolStripMenuItem.Checked)
            {
                loadAllToolStripMenuItem.Text = @"Load Difference";
                loadOneToolStripMenuItem.Enabled = false;
                updateModeToolStripMenuItem.Enabled = true;
            }
            if (differenceModeToolStripMenuItem.Checked == false)
            {
                loadAllToolStripMenuItem.Text = @"Load All PAKs";
                loadOneToolStripMenuItem.Enabled = true;
                updateModeToolStripMenuItem.Enabled = false;
                if (updateModeToolStripMenuItem.Checked)
                    updateModeToolStripMenuItem.Checked = false;
            }
            if (updateModeToolStripMenuItem.Checked == false && differenceModeToolStripMenuItem.Checked == false)
            {
                loadAllToolStripMenuItem.Text = @"Load All PAKs";
            }
        }
        private void updateModeToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (updateModeToolStripMenuItem.Checked)
            {
                loadAllToolStripMenuItem.Text = @"Load And Extract Difference";
                var updateModeForm = new UpdateModeSettings();
                if (Application.OpenForms[updateModeForm.Name] == null)
                {
                    updateModeForm.Show();
                }
                else
                {
                    Application.OpenForms[updateModeForm.Name].Focus();
                }
            }
            if (updateModeToolStripMenuItem.Checked == false)
            {
                loadAllToolStripMenuItem.Text = @"Load Difference";
            }
        }
        private void scintilla1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.F)
            {
                _myFindReplace.ShowFind();
                e.SuppressKeyPress = true;
            }
            else if (e.Shift && e.KeyCode == Keys.F3)
            {
                _myFindReplace.Window.FindPrevious();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.F3)
            {
                _myFindReplace.Window.FindNext();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.H)
            {
                _myFindReplace.ShowReplace();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.I)
            {
                _myFindReplace.ShowIncrementalSearch();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.G)
            {
                GoTo myGoTo = new GoTo((Scintilla)sender);
                myGoTo.ShowGoToDialog();
                e.SuppressKeyPress = true;
            }
        }
        private void MyFindReplace_KeyPressed(object sender, KeyEventArgs e)
        {
            scintilla1_KeyDown(sender, e);
        }

        //FORMS
        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var settingsForm = new Forms.Settings();
            if (Application.OpenForms[settingsForm.Name] == null)
            {
                settingsForm.Show();
            }
            else
            {
                Application.OpenForms[settingsForm.Name].Focus();
            }
        }
        private void aboutFModelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var aboutForm = new About();
            if (Application.OpenForms[aboutForm.Name] == null)
            {
                aboutForm.Show();
            }
            else
            {
                Application.OpenForms[aboutForm.Name].Focus();
            }
        }
        private void searchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var searchForm = new SearchFiles();
            if (Application.OpenForms[searchForm.Name] == null)
            {
                searchForm.Show();
            }
            else
            {
                Application.OpenForms[searchForm.Name].Focus();
            }
            searchForm.FormClosing += (o, c) =>
            {
                OpenMe();
            };
        }
        #endregion

        #region PAKLIST & FILL TREE
        //METHODS
        private string ReadPakGuid(string pakPath)
        {
            using (BinaryReader reader = new BinaryReader(File.Open(pakPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                reader.BaseStream.Seek(reader.BaseStream.Length - 61 - 160, SeekOrigin.Begin);
                uint g1 = reader.ReadUInt32();
                reader.BaseStream.Seek(reader.BaseStream.Length - 57 - 160, SeekOrigin.Begin);
                uint g2 = reader.ReadUInt32();
                reader.BaseStream.Seek(reader.BaseStream.Length - 53 - 160, SeekOrigin.Begin);
                uint g3 = reader.ReadUInt32();
                reader.BaseStream.Seek(reader.BaseStream.Length - 49 - 160, SeekOrigin.Begin);
                uint g4 = reader.ReadUInt32();

                var guid = g1 + "-" + g2 + "-" + g3 + "-" + g4;
                return guid;
            }
        }
        private void RegisterPaKsinDict(string[] allYourPaKs, ToolStripItemClickedEventArgs theSinglePak = null, bool loadAllPaKs = false)
        {
            for (int i = 0; i < allYourPaKs.Length; i++)
            {
                string arCurrentUsedPak = allYourPaKs[i]; //SET CURRENT PAK
                string arCurrentUsedPakGuid = ReadPakGuid(Settings.Default.PAKsPath + "\\" + arCurrentUsedPak); //SET CURRENT PAK GUID

                if (arCurrentUsedPakGuid == "0-0-0-0") //NO DYNAMIC PAK IN DICTIONARY
                {
                    try
                    {
                        MyExtractor = new PakExtractor(Settings.Default.PAKsPath + "\\" + arCurrentUsedPak, Settings.Default.AESKey);
                    }
                    catch (Exception)
                    {
                        break;
                    }

                    if (MyExtractor.GetFileList() != null)
                    {
                        _paksMountPoint.Add(arCurrentUsedPak, MyExtractor.GetMountPoint().Substring(9));

                        if (loadAllPaKs)
                            if (!File.Exists(DefaultOutputPath + "\\FortnitePAKs.txt"))
                                File.Create(DefaultOutputPath + "\\FortnitePAKs.txt").Dispose();

                        string[] currentUsedPakLines = MyExtractor.GetFileList().ToArray();
                        for (int ii = 0; ii < currentUsedPakLines.Length; ii++)
                        {
                            currentUsedPakLines[ii] = MyExtractor.GetMountPoint().Substring(6) + currentUsedPakLines[ii];

                            string currentUsedPakFileName = currentUsedPakLines[ii].Substring(currentUsedPakLines[ii].LastIndexOf("/", StringComparison.Ordinal) + 1);
                            if (currentUsedPakFileName.Contains(".uasset") || currentUsedPakFileName.Contains(".uexp") || currentUsedPakFileName.Contains(".ubulk"))
                            {
                                if (!AllpaksDictionary.ContainsKey(currentUsedPakFileName.Substring(0, currentUsedPakFileName.LastIndexOf(".", StringComparison.Ordinal))))
                                {
                                    AllpaksDictionary.Add(currentUsedPakFileName.Substring(0, currentUsedPakFileName.LastIndexOf(".", StringComparison.Ordinal)), arCurrentUsedPak);
                                }
                            }
                            else
                            {
                                if (!AllpaksDictionary.ContainsKey(currentUsedPakFileName))
                                {
                                    AllpaksDictionary.Add(currentUsedPakFileName, arCurrentUsedPak);
                                }
                            }
                        }
                        if (loadAllPaKs)
                        {
                            UpdateConsole(".PAK mount point: " + MyExtractor.GetMountPoint().Substring(9), Color.FromArgb(255, 244, 132, 66), "Waiting");

                            File.AppendAllLines(DefaultOutputPath + "\\FortnitePAKs.txt", currentUsedPakLines);

                            CurrentUsedPak = null;
                            CurrentUsedPakGuid = null;
                        }
                    }
                }
                if (theSinglePak != null)
                {
                    CurrentUsedPak = theSinglePak.ClickedItem.Text;
                    CurrentUsedPakGuid = ReadPakGuid(Settings.Default.PAKsPath + "\\" + CurrentUsedPak);

                    if (arCurrentUsedPak == theSinglePak.ClickedItem.Text && MyExtractor.GetFileList() != null)
                        pakAsTxt = MyExtractor.GetFileList().ToArray();
                }
            }
            if (theSinglePak != null && ReadPakGuid(Settings.Default.PAKsPath + "\\" + theSinglePak.ClickedItem.Text) != "0-0-0-0") //LOADING DYNAMIC PAK
            {
                CurrentUsedPak = theSinglePak.ClickedItem.Text;
                CurrentUsedPakGuid = ReadPakGuid(Settings.Default.PAKsPath + "\\" + CurrentUsedPak);

                try
                {
                    MyExtractor = new PakExtractor(Settings.Default.PAKsPath + "\\" + theSinglePak.ClickedItem.Text, Settings.Default.AESKey);

                    if (MyExtractor.GetFileList() != null)
                    {
                        _paksMountPoint.Add(theSinglePak.ClickedItem.Text, MyExtractor.GetMountPoint().Substring(9));
                        pakAsTxt = MyExtractor.GetFileList().ToArray();
                    }
                }
                catch (Exception)
                {
                    UpdateConsole("Can't read " + theSinglePak.ClickedItem.Text + " with this key", Color.FromArgb(255, 244, 66, 66), "Error");
                }
            }
            UpdateConsole("Building tree, please wait...", Color.FromArgb(255, 244, 132, 66), "Loading");
        }
        private void TreeParsePath(TreeNodeCollection nodeList, string path) //https://social.msdn.microsoft.com/Forums/en-US/c75c1804-6933-40ba-b17a-0e36ae8bcbb5/how-to-create-a-tree-view-with-full-paths?forum=csharplanguage
        {
            TreeNode node;
            string folder;
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
                Invoke(new Action(() =>
                {
                    nodeList.Add(node);
                }));
            }
            if (path != "")
            {
                TreeParsePath(node.Nodes, path);
            }
        }
        private void ComparePaKs()
        {
            pakAsTxt = File.ReadAllLines(DefaultOutputPath + "\\FortnitePAKs.txt");
            File.Delete(DefaultOutputPath + "\\FortnitePAKs.txt");

            //ASK DIFFERENCE FILE AND COMPARE
            OpenFileDialog theDialog = new OpenFileDialog();
            theDialog.Title = @"Choose your Backup PAK File";
            theDialog.InitialDirectory = DefaultOutputPath + "\\Backup";
            theDialog.Multiselect = false;
            theDialog.Filter = @"TXT Files (*.txt)|*.txt|All Files (*.*)|*.*";
            Invoke(new Action(() =>
            {
                if (theDialog.ShowDialog() == DialogResult.OK)
                {
                    String[] linesA = File.ReadAllLines(theDialog.FileName);
                    for (int i = 0; i < linesA.Length; i++)
                        if (!linesA[i].StartsWith("../"))
                            linesA[i] = "../" + linesA[i];

                    IEnumerable<String> onlyB = pakAsTxt.Except(linesA);
                    IEnumerable<String> removed = linesA.Except(pakAsTxt);

                    File.WriteAllLines(DefaultOutputPath + "\\Result.txt", onlyB);
                    File.WriteAllLines(DefaultOutputPath + "\\Removed.txt", removed);
                }
            }));

            //GET REMOVED FILES
            var removedTxt = File.ReadAllLines(DefaultOutputPath + "\\Removed.txt");
            File.Delete(DefaultOutputPath + "\\Removed.txt");

            List<string> removedItems = new List<string>();
            for (int i = 0; i < removedTxt.Length; i++)
            {
                if (removedTxt[i].Contains("FortniteGame/Content/Athena/Items/Cosmetics/"))
                    removedItems.Add(removedTxt[i].Substring(0, removedTxt[i].LastIndexOf(".", StringComparison.Ordinal)));
            }
            if (removedItems.Count != 0)
            {
                Invoke(new Action(() =>
                {
                    AppendText("Items Removed/Renamed:", Color.Red, true);
                    removedItems = removedItems.Distinct().ToList();
                    for (int ii = 0; ii < removedItems.Count; ii++)
                        AppendText("    - " + removedItems[ii], Color.Black, true);
                }));
            }

            pakAsTxt = File.ReadAllLines(DefaultOutputPath + "\\Result.txt");
            File.Delete(DefaultOutputPath + "\\Result.txt");
        }
        private void CreatePakList(ToolStripItemClickedEventArgs selectedPak = null, bool loadAllPaKs = false, bool getDiff = false, bool updateMode = false)
        {
            AllpaksDictionary = new Dictionary<string, string>();
            _diffToExtract = new Dictionary<string, string>();
            _paksMountPoint = new Dictionary<string, string>();
            Settings.Default.AESKey = AESKeyTextBox.Text.Substring(2).ToUpper();
            Settings.Default.Save();

            if (selectedPak != null)
            {
                UpdateConsole(Settings.Default.PAKsPath + "\\" + selectedPak.ClickedItem.Text, Color.FromArgb(255, 244, 132, 66), "Loading");

                //ADD TO DICTIONNARY
                RegisterPaKsinDict(_paksArray, selectedPak);

                if (pakAsTxt != null)
                {
                    Invoke(new Action(() =>
                    {
                        treeView1.BeginUpdate();
                        for (int i = 0; i < pakAsTxt.Length; i++)
                        {
                            TreeParsePath(treeView1.Nodes, pakAsTxt[i].Replace(pakAsTxt[i].Split('/').Last(), ""));
                        }
                        treeView1.EndUpdate();
                    }));
                    UpdateConsole(Settings.Default.PAKsPath + "\\" + selectedPak.ClickedItem.Text, Color.FromArgb(255, 66, 244, 66), "Success");
                }
                else
                    UpdateConsole("Can't read " + selectedPak.ClickedItem.Text + " with this key", Color.FromArgb(255, 244, 66, 66), "Error");
            }
            if (loadAllPaKs)
            {
                //ADD TO DICTIONNARY
                RegisterPaKsinDict(_paksArray, null, true);

                if (!File.Exists(DefaultOutputPath + "\\FortnitePAKs.txt"))
                {
                    UpdateConsole("Can't read .PAK files with this key", Color.FromArgb(255, 244, 66, 66), "Error");
                }
                else
                {
                    pakAsTxt = File.ReadAllLines(DefaultOutputPath + "\\FortnitePAKs.txt");
                    File.Delete(DefaultOutputPath + "\\FortnitePAKs.txt");

                    Invoke(new Action(() =>
                    {
                        treeView1.BeginUpdate();
                        for (int i = 0; i < pakAsTxt.Length; i++)
                        {
                            TreeParsePath(treeView1.Nodes, pakAsTxt[i].Replace(pakAsTxt[i].Split('/').Last(), ""));
                        }
                        treeView1.EndUpdate();
                    }));
                    UpdateConsole(Settings.Default.PAKsPath, Color.FromArgb(255, 66, 244, 66), "Success");
                }
            }
            if (getDiff)
            {
                //ADD TO DICTIONNARY
                RegisterPaKsinDict(_paksArray, null, true);

                if (!File.Exists(DefaultOutputPath + "\\FortnitePAKs.txt"))
                {
                    UpdateConsole("Can't read .PAK files with this key", Color.FromArgb(255, 244, 66, 66), "Error");
                }
                else
                {
                    UpdateConsole("Comparing files...", Color.FromArgb(255, 244, 132, 66), "Loading");
                    ComparePaKs();
                    if (updateMode)
                    {
                        UmFilter(pakAsTxt, _diffToExtract);
                        UmWorking = true;
                    }

                    Invoke(new Action(() =>
                    {
                        treeView1.BeginUpdate();
                        for (int i = 0; i < pakAsTxt.Length; i++)
                        {
                            TreeParsePath(treeView1.Nodes, pakAsTxt[i].Replace(pakAsTxt[i].Split('/').Last(), ""));
                        }
                        treeView1.EndUpdate();
                    }));
                    UpdateConsole("Files compared", Color.FromArgb(255, 66, 244, 66), "Success");
                }
            }
        }
        private void CreateBackupList(string[] allYourPaKs)
        {
            bool connection = IsInternetAvailable();
            string url = "https://pastebin.com/raw/bbnhmjWN";
            if (connection)
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream ?? throw new InvalidOperationException()))
                {
                    _backupDynamicKeys = reader.ReadToEnd();
                }
            }

            Settings.Default.AESKey = AESKeyTextBox.Text.Substring(2).ToUpper();
            Settings.Default.Save();

            for (int i = 0; i < allYourPaKs.Length; i++)
            {
                string arCurrentUsedPak = allYourPaKs[i]; //SET CURRENT PAK
                string arCurrentUsedPakGuid = ReadPakGuid(Settings.Default.PAKsPath + "\\" + arCurrentUsedPak); //SET CURRENT PAK GUID

                if (arCurrentUsedPakGuid == "0-0-0-0") //NO DYNAMIC PAK
                {
                    try
                    {
                        MyExtractor = new PakExtractor(Settings.Default.PAKsPath + "\\" + arCurrentUsedPak, Settings.Default.AESKey);
                    }
                    catch (Exception)
                    {
                        break;
                    }

                    if (MyExtractor.GetFileList() != null)
                    {
                        if (!File.Exists(DefaultOutputPath + "\\Backup" + _backupFileName))
                            File.Create(DefaultOutputPath + "\\Backup" + _backupFileName).Dispose();

                        string[] currentUsedPakLines = MyExtractor.GetFileList().ToArray();
                        for (int ii = 0; ii < currentUsedPakLines.Length; ii++)
                        {
                            currentUsedPakLines[ii] = MyExtractor.GetMountPoint().Substring(6) + currentUsedPakLines[ii];
                        }
                        UpdateConsole(".PAK mount point: " + MyExtractor.GetMountPoint().Substring(9), Color.FromArgb(255, 244, 132, 66), "Waiting");

                        File.AppendAllLines(DefaultOutputPath + "\\Backup" + _backupFileName, currentUsedPakLines);
                    }
                }
                else
                {
                    foreach (var myString in _backupDynamicKeys.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var parts = myString.Split(':');
                        if (parts[0] == arCurrentUsedPak && parts[1].StartsWith("0x"))
                        {
                            try
                            {
                                MyExtractor = new PakExtractor(Settings.Default.PAKsPath + "\\" + arCurrentUsedPak, parts[1].Substring(2));
                            }
                            catch (Exception)
                            {
                                continue;
                            }

                            if (MyExtractor.GetFileList() != null)
                            {
                                if (!File.Exists(DefaultOutputPath + "\\Backup" + _backupFileName))
                                    File.Create(DefaultOutputPath + "\\Backup" + _backupFileName).Dispose();

                                string[] currentUsedPakLines = MyExtractor.GetFileList().ToArray();
                                for (int ii = 0; ii < currentUsedPakLines.Length; ii++)
                                {
                                    currentUsedPakLines[ii] = MyExtractor.GetMountPoint().Substring(6) + currentUsedPakLines[ii];
                                }
                                UpdateConsole(arCurrentUsedPak, Color.FromArgb(255, 244, 132, 66), "Waiting");

                                File.AppendAllLines(DefaultOutputPath + "\\Backup" + _backupFileName, currentUsedPakLines);
                            }
                        }
                        else if (parts[0] == arCurrentUsedPak && parts[1] == "undefined")
                        {
                            AppendText("No key found for ", Color.Black);
                            AppendText(arCurrentUsedPak, Color.DarkRed, true);

                            //TODO: BETTER VERSION KTHX
                            /*string promptValue = Prompt.ShowDialog("AES Key:", arCurrentUsedPak);
                            if (!string.IsNullOrEmpty(promptValue))
                            {
                                JwpmProcess("filelist \"" + Settings.Default.PAKsPath + "\\" + arCurrentUsedPak + "\" \"" + DefaultOutputPath + "\" " + promptValue.Substring(2));
                                if (File.Exists(DefaultOutputPath + "\\" + arCurrentUsedPak + ".txt"))
                                {
                                    if (!File.Exists(DefaultOutputPath + "\\Backup" + _backupFileName))
                                        File.Create(DefaultOutputPath + "\\Backup" + _backupFileName).Dispose();

                                    string[] currentUsedPakLines = File.ReadAllLines(DefaultOutputPath + "\\" + arCurrentUsedPak + ".txt");
                                    for (int ii = 0; ii < currentUsedPakLines.Length; ii++)
                                    {
                                        currentUsedPakLines[ii] = "FortniteGame/" + currentUsedPakLines[ii];
                                    }
                                    UpdateConsole(".PAK mount point: \"/FortniteGame/\"", Color.FromArgb(255, 244, 132, 66), "Waiting");

                                    File.AppendAllLines(DefaultOutputPath + "\\Backup" + _backupFileName, currentUsedPakLines);
                                    File.Delete(DefaultOutputPath + "\\" + arCurrentUsedPak + ".txt");
                                }
                            }*/
                        }
                    }
                }
            }
            if (File.Exists(DefaultOutputPath + "\\Backup" + _backupFileName))
                UpdateConsole("\\Backup" + _backupFileName + " successfully created", Color.FromArgb(255, 66, 244, 66), "Success");
            else
                UpdateConsole("Can't create " + _backupFileName.Substring(1), Color.FromArgb(255, 244, 66, 66), "Error");
        }
        private void UpdateModeExtractSave()
        {
            CreatePakList(null, false, true, true);

            _questStageDict = new Dictionary<string, long>();
            Invoke(new Action(() =>
            {
                ExtractButton.Enabled = false;
                OpenImageButton.Enabled = false;
                StopButton.Enabled = true;
            }));
            if (backgroundWorker2.IsBusy != true)
            {
                backgroundWorker2.RunWorkerAsync();
            }
        }
        private void UmFilter(String[] theFile, Dictionary<string, string> diffToExtract)
        {
            List<string> searchResults = new List<string>();

            if (Settings.Default.UMCosmetics)
                searchResults.Add("Athena/Items/Cosmetics/");
            if (Settings.Default.UMVariants)
                searchResults.Add("Athena/Items/CosmeticVariantTokens/");
            if (Settings.Default.UMConsumablesWeapons)
            {
                searchResults.Add("AGID_");
                searchResults.Add("WID_");
            }
            if (Settings.Default.UMTraps)
                searchResults.Add("Athena/Items/Traps/");
            if (Settings.Default.UMChallenges)
                searchResults.Add("Athena/Items/ChallengeBundles/");

            if (Settings.Default.UMTCosmeticsVariants)
            {
                searchResults.Add("UI/Foundation/Textures/Icons/Backpacks/");
                searchResults.Add("UI/Foundation/Textures/Icons/Emotes/");
                searchResults.Add("UI/Foundation/Textures/Icons/Heroes/Athena/Soldier/");
                searchResults.Add("UI/Foundation/Textures/Icons/Heroes/Variants/");
                searchResults.Add("UI/Foundation/Textures/Icons/Skydiving/");
                searchResults.Add("UI/Foundation/Textures/Icons/Pets/");
                searchResults.Add("UI/Foundation/Textures/Icons/Wraps/");
            }
            if (Settings.Default.UMTLoading)
            {
                searchResults.Add("FortniteGame/Content/2dAssets/Loadingscreens/");
                searchResults.Add("UI/Foundation/Textures/LoadingScreens/");
            }
            if (Settings.Default.UMTWeapons)
                searchResults.Add("UI/Foundation/Textures/Icons/Weapons/Items/");
            if (Settings.Default.UMTBanners)
            {
                searchResults.Add("FortniteGame/Content/2dAssets/Banners/");
                searchResults.Add("UI/Foundation/Textures/Banner/");
                searchResults.Add("FortniteGame/Content/2dAssets/Sprays/");
                searchResults.Add("FortniteGame/Content/2dAssets/Emoji/");
                searchResults.Add("FortniteGame/Content/2dAssets/Music/");
                searchResults.Add("FortniteGame/Content/2dAssets/Toys/");
            }
            if (Settings.Default.UMTFeaturedIMGs)
                searchResults.Add("UI/Foundation/Textures/BattleRoyale/");
            if (Settings.Default.UMTAthena)
                searchResults.Add("UI/Foundation/Textures/Icons/Athena/");
            if (Settings.Default.UMTAthena)
                searchResults.Add("UI/Foundation/Textures/Icons/Athena/");
            if (Settings.Default.UMTDevices)
                searchResults.Add("UI/Foundation/Textures/Icons/Devices/");
            if (Settings.Default.UMTVehicles)
                searchResults.Add("UI/Foundation/Textures/Icons/Vehicles/");

            for (int i = 0; i < theFile.Length; i++)
            {
                bool b = searchResults.Any(s => theFile[i].Contains(s));
                if (b)
                {
                    string filename = theFile[i].Substring(theFile[i].LastIndexOf("/", StringComparison.Ordinal) + 1);
                    if (filename.Contains(".uasset") || filename.Contains(".uexp") || filename.Contains(".ubulk"))
                    {
                        if (!diffToExtract.ContainsKey(filename.Substring(0, filename.LastIndexOf(".", StringComparison.Ordinal))))
                            diffToExtract.Add(filename.Substring(0, filename.LastIndexOf(".", StringComparison.Ordinal)), theFile[i]);
                    }
                }
            }
        }

        //EVENTS
        private async void loadOneToolStripMenuItem_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            await Task.Run(() => {
                Invoke(new Action(() =>
                {
                    scintilla1.Text = "";
                    pictureBox1.Image = null;

                    treeView1.Nodes.Clear(); //SMH HERE IT DOESN'T LAG
                    listBox1.Items.Clear();
                }));

                CreatePakList(e);
            });
        }
        private async void loadAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Invoke(new Action(() =>
            {
                scintilla1.Text = "";
                pictureBox1.Image = null;

                treeView1.Nodes.Clear(); //SMH HERE IT DOESN'T LAG
                listBox1.Items.Clear();
            }));

            if (differenceModeToolStripMenuItem.Checked == false)
            {
                await Task.Run(() => {
                    CreatePakList(null, true);
                });
            }
            if (differenceModeToolStripMenuItem.Checked && updateModeToolStripMenuItem.Checked == false)
            {
                await Task.Run(() => {
                    CreatePakList(null, false, true);
                });
            }
            if (differenceModeToolStripMenuItem.Checked && updateModeToolStripMenuItem.Checked)
            {
                await Task.Run(() => {
                    UpdateModeExtractSave();
                });
            }
        }
        private async void backupPAKsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await Task.Run(() => {
                CreateBackupList(_paksArray);
            });
        }
        //UPDATE MODE
        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            StopWatch = new Stopwatch();
            StopWatch.Start();
            CreateDir();
            ExtractAndSerializeItems(e, true);
        }
        private void backgroundWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            StopWatch.Stop();
            if (e.Cancelled)
            {
                UpdateConsole("Canceled!", Color.FromArgb(255, 244, 66, 66), "Error");
            }
            else if (e.Error != null)
            {
                UpdateConsole(e.Error.Message, Color.FromArgb(255, 244, 66, 66), "Error");
            }
            else if (UmWorking == false)
            {
                UpdateConsole("Can't read .PAK files with this key", Color.FromArgb(255, 244, 66, 66), "Error");
            }
            else
            {
                TimeSpan ts = StopWatch.Elapsed;
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                UpdateConsole("Time elapsed: " + elapsedTime, Color.FromArgb(255, 66, 244, 66), "Success");
            }

            SelectedItemsArray = null;
            UmWorking = false;
            Invoke(new Action(() =>
            {
                updateModeToolStripMenuItem.Checked = false;
                StopButton.Enabled = false;
                OpenImageButton.Enabled = true;
                ExtractButton.Enabled = true;
            }));
        }
        #endregion

        #region FILL LISTBOX & FILTER
        //METHODS
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
        private void GetFilesAndFill(TreeNodeMouseClickEventArgs selectedNode)
        {
            List<string> itemsNotToDisplay = new List<string>();
            _itemsToDisplay = new List<string>();

            Invoke(new Action(() =>
            {
                listBox1.Items.Clear();
                FilterTextBox.Text = string.Empty;
            }));

            var all = GetAncestors(selectedNode.Node, x => x.Parent).ToList();
            all.Reverse();
            var full = string.Join("/", all.Select(x => x.Text)) + "/" + selectedNode.Node.Text + "/";
            if (string.IsNullOrEmpty(full))
            {
                return;
            }

            var dirfiles = pakAsTxt.Where(x => x.StartsWith(full) && !x.Replace(full, "").Contains("/"));
            var enumerable = dirfiles as string[] ?? dirfiles.ToArray();
            if (!enumerable.Any())
            {
                return;
            }

            foreach (var i in enumerable)
            {
                string v;
                if (i.Contains(".uasset") || i.Contains(".uexp") || i.Contains(".ubulk"))
                {
                    v = i.Substring(0, i.LastIndexOf('.'));
                }
                else
                {
                    v = i.Replace(full, "");
                }
                itemsNotToDisplay.Add(v.Replace(full, ""));
            }
            _itemsToDisplay = itemsNotToDisplay.Distinct().ToList(); //NO DUPLICATION + NO EXTENSION = EASY TO FIND WHAT WE WANT
            Invoke(new Action(() =>
            {
                for (int i = 0; i < _itemsToDisplay.Count; i++)
                {
                    listBox1.Items.Add(_itemsToDisplay[i]);
                }
                ExtractButton.Enabled = listBox1.SelectedIndex >= 0; //DISABLE EXTRACT BUTTON IF NOTHING IS SELECTED IN LISTBOX
            }));
        }
        public static bool CaseInsensitiveContains(string text, string value, StringComparison stringComparison = StringComparison.CurrentCultureIgnoreCase)
        {
            return text.IndexOf(value, stringComparison) >= 0;
        } //FILTER INSENSITIVE
        private void FilterItems()
        {
            if (listBox1.InvokeRequired)
            {
                listBox1.Invoke(new Action(FilterItems));
                return;
            }

            listBox1.BeginUpdate();
            listBox1.Items.Clear();

            if (_itemsToDisplay != null)
            {
                if (!string.IsNullOrEmpty(FilterTextBox.Text))
                {
                    for (int i = 0; i < _itemsToDisplay.Count; i++)
                    {
                        if (CaseInsensitiveContains(_itemsToDisplay[i], FilterTextBox.Text))
                        {
                            listBox1.Items.Add(_itemsToDisplay[i]);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < _itemsToDisplay.Count; i++)
                    {
                        listBox1.Items.Add(_itemsToDisplay[i]);
                    }
                }
            }

            listBox1.EndUpdate();
        }
        public async void ExpandMyLitleBoys(TreeNode node, List<string> path)
        {
            path.RemoveAt(0);
            node.Expand();

            if (path.Count == 0)
                return;

            if (path.Count == 1)
            {
                treeView1.SelectedNode = node;
                await Task.Run(() => {
                    List<string> itemsNotToDisplay = new List<string>();
                    _itemsToDisplay = new List<string>();

                    Invoke(new Action(() =>
                    {
                        listBox1.Items.Clear();
                        FilterTextBox.Text = string.Empty;
                    }));

                    var all = GetAncestors(node, x => x.Parent).ToList();
                    all.Reverse();
                    var full = string.Join("/", all.Select(x => x.Text)) + "/" + node.Text + "/";
                    if (string.IsNullOrEmpty(full))
                    {
                        return;
                    }

                    var dirfiles = pakAsTxt.Where(x => x.StartsWith(full) && !x.Replace(full, "").Contains("/"));
                    var enumerable = dirfiles as string[] ?? dirfiles.ToArray();
                    if (!enumerable.Any())
                    {
                        return;
                    }

                    foreach (var i in enumerable)
                    {
                        string v;
                        if (i.Contains(".uasset") || i.Contains(".uexp") || i.Contains(".ubulk"))
                        {
                            v = i.Substring(0, i.LastIndexOf('.'));
                        }
                        else
                        {
                            v = i.Replace(full, "");
                        }
                        itemsNotToDisplay.Add(v.Replace(full, ""));
                    }
                    _itemsToDisplay = itemsNotToDisplay.Distinct().ToList(); //NO DUPLICATION + NO EXTENSION = EASY TO FIND WHAT WE WANT
                    Invoke(new Action(() =>
                    {
                        for (int i = 0; i < _itemsToDisplay.Count; i++)
                        {
                            listBox1.Items.Add(_itemsToDisplay[i]);
                        }
                        ExtractButton.Enabled = listBox1.SelectedIndex >= 0; //DISABLE EXTRACT BUTTON IF NOTHING IS SELECTED IN LISTBOX
                    }));
                });
                for (int i = 0; i < listBox1.Items.Count; i++)
                {
                    if (listBox1.Items[i].ToString() == SearchFiles.SfPath.Substring(SearchFiles.SfPath.LastIndexOf("/", StringComparison.Ordinal) + 1))
                    {
                        listBox1.SelectedItem = listBox1.Items[i];
                    }
                }
            }

            foreach (TreeNode mynode in node.Nodes)
                if (mynode.Text == path[0])
                {
                    ExpandMyLitleBoys(mynode, path); //recursive call
                    break;
                }
        }
        public void OpenMe()
        {
            if (SearchFiles.IsClosed)
            {
                treeView1.CollapseAll();
                var pathList = SearchFiles.SfPath.Split('/').ToList();
                foreach (TreeNode node in treeView1.Nodes)
                    if (node.Text == pathList[0])
                        ExpandMyLitleBoys(node, pathList);
            }
        }

        //EVENTS
        private async void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            await Task.Run(() => {
                GetFilesAndFill(e);
            });
        }
        private async void FilterTextBox_TextChanged(object sender, EventArgs e)
        {
            await Task.Run(() => {
                FilterItems();
            });
        }
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null && SelectedItemsArray == null)
            {
                ExtractButton.Enabled = true;
            }
        }
        #endregion

        #region EXTRACT BUTTON
        //METHODS
        private string ExtractAsset(string currentPak, string currentItem)
        {
            string toReturn = string.Empty;

            MyExtractor = new PakExtractor(Settings.Default.PAKsPath + "\\" + currentPak, Settings.Default.AESKey);

            string[] results = null;
            if (currentItem.Contains("."))
                results = Array.FindAll(MyExtractor.GetFileList().ToArray(), s => s.Contains("/" + currentItem));
            else
                results = Array.FindAll(MyExtractor.GetFileList().ToArray(), s => s.Contains("/" + currentItem + "."));

            for (int i = 0; i < results.Length; i++)
            {
                int index = Array.IndexOf(MyExtractor.GetFileList().ToArray(), results[i]);

                uint y = (uint)index;
                byte[] b = MyExtractor.GetData(y);

                if (CurrentUsedPakGuid != null && CurrentUsedPakGuid != "0-0-0-0")
                {
                    Directory.CreateDirectory(DefaultOutputPath + "\\Extracted\\" + _paksMountPoint[CurrentUsedPak] + results[i].Substring(0, results[i].LastIndexOf("/")));
                    File.WriteAllBytes(DefaultOutputPath + "\\Extracted\\" + _paksMountPoint[CurrentUsedPak] + results[i], b);
                    toReturn = DefaultOutputPath + "\\Extracted\\" + _paksMountPoint[CurrentUsedPak] + results[i];
                }
                else
                {
                    Directory.CreateDirectory(DefaultOutputPath + "\\Extracted\\" + _paksMountPoint[AllpaksDictionary[currentItem]] + results[i].Substring(0, results[i].LastIndexOf("/")));
                    File.WriteAllBytes(DefaultOutputPath + "\\Extracted\\" + _paksMountPoint[AllpaksDictionary[currentItem]] + results[i], b);
                    toReturn = DefaultOutputPath + "\\Extracted\\" + _paksMountPoint[AllpaksDictionary[currentItem]] + results[i];
                }
            }

            return toReturn.Replace("/", "\\");
        }
        private void ExtractAndSerializeItems(DoWorkEventArgs e, bool updateMode = false)
        {
            if (updateMode == false)
            {
                //REGISTER SELECTED ITEMS
                Invoke(new Action(() =>
                {
                    SelectedItemsArray = new string[listBox1.SelectedItems.Count];
                    for (int i = 0; i < listBox1.SelectedItems.Count; i++) //ADD SELECTED ITEM TO ARRAY
                    {
                        SelectedItemsArray[i] = listBox1.SelectedItems[i].ToString();
                    }
                }));
            }
            else
            {
                //REGISTER SELECTED ITEMS
                Invoke(new Action(() =>
                {
                    SelectedItemsArray = new string[_diffToExtract.Count];
                    for (int i = 0; i < _diffToExtract.Count; i++) //ADD DICT ITEM TO ARRAY
                    {
                        SelectedItemsArray[i] = _diffToExtract.Keys.ElementAt(i);
                    }
                }));
            }

            //DO WORK
            for (int i = 0; i < SelectedItemsArray.Length; i++)
            {
                if (backgroundWorker1.CancellationPending && backgroundWorker1.IsBusy)
                {
                    e.Cancel = true;
                    return;
                }
                if (backgroundWorker2.CancellationPending && backgroundWorker2.IsBusy)
                {
                    e.Cancel = true;
                    return;
                }

                CurrentUsedItem = SelectedItemsArray[i];

                if (CurrentUsedPakGuid != null && CurrentUsedPakGuid != "0-0-0-0")
                    ExtractedFilePath = ExtractAsset(CurrentUsedPak, CurrentUsedItem);
                else
                    ExtractedFilePath = ExtractAsset(AllpaksDictionary[CurrentUsedItem], CurrentUsedItem);

                if (ExtractedFilePath != null)
                {
                    UpdateConsole(CurrentUsedItem + " successfully extracted", Color.FromArgb(255, 66, 244, 66), "Success");
                    if (ExtractedFilePath.Contains(".uasset") || ExtractedFilePath.Contains(".uexp") || ExtractedFilePath.Contains(".ubulk"))
                    {
                        MyAsset = new PakAsset(ExtractedFilePath.Substring(0, ExtractedFilePath.LastIndexOf('.')));
                        JsonParseFile();
                    }
                    if (ExtractedFilePath.Contains(".ufont"))
                        ConvertToOtf(ExtractedFilePath);
                    if (ExtractedFilePath.Contains(".ini"))
                    {
                        Invoke(new Action(() =>
                        {
                            scintilla1.Text = File.ReadAllText(ExtractedFilePath);
                        }));
                    }
                }
                else
                    UpdateConsole("Error while extracting " + CurrentUsedItem, Color.FromArgb(255, 244, 66, 66), "Error");
            }
        }
        private void JsonParseFile()
        {
            if (MyAsset.GetSerialized() != null)
            {
                UpdateConsole(CurrentUsedItem + " successfully serialized", Color.FromArgb(255, 66, 244, 66), "Success");

                Invoke(new Action(() =>
                {
                    try
                    {
                        scintilla1.Text = JToken.Parse(MyAsset.GetSerialized()).ToString();
                    }
                    catch (JsonReaderException)
                    {
                        AppendText(CurrentUsedItem + " ", Color.Red);
                        AppendText(".JSON file can't be displayed", Color.Black, true);
                    }
                }));

                NavigateThroughJson(MyAsset, ExtractedFilePath);
            }
            else
                UpdateConsole("No serialized file found", Color.FromArgb(255, 244, 66, 66), "Error");
        }
        private void NavigateThroughJson(PakAsset theAsset, string questJson = null)
        {
            try
            {
                string parsedJson = JToken.Parse(theAsset.GetSerialized()).ToString();
                var itemId = ItemsIdParser.FromJson(parsedJson);

                UpdateConsole("Parsing " + CurrentUsedItem + "...", Color.FromArgb(255, 244, 132, 66), "Waiting");
                for (int i = 0; i < itemId.Length; i++)
                {
                    if (Settings.Default.createIconForCosmetics && (itemId[i].ExportType.Contains("Athena") && itemId[i].ExportType.Contains("Item") && itemId[i].ExportType.Contains("Definition")))
                        CreateItemIcon(itemId[i], "athIteDef");
                    else if (Settings.Default.createIconForConsumablesWeapons && (itemId[i].ExportType == "FortWeaponRangedItemDefinition" || itemId[i].ExportType == "FortWeaponMeleeItemDefinition"))
                        CreateItemIcon(itemId[i], "consAndWeap");
                    else if (Settings.Default.createIconForTraps && (itemId[i].ExportType == "FortTrapItemDefinition" || itemId[i].ExportType == "FortContextTrapItemDefinition"))
                        CreateItemIcon(itemId[i]);
                    else if (Settings.Default.createIconForVariants && (itemId[i].ExportType == "FortVariantTokenType"))
                        CreateItemIcon(itemId[i], "variant");
                    else if (Settings.Default.createIconForAmmo && (itemId[i].ExportType == "FortAmmoItemDefinition"))
                        CreateItemIcon(itemId[i], "ammo");
                    else if (Settings.Default.createIconForSTWHeroes && (itemId[i].ExportType == "FortHeroType" && (questJson.Contains("ItemDefinition") || questJson.Contains("TestDefsSkydive") || questJson.Contains("GameplayPrototypes")))) //Contains x not to trigger HID from BR
                        CreateItemIcon(itemId[i], "stwHeroes");
                    else if (Settings.Default.createIconForSTWDefenders && (itemId[i].ExportType == "FortDefenderItemDefinition"))
                        CreateItemIcon(itemId[i], "stwDefenders");
                    else if (Settings.Default.createIconForSTWCardPacks && (itemId[i].ExportType == "FortCardPackItemDefinition"))
                        CreateItemIcon(itemId[i]);
                    else if (itemId[i].ExportType == "FortChallengeBundleItemDefinition")
                        CreateChallengesIcon(itemId[i], parsedJson, questJson);
                    else if (itemId[i].ExportType == "Texture2D")
                        ConvertTexture2D();
                    else if (itemId[i].ExportType == "SoundWave")
                        ConvertSoundWave();
                    else
                        UpdateConsole(CurrentUsedItem + " successfully extracted", Color.FromArgb(255, 66, 244, 66), "Success");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private void CreateItemIcon(ItemsIdParser theItem, string SpecialMode = null)
        {
            UpdateConsole(CurrentUsedItem + " is a Cosmetic ID", Color.FromArgb(255, 66, 244, 66), "Success");

            Bitmap bmp = new Bitmap(522, 522);
            Graphics g = Graphics.FromImage(bmp);
            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            if (theItem.Series != null)
                Rarity.GetSeriesRarity(theItem, g);
            else
                // Special ammo force-rarity check
                if (SpecialMode == "ammo")
                Rarity.GetItemRarity(theItem, g, "ammo");
                else
                Rarity.GetItemRarity(theItem, g);

            ItemIconPath = string.Empty;
            if (Settings.Default.loadFeaturedImage == false)
            {
                GetItemIcon(theItem);
            }
            if (Settings.Default.loadFeaturedImage)
            {
                GetItemIcon(theItem, true);
            }

            #region DRAW ICON
            if (File.Exists(ItemIconPath))
            {
                Image itemIcon;
                using (var bmpTemp = new Bitmap(ItemIconPath))
                {
                    itemIcon = new Bitmap(bmpTemp);
                }
                g.DrawImage(Forms.Settings.ResizeImage(itemIcon, 512, 512), new Point(5, 5));
            }
            else
            {
                Image itemIcon = Resources.unknown512;
                g.DrawImage(itemIcon, new Point(0, 0));
            }
            #endregion

            #region WATERMARK
            if (UmWorking == false && (Settings.Default.isWatermark && !string.IsNullOrEmpty(Settings.Default.wFilename)))
            {
                Image watermark = Image.FromFile(Settings.Default.wFilename);
                var opacityImage = SetImageOpacity(watermark, (float)Settings.Default.wOpacity / 100);
                g.DrawImage(Forms.Settings.ResizeImage(opacityImage, Settings.Default.wSize, Settings.Default.wSize), (522 - Settings.Default.wSize) / 2, (522 - Settings.Default.wSize) / 2, Settings.Default.wSize, Settings.Default.wSize);
            }
            if (UmWorking && (Settings.Default.UMWatermark && !string.IsNullOrEmpty(Settings.Default.UMFilename)))
            {
                Image watermark = Image.FromFile(Settings.Default.UMFilename);
                var opacityImage = SetImageOpacity(watermark, (float)Settings.Default.UMOpacity / 100);
                g.DrawImage(Forms.Settings.ResizeImage(opacityImage, Settings.Default.UMSize, Settings.Default.UMSize), (522 - Settings.Default.UMSize) / 2, (522 - Settings.Default.UMSize) / 2, Settings.Default.UMSize, Settings.Default.UMSize);
            }
            #endregion

            Image bg512 = Resources.BG512;
            g.DrawImage(bg512, new Point(5, 383));

            #region DRAW TEXT
            try
            {
                g.DrawString(theItem.DisplayName, new Font(_pfc.Families[0], 35), new SolidBrush(Color.White), new Point(522 / 2, 395), _centeredString);
            }
            catch (NullReferenceException)
            {
                AppendText(CurrentUsedItem + " ", Color.Red);
                AppendText("No ", Color.Black);
                AppendText("DisplayName ", Color.SteelBlue);
                AppendText("found", Color.Black, true);
            } //NAME
            try
            {
                g.DrawString(theItem.Description, new Font("Arial", 10), new SolidBrush(Color.White), new RectangleF(5, 441, 512, 49), _centeredStringLine);
            }
            catch (NullReferenceException)
            {
                AppendText(CurrentUsedItem + " ", Color.Red);
                AppendText("No ", Color.Black);
                AppendText("Description ", Color.SteelBlue);
                AppendText("found", Color.Black, true);
            } //DESCRIPTION
            if (SpecialMode == "athIteDef")
            {
                try
                {
                    g.DrawString(theItem.ShortDescription, new Font(_pfc.Families[0], 13), new SolidBrush(Color.White), new Point(5, 500));
                }
                catch (NullReferenceException)
                {
                    AppendText(CurrentUsedItem + " ", Color.Red);
                    AppendText("No ", Color.Black);
                    AppendText("ShortDescription ", Color.SteelBlue);
                    AppendText("found", Color.Black, true);
                } //TYPE
                try
                {
                    g.DrawString(theItem.GameplayTags.GameplayTagsGameplayTags[Array.FindIndex(theItem.GameplayTags.GameplayTagsGameplayTags, x => x.StartsWith("Cosmetics.Source."))].Substring(17), new Font(_pfc.Families[0], 13), new SolidBrush(Color.White), new Point(522 - 5, 500), _rightString);
                }
                catch (NullReferenceException)
                {
                    AppendText(CurrentUsedItem + " ", Color.Red);
                    AppendText("No ", Color.Black);
                    AppendText("GameplayTags ", Color.SteelBlue);
                    AppendText("found", Color.Black, true);
                }
                catch (IndexOutOfRangeException)
                {
                    AppendText(CurrentUsedItem + " ", Color.Red);
                    AppendText("No ", Color.Black);
                    AppendText("GameplayTags ", Color.SteelBlue);
                    AppendText("as ", Color.Black);
                    AppendText("Cosmetics.Source ", Color.SteelBlue);
                    AppendText("found", Color.Black, true);
                } //COSMETIC SOURCE
            }
            if (SpecialMode == "consAndWeap")
            {
                try
                {
                    g.DrawString(theItem.GameplayTags.GameplayTagsGameplayTags[Array.FindIndex(theItem.GameplayTags.GameplayTagsGameplayTags, x => x.StartsWith("Athena.ItemAction."))].Substring(18), new Font(_pfc.Families[0], 13), new SolidBrush(Color.White), new Point(522 - 5, 500), _rightString);
                }
                catch (NullReferenceException)
                {
                    AppendText(CurrentUsedItem + " ", Color.Red);
                    AppendText("No ", Color.Black);
                    AppendText("GameplayTags ", Color.SteelBlue);
                    AppendText("found", Color.Black, true);
                }
                catch (IndexOutOfRangeException)
                {
                    try
                    {
                        g.DrawString(theItem.GameplayTags.GameplayTagsGameplayTags[Array.FindIndex(theItem.GameplayTags.GameplayTagsGameplayTags, x => x.StartsWith("Weapon."))].Substring(7), new Font(_pfc.Families[0], 13), new SolidBrush(Color.White), new Point(522 - 5, 500), _rightString);
                    }
                    catch (NullReferenceException)
                    {
                        AppendText(CurrentUsedItem + " ", Color.Red);
                        AppendText("No ", Color.Black);
                        AppendText("GameplayTags ", Color.SteelBlue);
                        AppendText("found", Color.Black, true);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        AppendText(CurrentUsedItem + " ", Color.Red);
                        AppendText("No ", Color.Black);
                        AppendText("GameplayTags ", Color.SteelBlue);
                        AppendText("as ", Color.Black);
                        AppendText("Athena.ItemAction ", Color.SteelBlue);
                        AppendText("or ", Color.Black);
                        AppendText("Weapon ", Color.SteelBlue);
                        AppendText("found", Color.Black, true);
                    }
                } //ACTION
                if (theItem.AmmoData != null && theItem.AmmoData.AssetPathName.Contains("Ammo")) //TO AVOID TRIGGERING CONSUMABLES, NAME SHOULD CONTAIN "AMMO"
                {
                    getAmmoData(theItem.AmmoData.AssetPathName, g);
                }
            }
            if (SpecialMode == "variant")
            {
                try
                {
                    g.DrawString(theItem.ShortDescription, new Font(_pfc.Families[0], 13), new SolidBrush(Color.White), new Point(5, 500));
                }
                catch (NullReferenceException)
                {
                    AppendText(CurrentUsedItem + " ", Color.Red);
                    AppendText("No ", Color.Black);
                    AppendText("ShortDescription ", Color.SteelBlue);
                    AppendText("found", Color.Black, true);
                } //TYPE
                try
                {
                    g.DrawString(theItem.CosmeticItem, new Font(_pfc.Families[0], 13), new SolidBrush(Color.White), new Point(522 - 5, 500), _rightString);
                }
                catch (NullReferenceException)
                {
                    AppendText(CurrentUsedItem + " ", Color.Red);
                    AppendText("No ", Color.Black);
                    AppendText("Cosmetic Item ", Color.SteelBlue);
                    AppendText("found", Color.Black, true);
                } //COSMETIC ITEM
            }
            try
            {
                if (theItem.GameplayTags.GameplayTagsGameplayTags[Array.FindIndex(theItem.GameplayTags.GameplayTagsGameplayTags, x => x.StartsWith("Cosmetics.UserFacingFlags."))].Contains("Animated"))
                {
                    Image animatedLogo = Resources.T_Icon_Animated_64;
                    g.DrawImage(Forms.Settings.ResizeImage(animatedLogo, 32, 32), new Point(6, -2));
                }
                else if (theItem.GameplayTags.GameplayTagsGameplayTags[Array.FindIndex(theItem.GameplayTags.GameplayTagsGameplayTags, x => x.StartsWith("Cosmetics.UserFacingFlags."))].Contains("HasUpgradeQuests") && theItem.ExportType != "AthenaPetCarrierItemDefinition")
                {
                    Image questLogo = Resources.T_Icon_Quests_64;
                    g.DrawImage(Forms.Settings.ResizeImage(questLogo, 32, 32), new Point(6, 6));
                }
                else if (theItem.GameplayTags.GameplayTagsGameplayTags[Array.FindIndex(theItem.GameplayTags.GameplayTagsGameplayTags, x => x.StartsWith("Cosmetics.UserFacingFlags."))].Contains("HasUpgradeQuests") && theItem.ExportType == "AthenaPetCarrierItemDefinition")
                {
                    Image petLogo = Resources.T_Icon_Pets_64;
                    g.DrawImage(Forms.Settings.ResizeImage(petLogo, 32, 32), new Point(6, 6));
                }
                else if (theItem.GameplayTags.GameplayTagsGameplayTags[Array.FindIndex(theItem.GameplayTags.GameplayTagsGameplayTags, x => x.StartsWith("Cosmetics.UserFacingFlags."))].Contains("HasVariants"))
                {
                    Image variantsLogo = Resources.T_Icon_Variant_64;
                    g.DrawImage(Forms.Settings.ResizeImage(variantsLogo, 32, 32), new Point(6, 6));
                }
                else if (theItem.GameplayTags.GameplayTagsGameplayTags[Array.FindIndex(theItem.GameplayTags.GameplayTagsGameplayTags, x => x.StartsWith("Cosmetics.UserFacingFlags."))].Contains("Reactive"))
                {
                    Image reactiveLogo = Resources.T_Icon_Adaptive_64;
                    g.DrawImage(Forms.Settings.ResizeImage(reactiveLogo, 32, 32), new Point(7, 7));
                }
                else if (theItem.GameplayTags.GameplayTagsGameplayTags[Array.FindIndex(theItem.GameplayTags.GameplayTagsGameplayTags, x => x.StartsWith("Cosmetics.UserFacingFlags."))].Contains("Traversal"))
                {
                    Image traversalLogo = Resources.T_Icon_Traversal_64;
                    g.DrawImage(Forms.Settings.ResizeImage(traversalLogo, 32, 32), new Point(6, 3));
                }
            }
            catch (Exception)
            {
            } //COSMETIC USER FACING FLAGS
            if (SpecialMode == "stwHeroes")
            {
                try
                {
                    g.DrawString(theItem.AttributeInitKey.AttributeInitCategory, new Font(_pfc.Families[0], 13), new SolidBrush(Color.White), new Point(522 - 5, 500), _rightString);
                }
                catch (NullReferenceException)
                {
                    AppendText(CurrentUsedItem + " ", Color.Red);
                    AppendText("No ", Color.Black);
                    AppendText("AttributeInitCategory ", Color.SteelBlue);
                    AppendText("found", Color.Black, true);
                } //CHARACTER TYPE
            }
            if (SpecialMode == "stwDefenders")
            {
                try
                {
                    g.DrawString(theItem.GameplayTags.GameplayTagsGameplayTags[Array.FindIndex(theItem.GameplayTags.GameplayTagsGameplayTags, x => x.StartsWith("NPC.CharacterType.Survivor.Defender."))].Substring(36), new Font(_pfc.Families[0], 13), new SolidBrush(Color.White), new Point(522 - 5, 500), _rightString);
                }
                catch (NullReferenceException)
                {
                    AppendText(CurrentUsedItem + " ", Color.Red);
                    AppendText("No ", Color.Black);
                    AppendText("GameplayTags ", Color.SteelBlue);
                    AppendText("found", Color.Black, true);
                }
                catch (IndexOutOfRangeException)
                {
                    AppendText(CurrentUsedItem + " ", Color.Red);
                    AppendText("No ", Color.Black);
                    AppendText("GameplayTags ", Color.SteelBlue);
                    AppendText("as ", Color.Black);
                    AppendText("NPC.CharacterType.Survivor.Defender ", Color.SteelBlue);
                    AppendText("found", Color.Black, true);
                } //CHARACTER TYPE
            }
            #endregion

            pictureBox1.Image = bmp;
            UpdateConsole(theItem.DisplayName, Color.FromArgb(255, 66, 244, 66), "Success");
            if (autoSaveImagesToolStripMenuItem.Checked || updateModeToolStripMenuItem.Checked)
            {
                Invoke(new Action(() =>
                {
                    pictureBox1.Image.Save(DefaultOutputPath + "\\Icons\\" + CurrentUsedItem + ".png", ImageFormat.Png);
                }));
                AppendText(CurrentUsedItem, Color.DarkRed);
                AppendText(" successfully saved", Color.Black, true);
            }
        }
        private void GetItemIcon(ItemsIdParser theItem, bool featured = false)
        {
            if (featured == false)
            {
                WasFeatured = false;
                SearchAthIteDefIcon(theItem);
            }
            if (featured)
            {
                if (theItem.DisplayAssetPath != null && theItem.DisplayAssetPath.AssetPathName.Contains("/Game/Catalog/DisplayAssets/") && theItem.ExportType != "AthenaItemWrapDefinition")
                {
                    string catalogName = theItem.DisplayAssetPath.AssetPathName;
                    SearchFeaturedCharacterIcon(theItem, catalogName);
                }
                else if (theItem.DisplayAssetPath == null && theItem.ExportType != "AthenaItemWrapDefinition")
                {
                    SearchFeaturedCharacterIcon(theItem, "DA_Featured_" + CurrentUsedItem, true);
                }
                else
                {
                    GetItemIcon(theItem);
                }
            }
        }
        private void SearchAthIteDefIcon(ItemsIdParser theItem)
        {
            if (theItem.HeroDefinition != null)
            {
                string heroFilePath = string.Empty;
                if (CurrentUsedPakGuid != null && CurrentUsedPakGuid != "0-0-0-0")
                    heroFilePath = ExtractAsset(CurrentUsedPak, theItem.HeroDefinition);
                else
                    heroFilePath = ExtractAsset(AllpaksDictionary[theItem.HeroDefinition], theItem.HeroDefinition);

                if (heroFilePath != null)
                {
                    UpdateConsole(theItem.HeroDefinition + " successfully extracted", Color.FromArgb(255, 66, 244, 66), "Success");
                    if (heroFilePath.Contains(".uasset") || heroFilePath.Contains(".uexp") || heroFilePath.Contains(".ubulk"))
                    {
                        MyAsset = new PakAsset(heroFilePath.Substring(0, heroFilePath.LastIndexOf('.')));
                        try
                        {
                            if (MyAsset.GetSerialized() != null)
                            {
                                UpdateConsole(theItem.HeroDefinition + " successfully serialized", Color.FromArgb(255, 66, 244, 66), "Success");

                                string parsedJson = JToken.Parse(MyAsset.GetSerialized()).ToString();
                                var itemId = ItemsIdParser.FromJson(parsedJson);
                                UpdateConsole("Parsing " + theItem.HeroDefinition + "...", Color.FromArgb(255, 244, 132, 66), "Waiting");
                                for (int i = 0; i < itemId.Length; i++)
                                {
                                    if (itemId[i].LargePreviewImage != null)
                                    {
                                        string textureFile = Path.GetFileName(itemId[i].LargePreviewImage.AssetPathName)
                                            ?.Substring(0,
                                                Path.GetFileName(itemId[i].LargePreviewImage.AssetPathName)
                                                    .LastIndexOf('.'));


                                        string textureFilePath = string.Empty;
                                        if (CurrentUsedPakGuid != null && CurrentUsedPakGuid != "0-0-0-0")
                                            textureFilePath = ExtractAsset(CurrentUsedPak, textureFile);
                                        else
                                            textureFilePath = ExtractAsset(AllpaksDictionary[textureFile ?? throw new InvalidOperationException()], textureFile);

                                        if (textureFilePath != null)
                                        {
                                            MyAsset = new PakAsset(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")));
                                            MyAsset.SaveTexture(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")) + ".png");
                                            ItemIconPath = textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")) + ".png";
                                            UpdateConsole(textureFile + " successfully converted to .PNG", Color.FromArgb(255, 66, 244, 66), "Success");
                                        }
                                        else
                                            UpdateConsole("Error while extracting " + textureFile, Color.FromArgb(255, 244, 66, 66), "Error");
                                    }
                                }
                            }
                            else
                                UpdateConsole("No serialized file found", Color.FromArgb(255, 244, 66, 66), "Error");
                        }
                        catch (JsonSerializationException)
                        {
                            AppendText(CurrentUsedItem + " ", Color.Red);
                            AppendText(".JSON file can't be displayed", Color.Black, true);
                        }
                    }
                }
                else
                    UpdateConsole("Error while extracting " + theItem.HeroDefinition, Color.FromArgb(255, 244, 66, 66), "Error");
            }
            else if (theItem.WeaponDefinition != null)
            {
                //MANUAL FIX
                if (theItem.WeaponDefinition == "WID_Harvest_Pickaxe_NutCracker")
                    theItem.WeaponDefinition = "WID_Harvest_Pickaxe_Nutcracker";
                if (theItem.WeaponDefinition == "WID_Harvest_Pickaxe_Wukong")
                    theItem.WeaponDefinition = "WID_Harvest_Pickaxe_WuKong";

                string weaponFilePath = string.Empty;
                if (CurrentUsedPakGuid != null && CurrentUsedPakGuid != "0-0-0-0")
                    weaponFilePath = ExtractAsset(CurrentUsedPak, theItem.WeaponDefinition);
                else
                    weaponFilePath = ExtractAsset(AllpaksDictionary[theItem.WeaponDefinition], theItem.WeaponDefinition);

                if (weaponFilePath != null)
                {
                    UpdateConsole(theItem.WeaponDefinition + " successfully extracted", Color.FromArgb(255, 66, 244, 66), "Success");
                    if (weaponFilePath.Contains(".uasset") || weaponFilePath.Contains(".uexp") || weaponFilePath.Contains(".ubulk"))
                    {
                        MyAsset = new PakAsset(weaponFilePath.Substring(0, weaponFilePath.LastIndexOf('.')));
                        try
                        {
                            if (MyAsset.GetSerialized() != null)
                            {
                                UpdateConsole(theItem.WeaponDefinition + " successfully serialized", Color.FromArgb(255, 66, 244, 66), "Success");

                                string parsedJson = JToken.Parse(MyAsset.GetSerialized()).ToString();
                                var itemId = ItemsIdParser.FromJson(parsedJson);
                                UpdateConsole("Parsing " + theItem.WeaponDefinition + "...", Color.FromArgb(255, 244, 132, 66), "Waiting");
                                for (int i = 0; i < itemId.Length; i++)
                                {
                                    if (itemId[i].LargePreviewImage != null)
                                    {
                                        string textureFile = Path.GetFileName(itemId[i].LargePreviewImage.AssetPathName)
                                            ?.Substring(0,
                                                Path.GetFileName(itemId[i].LargePreviewImage.AssetPathName)
                                                    .LastIndexOf('.'));

                                        string textureFilePath = string.Empty;
                                        if (CurrentUsedPakGuid != null && CurrentUsedPakGuid != "0-0-0-0")
                                            textureFilePath = ExtractAsset(CurrentUsedPak, textureFile);
                                        else
                                            textureFilePath = ExtractAsset(AllpaksDictionary[textureFile ?? throw new InvalidOperationException()], textureFile);

                                        if (textureFilePath != null)
                                        {
                                            MyAsset = new PakAsset(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")));
                                            MyAsset.SaveTexture(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")) + ".png");
                                            ItemIconPath = textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")) + ".png";
                                            UpdateConsole(textureFile + " successfully converted to .PNG", Color.FromArgb(255, 66, 244, 66), "Success");
                                        }
                                        else
                                            UpdateConsole("Error while extracting " + textureFile, Color.FromArgb(255, 244, 66, 66), "Error");
                                    }
                                }
                            }
                            else
                                UpdateConsole("No serialized file found", Color.FromArgb(255, 244, 66, 66), "Error");
                        }
                        catch (JsonSerializationException)
                        {
                            AppendText(CurrentUsedItem + " ", Color.Red);
                            AppendText(".JSON file can't be displayed", Color.Black, true);
                        }
                    }
                }
                else
                    UpdateConsole("Error while extracting " + theItem.WeaponDefinition, Color.FromArgb(255, 244, 66, 66), "Error");
            }
            else
                SearchLargeSmallIcon(theItem);
        }
        private void SearchLargeSmallIcon(ItemsIdParser theItem)
        {
            if (theItem.LargePreviewImage != null)
            {
                string textureFile = Path.GetFileName(theItem.LargePreviewImage.AssetPathName)?.Substring(0,
                    Path.GetFileName(theItem.LargePreviewImage.AssetPathName).LastIndexOf('.'));

                string textureFilePath = string.Empty;
                if (CurrentUsedPakGuid != null && CurrentUsedPakGuid != "0-0-0-0")
                    textureFilePath = ExtractAsset(CurrentUsedPak, textureFile);
                else
                    textureFilePath = ExtractAsset(AllpaksDictionary[textureFile ?? throw new InvalidOperationException()], textureFile);

                if (textureFilePath != null)
                {
                    MyAsset = new PakAsset(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")));
                    MyAsset.SaveTexture(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")) + ".png");
                    ItemIconPath = textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")) + ".png";
                    UpdateConsole(textureFile + " successfully converted to .PNG", Color.FromArgb(255, 66, 244, 66), "Success");
                }
                else
                    UpdateConsole("Error while extracting " + textureFile, Color.FromArgb(255, 244, 66, 66), "Error");
            }
            else if (theItem.SmallPreviewImage != null)
            {
                string textureFile = Path.GetFileName(theItem.SmallPreviewImage.AssetPathName)?.Substring(0,
                    Path.GetFileName(theItem.SmallPreviewImage.AssetPathName).LastIndexOf('.'));

                string textureFilePath = string.Empty;
                if (CurrentUsedPakGuid != null && CurrentUsedPakGuid != "0-0-0-0")
                    textureFilePath = ExtractAsset(CurrentUsedPak, textureFile);
                else
                    textureFilePath = ExtractAsset(AllpaksDictionary[textureFile ?? throw new InvalidOperationException()], textureFile);

                if (textureFilePath != null)
                {
                    MyAsset = new PakAsset(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")));
                    MyAsset.SaveTexture(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")) + ".png");
                    ItemIconPath = textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")) + ".png";
                    UpdateConsole(textureFile + " successfully converted to .PNG", Color.FromArgb(255, 66, 244, 66), "Success");
                }
                else
                    UpdateConsole("Error while extracting " + textureFile, Color.FromArgb(255, 244, 66, 66), "Error");
            }
        }
        private void SearchFeaturedCharacterIcon(ItemsIdParser theItem, string catName, bool manualSearch = false)
        {
            if (manualSearch == false)
            {
                CurrentUsedItem = catName.Substring(catName.LastIndexOf('.') + 1);

                if (CurrentUsedItem == "DA_Featured_Glider_ID_141_AshtonBoardwalk")
                    GetItemIcon(theItem);
                else
                {
                    string catalogFilePath = string.Empty;
                    if (CurrentUsedPakGuid != null && CurrentUsedPakGuid != "0-0-0-0")
                        catalogFilePath = ExtractAsset(CurrentUsedPak, catName.Substring(catName.LastIndexOf('.') + 1));
                    else
                        catalogFilePath = ExtractAsset(AllpaksDictionary[catName.Substring(catName.LastIndexOf('.') + 1)], catName.Substring(catName.LastIndexOf('.') + 1));

                    if (catalogFilePath != null)
                    {
                        WasFeatured = true;
                        UpdateConsole(catName.Substring(catName.LastIndexOf('.') + 1) + " successfully extracted", Color.FromArgb(255, 66, 244, 66), "Success");
                        if (catalogFilePath.Contains(".uasset") || catalogFilePath.Contains(".uexp") || catalogFilePath.Contains(".ubulk"))
                        {
                            MyAsset = new PakAsset(catalogFilePath.Substring(0, catalogFilePath.LastIndexOf('.')));
                            try
                            {
                                if (MyAsset.GetSerialized() != null)
                                {
                                    UpdateConsole(catName.Substring(catName.LastIndexOf('.') + 1) + " successfully serialized", Color.FromArgb(255, 66, 244, 66), "Success");
                                    string parsedJson = JToken.Parse(MyAsset.GetSerialized()).ToString();
                                    var featuredId = FeaturedParser.FromJson(parsedJson);
                                    UpdateConsole("Parsing " + catName.Substring(catName.LastIndexOf('.') + 1) + "...", Color.FromArgb(255, 244, 132, 66), "Waiting");
                                    for (int i = 0; i < featuredId.Length; i++)
                                    {
                                        //Thanks EPIC
                                        if (CurrentUsedItem == "DA_Featured_CID_319_Athena_Commando_F_Nautilus")
                                        {
                                            if (featuredId[i].TileImage != null)
                                            {
                                                string textureFile = featuredId[i].TileImage.ResourceObject;

                                                string textureFilePath = string.Empty;
                                                if (CurrentUsedPakGuid != null && CurrentUsedPakGuid != "0-0-0-0")
                                                    textureFilePath = ExtractAsset(CurrentUsedPak, textureFile);
                                                else
                                                    textureFilePath = ExtractAsset(AllpaksDictionary[textureFile], textureFile);

                                                if (textureFilePath != null)
                                                {
                                                    MyAsset = new PakAsset(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")));
                                                    MyAsset.SaveTexture(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")) + ".png");
                                                    ItemIconPath = textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")) + ".png";
                                                    UpdateConsole(textureFile + " successfully converted to .PNG", Color.FromArgb(255, 66, 244, 66), "Success");
                                                }
                                                else
                                                    UpdateConsole("Error while extracting " + textureFile, Color.FromArgb(255, 244, 66, 66), "Error");
                                            }
                                        }
                                        else
                                        {
                                            if (featuredId[i].DetailsImage != null)
                                            {
                                                string textureFile = featuredId[i].DetailsImage.ResourceObject;

                                                string textureFilePath = string.Empty;
                                                if (CurrentUsedPakGuid != null && CurrentUsedPakGuid != "0-0-0-0")
                                                    textureFilePath = ExtractAsset(CurrentUsedPak, textureFile);
                                                else
                                                    textureFilePath = ExtractAsset(AllpaksDictionary[textureFile], textureFile);

                                                if (textureFilePath != null && textureFilePath.Contains("MI_UI_FeaturedRenderSwitch_"))
                                                {
                                                    ItemIconPath = GetRenderSwitchMaterialTexture(textureFile, textureFilePath);
                                                }
                                                else if (textureFilePath != null && !textureFilePath.Contains("MI_UI_FeaturedRenderSwitch_"))
                                                {
                                                    MyAsset = new PakAsset(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")));
                                                    MyAsset.SaveTexture(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")) + ".png");
                                                    ItemIconPath = textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")) + ".png";
                                                    UpdateConsole(textureFile + " successfully converted to .PNG", Color.FromArgb(255, 66, 244, 66), "Success");
                                                }
                                                else
                                                    UpdateConsole("Error while extracting " + textureFile, Color.FromArgb(255, 244, 66, 66), "Error");
                                            }
                                        }
                                    }
                                }
                            }
                            catch (JsonSerializationException)
                            {
                                AppendText(CurrentUsedItem + " ", Color.Red);
                                AppendText(".JSON file can't be displayed", Color.Black, true);
                            }
                        }
                    }
                    else
                        UpdateConsole("Error while extracting " + catName.Substring(catName.LastIndexOf('.') + 1), Color.FromArgb(255, 244, 66, 66), "Error");
                }
            }
            if (manualSearch)
            {
                //Thanks EPIC
                if (catName == "DA_Featured_Glider_ID_015_Brite" || 
                    catName == "DA_Featured_Glider_ID_016_Tactical" || 
                    catName == "DA_Featured_Glider_ID_017_Assassin" || 
                    catName == "DA_Featured_Pickaxe_ID_027_Scavenger" || 
                    catName == "DA_Featured_Pickaxe_ID_028_Space" || 
                    catName == "DA_Featured_Pickaxe_ID_029_Assassin")
                    GetItemIcon(theItem);
                else if (AllpaksDictionary.ContainsKey(catName))
                {
                    CurrentUsedItem = catName;

                    string catalogFilePath = string.Empty;
                    if (CurrentUsedPakGuid != null && CurrentUsedPakGuid != "0-0-0-0")
                        catalogFilePath = ExtractAsset(CurrentUsedPak, catName);
                    else
                        catalogFilePath = ExtractAsset(AllpaksDictionary[catName], catName);

                    if (catalogFilePath != null)
                    {
                        WasFeatured = true;
                        UpdateConsole(catName + " successfully extracted", Color.FromArgb(255, 66, 244, 66), "Success");
                        if (catalogFilePath.Contains(".uasset") || catalogFilePath.Contains(".uexp") || catalogFilePath.Contains(".ubulk"))
                        {
                            MyAsset = new PakAsset(catalogFilePath.Substring(0, catalogFilePath.LastIndexOf('.')));
                            try
                            {
                                if (MyAsset.GetSerialized() != null)
                                {
                                    UpdateConsole(catName + " successfully serialized", Color.FromArgb(255, 66, 244, 66), "Success");
                                    string parsedJson = JToken.Parse(MyAsset.GetSerialized()).ToString();
                                    var featuredId = FeaturedParser.FromJson(parsedJson);
                                    UpdateConsole("Parsing " + catName + "...", Color.FromArgb(255, 244, 132, 66), "Waiting");
                                    for (int i = 0; i < featuredId.Length; i++)
                                    {
                                        //Thanks EPIC
                                        if (CurrentUsedItem == "DA_Featured_Glider_ID_070_DarkViking")
                                        {
                                            if (featuredId[i].TileImage != null)
                                            {
                                                string textureFile = featuredId[i].TileImage.ResourceObject;

                                                string textureFilePath = string.Empty;
                                                if (CurrentUsedPakGuid != null && CurrentUsedPakGuid != "0-0-0-0")
                                                    textureFilePath = ExtractAsset(CurrentUsedPak, textureFile);
                                                else
                                                    textureFilePath = ExtractAsset(AllpaksDictionary[textureFile], textureFile);

                                                if (textureFilePath != null)
                                                {
                                                    MyAsset = new PakAsset(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")));
                                                    MyAsset.SaveTexture(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")) + ".png");
                                                    ItemIconPath = textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")) + ".png";
                                                    UpdateConsole(textureFile + " successfully converted to .PNG", Color.FromArgb(255, 66, 244, 66), "Success");
                                                }
                                                else
                                                    UpdateConsole("Error while extracting " + textureFile, Color.FromArgb(255, 244, 66, 66), "Error");
                                            }
                                        }
                                        else
                                        {
                                            if (featuredId[i].DetailsImage != null)
                                            {
                                                string textureFile = featuredId[i].DetailsImage.ResourceObject;

                                                string textureFilePath = string.Empty;
                                                if (CurrentUsedPakGuid != null && CurrentUsedPakGuid != "0-0-0-0")
                                                    textureFilePath = ExtractAsset(CurrentUsedPak, textureFile);
                                                else
                                                    textureFilePath = ExtractAsset(AllpaksDictionary[textureFile], textureFile);

                                                if (textureFilePath != null && textureFilePath.Contains("MI_UI_FeaturedRenderSwitch_"))
                                                {
                                                    ItemIconPath = GetRenderSwitchMaterialTexture(textureFile, textureFilePath);
                                                }
                                                else if (textureFilePath != null && !textureFilePath.Contains("MI_UI_FeaturedRenderSwitch_"))
                                                {
                                                    MyAsset = new PakAsset(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")));
                                                    MyAsset.SaveTexture(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")) + ".png");
                                                    ItemIconPath = textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")) + ".png";
                                                    UpdateConsole(textureFile + " successfully converted to .PNG", Color.FromArgb(255, 66, 244, 66), "Success");
                                                }
                                                else
                                                    UpdateConsole("Error while extracting " + textureFile, Color.FromArgb(255, 244, 66, 66), "Error");
                                            }
                                        }
                                    }
                                }
                            }
                            catch (JsonSerializationException)
                            {
                                AppendText(CurrentUsedItem + " ", Color.Red);
                                AppendText(".JSON file can't be displayed", Color.Black, true);
                            }
                        }
                    }
                }
                else
                    GetItemIcon(theItem);
            }
        }
        private string GetRenderSwitchMaterialTexture(string theTexture, string theTexturePath)
        {
            string toReturn = string.Empty;

            UpdateConsole(theTexture + " successfully extracted", Color.FromArgb(255, 66, 244, 66), "Success");
            if (theTexturePath.Contains(".uasset") || theTexturePath.Contains(".uexp") || theTexturePath.Contains(".ubulk"))
            {
                MyAsset = new PakAsset(theTexturePath.Substring(0, theTexturePath.LastIndexOf('.')));
                try
                {
                    if (MyAsset.GetSerialized() != null)
                    {
                        UpdateConsole(theTexture + " successfully serialized", Color.FromArgb(255, 66, 244, 66), "Success");

                        string parsedRsmJson = JToken.Parse(MyAsset.GetSerialized()).ToString();
                        var rsmid = RenderSwitchMaterial.FromJson(parsedRsmJson);
                        UpdateConsole("Parsing " + theTexture + "...", Color.FromArgb(255, 244, 132, 66), "Waiting");
                        for (int i = 0; i < rsmid.Length; i++)
                        {
                            if (rsmid[i].TextureParameterValues.FirstOrDefault()?.ParameterValue != null)
                            {
                                string textureFile = rsmid[i].TextureParameterValues.FirstOrDefault()?.ParameterValue;

                                string textureFilePath = string.Empty;
                                if (CurrentUsedPakGuid != null && CurrentUsedPakGuid != "0-0-0-0")
                                    textureFilePath = ExtractAsset(CurrentUsedPak, textureFile);
                                else
                                    textureFilePath = ExtractAsset(AllpaksDictionary[textureFile ?? throw new InvalidOperationException()], textureFile);

                                if (textureFilePath != null)
                                {
                                    MyAsset = new PakAsset(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")));
                                    MyAsset.SaveTexture(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")) + ".png");
                                    toReturn = textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")) + ".png";
                                    UpdateConsole(textureFile + " successfully converted to .PNG", Color.FromArgb(255, 66, 244, 66), "Success");
                                }
                                else
                                    UpdateConsole("Error while extracting " + textureFile, Color.FromArgb(255, 244, 66, 66), "Error");
                            }
                        }
                    }
                }
                catch (JsonSerializationException)
                {
                    AppendText(CurrentUsedItem + " ", Color.Red);
                    AppendText(".JSON file can't be displayed", Color.Black, true);
                }
            }
            return toReturn;
        }
        private void getAmmoData(string ammoFile, Graphics toDrawOn)
        {
            string ammoFilePath = string.Empty;
            if (CurrentUsedPakGuid != null && CurrentUsedPakGuid != "0-0-0-0")
                ammoFilePath = ExtractAsset(CurrentUsedPak, ammoFile.Substring(ammoFile.LastIndexOf('.') + 1));
            else
                ammoFilePath = ExtractAsset(AllpaksDictionary[ammoFile.Substring(ammoFile.LastIndexOf('.') + 1)], ammoFile.Substring(ammoFile.LastIndexOf('.') + 1));

            if (ammoFilePath != null)
            {
                UpdateConsole(ammoFile.Substring(ammoFile.LastIndexOf('.') + 1) + " successfully extracted", Color.FromArgb(255, 66, 244, 66), "Success");
                if (ammoFilePath.Contains(".uasset") || ammoFilePath.Contains(".uexp") || ammoFilePath.Contains(".ubulk"))
                {
                    MyAsset = new PakAsset(ammoFilePath.Substring(0, ammoFilePath.LastIndexOf('.')));
                    try
                    {
                        if (MyAsset.GetSerialized() != null)
                        {
                            UpdateConsole(ammoFile.Substring(ammoFile.LastIndexOf('.') + 1) + " successfully serialized", Color.FromArgb(255, 66, 244, 66), "Success");
                            string parsedJson = JToken.Parse(MyAsset.GetSerialized()).ToString();
                            var ammoId = ItemsIdParser.FromJson(parsedJson);
                            UpdateConsole("Parsing " + ammoFile.Substring(ammoFile.LastIndexOf('.') + 1) + "...", Color.FromArgb(255, 244, 132, 66), "Waiting");
                            for (int i = 0; i < ammoId.Length; i++)
                            {
                                SearchLargeSmallIcon(ammoId[i]);

                                if (File.Exists(ItemIconPath))
                                {
                                    Image itemIcon;
                                    using (var bmpTemp = new Bitmap(ItemIconPath))
                                    {
                                        itemIcon = new Bitmap(bmpTemp);
                                    }
                                    toDrawOn.DrawImage(Forms.Settings.ResizeImage(itemIcon, 64, 64), new Point(6, 6));
                                }
                                else
                                {
                                    Image itemIcon = Resources.unknown512;
                                    toDrawOn.DrawImage(Forms.Settings.ResizeImage(itemIcon, 64, 64), new Point(6, 6));
                                }
                            }
                        }
                    }
                    catch (JsonSerializationException)
                    {
                        AppendText(CurrentUsedItem + " ", Color.Red);
                        AppendText(".JSON file can't be displayed", Color.Black, true);
                    }
                }
            }
            else
                UpdateConsole("Error while extracting " + ammoFile.Substring(ammoFile.LastIndexOf('.') + 1), Color.FromArgb(255, 244, 66, 66), "Error");
        }

        //TODO: SIMPLIFY
        private void CreateChallengesIcon(ItemsIdParser theItem, string theParsedJson, string questJson = null)
        {
            if (theItem.ExportType == "FortChallengeBundleItemDefinition")
            {
                if (CurrentUsedItem == "QuestBundle_S9_Fortbyte")
                    CreateFortByteChallengesIcon(theItem, theParsedJson, questJson);
                else
                {
                    Bitmap bmp = new Bitmap(Resources.Quest);
                    Graphics g = Graphics.FromImage(bmp);
                    g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    int iamY = 275;
                    int justSkip = 0;
                    YAfterLoop = 0;
                    bool v2 = false;

                    var bundleParser = ChallengeBundleIdParser.FromJson(theParsedJson);
                    for (int i = 0; i < bundleParser.Length; i++)
                    {
                        SelectedChallengesArray = new string[bundleParser[i].QuestInfos.Length];
                        for (int i2 = 0; i2 < bundleParser[i].QuestInfos.Length; i2++)
                        {
                            string cName = Path.GetFileName(bundleParser[i].QuestInfos[i2].QuestDefinition.AssetPathName);
                            SelectedChallengesArray[i2] = cName.Substring(0, cName.LastIndexOf('.'));
                        }

                        try
                        {
                            if (Settings.Default.createIconForChallenges && bundleParser[i].DisplayStyle.DisplayImage != null)
                            {
                                drawV2(bundleParser[i], theItem, questJson, g, bmp);
                                v2 = true;
                            }
                        }
                        catch (Exception)
                        {
                        }

                        for (int i2 = 0; i2 < SelectedChallengesArray.Length; i2++)
                        {
                            try
                            {
                                string challengeFilePath = string.Empty;
                                if (CurrentUsedPakGuid != null && CurrentUsedPakGuid != "0-0-0-0")
                                    challengeFilePath = ExtractAsset(CurrentUsedPak, SelectedChallengesArray[i2]);
                                else
                                    challengeFilePath = ExtractAsset(AllpaksDictionary[SelectedChallengesArray[i2]], SelectedChallengesArray[i2]);

                                if (challengeFilePath != null)
                                {
                                    UpdateConsole(SelectedChallengesArray[i2] + " successfully extracted", Color.FromArgb(255, 66, 244, 66), "Success");
                                    if (challengeFilePath.Contains(".uasset") || challengeFilePath.Contains(".uexp") || challengeFilePath.Contains(".ubulk"))
                                    {
                                        MyAsset = new PakAsset(challengeFilePath.Substring(0, challengeFilePath.LastIndexOf('.')));
                                        try
                                        {
                                            if (MyAsset.GetSerialized() != null)
                                            {
                                                UpdateConsole(SelectedChallengesArray[i2] + " successfully serialized", Color.FromArgb(255, 66, 244, 66), "Success");

                                                string parsedJson = JToken.Parse(MyAsset.GetSerialized()).ToString();
                                                var questParser = QuestParser.FromJson(parsedJson);
                                                UpdateConsole("Parsing " + SelectedChallengesArray[i2] + "...", Color.FromArgb(255, 244, 132, 66), "Waiting");
                                                for (int ii = 0; ii < questParser.Length; ii++)
                                                {
                                                    string oldQuest = string.Empty;
                                                    string oldCount = string.Empty;
                                                    for (int ii2 = 0; ii2 < questParser[ii].Objectives.Length; ii2++)
                                                    {
                                                        string newQuest = questParser[ii].Objectives[ii2].Description;
                                                        string newCount = questParser[ii].Objectives[ii2].Count.ToString();
                                                        if (newQuest != oldQuest && newCount != oldCount)
                                                        {
                                                            if (Settings.Default.createIconForChallenges)
                                                            {
                                                                justSkip += 1;
                                                                iamY += 140;
                                                                g.DrawString(questParser[ii].Objectives[ii2].Description, new Font(_pfc.Families[1], 50), new SolidBrush(Color.White), new Point(100, iamY));
                                                                Image slider = Resources.Challenges_Slider;
                                                                g.DrawImage(slider, new Point(108, iamY + 86));
                                                                g.DrawString(questParser[ii].Objectives[ii2].Count.ToString(), new Font(_pfc.Families[0], 20), new SolidBrush(Color.FromArgb(255, 255, 255, 255)), new Point(968, iamY + 87));
                                                                if (justSkip != 1)
                                                                {
                                                                    g.DrawLine(new Pen(Color.FromArgb(30, 255, 255, 255)), 100, iamY - 10, 2410, iamY - 10);
                                                                }
                                                            }
                                                            AppendText(questParser[ii].Objectives[ii2].Description, Color.SteelBlue);
                                                            if (questParser[ii].Rewards != null)
                                                            {
                                                                AppendText("\t\tCount: " + questParser[ii].Objectives[ii2].Count, Color.DarkRed);
                                                                try
                                                                {
                                                                    if (Settings.Default.createIconForChallenges)
                                                                    {
                                                                        string itemToExtract = questParser[ii].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest").Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token").FirstOrDefault().ItemPrimaryAssetId.PrimaryAssetName;
                                                                        if (string.Equals(itemToExtract, "athenabattlestar", StringComparison.CurrentCultureIgnoreCase))
                                                                        {
                                                                            #region DRAW ICON
                                                                            Image rewardIcon = Resources.T_FNBR_BattlePoints_L;
                                                                            g.DrawImage(Forms.Settings.ResizeImage(rewardIcon, 75, 75), new Point(2325, iamY + 22));

                                                                            GraphicsPath p = new GraphicsPath();
                                                                            p.AddString(
                                                                                questParser[ii].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest")
                                                                        .Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token")
                                                                        .FirstOrDefault().Quantity.ToString(),
                                                                                _pfc.Families[1],
                                                                                (int)FontStyle.Regular,
                                                                                60,
                                                                                new Point(2322, iamY + 25), _rightString);
                                                                            g.DrawPath(new Pen(Color.FromArgb(255, 143, 74, 32), 5), p);

                                                                            g.FillPath(new SolidBrush(Color.FromArgb(255, 255, 219, 103)), p);
                                                                            #endregion
                                                                        }
                                                                        else if (string.Equals(itemToExtract, "AthenaSeasonalXP", StringComparison.CurrentCultureIgnoreCase))
                                                                        {
                                                                            #region DRAW ICON
                                                                            Image rewardIcon = Resources.T_FNBR_SeasonalXP_L;
                                                                            g.DrawImage(Forms.Settings.ResizeImage(rewardIcon, 75, 75), new Point(2325, iamY + 22));

                                                                            GraphicsPath p = new GraphicsPath();
                                                                            p.AddString(
                                                                                questParser[ii].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest")
                                                                        .Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token")
                                                                        .FirstOrDefault().Quantity.ToString(),
                                                                                _pfc.Families[1],
                                                                                (int)FontStyle.Regular,
                                                                                60,
                                                                                new Point(2322, iamY + 25), _rightString);
                                                                            g.DrawPath(new Pen(Color.FromArgb(255, 81, 131, 15), 5), p);

                                                                            g.FillPath(new SolidBrush(Color.FromArgb(255, 230, 253, 177)), p);
                                                                            #endregion
                                                                        }
                                                                        else if (string.Equals(itemToExtract, "MtxGiveaway", StringComparison.CurrentCultureIgnoreCase))
                                                                        {
                                                                            #region DRAW ICON
                                                                            Image rewardIcon = Resources.T_Items_MTX_L;
                                                                            g.DrawImage(Forms.Settings.ResizeImage(rewardIcon, 75, 75), new Point(2325, iamY + 22));

                                                                            GraphicsPath p = new GraphicsPath();
                                                                            p.AddString(
                                                                                questParser[ii].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest")
                                                                        .Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token")
                                                                        .FirstOrDefault().Quantity.ToString(),
                                                                                _pfc.Families[1],
                                                                                (int)FontStyle.Regular,
                                                                                60,
                                                                                new Point(2322, iamY + 25), _rightString);
                                                                            g.DrawPath(new Pen(Color.FromArgb(255, 100, 160, 175), 5), p);

                                                                            g.FillPath(new SolidBrush(Color.FromArgb(255, 220, 230, 255)), p);
                                                                            #endregion
                                                                        }
                                                                        else
                                                                            DrawRewardIcon(itemToExtract, g, iamY);
                                                                    }

                                                                    AppendText("\t\t" + questParser[ii].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest")
                                                                        .Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token")
                                                                        .FirstOrDefault().ItemPrimaryAssetId.PrimaryAssetType.Name + ":"
                                                                        + questParser[ii].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest")
                                                                        .Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token")
                                                                        .FirstOrDefault().ItemPrimaryAssetId.PrimaryAssetName + ":"
                                                                        + questParser[ii].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest")
                                                                            .Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token")
                                                                            .FirstOrDefault().Quantity, Color.DarkGreen, true);
                                                                }
                                                                catch (NullReferenceException)
                                                                {
                                                                    if (questParser[ii].HiddenRewards != null)
                                                                    {
                                                                        if (Settings.Default.createIconForChallenges)
                                                                        {
                                                                            var partsofbruhreally = questParser[ii].HiddenRewards.FirstOrDefault().TemplateId.Split(':');
                                                                            if (partsofbruhreally[0] != "HomebaseBannerIcon")
                                                                                DrawRewardIcon(partsofbruhreally[1], g, iamY);
                                                                            else
                                                                                DrawRewardBanner(partsofbruhreally[1], g, iamY);
                                                                        }

                                                                        AppendText("\t\t" + questParser[ii].HiddenRewards.FirstOrDefault().TemplateId + ":"
                                                                            + questParser[ii].HiddenRewards.FirstOrDefault().Quantity, Color.DarkGreen, true);
                                                                    }
                                                                    else
                                                                    {
                                                                        AppendText("", Color.Black, true);
                                                                    }
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    Console.WriteLine(ex.Message);
                                                                }
                                                            }
                                                            else
                                                            {
                                                                AppendText("\t\tCount: " + questParser[ii].Objectives[ii2].Count, Color.DarkRed, true);
                                                            }

                                                            oldQuest = questParser[ii].Objectives[ii2].Description;
                                                            oldCount = questParser[ii].Objectives[ii2].Count.ToString();
                                                        }
                                                        try
                                                        {
                                                            for (int ii3 = 0; ii3 < questParser[ii].Rewards.Length; ii3++)
                                                            {
                                                                LoopStageQuest(questParser[ii].Rewards[ii3].ItemPrimaryAssetId.PrimaryAssetType.Name, questParser[ii].Rewards[ii3].ItemPrimaryAssetId.PrimaryAssetName, g, iamY, justSkip);
                                                                iamY = YAfterLoop;
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
                                                UpdateConsole("No serialized file found", Color.FromArgb(255, 244, 66, 66), "Error");
                                        }
                                        catch (JsonSerializationException)
                                        {
                                            AppendText(CurrentUsedItem + " ", Color.Red);
                                            AppendText(".JSON file can't be displayed", Color.Black, true);
                                        }
                                    }
                                }
                                else
                                    UpdateConsole("Error while extracting " + SelectedChallengesArray[i2], Color.FromArgb(255, 244, 66, 66), "Error");
                            }
                            catch (KeyNotFoundException)
                            {
                                AppendText("Can't extract ", Color.Black);
                                AppendText(SelectedChallengesArray[i2], Color.SteelBlue, true);
                            }
                        }

                        iamY += 100;

                        //BundleCompletionRewards
                        try
                        {
                            for (int i2 = 0; i2 < bundleParser[i].BundleCompletionRewards.Length; i2++)
                            {
                                for (int i3 = 0; i3 < bundleParser[i].BundleCompletionRewards[i2].Rewards.Length; i3++)
                                {
                                    string itemReward = Path.GetFileName(bundleParser[i].BundleCompletionRewards[i2].Rewards[i3].ItemDefinition.AssetPathName.Substring(0, bundleParser[i].BundleCompletionRewards[i2].Rewards[i3].ItemDefinition.AssetPathName.LastIndexOf(".", StringComparison.Ordinal)));
                                    string compCount = bundleParser[i].BundleCompletionRewards[i2].CompletionCount.ToString();

                                    if (itemReward != "AthenaBattlePass_WeeklyChallenge_Token" && itemReward != "AthenaBattlePass_WeeklyBundle_Token")
                                    {
                                        justSkip += 1;
                                        iamY += 140;

                                        if (itemReward.Contains("Fortbyte_WeeklyChallengesComplete_"))
                                        {
                                            #region DRAW ICON
                                            string textureFile = "T_UI_PuzzleIcon_64";

                                            string textureFilePath = string.Empty;
                                            if (CurrentUsedPakGuid != null && CurrentUsedPakGuid != "0-0-0-0")
                                                textureFilePath = ExtractAsset(CurrentUsedPak, textureFile);
                                            else
                                                textureFilePath = ExtractAsset(AllpaksDictionary[textureFile ?? throw new InvalidOperationException()], textureFile);

                                            if (textureFilePath != null)
                                            {
                                                MyAsset = new PakAsset(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")));
                                                MyAsset.SaveTexture(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")) + ".png");
                                                ItemIconPath = textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")) + ".png";
                                                UpdateConsole(textureFile + " successfully converted to .PNG", Color.FromArgb(255, 66, 244, 66), "Success");
                                            }
                                            else
                                                UpdateConsole("Error while extracting " + textureFile, Color.FromArgb(255, 244, 66, 66), "Error");

                                            if (File.Exists(ItemIconPath))
                                            {
                                                Image itemIcon;
                                                using (var bmpTemp = new Bitmap(ItemIconPath))
                                                {
                                                    itemIcon = new Bitmap(bmpTemp);
                                                }
                                                g.DrawImage(Forms.Settings.ResizeImage(itemIcon, 110, 110), new Point(2300, iamY + 6));
                                            }
                                            else
                                            {
                                                Image itemIcon = Resources.unknown512;
                                                g.DrawImage(Forms.Settings.ResizeImage(itemIcon, 110, 110), new Point(2300, iamY + 6));
                                            }
                                            #endregion

                                            if (compCount == "-1")
                                                g.DrawString("Complete ALL CHALLENGES to earn the reward item", new Font(_pfc.Families[1], 50), new SolidBrush(Color.White), new Point(100, iamY + 22));
                                            else
                                                g.DrawString("Complete ANY " + compCount + " CHALLENGES to earn the reward item", new Font(_pfc.Families[1], 50), new SolidBrush(Color.White), new Point(100, iamY + 22));
                                        }
                                        else
                                        {
                                            if (bundleParser[i].BundleCompletionRewards[i2].Rewards[i3].ItemDefinition.AssetPathName == "None")
                                            {
                                                var partsofbruhreally = bundleParser[i].BundleCompletionRewards[i2].Rewards[i3].TemplateId.Split(':');
                                                DrawRewardBanner(partsofbruhreally[1], g, iamY);
                                            }
                                            else if (string.Equals(itemReward, "athenabattlestar", StringComparison.CurrentCultureIgnoreCase))
                                            {
                                                #region DRAW ICON
                                                Image rewardIcon = Resources.T_FNBR_BattlePoints_L;
                                                g.DrawImage(Forms.Settings.ResizeImage(rewardIcon, 75, 75), new Point(2325, iamY + 22));

                                                GraphicsPath p = new GraphicsPath();
                                                p.AddString(
                                                    bundleParser[i].BundleCompletionRewards[i2].Rewards[i3].Quantity.ToString(),
                                                    _pfc.Families[1],
                                                    (int)FontStyle.Regular,
                                                    60,
                                                    new Point(2322, iamY + 25), _rightString);
                                                g.DrawPath(new Pen(Color.FromArgb(255, 143, 74, 32), 5), p);

                                                g.FillPath(new SolidBrush(Color.FromArgb(255, 255, 219, 103)), p);
                                                #endregion
                                            }
                                            else if (string.Equals(itemReward, "AthenaSeasonalXP", StringComparison.CurrentCultureIgnoreCase))
                                            {
                                                #region DRAW ICON
                                                Image rewardIcon = Resources.T_FNBR_SeasonalXP_L;
                                                g.DrawImage(Forms.Settings.ResizeImage(rewardIcon, 75, 75), new Point(2325, iamY + 22));

                                                GraphicsPath p = new GraphicsPath();
                                                p.AddString(
                                                    bundleParser[i].BundleCompletionRewards[i2].Rewards[i3].Quantity.ToString(),
                                                    _pfc.Families[1],
                                                    (int)FontStyle.Regular,
                                                    60,
                                                    new Point(2322, iamY + 25), _rightString);
                                                g.DrawPath(new Pen(Color.FromArgb(255, 81, 131, 15), 5), p);

                                                g.FillPath(new SolidBrush(Color.FromArgb(255, 230, 253, 177)), p);
                                                #endregion
                                            }
                                            else if (string.Equals(itemReward, "MtxGiveaway", StringComparison.CurrentCultureIgnoreCase))
                                            {
                                                #region DRAW ICON
                                                Image rewardIcon = Resources.T_Items_MTX_L;
                                                g.DrawImage(Forms.Settings.ResizeImage(rewardIcon, 75, 75), new Point(2325, iamY + 22));

                                                GraphicsPath p = new GraphicsPath();
                                                p.AddString(
                                                    bundleParser[i].BundleCompletionRewards[i2].Rewards[i3].Quantity.ToString(),
                                                    _pfc.Families[1],
                                                    (int)FontStyle.Regular,
                                                    60,
                                                    new Point(2322, iamY + 25), _rightString);
                                                g.DrawPath(new Pen(Color.FromArgb(255, 100, 160, 175), 5), p);

                                                g.FillPath(new SolidBrush(Color.FromArgb(255, 220, 230, 255)), p);
                                                #endregion
                                            }
                                            else
                                                DrawRewardIcon(itemReward, g, iamY);

                                            if (compCount == "-1")
                                                g.DrawString("Complete ALL CHALLENGES to earn the reward item", new Font(_pfc.Families[1], 50), new SolidBrush(Color.White), new Point(100, iamY + 22));
                                            else
                                                g.DrawString("Complete ANY " + compCount + " CHALLENGES to earn the reward item", new Font(_pfc.Families[1], 50), new SolidBrush(Color.White), new Point(100, iamY + 22));
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            UpdateConsole(ex.Message, Color.FromArgb(255, 244, 66, 66), "Error");
                            iamY -= 100;
                        }
                    }

                    if (Settings.Default.createIconForChallenges)
                    {
                        #region WATERMARK
                        g.FillRectangle(new SolidBrush(Color.FromArgb(100, 0, 0, 0)), new Rectangle(0, iamY + 240, bmp.Width, 40));
                        g.DrawString(theItem.DisplayName + " Generated using FModel & JohnWickParse - " + DateTime.Now.ToString("dd/MM/yyyy"), new Font(_pfc.Families[0], 20), new SolidBrush(Color.FromArgb(150, 255, 255, 255)), new Point(bmp.Width / 2, iamY + 250), _centeredString);
                        #endregion
                        if (v2 == false)
                        {
                            #region DRAW TEXT
                            try
                            {
                                string seasonFolder = questJson.Substring(questJson.Substring(0, questJson.LastIndexOf("\\", StringComparison.Ordinal)).LastIndexOf("\\", StringComparison.Ordinal) + 1).ToUpper();
                                g.DrawString(seasonFolder.Substring(0, seasonFolder.LastIndexOf("\\", StringComparison.Ordinal)), new Font(_pfc.Families[1], 42), new SolidBrush(Color.FromArgb(255, 149, 213, 255)), new Point(340, 40));
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
                                g.DrawString(theItem.DisplayName.ToUpper(), new Font(_pfc.Families[1], 115), new SolidBrush(Color.White), new Point(325, 70));
                            }
                            catch (NullReferenceException)
                            {
                                AppendText("[NullReferenceException] ", Color.Red);
                                AppendText("No ", Color.Black);
                                AppendText("DisplayName ", Color.SteelBlue);
                                AppendText("found", Color.Black, true);
                            } //NAME
                            #endregion
                        }
                        #region CUT IMAGE
                        using (Bitmap bmp2 = bmp)
                        {
                            var newImg = bmp2.Clone(
                                new Rectangle { X = 0, Y = 0, Width = bmp.Width, Height = iamY + 280 },
                                bmp2.PixelFormat);
                            pictureBox1.Image = newImg;
                        } //CUT
                        #endregion
                    }

                    UpdateConsole(theItem.DisplayName, Color.FromArgb(255, 66, 244, 66), "Success");
                    if (autoSaveImagesToolStripMenuItem.Checked || updateModeToolStripMenuItem.Checked)
                    {
                        Invoke(new Action(() =>
                        {
                            pictureBox1.Image.Save(DefaultOutputPath + "\\Icons\\" + CurrentUsedItem + ".png", ImageFormat.Png);
                        }));
                        AppendText(CurrentUsedItem, Color.DarkRed);
                        AppendText(" successfully saved", Color.Black, true);
                    }

                    AppendText("", Color.Black, true);
                }
            }
        }
        private void drawV2(ChallengeBundleIdParser myBundle, ItemsIdParser theItem, string questJson, Graphics toDrawOn, Bitmap myBitmap)
        {
            int sRed;
            int sGreen;
            int sBlue;

            string seasonFolder = questJson.Substring(questJson.Substring(0, questJson.LastIndexOf("\\", StringComparison.Ordinal)).LastIndexOf("\\", StringComparison.Ordinal) + 1).ToUpper();

            if (seasonFolder.Substring(0, seasonFolder.LastIndexOf("\\", StringComparison.Ordinal)) != "LTM")
            {
                sRed = (int)(myBundle.DisplayStyle.SecondaryColor.R * 255);
                sGreen = (int)(myBundle.DisplayStyle.SecondaryColor.G * 255);
                sBlue = (int)(myBundle.DisplayStyle.SecondaryColor.B * 255);
            }
            else
            {
                sRed = (int)(myBundle.DisplayStyle.AccentColor.R * 255);
                sGreen = (int)(myBundle.DisplayStyle.AccentColor.G * 255);
                sBlue = (int)(myBundle.DisplayStyle.AccentColor.B * 255);
            }

            int seasonRed = Convert.ToInt32(sRed / 1.5);
            int seasonGreen = Convert.ToInt32(sGreen / 1.5);
            int seasonBlue = Convert.ToInt32(sBlue / 1.5);

            toDrawOn.FillRectangle(new SolidBrush(Color.FromArgb(255, sRed, sGreen, sBlue)), new Rectangle(0, 0, myBitmap.Width, 271));
            toDrawOn.FillRectangle(new SolidBrush(Color.FromArgb(255, seasonRed, seasonGreen, seasonBlue)), new Rectangle(0, 271, myBitmap.Width, myBitmap.Height - 271));

            try
            {
                toDrawOn.DrawString(seasonFolder.Substring(0, seasonFolder.LastIndexOf("\\", StringComparison.Ordinal)), new Font(_pfc.Families[1], 42), new SolidBrush(Color.FromArgb(255, seasonRed, seasonGreen, seasonBlue)), new Point(340, 40));
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
                toDrawOn.DrawString(theItem.DisplayName.ToUpper(), new Font(_pfc.Families[1], 115), new SolidBrush(Color.White), new Point(325, 70));
            }
            catch (NullReferenceException)
            {
                AppendText("[NullReferenceException] ", Color.Red);
                AppendText("No ", Color.Black);
                AppendText("DisplayName ", Color.SteelBlue);
                AppendText("found", Color.Black, true);
            } //NAME

            string pngPath;
            string textureFile = Path.GetFileName(myBundle.DisplayStyle.DisplayImage.AssetPathName).Substring(0, Path.GetFileName(myBundle.DisplayStyle.DisplayImage.AssetPathName).LastIndexOf('.'));

            string textureFilePath = string.Empty;
            if (CurrentUsedPakGuid != null && CurrentUsedPakGuid != "0-0-0-0")
                textureFilePath = ExtractAsset(CurrentUsedPak, textureFile);
            else
                textureFilePath = ExtractAsset(AllpaksDictionary[textureFile], textureFile);

            if (textureFilePath != null && textureFile == "M_UI_ChallengeTile_PCB")
            {
                pngPath = GetRenderSwitchMaterialTexture(textureFile, textureFilePath);

                Image challengeIcon = Image.FromFile(pngPath);
                toDrawOn.DrawImage(Forms.Settings.ResizeImage(challengeIcon, 271, 271), new Point(40, 0));
            }
            else if (textureFilePath != null)
            {
                MyAsset = new PakAsset(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")));
                MyAsset.SaveTexture(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")) + ".png");
                pngPath = textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")) + ".png";
                UpdateConsole(textureFile + " successfully converted to .PNG", Color.FromArgb(255, 66, 244, 66), "Success");

                Image challengeIcon;
                using (var bmpTemp = new Bitmap(pngPath))
                {
                    challengeIcon = new Bitmap(bmpTemp);
                }
                toDrawOn.DrawImage(Forms.Settings.ResizeImage(challengeIcon, 271, 271), new Point(40, 0));
            }
        }
        private void LoopStageQuest(string qAssetType, string qAssetName, Graphics toDrawOn, int yeay, int line)
        {
            Graphics toDrawOnLoop = toDrawOn;
            int yeayLoop = yeay;
            int lineLoop = line;

            if (qAssetType == "Quest")
            {
                try
                {
                    string challengeFilePathLoop = ExtractAsset(AllpaksDictionary[qAssetName], qAssetName);

                    if (challengeFilePathLoop != null)
                    {
                        UpdateConsole(qAssetName + " successfully extracted", Color.FromArgb(255, 66, 244, 66), "Success");
                        if (challengeFilePathLoop.Contains(".uasset") || challengeFilePathLoop.Contains(".uexp") || challengeFilePathLoop.Contains(".ubulk"))
                        {
                            MyAsset = new PakAsset(challengeFilePathLoop.Substring(0, challengeFilePathLoop.LastIndexOf('.')));
                            try
                            {
                                if (MyAsset.GetSerialized() != null)
                                {
                                    UpdateConsole(qAssetName + " successfully serialized", Color.FromArgb(255, 66, 244, 66), "Success");

                                    string parsedJson = JToken.Parse(MyAsset.GetSerialized()).ToString();
                                    var questParser = QuestParser.FromJson(parsedJson);
                                    UpdateConsole("Parsing " + qAssetName + "...", Color.FromArgb(255, 244, 132, 66), "Waiting");
                                    for (int i = 0; i < questParser.Length; i++)
                                    {
                                        string oldQuest = string.Empty;
                                        string oldCount = string.Empty;
                                        for (int ii = 0; ii < questParser[i].Objectives.Length; ii++)
                                        {
                                            if (CurrentUsedItem == "QuestBundle_S8_ExtraCredit" || CurrentUsedItem == "QuestBundle_S7_Overtime")
                                            {
                                                string newQuest = questParser[i].Objectives[ii].Description;
                                                string newCount = questParser[i].Objectives[ii].Count.ToString();

                                                if (newQuest != oldQuest && newCount != oldCount)
                                                {
                                                    if (Settings.Default.createIconForChallenges)
                                                    {
                                                        toDrawOnLoop.TextRenderingHint = TextRenderingHint.AntiAlias;
                                                        lineLoop += 1;
                                                        yeayLoop += 140;
                                                        toDrawOnLoop.DrawString(questParser[i].Objectives[ii].Description, new Font(_pfc.Families[1], 50), new SolidBrush(Color.White), new Point(100, yeayLoop));
                                                        Image slider = Resources.Challenges_Slider;
                                                        toDrawOnLoop.DrawImage(slider, new Point(108, yeayLoop + 86));
                                                        toDrawOnLoop.DrawString(questParser[i].Objectives[ii].Count.ToString(), new Font(_pfc.Families[0], 20), new SolidBrush(Color.FromArgb(255, 255, 255, 255)), new Point(968, yeayLoop + 87));
                                                        if (lineLoop != 1)
                                                        {
                                                            toDrawOnLoop.DrawLine(new Pen(Color.FromArgb(30, 255, 255, 255)), 100, yeayLoop - 10, 2410, yeayLoop - 10);
                                                        }
                                                    }
                                                    AppendText(questParser[i].Objectives[ii].Description, Color.SteelBlue);
                                                    AppendText("\t\tCount: " + questParser[i].Objectives[ii].Count, Color.DarkRed);
                                                    if (questParser[i].Rewards != null)
                                                    {
                                                        AppendText("\t\tCount: " + questParser[i].Objectives[ii].Count, Color.DarkRed);
                                                        try
                                                        {
                                                            if (Settings.Default.createIconForChallenges)
                                                            {
                                                                string itemToExtract = questParser[ii].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest").Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token").FirstOrDefault().ItemPrimaryAssetId.PrimaryAssetName;
                                                                if (string.Equals(itemToExtract, "athenabattlestar", StringComparison.CurrentCultureIgnoreCase))
                                                                {
                                                                    #region DRAW ICON
                                                                    Image rewardIcon = Resources.T_FNBR_BattlePoints_L;
                                                                    toDrawOnLoop.DrawImage(Forms.Settings.ResizeImage(rewardIcon, 75, 75), new Point(2325, yeayLoop + 22));

                                                                    GraphicsPath p = new GraphicsPath();
                                                                    p.AddString(
                                                                        questParser[ii].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest")
                                                                .Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token")
                                                                .FirstOrDefault().Quantity.ToString(),
                                                                        _pfc.Families[1],
                                                                        (int)FontStyle.Regular,
                                                                        60,
                                                                        new Point(2322, yeayLoop + 25), _rightString);
                                                                    toDrawOnLoop.DrawPath(new Pen(Color.FromArgb(255, 143, 74, 32), 5), p);

                                                                    toDrawOnLoop.FillPath(new SolidBrush(Color.FromArgb(255, 255, 219, 103)), p);
                                                                    #endregion
                                                                }
                                                                else if (string.Equals(itemToExtract, "AthenaSeasonalXP", StringComparison.CurrentCultureIgnoreCase))
                                                                {
                                                                    #region DRAW ICON
                                                                    Image rewardIcon = Resources.T_FNBR_SeasonalXP_L;
                                                                    toDrawOnLoop.DrawImage(Forms.Settings.ResizeImage(rewardIcon, 75, 75), new Point(2325, yeayLoop + 22));

                                                                    GraphicsPath p = new GraphicsPath();
                                                                    p.AddString(
                                                                        questParser[ii].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest")
                                                                .Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token")
                                                                .FirstOrDefault().Quantity.ToString(),
                                                                        _pfc.Families[1],
                                                                        (int)FontStyle.Regular,
                                                                        60,
                                                                        new Point(2322, yeayLoop + 25), _rightString);
                                                                    toDrawOnLoop.DrawPath(new Pen(Color.FromArgb(255, 81, 131, 15), 5), p);

                                                                    toDrawOnLoop.FillPath(new SolidBrush(Color.FromArgb(255, 230, 253, 177)), p);
                                                                    #endregion
                                                                }
                                                                else if (string.Equals(itemToExtract, "MtxGiveaway", StringComparison.CurrentCultureIgnoreCase))
                                                                {
                                                                    #region DRAW ICON
                                                                    Image rewardIcon = Resources.T_Items_MTX_L;
                                                                    toDrawOnLoop.DrawImage(Forms.Settings.ResizeImage(rewardIcon, 75, 75), new Point(2325, yeayLoop + 22));

                                                                    GraphicsPath p = new GraphicsPath();
                                                                    p.AddString(
                                                                        questParser[ii].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest")
                                                                .Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token")
                                                                .FirstOrDefault().Quantity.ToString(),
                                                                        _pfc.Families[1],
                                                                        (int)FontStyle.Regular,
                                                                        60,
                                                                        new Point(2322, yeayLoop + 25), _rightString);
                                                                    toDrawOnLoop.DrawPath(new Pen(Color.FromArgb(255, 100, 160, 175), 5), p);

                                                                    toDrawOnLoop.FillPath(new SolidBrush(Color.FromArgb(255, 220, 230, 255)), p);
                                                                    #endregion
                                                                }
                                                                else
                                                                    DrawRewardIcon(itemToExtract, toDrawOnLoop, yeayLoop);
                                                            }

                                                            AppendText("\t\t" + questParser[i].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest")
                                                                .Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token")
                                                                .FirstOrDefault().ItemPrimaryAssetId.PrimaryAssetType.Name + ":"
                                                                + questParser[i].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest")
                                                                .Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token")
                                                                .FirstOrDefault().ItemPrimaryAssetId.PrimaryAssetName + ":"
                                                                + questParser[i].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest")
                                                                    .Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token")
                                                                    .FirstOrDefault().Quantity, Color.DarkGreen, true);
                                                        }
                                                        catch (NullReferenceException)
                                                        {
                                                            if (questParser[i].HiddenRewards != null)
                                                            {
                                                                if (Settings.Default.createIconForChallenges)
                                                                {
                                                                    var partsofbruhreally = questParser[i].HiddenRewards.FirstOrDefault().TemplateId.Split(':');
                                                                    if (partsofbruhreally[0] != "HomebaseBannerIcon")
                                                                        DrawRewardIcon(partsofbruhreally[1], toDrawOnLoop, yeayLoop);
                                                                }

                                                                AppendText("\t\t" + questParser[i].HiddenRewards.FirstOrDefault().TemplateId + ":"
                                                                    + questParser[i].HiddenRewards.FirstOrDefault().Quantity, Color.DarkGreen, true);
                                                            }
                                                            else
                                                            {
                                                                AppendText("", Color.Black, true);
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            Console.WriteLine(ex.Message);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        AppendText("\t\tCount: " + questParser[i].Objectives[ii].Count, Color.DarkRed, true);
                                                    }

                                                    oldQuest = questParser[i].Objectives[ii].Description;
                                                    oldCount = questParser[i].Objectives[ii].Count.ToString();
                                                }
                                                for (int iii = 0; iii < questParser[i].Rewards.Length; iii++)
                                                {
                                                    LoopStageQuest(questParser[i].Rewards[iii].ItemPrimaryAssetId.PrimaryAssetType.Name, questParser[i].Rewards[iii].ItemPrimaryAssetId.PrimaryAssetName, toDrawOnLoop, yeayLoop, lineLoop);
                                                    yeayLoop = YAfterLoop;
                                                }
                                            }
                                            else if (!_questStageDict.ContainsKey(questParser[i].Objectives[ii].Description))
                                            {
                                                string newQuest = questParser[i].Objectives[ii].Description;
                                                string newCount = questParser[i].Objectives[ii].Count.ToString();
                                                _questStageDict.Add(questParser[i].Objectives[ii].Description, questParser[i].Objectives[ii].Count);

                                                if (newQuest != oldQuest && newCount != oldCount)
                                                {
                                                    if (Settings.Default.createIconForChallenges)
                                                    {
                                                        lineLoop += 1;
                                                        yeayLoop += 140;
                                                        toDrawOnLoop.DrawString(questParser[i].Objectives[ii].Description, new Font(_pfc.Families[1], 50), new SolidBrush(Color.White), new Point(100, yeayLoop));
                                                        Image slider = Resources.Challenges_Slider;
                                                        toDrawOnLoop.DrawImage(slider, new Point(108, yeayLoop + 86));
                                                        toDrawOnLoop.DrawString(questParser[i].Objectives[ii].Count.ToString(), new Font(_pfc.Families[0], 20), new SolidBrush(Color.FromArgb(255, 255, 255, 255)), new Point(968, yeayLoop + 87));
                                                        if (lineLoop != 1)
                                                        {
                                                            toDrawOnLoop.DrawLine(new Pen(Color.FromArgb(30, 255, 255, 255)), 100, yeayLoop - 10, 2410, yeayLoop - 10);
                                                        }
                                                    }
                                                    AppendText(questParser[i].Objectives[ii].Description, Color.SteelBlue);
                                                    AppendText("\t\tCount: " + questParser[i].Objectives[ii].Count, Color.DarkRed);
                                                    if (questParser[i].Rewards != null)
                                                    {
                                                        AppendText("\t\tCount: " + questParser[i].Objectives[ii].Count, Color.DarkRed);
                                                        try
                                                        {
                                                            if (Settings.Default.createIconForChallenges)
                                                            {
                                                                string itemToExtract = questParser[ii].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest").Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token").FirstOrDefault().ItemPrimaryAssetId.PrimaryAssetName;
                                                                if (string.Equals(itemToExtract, "athenabattlestar", StringComparison.CurrentCultureIgnoreCase))
                                                                {
                                                                    #region DRAW ICON
                                                                    Image rewardIcon = Resources.T_FNBR_BattlePoints_L;
                                                                    toDrawOnLoop.DrawImage(Forms.Settings.ResizeImage(rewardIcon, 75, 75), new Point(2325, yeayLoop + 22));

                                                                    GraphicsPath p = new GraphicsPath();
                                                                    p.AddString(
                                                                        questParser[ii].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest")
                                                                .Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token")
                                                                .FirstOrDefault().Quantity.ToString(),
                                                                        _pfc.Families[1],
                                                                        (int)FontStyle.Regular,
                                                                        60,
                                                                        new Point(2322, yeayLoop + 25), _rightString);
                                                                    toDrawOnLoop.DrawPath(new Pen(Color.FromArgb(255, 143, 74, 32), 5), p);

                                                                    toDrawOnLoop.FillPath(new SolidBrush(Color.FromArgb(255, 255, 219, 103)), p);
                                                                    #endregion
                                                                }
                                                                else if (string.Equals(itemToExtract, "AthenaSeasonalXP", StringComparison.CurrentCultureIgnoreCase))
                                                                {
                                                                    #region DRAW ICON
                                                                    Image rewardIcon = Resources.T_FNBR_SeasonalXP_L;
                                                                    toDrawOnLoop.DrawImage(Forms.Settings.ResizeImage(rewardIcon, 75, 75), new Point(2325, yeayLoop + 22));

                                                                    GraphicsPath p = new GraphicsPath();
                                                                    p.AddString(
                                                                        questParser[ii].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest")
                                                                .Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token")
                                                                .FirstOrDefault().Quantity.ToString(),
                                                                        _pfc.Families[1],
                                                                        (int)FontStyle.Regular,
                                                                        60,
                                                                        new Point(2322, yeayLoop + 25), _rightString);
                                                                    toDrawOnLoop.DrawPath(new Pen(Color.FromArgb(255, 81, 131, 15), 5), p);

                                                                    toDrawOnLoop.FillPath(new SolidBrush(Color.FromArgb(255, 230, 253, 177)), p);
                                                                    #endregion
                                                                }
                                                                else if (string.Equals(itemToExtract, "MtxGiveaway", StringComparison.CurrentCultureIgnoreCase))
                                                                {
                                                                    #region DRAW ICON
                                                                    Image rewardIcon = Resources.T_Items_MTX_L;
                                                                    toDrawOnLoop.DrawImage(Forms.Settings.ResizeImage(rewardIcon, 75, 75), new Point(2325, yeayLoop + 22));

                                                                    GraphicsPath p = new GraphicsPath();
                                                                    p.AddString(
                                                                        questParser[ii].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest")
                                                                .Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token")
                                                                .FirstOrDefault().Quantity.ToString(),
                                                                        _pfc.Families[1],
                                                                        (int)FontStyle.Regular,
                                                                        60,
                                                                        new Point(2322, yeayLoop + 25), _rightString);
                                                                    toDrawOnLoop.DrawPath(new Pen(Color.FromArgb(255, 100, 160, 175), 5), p);

                                                                    toDrawOnLoop.FillPath(new SolidBrush(Color.FromArgb(255, 220, 230, 255)), p);
                                                                    #endregion
                                                                }
                                                                else
                                                                    DrawRewardIcon(itemToExtract, toDrawOnLoop, yeayLoop);
                                                            }

                                                            AppendText("\t\t" + questParser[i].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest")
                                                                .Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token")
                                                                .FirstOrDefault().ItemPrimaryAssetId.PrimaryAssetType.Name + ":"
                                                                + questParser[i].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest")
                                                                .Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token")
                                                                .FirstOrDefault().ItemPrimaryAssetId.PrimaryAssetName + ":"
                                                                + questParser[i].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest")
                                                                    .Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token")
                                                                    .FirstOrDefault().Quantity, Color.DarkGreen, true);
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            if (questParser[i].HiddenRewards != null)
                                                            {
                                                                if (Settings.Default.createIconForChallenges)
                                                                {
                                                                    var partsofbruhreally = questParser[i].HiddenRewards.FirstOrDefault().TemplateId.Split(':');
                                                                    if (partsofbruhreally[0] != "HomebaseBannerIcon")
                                                                        DrawRewardIcon(partsofbruhreally[1], toDrawOnLoop, yeayLoop);
                                                                    else
                                                                        DrawRewardBanner(partsofbruhreally[1], toDrawOnLoop, yeayLoop);
                                                                }

                                                                AppendText("\t\t" + questParser[i].HiddenRewards.FirstOrDefault().TemplateId + ":"
                                                                    + questParser[i].HiddenRewards.FirstOrDefault().Quantity, Color.DarkGreen, true);
                                                            }
                                                            else
                                                            {
                                                                Console.WriteLine(ex.Message);
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        AppendText("\t\tCount: " + questParser[i].Objectives[ii].Count, Color.DarkRed, true);
                                                    }

                                                    oldQuest = questParser[i].Objectives[ii].Description;
                                                    oldCount = questParser[i].Objectives[ii].Count.ToString();
                                                }
                                                for (int iii = 0; iii < questParser[i].Rewards.Length; iii++)
                                                {
                                                    LoopStageQuest(questParser[i].Rewards[iii].ItemPrimaryAssetId.PrimaryAssetType.Name, questParser[i].Rewards[iii].ItemPrimaryAssetId.PrimaryAssetName, toDrawOnLoop, yeayLoop, lineLoop);
                                                    yeayLoop = YAfterLoop;
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                    UpdateConsole("No serialized file found", Color.FromArgb(255, 244, 66, 66), "Error");
                            }
                            catch (JsonSerializationException)
                            {
                                AppendText(CurrentUsedItem + " ", Color.Red);
                                AppendText(".JSON file can't be displayed", Color.Black, true);
                            }
                        }
                    }
                }
                catch (KeyNotFoundException)
                {
                    AppendText("Can't extract ", Color.Black);
                    AppendText(qAssetName, Color.SteelBlue);
                }
            }
            YAfterLoop = yeayLoop;
        }
        private void DrawRewardIcon(string iconName, Graphics toDrawOn, int y)
        {
            ItemIconPath = string.Empty;
            try
            {
                var value = AllpaksDictionary.Where(x => String.Equals(x.Key, iconName, StringComparison.CurrentCultureIgnoreCase)).Select(d => d.Key).FirstOrDefault();
                if (value != null)
                {
                    iconName = value;

                    string extractedIconPath = ExtractAsset(AllpaksDictionary[iconName], iconName);
                    if (extractedIconPath != null)
                    {
                        UpdateConsole(iconName + " successfully extracted", Color.FromArgb(255, 66, 244, 66), "Success");
                        if (extractedIconPath.Contains(".uasset") || extractedIconPath.Contains(".uexp") || extractedIconPath.Contains(".ubulk"))
                        {
                            MyAsset = new PakAsset(extractedIconPath.Substring(0, extractedIconPath.LastIndexOf('.')));
                            try
                            {
                                if (MyAsset.GetSerialized() != null)
                                {
                                    UpdateConsole(iconName + " successfully serialized", Color.FromArgb(255, 66, 244, 66), "Success");

                                    string parsedJson = JToken.Parse(MyAsset.GetSerialized()).ToString();
                                    var itemId = ItemsIdParser.FromJson(parsedJson);
                                    UpdateConsole("Parsing " + iconName + "...", Color.FromArgb(255, 244, 132, 66), "Waiting");
                                    for (int i = 0; i < itemId.Length; i++)
                                    {
                                        SearchAthIteDefIcon(itemId[i]);

                                        if (File.Exists(ItemIconPath))
                                        {
                                            Image itemIcon;
                                            using (var bmpTemp = new Bitmap(ItemIconPath))
                                            {
                                                itemIcon = new Bitmap(bmpTemp);
                                            }
                                            toDrawOn.DrawImage(Forms.Settings.ResizeImage(itemIcon, 110, 110), new Point(2300, y + 6));
                                        }
                                        else
                                        {
                                            Image itemIcon = Resources.unknown512;
                                            toDrawOn.DrawImage(Forms.Settings.ResizeImage(itemIcon, 110, 110), new Point(2300, y + 6));
                                        }
                                    }
                                }
                                else
                                    UpdateConsole("No serialized file found", Color.FromArgb(255, 244, 66, 66), "Error");
                            }
                            catch (JsonSerializationException)
                            {
                                AppendText(CurrentUsedItem + " ", Color.Red);
                                AppendText(".JSON file can't be displayed", Color.Black, true);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private void DrawRewardBanner(string bannerName, Graphics toDrawOn, int y)
        {
            ItemIconPath = string.Empty;

            string extractedBannerPath = ExtractAsset(AllpaksDictionary["BannerIcons"], "BannerIcons");
            if (extractedBannerPath != null)
            {
                UpdateConsole("BannerIcons successfully extracted", Color.FromArgb(255, 66, 244, 66), "Success");
                if (extractedBannerPath.Contains(".uasset") || extractedBannerPath.Contains(".uexp") || extractedBannerPath.Contains(".ubulk"))
                {
                    MyAsset = new PakAsset(extractedBannerPath.Substring(0, extractedBannerPath.LastIndexOf('.')));
                    try
                    {
                        if (MyAsset.GetSerialized() != null)
                        {
                            UpdateConsole("BannerIcons successfully serialized", Color.FromArgb(255, 66, 244, 66), "Success");

                            string parsedJson = JToken.Parse(MyAsset.GetSerialized()).ToString();
                            parsedJson = parsedJson.TrimStart('[').TrimEnd(']');
                            JObject jo = JObject.Parse(parsedJson);
                            foreach (JToken token in jo.FindTokens(bannerName))
                            {
                                var bannerId = BannersParser.FromJson(token.ToString());
                                UpdateConsole("Parsing " + token.Path + "...", Color.FromArgb(255, 244, 132, 66), "Waiting");

                                if (bannerId.LargeImage != null)
                                {
                                    string textureFile = Path.GetFileName(bannerId.LargeImage.AssetPathName)
                                        ?.Substring(0,
                                            Path.GetFileName(bannerId.LargeImage.AssetPathName).LastIndexOf('.'));

                                    string textureFilePath = string.Empty;
                                    if (CurrentUsedPakGuid != null && CurrentUsedPakGuid != "0-0-0-0")
                                        textureFilePath = ExtractAsset(CurrentUsedPak, textureFile);
                                    else
                                        textureFilePath = ExtractAsset(AllpaksDictionary[textureFile ?? throw new InvalidOperationException()], textureFile);

                                    if (textureFilePath != null)
                                    {
                                        MyAsset = new PakAsset(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")));
                                        MyAsset.SaveTexture(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")) + ".png");
                                        ItemIconPath = textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")) + ".png";
                                        UpdateConsole(textureFile + " successfully converted to .PNG", Color.FromArgb(255, 66, 244, 66), "Success");
                                    }
                                    else
                                        UpdateConsole("Error while extracting " + textureFile, Color.FromArgb(255, 244, 66, 66), "Error");
                                }
                                else if (bannerId.SmallImage != null)
                                {
                                    string textureFile = Path.GetFileName(bannerId.SmallImage.AssetPathName)
                                        ?.Substring(0,
                                            Path.GetFileName(bannerId.SmallImage.AssetPathName).LastIndexOf('.'));

                                    string textureFilePath = string.Empty;
                                    if (CurrentUsedPakGuid != null && CurrentUsedPakGuid != "0-0-0-0")
                                        textureFilePath = ExtractAsset(CurrentUsedPak, textureFile);
                                    else
                                        textureFilePath = ExtractAsset(AllpaksDictionary[textureFile ?? throw new InvalidOperationException()], textureFile);

                                    if (textureFilePath != null)
                                    {
                                        MyAsset = new PakAsset(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")));
                                        MyAsset.SaveTexture(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")) + ".png");
                                        ItemIconPath = textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")) + ".png";
                                        UpdateConsole(textureFile + " successfully converted to .PNG", Color.FromArgb(255, 66, 244, 66), "Success");
                                    }
                                    else
                                        UpdateConsole("Error while extracting " + textureFile, Color.FromArgb(255, 244, 66, 66), "Error");
                                }

                                if (File.Exists(ItemIconPath))
                                {
                                    Image itemIcon;
                                    using (var bmpTemp = new Bitmap(ItemIconPath))
                                    {
                                        itemIcon = new Bitmap(bmpTemp);
                                    }
                                    toDrawOn.DrawImage(Forms.Settings.ResizeImage(itemIcon, 110, 110), new Point(2300, y + 6));
                                }
                                else
                                {
                                    Image itemIcon = Resources.unknown512;
                                    toDrawOn.DrawImage(Forms.Settings.ResizeImage(itemIcon, 110, 110), new Point(2300, y + 6));
                                }
                            }
                        }
                        else
                            UpdateConsole("No serialized file found", Color.FromArgb(255, 244, 66, 66), "Error");
                    }
                    catch (JsonSerializationException)
                    {
                        AppendText(CurrentUsedItem + " ", Color.Red);
                        AppendText(".JSON file can't be displayed", Color.Black, true);
                    }
                }
            }
        }
        private void CreateFortByteChallengesIcon(ItemsIdParser theItem, string theParsedJson, string questJson = null)
        {
            Bitmap bmp = new Bitmap(2500, 7500);
            Graphics g = Graphics.FromImage(bmp);
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.SmoothingMode = SmoothingMode.HighQuality;
            int iamY = 275;
            int justSkip = 0;
            YAfterLoop = 0;
            bool v2 = false;
            int sRed;
            int sGreen;
            int sBlue;

            var bundleParser = ChallengeBundleIdParser.FromJson(theParsedJson);
            for (int i = 0; i < bundleParser.Length; i++)
            {
                #region DRAW BUNDLE ICON
                try
                {
                    if (Settings.Default.createIconForChallenges)
                    {
                        if (bundleParser[i].DisplayStyle.DisplayImage != null)
                        {
                            v2 = true;
                            string seasonFolder = questJson.Substring(questJson.Substring(0, questJson.LastIndexOf("\\", StringComparison.Ordinal)).LastIndexOf("\\", StringComparison.Ordinal) + 1).ToUpper();
                            #region COLORS + IMAGE
                            if (seasonFolder.Substring(0, seasonFolder.LastIndexOf("\\", StringComparison.Ordinal)) != "LTM")
                            {
                                sRed = (int)(bundleParser[i].DisplayStyle.SecondaryColor.R * 255);
                                sGreen = (int)(bundleParser[i].DisplayStyle.SecondaryColor.G * 255);
                                sBlue = (int)(bundleParser[i].DisplayStyle.SecondaryColor.B * 255);
                            }
                            else
                            {
                                sRed = (int)(bundleParser[i].DisplayStyle.AccentColor.R * 255);
                                sGreen = (int)(bundleParser[i].DisplayStyle.AccentColor.G * 255);
                                sBlue = (int)(bundleParser[i].DisplayStyle.AccentColor.B * 255);
                            }

                            int seasonRed = Convert.ToInt32(sRed / 1.5);
                            int seasonGreen = Convert.ToInt32(sGreen / 1.5);
                            int seasonBlue = Convert.ToInt32(sBlue / 1.5);

                            g.FillRectangle(new SolidBrush(Color.FromArgb(255, sRed, sGreen, sBlue)), new Rectangle(0, 0, bmp.Width, 271));
                            g.FillRectangle(new SolidBrush(Color.FromArgb(255, seasonRed, seasonGreen, seasonBlue)), new Rectangle(0, 271, bmp.Width, bmp.Height - 271));

                            try
                            {
                                g.DrawString(seasonFolder.Substring(0, seasonFolder.LastIndexOf("\\", StringComparison.Ordinal)), new Font(_pfc.Families[1], 42), new SolidBrush(Color.FromArgb(255, seasonRed, seasonGreen, seasonBlue)), new Point(340, 40));
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
                                g.DrawString(theItem.DisplayName.ToUpper(), new Font(_pfc.Families[1], 115), new SolidBrush(Color.White), new Point(325, 70));
                            }
                            catch (NullReferenceException)
                            {
                                AppendText("[NullReferenceException] ", Color.Red);
                                AppendText("No ", Color.Black);
                                AppendText("DisplayName ", Color.SteelBlue);
                                AppendText("found", Color.Black, true);
                            } //NAME

                            string pngPath;
                            string textureFile = Path.GetFileName(bundleParser[i].DisplayStyle.DisplayImage.AssetPathName).Substring(0, Path.GetFileName(bundleParser[i].DisplayStyle.DisplayImage.AssetPathName).LastIndexOf('.'));

                            string textureFilePath = string.Empty;
                            if (CurrentUsedPakGuid != null && CurrentUsedPakGuid != "0-0-0-0")
                                textureFilePath = ExtractAsset(CurrentUsedPak, textureFile);
                            else
                                textureFilePath = ExtractAsset(AllpaksDictionary[textureFile], textureFile);

                            if (textureFilePath != null && textureFile == "M_UI_ChallengeTile_PCB")
                            {
                                pngPath = GetRenderSwitchMaterialTexture(textureFile, textureFilePath);

                                Image challengeIcon = Image.FromFile(pngPath);
                                g.DrawImage(Forms.Settings.ResizeImage(challengeIcon, 271, 271), new Point(40, 0)); //327
                            }
                            else if (textureFilePath != null)
                            {
                                MyAsset = new PakAsset(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")));
                                MyAsset.SaveTexture(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")) + ".png");
                                pngPath = textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")) + ".png";
                                UpdateConsole(textureFile + " successfully converted to .PNG", Color.FromArgb(255, 66, 244, 66), "Success");

                                Image challengeIcon;
                                using (var bmpTemp = new Bitmap(pngPath))
                                {
                                    challengeIcon = new Bitmap(bmpTemp);
                                }
                                g.DrawImage(Forms.Settings.ResizeImage(challengeIcon, 271, 271), new Point(40, 0)); //327
                            }
                            #endregion
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                #endregion

                SelectedChallengesArray = new string[bundleParser[i].QuestInfos.Length];
                for (int i2 = 0; i2 < bundleParser[i].QuestInfos.Length; i2++)
                {
                    string cName = Path.GetFileName(bundleParser[i].QuestInfos[i2].QuestDefinition.AssetPathName);
                    SelectedChallengesArray[i2] = cName.Substring(0, cName.LastIndexOf('.'));
                }

                int damageOpCount = 0;
                int damageOpPosition = 0;

                for (int i2 = 0; i2 < SelectedChallengesArray.Length; i2++)
                {
                    try
                    {
                        string challengeFilePath = string.Empty;
                        if (CurrentUsedPakGuid != null && CurrentUsedPakGuid != "0-0-0-0")
                            challengeFilePath = ExtractAsset(CurrentUsedPak, SelectedChallengesArray[i2]);
                        else
                            challengeFilePath = ExtractAsset(AllpaksDictionary[SelectedChallengesArray[i2]], SelectedChallengesArray[i2]);

                        if (challengeFilePath != null)
                        {
                            UpdateConsole(SelectedChallengesArray[i2] + " successfully extracted", Color.FromArgb(255, 66, 244, 66), "Success");
                            if (challengeFilePath.Contains(".uasset") || challengeFilePath.Contains(".uexp") || challengeFilePath.Contains(".ubulk"))
                            {
                                MyAsset = new PakAsset(challengeFilePath.Substring(0, challengeFilePath.LastIndexOf('.')));
                                try
                                {
                                    if (MyAsset.GetSerialized() != null)
                                    {
                                        UpdateConsole(SelectedChallengesArray[i2] + " successfully serialized", Color.FromArgb(255, 66, 244, 66), "Success");

                                        string parsedJson = JToken.Parse(MyAsset.GetSerialized()).ToString();
                                        var questParser = QuestParser.FromJson(parsedJson);
                                        UpdateConsole("Parsing " + SelectedChallengesArray[i2] + "...", Color.FromArgb(255, 244, 132, 66), "Waiting");
                                        for (int ii = 0; ii < questParser.Length; ii++)
                                        {
                                            string oldQuest = string.Empty;
                                            string oldCount = string.Empty;
                                            for (int ii2 = 0; ii2 < questParser[ii].Objectives.Length; ii2++)
                                            {
                                                string newQuest = questParser[ii].Objectives[ii2].Description;
                                                string newCount = questParser[ii].Objectives[ii2].Count.ToString();
                                                if (newQuest != oldQuest && newCount != oldCount)
                                                {
                                                    if (Settings.Default.createIconForChallenges)
                                                    {
                                                        if (questParser[ii].Objectives[ii2].Description == "Deal damage to opponents")
                                                        {
                                                            damageOpCount += 1;
                                                            if (damageOpCount == 1)
                                                            {
                                                                AppendText(questParser[ii].Objectives[ii2].Description, Color.SteelBlue);
                                                                AppendText("\t\tCount: " + questParser[ii].Objectives[ii2].Count, Color.DarkRed, true);

                                                                justSkip += 1;
                                                                iamY += 140;

                                                                g.DrawString(questParser[ii].Objectives[ii2].Description, new Font(_pfc.Families[1], 50), new SolidBrush(Color.White), new Point(100, iamY));
                                                                damageOpPosition = iamY;
                                                                Image slider = Resources.Challenges_Slider;
                                                                g.DrawImage(slider, new Point(108, iamY + 86));
                                                                g.DrawString(questParser[ii].Objectives[ii2].Count.ToString(), new Font(_pfc.Families[0], 20), new SolidBrush(Color.FromArgb(255, 255, 255, 255)), new Point(968, iamY + 87));
                                                                if (justSkip != 1)
                                                                {
                                                                    g.DrawLine(new Pen(Color.FromArgb(30, 255, 255, 255)), 100, iamY - 10, 2410, iamY - 10);
                                                                }

                                                                #region getIcon
                                                                string textureFile = Path.GetFileName(questParser[ii].LargePreviewImage.AssetPathName)?.Substring(0, Path.GetFileName(questParser[ii].LargePreviewImage.AssetPathName).LastIndexOf('.'));

                                                                string textureFilePath = string.Empty;
                                                                if (CurrentUsedPakGuid != null && CurrentUsedPakGuid != "0-0-0-0")
                                                                    textureFilePath = ExtractAsset(CurrentUsedPak, textureFile);
                                                                else
                                                                    textureFilePath = ExtractAsset(AllpaksDictionary[textureFile ?? throw new InvalidOperationException()], textureFile);

                                                                if (textureFilePath != null)
                                                                {
                                                                    MyAsset = new PakAsset(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")));
                                                                    MyAsset.SaveTexture(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")) + ".png");
                                                                    ItemIconPath = textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")) + ".png";
                                                                    UpdateConsole(textureFile + " successfully converted to .PNG", Color.FromArgb(255, 66, 244, 66), "Success");
                                                                }
                                                                else
                                                                    UpdateConsole("Error while extracting " + textureFile, Color.FromArgb(255, 244, 66, 66), "Error");

                                                                if (File.Exists(ItemIconPath))
                                                                {
                                                                    Image itemIcon;
                                                                    using (var bmpTemp = new Bitmap(ItemIconPath))
                                                                    {
                                                                        itemIcon = new Bitmap(bmpTemp);
                                                                    }
                                                                    g.DrawImage(Forms.Settings.ResizeImage(itemIcon, 110, 110), new Point(2300, iamY + 6));
                                                                }
                                                                else
                                                                {
                                                                    Image itemIcon = Resources.unknown512;
                                                                    g.DrawImage(Forms.Settings.ResizeImage(itemIcon, 110, 110), new Point(2300, iamY + 6));
                                                                }
                                                                #endregion
                                                            }
                                                        }
                                                        else
                                                        {
                                                            AppendText(questParser[ii].Objectives[ii2].Description, Color.SteelBlue);
                                                            AppendText("\t\tCount: " + questParser[ii].Objectives[ii2].Count, Color.DarkRed, true);

                                                            justSkip += 1;
                                                            iamY += 140;

                                                            g.DrawString(questParser[ii].Objectives[ii2].Description, new Font(_pfc.Families[1], 50), new SolidBrush(Color.White), new Point(100, iamY));
                                                            Image slider = Resources.Challenges_Slider;
                                                            g.DrawImage(slider, new Point(108, iamY + 86));
                                                            g.DrawString(questParser[ii].Objectives[ii2].Count.ToString(), new Font(_pfc.Families[0], 20), new SolidBrush(Color.FromArgb(255, 255, 255, 255)), new Point(968, iamY + 87));
                                                            if (justSkip != 1)
                                                            {
                                                                g.DrawLine(new Pen(Color.FromArgb(30, 255, 255, 255)), 100, iamY - 10, 2410, iamY - 10);
                                                            }

                                                            #region getIcon
                                                            string textureFile = Path.GetFileName(questParser[ii].LargePreviewImage.AssetPathName)?.Substring(0, Path.GetFileName(questParser[ii].LargePreviewImage.AssetPathName).LastIndexOf('.'));

                                                            string textureFilePath = string.Empty;
                                                            if (CurrentUsedPakGuid != null && CurrentUsedPakGuid != "0-0-0-0")
                                                                textureFilePath = ExtractAsset(CurrentUsedPak, textureFile);
                                                            else
                                                                textureFilePath = ExtractAsset(AllpaksDictionary[textureFile ?? throw new InvalidOperationException()], textureFile);

                                                            if (textureFilePath != null)
                                                            {
                                                                MyAsset = new PakAsset(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")));
                                                                MyAsset.SaveTexture(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")) + ".png");
                                                                ItemIconPath = textureFilePath.Substring(0, textureFilePath.LastIndexOf(".")) + ".png";
                                                                UpdateConsole(textureFile + " successfully converted to .PNG", Color.FromArgb(255, 66, 244, 66), "Success");
                                                            }
                                                            else
                                                                UpdateConsole("Error while extracting " + textureFile, Color.FromArgb(255, 244, 66, 66), "Error");

                                                            if (File.Exists(ItemIconPath))
                                                            {
                                                                Image itemIcon;
                                                                using (var bmpTemp = new Bitmap(ItemIconPath))
                                                                {
                                                                    itemIcon = new Bitmap(bmpTemp);
                                                                }
                                                                g.DrawImage(Forms.Settings.ResizeImage(itemIcon, 110, 110), new Point(2300, iamY + 6));
                                                            }
                                                            else
                                                            {
                                                                Image itemIcon = Resources.unknown512;
                                                                g.DrawImage(Forms.Settings.ResizeImage(itemIcon, 110, 110), new Point(2300, iamY + 6));
                                                            }
                                                            #endregion
                                                        }
                                                    }

                                                    oldQuest = questParser[ii].Objectives[ii2].Description;
                                                    oldCount = questParser[ii].Objectives[ii2].Count.ToString();
                                                }
                                            }
                                        }
                                    }
                                    else
                                        UpdateConsole("No serialized file found", Color.FromArgb(255, 244, 66, 66), "Error");
                                }
                                catch (JsonSerializationException)
                                {
                                    AppendText(CurrentUsedItem + " ", Color.Red);
                                    AppendText(".JSON file can't be displayed", Color.Black, true);
                                }
                            }
                        }
                        else
                            UpdateConsole("Error while extracting " + SelectedChallengesArray[i2], Color.FromArgb(255, 244, 66, 66), "Error");
                    }
                    catch (KeyNotFoundException)
                    {
                        AppendText("Can't extract ", Color.Black);
                        AppendText(SelectedChallengesArray[i2], Color.SteelBlue, true);
                    }
                }

                g.DrawString("Same Quest x" + damageOpCount, new Font(_pfc.Families[1], 50), new SolidBrush(Color.White), new Point(1500, damageOpPosition + 25));
                iamY += 100;

                //BundleCompletionRewards
                try
                {
                    for (int i2 = 0; i2 < bundleParser[i].BundleCompletionRewards.Length; i2++)
                    {
                        string itemReward = bundleParser[i].BundleCompletionRewards[i2].Rewards.FirstOrDefault().ItemDefinition.AssetPathName.Substring(bundleParser[i].BundleCompletionRewards[i2].Rewards.FirstOrDefault().ItemDefinition.AssetPathName.LastIndexOf(".", StringComparison.Ordinal) + 1);
                        string compCount = bundleParser[i].BundleCompletionRewards[i2].CompletionCount.ToString();

                        if (itemReward != "AthenaBattlePass_WeeklyChallenge_Token" && itemReward != "AthenaBattlePass_WeeklyBundle_Token")
                        {
                            justSkip += 1;
                            iamY += 140;

                            if (bundleParser[i].BundleCompletionRewards[i2].Rewards.FirstOrDefault().ItemDefinition.AssetPathName == "None")
                            {
                                var partsofbruhreally = bundleParser[i].BundleCompletionRewards[i2].Rewards.FirstOrDefault().TemplateId.Split(':');
                                DrawRewardBanner(partsofbruhreally[1], g, iamY);
                            }
                            else
                                DrawRewardIcon(itemReward, g, iamY);

                            if (compCount == "-1")
                                g.DrawString("Complete ALL CHALLENGES to earn the reward item", new Font(_pfc.Families[1], 50), new SolidBrush(Color.White), new Point(100, iamY + 22));
                            else
                                g.DrawString("Complete ANY " + compCount + " CHALLENGES to earn the reward item", new Font(_pfc.Families[1], 50), new SolidBrush(Color.White), new Point(100, iamY + 22));
                        }
                    }
                }
                catch (Exception ex)
                {
                    UpdateConsole(ex.Message, Color.FromArgb(255, 244, 66, 66), "Error");
                    iamY -= 100;
                }
            }

            if (Settings.Default.createIconForChallenges)
            {
                #region WATERMARK
                g.FillRectangle(new SolidBrush(Color.FromArgb(100, 0, 0, 0)), new Rectangle(0, iamY + 240, bmp.Width, 40));
                g.DrawString(theItem.DisplayName + " Generated using FModel & JohnWickParse - " + DateTime.Now.ToString("dd/MM/yyyy"), new Font(_pfc.Families[0], 20), new SolidBrush(Color.FromArgb(150, 255, 255, 255)), new Point(bmp.Width / 2, iamY + 250), _centeredString);
                #endregion
                if (v2 == false)
                {
                    #region DRAW TEXT
                    try
                    {
                        string seasonFolder = questJson.Substring(questJson.Substring(0, questJson.LastIndexOf("\\", StringComparison.Ordinal)).LastIndexOf("\\", StringComparison.Ordinal) + 1).ToUpper();
                        g.DrawString(seasonFolder.Substring(0, seasonFolder.LastIndexOf("\\", StringComparison.Ordinal)), new Font(_pfc.Families[1], 42), new SolidBrush(Color.FromArgb(255, 149, 213, 255)), new Point(340, 40));
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
                        g.DrawString(theItem.DisplayName.ToUpper(), new Font(_pfc.Families[1], 115), new SolidBrush(Color.White), new Point(325, 70));
                    }
                    catch (NullReferenceException)
                    {
                        AppendText("[NullReferenceException] ", Color.Red);
                        AppendText("No ", Color.Black);
                        AppendText("DisplayName ", Color.SteelBlue);
                        AppendText("found", Color.Black, true);
                    } //NAME
                    #endregion
                }
                #region CUT IMAGE
                using (Bitmap bmp2 = bmp)
                {
                    var newImg = bmp2.Clone(
                        new Rectangle { X = 0, Y = 0, Width = bmp.Width, Height = iamY + 280 },
                        bmp2.PixelFormat);
                    pictureBox1.Image = newImg;
                } //CUT
                #endregion
            }

            UpdateConsole(theItem.DisplayName, Color.FromArgb(255, 66, 244, 66), "Success");
            if (autoSaveImagesToolStripMenuItem.Checked || updateModeToolStripMenuItem.Checked)
            {
                Invoke(new Action(() =>
                {
                    pictureBox1.Image.Save(DefaultOutputPath + "\\Icons\\" + CurrentUsedItem + ".png", ImageFormat.Png);
                }));
                AppendText(CurrentUsedItem, Color.DarkRed);
                AppendText(" successfully saved", Color.Black, true);
            }

            AppendText("", Color.Black, true);
        }

        private void ConvertTexture2D()
        {
            UpdateConsole(CurrentUsedItem + " is a Texture2D", Color.FromArgb(255, 66, 244, 66), "Success");

            MyAsset = new PakAsset(ExtractedFilePath.Substring(0, ExtractedFilePath.LastIndexOf(".")));
            MyAsset.SaveTexture(ExtractedFilePath.Substring(0, ExtractedFilePath.LastIndexOf(".")) + ".png");
            string imgPath = ExtractedFilePath.Substring(0, ExtractedFilePath.LastIndexOf(".")) + ".png";

            if (File.Exists(imgPath))
            {
                pictureBox1.Image = Image.FromFile(imgPath);
            }

            if (autoSaveImagesToolStripMenuItem.Checked || updateModeToolStripMenuItem.Checked)
            {
                Invoke(new Action(() =>
                {
                    pictureBox1.Image.Save(DefaultOutputPath + "\\Icons\\" + CurrentUsedItem + ".png", ImageFormat.Png);
                }));
                AppendText(CurrentUsedItem, Color.DarkRed);
                AppendText(" successfully saved", Color.Black, true);
            }
        }
        private void ConvertSoundWave()
        {
            UpdateConsole(CurrentUsedItem + " is a Sound", Color.FromArgb(255, 66, 244, 66), "Success");

            string soundPathToConvert = ExtractedFilePath.Substring(0, ExtractedFilePath.LastIndexOf('\\')) + "\\" + CurrentUsedItem + ".uexp";
            UpdateConsole("Converting " + CurrentUsedItem, Color.FromArgb(255, 244, 132, 66), "Processing");
            OpenWithDefaultProgramAndNoFocus(UnrealEngineDataToOgg.ConvertToOgg(soundPathToConvert));
            UpdateConsole("Opening " + CurrentUsedItem + ".ogg", Color.FromArgb(255, 66, 244, 66), "Success");
        }
        private void ConvertToOtf(string file)
        {
            File.Move(file, Path.ChangeExtension(file, ".otf") ?? throw new InvalidOperationException());
            UpdateConsole(CurrentUsedItem + " successfully converter to a font", Color.FromArgb(255, 66, 244, 66), "Success");
        }

        //EVENTS
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            StopWatch = new Stopwatch();
            StopWatch.Start();
            CreateDir();
            ExtractAndSerializeItems(e);
        }
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            StopWatch.Stop();
            if (e.Cancelled)
            {
                UpdateConsole("Canceled!", Color.FromArgb(255, 244, 66, 66), "Error");
            }
            else if (e.Error != null)
            {
                UpdateConsole(e.Error.Message, Color.FromArgb(255, 244, 66, 66), "Error");
            }
            else
            {
                TimeSpan ts = StopWatch.Elapsed;
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                UpdateConsole("Time elapsed: " + elapsedTime, Color.FromArgb(255, 66, 244, 66), "Success");
            }

            SelectedItemsArray = null;
            Invoke(new Action(() =>
            {
                StopButton.Enabled = false;
                OpenImageButton.Enabled = true;
                ExtractButton.Enabled = true;
            }));
        }
        private void ExtractButton_Click(object sender, EventArgs e)
        {
            scintilla1.Text = "";
            pictureBox1.Image = null;
            _questStageDict = new Dictionary<string, long>();
            ExtractButton.Enabled = false;
            OpenImageButton.Enabled = false;
            StopButton.Enabled = true;

            if (backgroundWorker1.IsBusy != true)
            {
                backgroundWorker1.RunWorkerAsync();
            }
        }
        private void StopButton_Click(object sender, EventArgs e)
        {
            if (backgroundWorker1.WorkerSupportsCancellation)
            {
                backgroundWorker1.CancelAsync();
            }
            if (backgroundWorker2.WorkerSupportsCancellation)
            {
                backgroundWorker2.CancelAsync();
            }
        }
        #endregion

        #region IMAGES SAVE & MERGE
        //METHODS
        private void AskMergeImages()
        {
            if (string.IsNullOrEmpty(Settings.Default.mergerFileName))
            {
                MessageBox.Show(@"Please, set a name to your Merger file before trying to merge images

Steps:
	- Load
	- Settings", @"Merger File Name Missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                OpenFileDialog theDialog = new OpenFileDialog();
                theDialog.Multiselect = true;
                theDialog.InitialDirectory = DefaultOutputPath + "\\Icons\\";
                theDialog.Title = @"Choose your images";
                theDialog.Filter = @"PNG Files (*.png)|*.png|JPEG Files (*.jpg)|*.jpg|BMP Files (*.bmp)|*.bmp|All Files (*.*)|*.*";

                Invoke(new Action(() =>
                {
                    if (theDialog.ShowDialog() == DialogResult.OK)
                    {
                        List<Image> selectedImages = new List<Image>();
                        foreach (var files in theDialog.FileNames)
                        {
                            selectedImages.Add(Image.FromFile(files));
                        }

                        MergeSelected(selectedImages);
                    }
                }));
            }
        }
        private void MergeSelected(List<Image> mySelectedImages)
        {
            if (Settings.Default.mergerImagesRow == 0)
            {
                Settings.Default.mergerImagesRow = 7;
                Settings.Default.Save();
            }

            int numperrow = Settings.Default.mergerImagesRow;
            var w = 530 * numperrow;
            if (mySelectedImages.Count * 530 < 530 * numperrow)
            {
                w = mySelectedImages.Count * 530;
            }

            int h = int.Parse(Math.Ceiling(double.Parse(mySelectedImages.Count.ToString()) / numperrow).ToString(CultureInfo.InvariantCulture)) * 530;
            Bitmap bmp = new Bitmap(w - 8, h - 8);

            var num = 1;
            var curW = 0;
            var curH = 0;

            for (int i = 0; i < mySelectedImages.Count; i++)
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.DrawImage(Forms.Settings.ResizeImage(mySelectedImages[i], 522, 522), new PointF(curW, curH));
                    if (num % numperrow == 0)
                    {
                        curW = 0;
                        curH += 530;
                        num += 1;
                    }
                    else
                    {
                        curW += 530;
                        num += 1;
                    }
                }
            }
            bmp.Save(DefaultOutputPath + "\\" + Settings.Default.mergerFileName + ".png", ImageFormat.Png);

            OpenMerged(bmp);
        }
        private void OpenMerged(Bitmap mergedImage)
        {
            if (mergedImage != null)
            {
                var newForm = new Form();
                PictureBox pb = new PictureBox();
                pb.Dock = DockStyle.Fill;
                pb.Image = mergedImage;
                pb.SizeMode = PictureBoxSizeMode.Zoom;

                newForm.WindowState = FormWindowState.Maximized;
                newForm.Size = mergedImage.Size;
                newForm.Icon = Resources.FModel;
                newForm.Text = DefaultOutputPath + @"\" + Settings.Default.mergerFileName + @".png";
                newForm.StartPosition = FormStartPosition.CenterScreen;
                newForm.Controls.Add(pb);
                newForm.Show();
            }
        }

        //EVENTS
        private void OpenImageButton_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                var newForm = new Form();

                PictureBox pb = new PictureBox();
                pb.Dock = DockStyle.Fill;
                pb.Image = pictureBox1.Image;
                pb.SizeMode = PictureBoxSizeMode.Zoom;

                newForm.Size = pictureBox1.Image.Size;
                newForm.Icon = Resources.FModel;
                newForm.Text = CurrentUsedItem;
                newForm.StartPosition = FormStartPosition.CenterScreen;
                newForm.Controls.Add(pb);
                newForm.Show();
            }
        }
        private void saveImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                SaveFileDialog saveTheDialog = new SaveFileDialog();
                saveTheDialog.Title = @"Save Icon";
                saveTheDialog.Filter = @"PNG Files (*.png)|*.png";
                saveTheDialog.InitialDirectory = DefaultOutputPath + "\\Icons\\";
                saveTheDialog.FileName = CurrentUsedItem;
                if (saveTheDialog.ShowDialog() == DialogResult.OK)
                {
                    pictureBox1.Image.Save(saveTheDialog.FileName, ImageFormat.Png);
                    AppendText(CurrentUsedItem, Color.DarkRed);
                    AppendText(" successfully saved", Color.Black, true);
                }
            }
        }
        private async void mergeImagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await Task.Run(() => {
                AskMergeImages();
            });
        }
        #endregion
    }
}
