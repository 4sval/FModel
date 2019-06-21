using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AutoUpdaterDotNET;
using csharp_wick;
using FModel.Converter;
using FModel.Forms;
using FModel.Parser.Challenges;
using FModel.Methods.BackupPAKs.Parser.AESKeyParser;
using FModel.Parser.Items;
using FModel.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Image = System.Drawing.Image;
using Settings = FModel.Properties.Settings;
using System.Text;

namespace FModel
{
    public partial class MainWindow : Form
    {
        #region to refactor
        private static Stopwatch StopWatch { get; set; }
        public static string[] PakAsTxt { get; set; }
        private static Dictionary<string, string> _diffToExtract { get; set; }
        private static string _backupFileName { get; set; }
        private static string[] _backupDynamicKeys { get; set; }
        private static List<string> _itemsToDisplay { get; set; }
        public static string ExtractedFilePath { get; set; }
        public static string[] SelectedItemsArray { get; set; }
        private bool bIsLocres { get; set; }
        private bool differenceFileExists = false;
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            //FModel version
            toolStripStatusLabel1.Text += @" " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString().Substring(0, 5);

            treeView1.Sort();

            //Remove space caused by SizingGrip
            statusStrip1.Padding = new Padding(statusStrip1.Padding.Left, statusStrip1.Padding.Top, statusStrip1.Padding.Left, statusStrip1.Padding.Bottom);

            MyScintilla.ScintillaInstance(scintilla1);
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
        #endregion

        #region LOAD & LEAVE
        /// <summary>
        /// add all paks found to the toolstripmenu as an item
        /// </summary>
        /// <param name="thePaks"></param>
        /// <param name="index"></param>
        private void AddPaKs(string thePak)
        {
            Invoke(new Action(() =>
            {
                loadOneToolStripMenuItem.DropDownItems.Add(thePak);
            }));
        }

        /// <summary>
        /// check if path exists
        /// get all files with extension .pak, add them to the toolstripmenu, read the guid and add them to the right list
        /// </summary>
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
                for (int i = 0; i < yourPaKs.Count(); i++)
                {
                    string arCurrentUsedPak = yourPaKs.ElementAt(i); //SET CURRENT PAK
                    string arCurrentUsedPakGuid = ThePak.ReadPakGuid(Settings.Default.PAKsPath + "\\" + Path.GetFileName(arCurrentUsedPak)); //SET CURRENT PAK GUID

                    if (arCurrentUsedPakGuid == "0-0-0-0")
                    {
                        ThePak.mainPaksList.Add(new PaksEntry(Path.GetFileName(arCurrentUsedPak), arCurrentUsedPakGuid));
                        AddPaKs(Path.GetFileName(arCurrentUsedPak)); //add to toolstrip
                    }
                    if (arCurrentUsedPakGuid != "0-0-0-0")
                    {
                        ThePak.dynamicPaksList.Add(new PaksEntry(Path.GetFileName(arCurrentUsedPak), arCurrentUsedPakGuid));
                        AddPaKs(Path.GetFileName(arCurrentUsedPak)); //add to toolstrip
                    }

                    //IT'S TRIGGERED WHEN FORTNITE IS RUNNING BUT FILES CAN BE READ AND I WANT IT TO BE TRIGGERED WHEN FILE IS FULLY LOCKED AND CAN'T BE USED AT ALL
                    //aka while you're updating the game
                    /*if (!Utilities.IsFileLocked(new System.IO.FileInfo(arCurrentUsedPak)))
                    {
                        string arCurrentUsedPakGuid = ThePak.ReadPakGuid(Settings.Default.PAKsPath + "\\" + Path.GetFileName(arCurrentUsedPak)); //SET CURRENT PAK GUID

                        if (arCurrentUsedPakGuid == "0-0-0-0")
                        {
                            ThePak.mainPaksList.Add(new PaksEntry(Path.GetFileName(arCurrentUsedPak), arCurrentUsedPakGuid));
                            AddPaKs(Path.GetFileName(arCurrentUsedPak)); //add to toolstrip
                        }
                        if (arCurrentUsedPakGuid != "0-0-0-0")
                        {
                            ThePak.dynamicPaksList.Add(new PaksEntry(Path.GetFileName(arCurrentUsedPak), arCurrentUsedPakGuid));
                            AddPaKs(Path.GetFileName(arCurrentUsedPak)); //add to toolstrip
                        }
                    }
                    else { AppendText(Path.GetFileName(arCurrentUsedPak) + " is locked by another process.", Color.Red, true); }*/
                }
            }
        }

        //EVENTS
        private async void MainWindow_Load(object sender, EventArgs e)
        {
            AutoUpdater.Start("https://dl.dropbox.com/s/3kv2pukqu6tj1r0/FModel.xml?dl=0");

            DLLImport.SetTreeViewTheme(treeView1.Handle);

            DynamicKeysManager.deserialize();

            _backupFileName = "\\FortniteGame_" + DateTime.Now.ToString("MMddyyyy") + ".txt";
            ThePak.dynamicPaksList = new List<PaksEntry>();
            ThePak.mainPaksList = new List<PaksEntry>();

            // Copy user settings from previous application version if necessary
            if (Settings.Default.UpdateSettings)
            {
                Settings.Default.Upgrade();
                Settings.Default.UpdateSettings = false;
                Settings.Default.Save();
            }

            await Task.Run(() => {
                FillWithPaKs();
                Utilities.SetOutputFolder();
                Utilities.SetFolderPermission(App.DefaultOutputPath);
                Utilities.JohnWickCheck();
                Utilities.CreateDefaultFolders();
                FontUtilities.SetFont();
            });

            MyScintilla.SetScintillaStyle(scintilla1);
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
        private void Button1_Click(object sender, EventArgs e)
        {
            var aesForms = new Forms.AESManager();
            if (Application.OpenForms[aesForms.Name] == null)
            {
                aesForms.Show();
            }
            else
            {
                Application.OpenForms[aesForms.Name].Focus();
            }
        }
        #endregion

        #region PAKLIST & FILL TREE
        //METHODS
        private void RegisterPaKsinDict(ToolStripItemClickedEventArgs theSinglePak = null, bool loadAllPaKs = false)
        {
            StringBuilder sb = new StringBuilder();
            ThePak.CurrentUsedPak = null;
            ThePak.CurrentUsedPakGuid = null;
            bool bMainKeyWorking = false;
            bIsLocres = false;

            for (int i = 0; i < ThePak.mainPaksList.Count; i++)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(Settings.Default.AESKey))
                    {
                        JohnWick.MyExtractor = new PakExtractor(Settings.Default.PAKsPath + "\\" + ThePak.mainPaksList[i].thePak, Settings.Default.AESKey);
                    }
                    else { break; }
                }
                catch (Exception)
                {
                    break;
                }

                string[] CurrentUsedPakLines = JohnWick.MyExtractor.GetFileList().ToArray();
                if (CurrentUsedPakLines != null)
                {
                    bMainKeyWorking = true;

                    JohnWick.MyKey = Settings.Default.AESKey;
                    string mountPoint = JohnWick.MyExtractor.GetMountPoint();
                    ThePak.PaksMountPoint.Add(ThePak.mainPaksList[i].thePak, mountPoint.Substring(9));

                    for (int ii = 0; ii < CurrentUsedPakLines.Length; ii++)
                    {
                        CurrentUsedPakLines[ii] = mountPoint.Substring(6) + CurrentUsedPakLines[ii];

                        string CurrentUsedPakFileName = CurrentUsedPakLines[ii].Substring(CurrentUsedPakLines[ii].LastIndexOf("/", StringComparison.Ordinal) + 1);
                        if (CurrentUsedPakFileName.Contains(".uasset") || CurrentUsedPakFileName.Contains(".uexp") || CurrentUsedPakFileName.Contains(".ubulk"))
                        {
                            if (!ThePak.AllpaksDictionary.ContainsKey(CurrentUsedPakFileName.Substring(0, CurrentUsedPakFileName.LastIndexOf(".", StringComparison.Ordinal))))
                            {
                                ThePak.AllpaksDictionary.Add(CurrentUsedPakFileName.Substring(0, CurrentUsedPakFileName.LastIndexOf(".", StringComparison.Ordinal)), ThePak.mainPaksList[i].thePak);
                            }
                        }
                        else
                        {
                            if (!ThePak.AllpaksDictionary.ContainsKey(CurrentUsedPakFileName))
                            {
                                ThePak.AllpaksDictionary.Add(CurrentUsedPakFileName, ThePak.mainPaksList[i].thePak);
                            }
                        }

                        if (loadAllPaKs)
                        {
                            sb.Append(CurrentUsedPakLines[ii] + "\n");
                        }
                    }

                    if (loadAllPaKs) { UpdateConsole(".PAK mount point: " + mountPoint.Substring(9), Color.FromArgb(255, 244, 132, 66), "Waiting"); }
                    if (theSinglePak != null && ThePak.mainPaksList[i].thePak == theSinglePak.ClickedItem.Text) { PakAsTxt = CurrentUsedPakLines; }
                }
            }
            if (bMainKeyWorking) { LoadLocRes.LoadMySelectedLocRes(Settings.Default.IconLanguage); }

            if (theSinglePak != null) //IMPORTANT: IT STILLS LOAD THE DICTIONARY -> IT'S GONNA BE USEFUL FOR TRANSLATIONS
            {
                ThePak.CurrentUsedPak = theSinglePak.ClickedItem.Text;
                ThePak.CurrentUsedPakGuid = ThePak.ReadPakGuid(Settings.Default.PAKsPath + "\\" + ThePak.CurrentUsedPak);

                if (ThePak.CurrentUsedPakGuid != "0-0-0-0") //LOADING DYNAMIC PAK
                {
                    if (DynamicKeysManager.AESEntries != null)
                    {
                        foreach (AESEntry s in DynamicKeysManager.AESEntries)
                        {
                            if (s.thePak == ThePak.CurrentUsedPak && s.theKey.Length > 2)
                            {
                                try
                                {
                                    JohnWick.MyExtractor = new PakExtractor(Settings.Default.PAKsPath + "\\" + ThePak.CurrentUsedPak, s.theKey);

                                    PakAsTxt = JohnWick.MyExtractor.GetFileList().ToArray();
                                    if (PakAsTxt != null)
                                    {
                                        JohnWick.MyKey = s.theKey;
                                        string mountPoint = JohnWick.MyExtractor.GetMountPoint();
                                        ThePak.PaksMountPoint.Add(ThePak.CurrentUsedPak, mountPoint.Substring(9));

                                        for (int i = 0; i < PakAsTxt.Length; i++)
                                        {
                                            PakAsTxt[i] = mountPoint.Substring(6) + PakAsTxt[i];
                                        }
                                    }
                                }
                                catch (Exception)
                                {
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            if (loadAllPaKs)
            {
                File.WriteAllText(App.DefaultOutputPath + "\\FortnitePAKs.txt", sb.ToString()); //File will always exist
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
            PakAsTxt = File.ReadAllLines(App.DefaultOutputPath + "\\FortnitePAKs.txt");
            File.Delete(App.DefaultOutputPath + "\\FortnitePAKs.txt");

            //ASK DIFFERENCE FILE AND COMPARE
            OpenFileDialog theDialog = new OpenFileDialog();
            theDialog.Title = @"Choose your Backup PAK File";
            theDialog.InitialDirectory = App.DefaultOutputPath + "\\Backup";
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

                    IEnumerable<String> onlyB = PakAsTxt.Except(linesA);
                    IEnumerable<String> removed = linesA.Except(PakAsTxt);

                    File.WriteAllLines(App.DefaultOutputPath + "\\Result.txt", onlyB);
                    File.WriteAllLines(App.DefaultOutputPath + "\\Removed.txt", removed);
                }
            }));

            //GET REMOVED FILES
            if (File.Exists(App.DefaultOutputPath + "\\Removed.txt"))
            {
                var removedTxt = File.ReadAllLines(App.DefaultOutputPath + "\\Removed.txt");
                File.Delete(App.DefaultOutputPath + "\\Removed.txt");

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
            }
            else
            {
                AppendText("Canceled! ", Color.Red);
                AppendText("All pak files loaded...", Color.Black, true);
                return;
            }

            if (File.Exists(App.DefaultOutputPath + "\\Result.txt"))
            {
                PakAsTxt = File.ReadAllLines(App.DefaultOutputPath + "\\Result.txt");
                File.Delete(App.DefaultOutputPath + "\\Result.txt");
                differenceFileExists = true;
            }
        }
        private void CreatePakList(ToolStripItemClickedEventArgs selectedPak = null, bool loadAllPaKs = false, bool getDiff = false, bool updateMode = false)
        {
            ThePak.AllpaksDictionary = new Dictionary<string, string>();
            _diffToExtract = new Dictionary<string, string>();
            ThePak.PaksMountPoint = new Dictionary<string, string>();
            PakAsTxt = null;

            if (selectedPak != null)
            {
                UpdateConsole(Settings.Default.PAKsPath + "\\" + selectedPak.ClickedItem.Text, Color.FromArgb(255, 244, 132, 66), "Loading");

                //ADD TO DICTIONNARY
                RegisterPaKsinDict(selectedPak);

                if (PakAsTxt != null)
                {
                    Invoke(new Action(() =>
                    {
                        treeView1.BeginUpdate();
                        for (int i = 0; i < PakAsTxt.Length; i++)
                        {
                            TreeParsePath(treeView1.Nodes, PakAsTxt[i].Replace(PakAsTxt[i].Split('/').Last(), ""));
                        }
                        treeView1.EndUpdate();
                    }));
                    UpdateConsole(Settings.Default.PAKsPath + "\\" + selectedPak.ClickedItem.Text, Color.FromArgb(255, 66, 244, 66), "Success");
                }
                else
                    UpdateConsole("Please, provide a working key in the AES Manager for " + selectedPak.ClickedItem.Text, Color.FromArgb(255, 244, 66, 66), "Error");
            }
            if (loadAllPaKs)
            {
                //ADD TO DICTIONNARY
                RegisterPaKsinDict(null, true);

                if (new System.IO.FileInfo(App.DefaultOutputPath + "\\FortnitePAKs.txt").Length <= 0) //File will always exist so we check the file size instead
                {
                    File.Delete(App.DefaultOutputPath + "\\FortnitePAKs.txt");
                    UpdateConsole("Can't read .PAK files with this key", Color.FromArgb(255, 244, 66, 66), "Error");
                }
                else
                {
                    PakAsTxt = File.ReadAllLines(App.DefaultOutputPath + "\\FortnitePAKs.txt");
                    File.Delete(App.DefaultOutputPath + "\\FortnitePAKs.txt");

                    Invoke(new Action(() =>
                    {
                        treeView1.BeginUpdate();
                        for (int i = 0; i < PakAsTxt.Length; i++)
                        {
                            TreeParsePath(treeView1.Nodes, PakAsTxt[i].Replace(PakAsTxt[i].Split('/').Last(), ""));
                        }
                        treeView1.EndUpdate();
                    }));
                    UpdateConsole(Settings.Default.PAKsPath, Color.FromArgb(255, 66, 244, 66), "Success");
                }
            }
            if (getDiff)
            {
                //ADD TO DICTIONNARY
                RegisterPaKsinDict(null, true);

                if (new System.IO.FileInfo(App.DefaultOutputPath + "\\FortnitePAKs.txt").Length <= 0)
                {
                    UpdateConsole("Can't read .PAK files with this key", Color.FromArgb(255, 244, 66, 66), "Error");
                }
                else
                {
                    UpdateConsole("Comparing files...", Color.FromArgb(255, 244, 132, 66), "Loading");
                    ComparePaKs();
                    if (updateMode && differenceFileExists)
                    {
                        UmFilter(PakAsTxt, _diffToExtract);
                        Checking.UmWorking = true;
                    }

                    Invoke(new Action(() =>
                    {
                        treeView1.BeginUpdate();
                        for (int i = 0; i < PakAsTxt.Length; i++)
                        {
                            TreeParsePath(treeView1.Nodes, PakAsTxt[i].Replace(PakAsTxt[i].Split('/').Last(), ""));
                        }
                        treeView1.EndUpdate();
                    }));

                    differenceFileExists = false;
                    UpdateConsole("Files compared", Color.FromArgb(255, 66, 244, 66), "Success");
                }
            }
        }
        private void CreateBackupList()
        {
            _backupDynamicKeys = null;
            StringBuilder sb = new StringBuilder();

            if (DLLImport.IsInternetAvailable() && (!string.IsNullOrWhiteSpace(Settings.Default.eEmail) || !string.IsNullOrWhiteSpace(Settings.Default.ePassword)))
            {
                string myContent = DynamicPAKs.GetEndpoint("https://fortnite-public-service-prod11.ol.epicgames.com/fortnite/api/storefront/v2/keychain", true);

                if (myContent.Contains("\"errorCode\": \"errors.com.epicgames.common.authentication.authentication_failed\""))
                {
                    AppendText("EPIC Authentication Failed.", Color.Red, true);
                }
                else
                {
                    AppendText("Successfully Authenticated.", Color.Green, true);
                    _backupDynamicKeys = AesKeyParser.FromJson(myContent);
                }
            }

            for (int i = 0; i < ThePak.mainPaksList.Count; i++)
            {
                try
                {
                    JohnWick.MyExtractor = new PakExtractor(Settings.Default.PAKsPath + "\\" + ThePak.mainPaksList[i].thePak, Settings.Default.AESKey);
                }
                catch (Exception)
                {
                    AppendText("0x" + Settings.Default.AESKey + " doesn't work with the main paks.", Color.Red, true);
                    break;
                }

                string[] CurrentUsedPakLines = JohnWick.MyExtractor.GetFileList().ToArray();
                if (CurrentUsedPakLines != null)
                {
                    for (int ii = 0; ii < CurrentUsedPakLines.Length; ii++)
                    {
                        CurrentUsedPakLines[ii] = JohnWick.MyExtractor.GetMountPoint().Substring(6) + CurrentUsedPakLines[ii];

                        sb.Append(CurrentUsedPakLines[ii] + "\n");
                    }
                    UpdateConsole(".PAK mount point: " + JohnWick.MyExtractor.GetMountPoint().Substring(9), Color.FromArgb(255, 244, 132, 66), "Waiting");
                }
            }

            for (int i = 0; i < ThePak.dynamicPaksList.Count; i++)
            {
                if (_backupDynamicKeys != null)
                {
                    string oldGuid = string.Empty;
                    foreach (string myString in _backupDynamicKeys)
                    {
                        string[] parts = myString.Split(':');
                        string newGuid = DynamicPAKs.getPakGuidFromKeychain(parts);

                        /***
                         * if same guid several time in keychain do not backup twice
                         * it works fine that way because of the loop through all the paks
                         * even if in keychain we do "found 1004" -> "found 1001" -> "found 1004" through the paks we do 1000 -> 1001 -> 1002...
                        ***/
                        if (newGuid == ThePak.dynamicPaksList[i].thePakGuid && oldGuid != newGuid)
                        {
                            byte[] bytes = Convert.FromBase64String(parts[1]);
                            string aeskey = BitConverter.ToString(bytes).Replace("-", "");
                            oldGuid = newGuid;

                            try
                            {
                                JohnWick.MyExtractor = new PakExtractor(Settings.Default.PAKsPath + "\\" + ThePak.dynamicPaksList[i].thePak, aeskey);
                            }
                            catch (Exception)
                            {
                                AppendText("0x" + aeskey + " doesn't work with " + ThePak.dynamicPaksList[i].thePak, Color.Red, true); //this should never be triggered
                                continue;
                            }

                            string[] CurrentUsedPakLines = JohnWick.MyExtractor.GetFileList().ToArray();
                            if (CurrentUsedPakLines != null)
                            {
                                for (int ii = 0; ii < CurrentUsedPakLines.Length; ii++)
                                {
                                    CurrentUsedPakLines[ii] = JohnWick.MyExtractor.GetMountPoint().Substring(6) + CurrentUsedPakLines[ii];

                                    sb.Append(CurrentUsedPakLines[ii] + "\n");
                                }
                                AppendText("Backing up ", Color.Black);
                                AppendText(ThePak.dynamicPaksList[i].thePak, Color.DarkRed, true);
                            }
                        }
                    }
                }
            }

            File.WriteAllText(App.DefaultOutputPath + "\\Backup" + _backupFileName, sb.ToString()); //File will always exist so we check the file size instead
            if (new System.IO.FileInfo(App.DefaultOutputPath + "\\Backup" + _backupFileName).Length > 0)
            {
                UpdateConsole("\\Backup" + _backupFileName + " successfully created", Color.FromArgb(255, 66, 244, 66), "Success");
            }
            else
            {
                File.Delete(App.DefaultOutputPath + "\\Backup" + _backupFileName);
                UpdateConsole("Can't create " + _backupFileName.Substring(1), Color.FromArgb(255, 244, 66, 66), "Error");
            }
        }
        private void UpdateModeExtractSave()
        {
            CreatePakList(null, false, true, true);

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
                JohnWick.myArray = null;
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
            JohnWick.myArray = null;
            Invoke(new Action(() =>
            {
                scintilla1.Text = "";
                pictureBox1.Image = null;

                treeView1.Nodes.Clear(); //SMH HERE IT DOESN'T LAG
                listBox1.Items.Clear();
            }));

            if (!differenceModeToolStripMenuItem.Checked)
            {
                await Task.Run(() => {
                    CreatePakList(null, true);
                });
            }
            if (differenceModeToolStripMenuItem.Checked && !updateModeToolStripMenuItem.Checked)
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
                CreateBackupList();
            });
        }
        //UPDATE MODE
        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            StopWatch = new Stopwatch();
            StopWatch.Start();
            Utilities.CreateDefaultFolders();
            RegisterInArray(e, true);
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
            else if (Checking.UmWorking == false)
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
            Checking.UmWorking = false;
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

            var dirfiles = PakAsTxt.Where(x => x.StartsWith(full) && !x.Replace(full, "").Contains("/"));
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

                    var dirfiles = PakAsTxt.Where(x => x.StartsWith(full) && !x.Replace(full, "").Contains("/"));
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
            else if (SearchFiles.FilesToSearch)
            {
                AddAndSelectAllItems(SearchFiles.myItems);
            }
        }
        private void AddAndSelectAllItems(string[] myItemsToAdd)
        {
            listBox1.BeginUpdate();
            listBox1.Items.Clear();
            for (int i = 0; i < myItemsToAdd.Length; i++)
            {
                listBox1.Items.Add(myItemsToAdd[i]);
            }
            for (int i = 0; i < listBox1.Items.Count; i++) { listBox1.SetSelected(i, true); }
            listBox1.EndUpdate();

            //same as click on extract button
            scintilla1.Text = "";
            pictureBox1.Image = null;
            ExtractButton.Enabled = false;
            OpenImageButton.Enabled = false;
            StopButton.Enabled = true;

            if (backgroundWorker1.IsBusy != true)
            {
                backgroundWorker1.RunWorkerAsync();
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
        private void RegisterInArray(DoWorkEventArgs e, bool updateMode = false)
        {
            if (updateMode)
            {
                Invoke(new Action(() =>
                {
                    SelectedItemsArray = new string[_diffToExtract.Count];
                    for (int i = 0; i < _diffToExtract.Count; i++) //ADD DICT ITEM TO ARRAY
                    {
                        SelectedItemsArray[i] = _diffToExtract.Keys.ElementAt(i);
                    }
                }));
            }
            else
            {
                Invoke(new Action(() =>
                {
                    SelectedItemsArray = new string[listBox1.SelectedItems.Count];
                    for (int i = 0; i < listBox1.SelectedItems.Count; i++) //ADD SELECTED ITEM TO ARRAY
                    {
                        SelectedItemsArray[i] = listBox1.SelectedItems[i].ToString();
                    }
                }));
            }

            ExtractAndSerializeItems(e);
        }
        private void ExtractAndSerializeItems(DoWorkEventArgs e)
        {
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

                ThePak.CurrentUsedItem = SelectedItemsArray[i];

                if (ThePak.CurrentUsedPakGuid != null && ThePak.CurrentUsedPakGuid != "0-0-0-0")
                    ExtractedFilePath = JohnWick.ExtractAsset(ThePak.CurrentUsedPak, ThePak.CurrentUsedItem);
                else
                    ExtractedFilePath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary[ThePak.CurrentUsedItem], ThePak.CurrentUsedItem);

                if (ExtractedFilePath != null)
                {
                    bIsLocres = false;
                    UpdateConsole(ThePak.CurrentUsedItem + " successfully extracted", Color.FromArgb(255, 66, 244, 66), "Success");
                    if (ExtractedFilePath.Contains(".uasset") || ExtractedFilePath.Contains(".uexp") || ExtractedFilePath.Contains(".ubulk"))
                    {
                        JohnWick.MyAsset = new PakAsset(ExtractedFilePath.Substring(0, ExtractedFilePath.LastIndexOf('.')));
                        JsonParseFile();
                    }
                    if (ExtractedFilePath.Contains(".ufont"))
                        ConvertToTtf(ExtractedFilePath);
                    if (ExtractedFilePath.Contains(".ini"))
                    {
                        Invoke(new Action(() =>
                        {
                            scintilla1.Text = File.ReadAllText(ExtractedFilePath);
                        }));
                    }
                    if (ExtractedFilePath.Contains(".locres") && !ExtractedFilePath.Contains("EngineOverrides"))
                    {
                        SerializeLocRes();
                    }
                }
                else
                    UpdateConsole("Error while extracting " + ThePak.CurrentUsedItem, Color.FromArgb(255, 244, 66, 66), "Error");
            }
        }
        private void JsonParseFile()
        {
            if (JohnWick.MyAsset.GetSerialized() != null)
            {
                UpdateConsole(ThePak.CurrentUsedItem + " successfully serialized", Color.FromArgb(255, 66, 244, 66), "Success");

                Invoke(new Action(() =>
                {
                    try
                    {
                        scintilla1.Text = JToken.Parse(JohnWick.MyAsset.GetSerialized()).ToString();
                    }
                    catch (JsonReaderException)
                    {
                        AppendText(ThePak.CurrentUsedItem + " ", Color.Red);
                        AppendText(".JSON file can't be displayed", Color.Black, true);
                    }
                }));

                NavigateThroughJson(JohnWick.MyAsset, ExtractedFilePath);
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

                UpdateConsole("Parsing " + ThePak.CurrentUsedItem + "...", Color.FromArgb(255, 244, 132, 66), "Waiting");
                for (int i = 0; i < itemId.Length; i++)
                {
                    if (Settings.Default.createIconForCosmetics && itemId[i].ExportType.Contains("Athena") && itemId[i].ExportType.Contains("Item") && itemId[i].ExportType.Contains("Definition"))
                    {
                        pictureBox1.Image = CreateItemIcon(itemId[i], "athIteDef");
                    }
                    else if (Settings.Default.createIconForConsumablesWeapons && (itemId[i].ExportType == "FortWeaponRangedItemDefinition" || itemId[i].ExportType == "FortWeaponMeleeItemDefinition"))
                    {
                        pictureBox1.Image = CreateItemIcon(itemId[i], "consAndWeap");
                    }
                    else if (Settings.Default.createIconForTraps && (itemId[i].ExportType == "FortTrapItemDefinition" || itemId[i].ExportType == "FortContextTrapItemDefinition"))
                    {
                        pictureBox1.Image = CreateItemIcon(itemId[i]);
                    }
                    else if (Settings.Default.createIconForVariants && (itemId[i].ExportType == "FortVariantTokenType"))
                    {
                        pictureBox1.Image = CreateItemIcon(itemId[i], "variant");
                    }
                    else if (Settings.Default.createIconForAmmo && (itemId[i].ExportType == "FortAmmoItemDefinition"))
                    {
                        pictureBox1.Image = CreateItemIcon(itemId[i], "ammo");
                    }
                    else if (questJson != null && (Settings.Default.createIconForSTWHeroes && (itemId[i].ExportType == "FortHeroType" && (questJson.Contains("ItemDefinition") || questJson.Contains("TestDefsSkydive") || questJson.Contains("GameplayPrototypes"))))) //Contains x not to trigger HID from BR
                    {
                        pictureBox1.Image = CreateItemIcon(itemId[i], "stwHeroes");
                    }
                    else if (Settings.Default.createIconForSTWDefenders && (itemId[i].ExportType == "FortDefenderItemDefinition"))
                    {
                        pictureBox1.Image = CreateItemIcon(itemId[i], "stwDefenders");
                    }
                    else if (Settings.Default.createIconForSTWCardPacks && (itemId[i].ExportType == "FortCardPackItemDefinition"))
                    {
                        pictureBox1.Image = CreateItemIcon(itemId[i]);
                    }
                    else if (Settings.Default.createIconForCreativeGalleries && (itemId[i].ExportType == "FortPlaysetGrenadeItemDefinition"))
                    {
                        pictureBox1.Image = CreateItemIcon(itemId[i]);
                    }
                    else if (itemId[i].ExportType == "FortChallengeBundleItemDefinition")
                    {
                        CreateBundleChallengesIcon(itemId[i], parsedJson, questJson);
                    }
                    else if (itemId[i].ExportType == "Texture2D") { ConvertTexture2D(); }
                    else if (itemId[i].ExportType == "SoundWave") { ConvertSoundWave(); }
                    else { UpdateConsole(ThePak.CurrentUsedItem + " successfully extracted", Color.FromArgb(255, 66, 244, 66), "Success"); }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private Bitmap CreateItemIcon(ItemsIdParser theItem, string specialMode = null)
        {
            UpdateConsole(ThePak.CurrentUsedItem + " is an Item Definition", Color.FromArgb(255, 66, 244, 66), "Success");

            Bitmap bmp = new Bitmap(522, 522);
            Graphics g = Graphics.FromImage(bmp);
            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            Rarity.DrawRarity(theItem, g, specialMode);

            ItemIcon.ItemIconPath = string.Empty;
            ItemIcon.GetItemIcon(theItem, Settings.Default.loadFeaturedImage);
            if (File.Exists(ItemIcon.ItemIconPath))
            {
                Image itemIcon;
                using (var bmpTemp = new Bitmap(ItemIcon.ItemIconPath))
                {
                    itemIcon = new Bitmap(bmpTemp);
                }
                g.DrawImage(ImageUtilities.ResizeImage(itemIcon, 512, 512), new Point(5, 5));
            }
            else
            {
                Image itemIcon = Resources.unknown512;
                g.DrawImage(itemIcon, new Point(0, 0));
            }

            ItemIcon.DrawWatermark(g);

            Image bg512 = Resources.BG512;
            g.DrawImage(bg512, new Point(5, 383));

            DrawText.DrawTexts(theItem, g, specialMode);

            if (autoSaveImagesToolStripMenuItem.Checked || updateModeToolStripMenuItem.Checked)
            {
                bmp.Save(App.DefaultOutputPath + "\\Icons\\" + ThePak.CurrentUsedItem + ".png", ImageFormat.Png);

                if (File.Exists(App.DefaultOutputPath + "\\Icons\\" + ThePak.CurrentUsedItem + ".png"))
                {
                    AppendText(ThePak.CurrentUsedItem, Color.DarkRed);
                    AppendText(" successfully saved", Color.Black, true);
                }
            }
            g.Dispose();
            return bmp;
        }


        /// <summary>
        /// this is the main method that draw the bundle of challenges
        /// </summary>
        /// <param name="theItem"> needed for the DisplayName </param>
        /// <param name="theParsedJson"> to parse from this instead of calling MyAsset.GetSerialized() again </param>
        /// <param name="extractedBundlePath"> needed for the LastFolder </param>
        /// <returns> the bundle image ready to be displayed in pictureBox1 </returns>
        private void CreateBundleChallengesIcon(ItemsIdParser theItem, string theParsedJson, string extractedBundlePath)
        {
            ChallengeBundleIdParser bundleParser = ChallengeBundleIdParser.FromJson(theParsedJson).FirstOrDefault();
            BundleInfos.getBundleData(bundleParser);
            Bitmap bmp = null;
            bool isFortbyte = false;

            if (Settings.Default.createIconForChallenges)
            {
                bmp = new Bitmap(2500, 15000);
                BundleDesign.BundlePath = extractedBundlePath;
                BundleDesign.theY = 275;
                BundleDesign.toDrawOn = Graphics.FromImage(bmp);
                BundleDesign.toDrawOn.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                BundleDesign.toDrawOn.SmoothingMode = SmoothingMode.HighQuality;
                BundleDesign.myItem = theItem;

                BundleDesign.drawBackground(bmp, bundleParser);
            }

            if (BundleInfos.BundleData[0].rewardItemId != null && string.Equals(BundleInfos.BundleData[0].rewardItemId, "AthenaFortbyte", StringComparison.CurrentCultureIgnoreCase))
                isFortbyte = true;

            // Fortbytes: Sort by number
            if (isFortbyte)
            {
                BundleInfos.BundleData.Sort(delegate (BundleInfoEntry a, BundleInfoEntry b)
                {
                    return Int32.Parse(a.rewardItemQuantity).CompareTo(Int32.Parse(b.rewardItemQuantity));
                });
            }

            for (int i = 0; i < BundleInfos.BundleData.Count; i++)
            {
                AppendText(BundleInfos.BundleData[i].questDescr, Color.SteelBlue);
                AppendText("\t\tCount: " + BundleInfos.BundleData[i].questCount, Color.DarkRed);
                AppendText("\t\t" + BundleInfos.BundleData[i].rewardItemId + ":" + BundleInfos.BundleData[i].rewardItemQuantity, Color.DarkGreen, true);

                if (Settings.Default.createIconForChallenges)
                {
                    BundleDesign.theY += 140;

                    //in case you wanna make some changes
                    //BundleDesign.toDrawOn.DrawRectangle(new Pen(new SolidBrush(Color.Red)), new Rectangle(107, BundleDesign.theY + 7, 2000, 93)); //rectangle that resize the font -> used for "Font goodFont = "
                    //BundleDesign.toDrawOn.DrawRectangle(new Pen(new SolidBrush(Color.Blue)), new Rectangle(107, BundleDesign.theY + 7, 2000, 75)); //rectangle the font needs to be fit with
                    
                    //draw quest description
                    Font goodFont = FontUtilities.FindFont(BundleDesign.toDrawOn, BundleInfos.BundleData[i].questDescr, new Rectangle(107, BundleDesign.theY + 7, 2000, 93).Size, new Font(FontUtilities.pfc.Families[1], 50)); //size in "new Font()" is never check
                    BundleDesign.toDrawOn.DrawString(BundleInfos.BundleData[i].questDescr, goodFont, new SolidBrush(Color.White), new Point(100, BundleDesign.theY));

                    //draw slider + quest count
                    Image slider = Resources.Challenges_Slider;
                    BundleDesign.toDrawOn.DrawImage(slider, new Point(108, BundleDesign.theY + 86));
                    BundleDesign.toDrawOn.DrawString(BundleInfos.BundleData[i].questCount.ToString(), new Font(FontUtilities.pfc.Families[0], 20), new SolidBrush(Color.FromArgb(255, 255, 255, 255)), new Point(968, BundleDesign.theY + 87));

                    //draw quest reward
                    DrawingRewards.getRewards(BundleInfos.BundleData[i].rewardItemId, BundleInfos.BundleData[i].rewardItemQuantity);

                    if (i != 0)
                    {
                        //draw separator
                        BundleDesign.toDrawOn.DrawLine(new Pen(Color.FromArgb(30, 255, 255, 255)), 100, BundleDesign.theY - 10, 2410, BundleDesign.theY - 10);
                    }
                }
            }
            AppendText("", Color.Black, true);

            if (Settings.Default.createIconForChallenges)
            {
                BundleDesign.drawCompletionReward(bundleParser);
                BundleDesign.drawWatermark(bmp);

                //cut if too long and return the bitmap
                using (Bitmap bmp2 = bmp)
                {
                    var newImg = bmp2.Clone(
                        new Rectangle { X = 0, Y = 0, Width = bmp.Width, Height = BundleDesign.theY + 280 },
                        bmp2.PixelFormat);

                    pictureBox1.Image = newImg;
                }
            }

            UpdateConsole(theItem.DisplayName.SourceString, Color.FromArgb(255, 66, 244, 66), "Success");
            if (autoSaveImagesToolStripMenuItem.Checked || updateModeToolStripMenuItem.Checked)
            {
                Invoke(new Action(() =>
                {
                    pictureBox1.Image.Save(App.DefaultOutputPath + "\\Icons\\" + ThePak.CurrentUsedItem + ".png", ImageFormat.Png);
                }));

                if (File.Exists(App.DefaultOutputPath + "\\Icons\\" + ThePak.CurrentUsedItem + ".png"))
                {
                    AppendText(ThePak.CurrentUsedItem, Color.DarkRed);
                    AppendText(" successfully saved", Color.Black, true);
                }
            }
            BundleDesign.toDrawOn.Dispose(); //actually this is the most useful thing in this method
        }

        /// <summary>
        /// because the filename is usually the same for each language, John Wick extract all of them
        /// but then i have to manually get the right path from the treeview
        /// TODO: find bug for EngineOverrides
        /// </summary>
        private void SerializeLocRes()
        {
            Invoke(new Action(() =>
            {
                string treeviewPath = treeView1.SelectedNode.FullPath;
                if (treeviewPath.StartsWith("..\\")) { treeviewPath = treeviewPath.Substring(3); } //if loading all paks

                string filePath = App.DefaultOutputPath + "\\Extracted\\" + treeviewPath + "\\" + listBox1.SelectedItem;
                if (File.Exists(filePath))
                {
                    bIsLocres = true;
                    scintilla1.Text = LocResSerializer.StringFinder(filePath);
                }
                else
                {
                    bIsLocres = false;
                    AppendText("Error while searching " + listBox1.SelectedItem, Color.DarkRed, true);
                }
            }));
        }

        private void ConvertTexture2D()
        {
            UpdateConsole(ThePak.CurrentUsedItem + " is a Texture2D", Color.FromArgb(255, 66, 244, 66), "Success");

            JohnWick.MyAsset = new PakAsset(ExtractedFilePath.Substring(0, ExtractedFilePath.LastIndexOf(".", StringComparison.Ordinal)));
            JohnWick.MyAsset.SaveTexture(ExtractedFilePath.Substring(0, ExtractedFilePath.LastIndexOf(".", StringComparison.Ordinal)) + ".png");
            string imgPath = ExtractedFilePath.Substring(0, ExtractedFilePath.LastIndexOf(".", StringComparison.Ordinal)) + ".png";

            if (File.Exists(imgPath))
            {
                Image img;
                using (var bmpTemp = new Bitmap(imgPath))
                {
                    img = new Bitmap(bmpTemp);
                }
                pictureBox1.Image = img;
            }

            if (autoSaveImagesToolStripMenuItem.Checked || updateModeToolStripMenuItem.Checked)
            {
                Invoke(new Action(() =>
                {
                    pictureBox1.Image.Save(App.DefaultOutputPath + "\\Icons\\" + ThePak.CurrentUsedItem + ".png", ImageFormat.Png);
                }));
                AppendText(ThePak.CurrentUsedItem, Color.DarkRed);
                AppendText(" successfully saved", Color.Black, true);
            }
        }
        private void ConvertSoundWave()
        {
            UpdateConsole(ThePak.CurrentUsedItem + " is a Sound", Color.FromArgb(255, 66, 244, 66), "Success");

            string soundPathToConvert = ExtractedFilePath.Substring(0, ExtractedFilePath.LastIndexOf('\\')) + "\\" + ThePak.CurrentUsedItem + ".uexp";
            string soundPathConverted = UnrealEngineDataToOgg.ConvertToOgg(soundPathToConvert);
            UpdateConsole("Converting " + ThePak.CurrentUsedItem, Color.FromArgb(255, 244, 132, 66), "Processing");

            if (File.Exists(soundPathConverted))
            {
                Utilities.OpenWithDefaultProgramAndNoFocus(soundPathConverted);
                UpdateConsole("Opening " + ThePak.CurrentUsedItem + ".ogg", Color.FromArgb(255, 66, 244, 66), "Success");
            }
            else
                UpdateConsole("Couldn't convert " + ThePak.CurrentUsedItem, Color.FromArgb(255, 244, 66, 66), "Error");
        }

        /// <summary>
        /// todo: overwrite existing extracted font
        /// </summary>
        /// <param name="file"></param>
        private void ConvertToTtf(string file)
        {
            File.Move(file, Path.ChangeExtension(file, ".ttf") ?? throw new InvalidOperationException());
            UpdateConsole(ThePak.CurrentUsedItem + " successfully converter to a font", Color.FromArgb(255, 66, 244, 66), "Success");
        }

        //EVENTS
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            StopWatch = new Stopwatch();
            StopWatch.Start();
            Utilities.CreateDefaultFolders();
            RegisterInArray(e);
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
                newForm.Text = ThePak.CurrentUsedItem;
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
                saveTheDialog.InitialDirectory = App.DefaultOutputPath + "\\Icons\\";
                saveTheDialog.FileName = ThePak.CurrentUsedItem;
                if (saveTheDialog.ShowDialog() == DialogResult.OK)
                {
                    pictureBox1.Image.Save(saveTheDialog.FileName, ImageFormat.Png);
                    AppendText(ThePak.CurrentUsedItem, Color.DarkRed);
                    AppendText(" successfully saved", Color.Black, true);
                }
            }
        }
        private void mergeImagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ImagesMerger.AskMergeImages();
        }
        #endregion

        private void CopySelectedFilePathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                string treeviewPath = treeView1.SelectedNode.FullPath;
                if (treeviewPath.StartsWith("..\\")) { treeviewPath = treeviewPath.Substring(3); } //if loading all paks

                string path = treeviewPath + "\\" + listBox1.SelectedItem;
                if (!path.Contains(".")) //if file uasset/uexp/ubulk
                {
                    Clipboard.SetText(path.Replace("\\", "/") + ".uasset");
                }
                else
                {
                    Clipboard.SetText(path.Replace("\\", "/"));
                }
                AppendText("Copied!", Color.Green, true);
            }
        }

        private void SaveCurrentLocResToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (bIsLocres)
            {
                SaveFileDialog saveTheDialog = new SaveFileDialog();
                saveTheDialog.Title = @"Save LocRes";
                saveTheDialog.Filter = @"JSON Files (*.json)|*.json";
                saveTheDialog.InitialDirectory = App.DefaultOutputPath + "\\LocRes\\";
                saveTheDialog.FileName = ThePak.CurrentUsedItem.Substring(0, ThePak.CurrentUsedItem.LastIndexOf('.'));
                if (saveTheDialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(saveTheDialog.FileName, scintilla1.Text);
                    AppendText(ThePak.CurrentUsedItem, Color.DarkRed);
                    AppendText(" successfully saved", Color.Black, true);
                }
            }
            else
            {
                AppendText("Please load a .locres file first.\t\t\t", Color.Black);
                AppendText(@"FortniteGame\Content\Localization\ - pakchunk0-WindowsClient.pak", Color.DarkRed, true);
            }
        }
    }
}
