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
using FModel.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Image = System.Drawing.Image;
using Settings = FModel.Properties.Settings;

namespace FModel
{
    public partial class MainWindow : Form
    {
        private static Stopwatch _stopWatch { get; set; }
        private static List<string> _itemsToDisplay { get; set; }
        private static string[] _selectedItemsArray { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            App.MainFormToUse = this;

            // Check if watermark exists
            Utilities.CheckWatermark();

            //FModel version
            toolStripStatusLabel1.Text += @" " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString().Substring(0, 5);

            treeView1.Sort();

            //Remove space caused by SizingGrip
            statusStrip1.Padding = new Padding(statusStrip1.Padding.Left, statusStrip1.Padding.Top, statusStrip1.Padding.Left, statusStrip1.Padding.Bottom);

            MyScintilla.ScintillaInstance(scintilla1);
        }

        public void UpdateProcessState(string textToDisplay, string seText)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, string>(UpdateProcessState), textToDisplay, seText);
                return;
            }

            toolStripStatusLabel2.Text = textToDisplay;
            switch(seText)
            {
                case "Error":
                    toolStripStatusLabel3.BackColor = Color.FromArgb(255, 244, 66, 66);
                    break;
                case "Waiting":
                case "Loading":
                case "Processing":
                    toolStripStatusLabel3.BackColor = Color.FromArgb(255, 244, 132, 66);
                    break;
                case "Success":
                    toolStripStatusLabel3.BackColor = Color.FromArgb(255, 66, 244, 66);
                    break;
            }
            toolStripStatusLabel3.Text = seText;
        }
        public void AppendTextToConsole(string text, Color color, bool addNewLine = false, HorizontalAlignment align = HorizontalAlignment.Left)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, Color, bool, HorizontalAlignment>(AppendTextToConsole), text, color, addNewLine, align);
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
        private void AddBackupFiles()
        {
            if (CreateBackup.backupFilesList != null && CreateBackup.backupFilesList.Count > 0)
            {
                foreach (var item in CreateBackup.backupFilesList)
                {
                    Invoke(new Action(() =>
                    {
                        downloadBackupsToolStripMenuItem.DropDownItems.Add(item.bFileName);
                    }));
                }
            }
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

                new UpdateMyState(".PAK Files Path is missing", "Error").ChangeProcessState();
            }
            else
            {
                IEnumerable<string> yourPaKs = Directory.GetFiles(Settings.Default.PAKsPath).Where(x => x.EndsWith(".pak"));
                for (int i = 0; i < yourPaKs.Count(); i++)
                {
                    string arCurrentUsedPak = yourPaKs.ElementAt(i); //SET CURRENT PAK

                    if (!Utilities.IsFileLocked(new System.IO.FileInfo(arCurrentUsedPak)))
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
                    else { new UpdateMyConsole("[FModel] " + Path.GetFileName(arCurrentUsedPak) + " is locked by another process.", Color.Red, true).AppendToConsole(); }
                }
            }
        }

        //EVENTS
        private async void MainWindow_Load(object sender, EventArgs e)
        {
            AutoUpdater.Start("https://dl.dropbox.com/s/3kv2pukqu6tj1r0/FModel.xml?dl=0");

            DLLImport.SetTreeViewTheme(treeView1.Handle);

            Checking.BackupFileName = "\\FortniteGame_" + DateTime.Now.ToString("MMddyyyy") + ".txt";
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
                FontUtilities.SetFont();
                Utilities.SetOutputFolder();
                FillWithPaKs();
                AddToUI.checkAndAddDynamicKeys();
                Utilities.colorMyPaks(loadOneToolStripMenuItem);
                CreateBackup.GetFilesFromDropbox();
                AddBackupFiles();
                Utilities.SetFolderPermission(App.DefaultOutputPath);
                Utilities.CreateDefaultFolders();
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
        private void AESManagerButton_Click(object sender, EventArgs e)
        {
            var aesForms = new AESManager();
            if (Application.OpenForms[aesForms.Name] == null)
            {
                aesForms.Show();
            }
            else
            {
                Application.OpenForms[aesForms.Name].Focus();
            }
            aesForms.FormClosing += (o, c) =>
            {
                if (AESManager.isClosed)
                {
                    Utilities.colorMyPaks(loadOneToolStripMenuItem);
                }
            };
        }
        #endregion

        #region PAKLIST & FILL TREE
        //METHODS
        private void TreeParsePath(TreeNodeCollection nodeList, string path)
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
            PakHelper.PakAsTxt = File.ReadAllLines(App.DefaultOutputPath + "\\FortnitePAKs.txt");
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
                        if (linesA[i].StartsWith("../"))
                            linesA[i] = linesA[i].Substring(3);

                    IEnumerable<String> onlyB = PakHelper.PakAsTxt.Except(linesA);
                    IEnumerable<String> removed = linesA.Except(PakHelper.PakAsTxt);

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
                        new UpdateMyConsole("Items Removed/Renamed:", Color.Red, true).AppendToConsole();
                        removedItems = removedItems.Distinct().ToList();
                        for (int ii = 0; ii < removedItems.Count; ii++)
                            new UpdateMyConsole("    - " + removedItems[ii], Color.Black, true).AppendToConsole();
                    }));
                }
            }
            else
            {
                new UpdateMyConsole("Canceled! ", Color.Red).AppendToConsole();
                new UpdateMyConsole("All pak files loaded...", Color.Black, true).AppendToConsole();
                return;
            }

            if (File.Exists(App.DefaultOutputPath + "\\Result.txt"))
            {
                PakHelper.PakAsTxt = File.ReadAllLines(App.DefaultOutputPath + "\\Result.txt");
                File.Delete(App.DefaultOutputPath + "\\Result.txt");
                Checking.DifferenceFileExists = true;
            }
        }
        private void CreatePakList(ToolStripItemClickedEventArgs selectedPak = null, bool loadAllPaKs = false, bool getDiff = false, bool updateMode = false)
        {
            ThePak.AllpaksDictionary = new Dictionary<string, string>();
            ThePak.PaksExtractorDictionary = new Dictionary<string, PakExtractor>();
            ThePak.PaksFileArrayDictionary = new Dictionary<PakExtractor, string[]>();
            RegisterSettings.updateModeDictionary = new Dictionary<string, string>();
            PakHelper.PakAsTxt = null;

            if (selectedPak != null)
            {
                new UpdateMyState(Settings.Default.PAKsPath + "\\" + selectedPak.ClickedItem.Text, "Loading").ChangeProcessState();

                //ADD TO DICTIONNARY
                PakHelper.RegisterPaKsinDict(selectedPak);

                if (PakHelper.PakAsTxt != null)
                {
                    Invoke(new Action(() =>
                    {
                        treeView1.BeginUpdate();
                        for (int i = 0; i < PakHelper.PakAsTxt.Length; i++)
                        {
                            TreeParsePath(treeView1.Nodes, PakHelper.PakAsTxt[i].Replace(PakHelper.PakAsTxt[i].Split('/').Last(), ""));
                        }
                        Utilities.ExpandToLevel(treeView1.Nodes, 1);
                        treeView1.EndUpdate();
                    }));
                    new UpdateMyState(Settings.Default.PAKsPath + "\\" + selectedPak.ClickedItem.Text, "Success").ChangeProcessState();
                }
                else
                    new UpdateMyState("Please, provide a working key in the AES Manager for " + selectedPak.ClickedItem.Text, "Error").ChangeProcessState();
            }
            if (loadAllPaKs)
            {
                //ADD TO DICTIONNARY
                PakHelper.RegisterPaKsinDict(null, true);

                if (!File.Exists(App.DefaultOutputPath + "\\FortnitePAKs.txt"))
                {
                    new UpdateMyState("Error while registering pak files in the dictionary", "Error").ChangeProcessState();
                }
                else if (new System.IO.FileInfo(App.DefaultOutputPath + "\\FortnitePAKs.txt").Length <= 0) //File will always exist so we check the file size instead
                {
                    File.Delete(App.DefaultOutputPath + "\\FortnitePAKs.txt");
                    new UpdateMyState("Can't read .PAK files with this key", "Error").ChangeProcessState();
                }
                else
                {
                    PakHelper.PakAsTxt = File.ReadAllLines(App.DefaultOutputPath + "\\FortnitePAKs.txt");
                    File.Delete(App.DefaultOutputPath + "\\FortnitePAKs.txt");

                    Invoke(new Action(() =>
                    {
                        treeView1.BeginUpdate();
                        for (int i = 0; i < PakHelper.PakAsTxt.Length; i++)
                        {
                            TreeParsePath(treeView1.Nodes, PakHelper.PakAsTxt[i].Replace(PakHelper.PakAsTxt[i].Split('/').Last(), ""));
                        }
                        Utilities.ExpandToLevel(treeView1.Nodes, 1);
                        treeView1.EndUpdate();
                    }));
                    new UpdateMyState(Settings.Default.PAKsPath, "Success").ChangeProcessState();
                }
            }
            if (getDiff)
            {
                //ADD TO DICTIONNARY
                PakHelper.RegisterPaKsinDict(null, true);

                if (!File.Exists(App.DefaultOutputPath + "\\FortnitePAKs.txt"))
                {
                    new UpdateMyState("Error while registering pak files in the dictionary", "Error").ChangeProcessState();
                }
                else if (new System.IO.FileInfo(App.DefaultOutputPath + "\\FortnitePAKs.txt").Length <= 0)
                {
                    new UpdateMyState("Can't read .PAK files with this key", "Error").ChangeProcessState();
                }
                else
                {
                    new UpdateMyState("Comparing files...", "Loading").ChangeProcessState();
                    ComparePaKs();
                    if (updateMode && Checking.DifferenceFileExists)
                    {
                        RegisterSettings.UpdateModeAddToDict(PakHelper.PakAsTxt);
                        Checking.UmWorking = true;
                    }

                    Invoke(new Action(() =>
                    {
                        treeView1.BeginUpdate();
                        for (int i = 0; i < PakHelper.PakAsTxt.Length; i++)
                        {
                            TreeParsePath(treeView1.Nodes, PakHelper.PakAsTxt[i].Replace(PakHelper.PakAsTxt[i].Split('/').Last(), ""));
                        }
                        Utilities.ExpandToLevel(treeView1.Nodes, 1);
                        treeView1.EndUpdate();
                    }));

                    Checking.DifferenceFileExists = false;
                    new UpdateMyState("Files compared", "Success").ChangeProcessState();
                }
            }
        }
        private void UpdateModeHereWeGooooo()
        {
            // Check if watermark exists
            Utilities.CheckWatermark();

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

        //EVENTS
        private async void loadOneToolStripMenuItem_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            await Task.Run(() => {
                Invoke(new Action(() =>
                {
                    scintilla1.Text = "";
                    pictureBox1.Image = null;
                    loadOneToolStripMenuItem.Enabled = false;
                    loadAllToolStripMenuItem.Enabled = false;

                    treeView1.Nodes.Clear(); //SMH HERE IT DOESN'T LAG
                    listBox1.Items.Clear();
                }));

                CreatePakList(e);

                Invoke(new Action(() =>
                {
                    loadOneToolStripMenuItem.Enabled = true;
                    loadAllToolStripMenuItem.Enabled = true;
                }));
            });
        }
        private async void loadAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Invoke(new Action(() =>
            {
                scintilla1.Text = "";
                pictureBox1.Image = null;
                loadOneToolStripMenuItem.Enabled = false;
                loadAllToolStripMenuItem.Enabled = false;

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
                    UpdateModeHereWeGooooo();
                });
            }

            Invoke(new Action(() =>
            {
                loadOneToolStripMenuItem.Enabled = true;
                loadAllToolStripMenuItem.Enabled = true;
            }));
        }
        private async void backupPAKsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await Task.Run(() => {
                CreateBackup.CreateBackupList();
            });
        }
        private async void DownloadBackupsToolStripMenuItem_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            await Task.Run(() => {
                if (CreateBackup.backupFilesList != null && CreateBackup.backupFilesList.Count > 0)
                {
                    string filename = CreateBackup.backupFilesList.Where(x => string.Equals(x.bFileName, e.ClickedItem.Text)).Select(x => x.bFileName).FirstOrDefault();
                    string downloadLink = CreateBackup.backupFilesList.Where(x => string.Equals(x.bFileName, e.ClickedItem.Text)).Select(x => x.bFileDownload).FirstOrDefault();
                    if (!string.IsNullOrEmpty(downloadLink))
                    {
                        new UpdateMyState("Downloading " + filename, "Waiting").ChangeProcessState();

                        RestClient EndpointClient = new RestClient(downloadLink);
                        RestRequest EndpointRequest = new RestRequest(Method.GET);
                        string content = EndpointClient.Execute(EndpointRequest).Content;

                        File.WriteAllText(App.DefaultOutputPath + "\\Backup\\" + filename, content);
                        if (new System.IO.FileInfo(App.DefaultOutputPath + "\\Backup\\" + filename).Length > 0)
                        {
                            new UpdateMyState("\\Backup\\" + filename + " successfully downloaded", "Success").ChangeProcessState();
                        }
                        else
                        {
                            File.Delete(App.DefaultOutputPath + "\\Backup\\" + filename);
                            new UpdateMyState("Can't download " + filename, "Error").ChangeProcessState();
                        }
                    }
                }
            });
        }

        //UPDATE MODE
        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            _stopWatch = new Stopwatch();
            _stopWatch.Start();
            Utilities.CreateDefaultFolders();
            RegisterInArray(e, true);
        }
        private void backgroundWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _stopWatch.Stop();
            if (e.Cancelled)
            {
                new UpdateMyState("Canceled!", "Error").ChangeProcessState();
            }
            else if (e.Error != null)
            {
                new UpdateMyState(e.Error.Message, "Error").ChangeProcessState();
            }
            else if (Checking.UmWorking == false)
            {
                new UpdateMyState("Can't read .PAK files with this key", "Error").ChangeProcessState();
            }
            else
            {
                TimeSpan ts = _stopWatch.Elapsed;
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                new UpdateMyState("Time elapsed: " + elapsedTime, "Success").ChangeProcessState();
            }

            _selectedItemsArray = null;
            backgroundWorker2.Dispose();
            Checking.UmWorking = false;
            GC.Collect();
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
        private void GetFilesAndFill(TreeNodeMouseClickEventArgs selectedNode)
        {
            List<string> itemsNotToDisplay = new List<string>();
            _itemsToDisplay = new List<string>();

            Invoke(new Action(() =>
            {
                listBox1.Items.Clear();
                FilterTextBox.Text = string.Empty;
            }));

            var all = Utilities.GetAncestors(selectedNode.Node, x => x.Parent).ToList();
            all.Reverse();
            var full = string.Join("/", all.Select(x => x.Text)) + "/" + selectedNode.Node.Text + "/";
            if (string.IsNullOrEmpty(full))
            {
                return;
            }

            var dirfiles = PakHelper.PakAsTxt.Where(x => x.StartsWith(full) && !x.Replace(full, "").Contains("/"));
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
                        if (Utilities.CaseInsensitiveContains(_itemsToDisplay[i], FilterTextBox.Text))
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

        //EVENTS
        private async void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node != null && e.Button == MouseButtons.Right)
            {
                Checking.currentSelectedNodePartialPath = e.Node.FullPath + "\\";
                extractFolderContentsToolStripMenuItem.Text = "Extract " + e.Node.Text + " Folder Contents";
                contextMenuStrip2.Show(Cursor.Position);
            }
            else
            {
                await Task.Run(() => {
                    GetFilesAndFill(e);
                });
            }
        }
        private async void FilterTextBox_TextChanged(object sender, EventArgs e)
        {
            await Task.Run(() => {
                FilterItems();
            });
        }
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null && _selectedItemsArray == null)
            {
                ExtractButton.Enabled = true;
            }
        }

        private void listBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (listBox1.SelectedIndex != -1 && e.Button == MouseButtons.Right)
            {
                bool isActive = !string.IsNullOrEmpty(scintilla1.Text);
                saveAsJSONToolStripMenuItem1.Enabled = isActive;
                saveAsJSONToolStripMenuItem1.ToolTipText = !isActive ? "Need extract a file" : "";

                contextMenuStrip1.Show(Cursor.Position);
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
                    _selectedItemsArray = new string[RegisterSettings.updateModeDictionary.Count];
                    for (int i = 0; i < RegisterSettings.updateModeDictionary.Count; i++) //ADD DICT ITEM TO ARRAY
                    {
                        _selectedItemsArray[i] = RegisterSettings.updateModeDictionary.Keys.ElementAt(i);
                    }
                }));
            }
            else
            {
                Invoke(new Action(() =>
                {
                    _selectedItemsArray = new string[listBox1.SelectedItems.Count];
                    for (int i = 0; i < listBox1.SelectedItems.Count; i++) //ADD SELECTED ITEM TO ARRAY
                    {
                        _selectedItemsArray[i] = listBox1.SelectedItems[i].ToString();
                    }
                }));
            }

            ExtractAndSerializeItems(e);
        }
        private void ExtractAndSerializeItems(DoWorkEventArgs e)
        {
            for (int i = 0; i < _selectedItemsArray.Length; i++)
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

                ThePak.CurrentUsedItem = _selectedItemsArray[i];

                Checking.ExtractedFilePath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary[ThePak.CurrentUsedItem], ThePak.CurrentUsedItem);

                if (Checking.ExtractedFilePath != null)
                {
                    new UpdateMyState(ThePak.CurrentUsedItem + " successfully extracted", "Success").ChangeProcessState();
                    if (Checking.ExtractedFilePath.Contains(".uasset") || Checking.ExtractedFilePath.Contains(".uexp") || Checking.ExtractedFilePath.Contains(".ubulk"))
                    {
                        JohnWick.MyAsset = new PakAsset(Checking.ExtractedFilePath.Substring(0, Checking.ExtractedFilePath.LastIndexOf('.')));
                        JsonParseFile();
                    }
                    if (Checking.ExtractedFilePath.Contains(".ufont"))
                        FontUtilities.ConvertToTtf(Checking.ExtractedFilePath);
                    if (Checking.ExtractedFilePath.Contains(".ini"))
                    {
                        Invoke(new Action(() =>
                        {
                            scintilla1.Text = File.ReadAllText(Checking.ExtractedFilePath);
                        }));
                    }
                    if (Checking.ExtractedFilePath.Contains(".locres") && !Checking.ExtractedFilePath.Contains("EngineOverrides"))
                        SerializeLocRes();
                    if (Checking.ExtractedFilePath.Contains(".uplugin") || Checking.ExtractedFilePath.Contains(".uproject"))
                        uPluginConvertToJson(Checking.ExtractedFilePath);
                }
                else { throw new ArgumentException("Error while extracting " + ThePak.CurrentUsedItem); }
            }
        }
        private void JsonParseFile()
        {
            if (JohnWick.MyAsset.GetSerialized() != null)
            {
                new UpdateMyState(ThePak.CurrentUsedItem + " successfully serialized", "Success").ChangeProcessState();

                try
                {
                    string parsedData = JToken.Parse(JohnWick.MyAsset.GetSerialized()).ToString();
                    Invoke(new Action(() =>
                    {
                        scintilla1.Text = parsedData;
                    }));
                }
                catch (JsonReaderException)
                {
                    new UpdateMyConsole(ThePak.CurrentUsedItem + " ", Color.Red).AppendToConsole();
                    new UpdateMyConsole(".JSON file can't be displayed", Color.Black, true).AppendToConsole();
                }
                finally
                {
                    AutoSaveAsJSON();
                    NavigateThroughJson(JohnWick.MyAsset, Checking.ExtractedFilePath);
                }
            }
            else { throw new ArgumentException("Can't serialize this file"); }
        }
        private void NavigateThroughJson(PakAsset theAsset, string theAssetExtractedPath = null)
        {
            try
            {
                dynamic AssetData = JsonConvert.DeserializeObject(theAsset.GetSerialized());
                JArray AssetArray = JArray.FromObject(AssetData);

                new UpdateMyState("Parsing " + ThePak.CurrentUsedItem + "...", "Waiting").ChangeProcessState();
                JToken exportToken = AssetArray[0]["export_type"];
                switch (exportToken != null ? exportToken.Value<string>() : "")
                {
                    case "AthenaBackpackItemDefinition":
                    case "AthenaBattleBusItemDefinition":
                    case "AthenaCharacterItemDefinition":
                    case "AthenaConsumableEmoteItemDefinition":
                    case "AthenaSkyDiveContrailItemDefinition":
                    case "AthenaDanceItemDefinition":
                    case "AthenaEmojiItemDefinition":
                    case "AthenaGliderItemDefinition":
                    case "AthenaItemWrapDefinition":
                    case "AthenaLoadingScreenItemDefinition":
                    case "AthenaMusicPackItemDefinition":
                    case "AthenaPetCarrierItemDefinition":
                    case "AthenaPickaxeItemDefinition":
                    case "AthenaSprayItemDefinition":
                    case "AthenaToyItemDefinition":
                    case "AthenaVictoryPoseItemDefinition":
                    case "FortBannerTokenType":
                    case "AthenaGadgetItemDefinition":
                        CreateItemIcon(AssetArray[0], "athIteDef");
                        break;
                    case "FortWeaponRangedItemDefinition":
                    case "FortWeaponMeleeItemDefinition":
                    case "FortIngredientItemDefinition":
                        CreateItemIcon(AssetArray[0], "consAndWeap");
                        break;
                    case "FortVariantTokenType":
                        CreateItemIcon(AssetArray[0], "variant");
                        break;
                    case "FortAmmoItemDefinition":
                        CreateItemIcon(AssetArray[0], "ammo");
                        break;
                    case "FortHeroType":
                        CreateItemIcon(AssetArray[0], "stwHeroes");
                        break;
                    case "FortDefenderItemDefinition":
                        CreateItemIcon(AssetArray[0], "stwDefenders");
                        break;
                    case "FortContextTrapItemDefinition":
                    case "FortTrapItemDefinition":
                    case "FortCardPackItemDefinition":
                    case "FortPlaysetGrenadeItemDefinition":
                    case "FortConsumableAccountItemDefinition":
                    case "FortBadgeItemDefinition":
                    case "FortCurrencyItemDefinition":
                    case "FortConversionControlItemDefinition":
                    case "FortHomebaseNodeItemDefinition":
                    case "FortPersonalVehicleItemDefinition":
                    case "FortCampaignHeroLoadoutItemDefinition":
                    case "FortNeverPersistItemDefinition":
                    case "FortPersistentResourceItemDefinition":
                    case "FortResourceItemDefinition":
                    case "FortGadgetItemDefinition":
                    case "FortStatItemDefinition":
                    case "FortTokenType":
                    case "FortDailyRewardScheduleTokenDefinition":
                    case "FortWorkerType":
                    case "FortConditionalResourceItemDefinition":
                    case "FortAwardItemDefinition":
                    case "FortChallengeBundleScheduleDefinition":
                    case "FortAbilityKit":
                        CreateItemIcon(AssetArray[0]);
                        break;
                    case "FortChallengeBundleItemDefinition":
                        CreateBundleChallengesIcon(AssetArray[0], theAssetExtractedPath);
                        break;
                    case "FortSchematicItemDefinition":
                        CreateSchematicIcon(AssetArray[0]);
                        break;
                    case "Texture2D":
                        ConvertTexture2D();
                        break;
                    case "SoundWave":
                        ConvertSoundWave();
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                if (Checking.UmWorking)
                {
                    new UpdateMyConsole(ex.Message, Color.Red, true).AppendToConsole();
                }
                else
                {
                    throw new ArgumentException(ex.Message);
                }
            }
        }
        private void CreateItemIcon(JToken theItem, string specialMode = null)
        {
            new UpdateMyState(ThePak.CurrentUsedItem + " is an Item Definition", "Success").ChangeProcessState();

            Bitmap bmp = new Bitmap(522, 522);
            Graphics g = Graphics.FromImage(bmp);
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            g.SmoothingMode = SmoothingMode.HighQuality;

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

            if (string.Equals(Settings.Default.rarityDesign, "Flat"))
            {
                GraphicsPath p = new GraphicsPath();
                p.StartFigure();
                p.AddLine(4, 438, 517, 383);
                p.AddLine(517, 383, 517, 383 + 134);
                p.AddLine(4, 383 + 134, 4, 383 + 134);
                p.AddLine(4, 383 + 134, 4, 438);
                p.CloseFigure();
                g.FillPath(new SolidBrush(Color.FromArgb(70, 0, 0, 50)), p);
            }
            else { g.FillRectangle(new SolidBrush(Color.FromArgb(70, 0, 0, 50)), new Rectangle(5, 383, 512, 134)); }

            DrawText.DrawTexts(theItem, g, specialMode);

            pictureBox1.Image = bmp;

            AutoSaveImage();
            g.Dispose();
        }


        /// <summary>
        /// this is the main method that draw the bundle of challenges
        /// </summary>
        /// <param name="theItem"> needed for the DisplayName </param>
        /// <param name="theParsedJson"> to parse from this instead of calling MyAsset.GetSerialized() again </param>
        /// <param name="extractedBundlePath"> needed for the LastFolder </param>
        /// <returns> the bundle image ready to be displayed in pictureBox1 </returns>
        private void CreateBundleChallengesIcon(JToken theItem, string extractedBundlePath)
        {
            BundleInfos.getBundleData(theItem);
            bool isFortbyte = false;

            Bitmap bmp = new Bitmap(1024, 10000);
            BundleDesign.BundlePath = extractedBundlePath;
            BundleDesign.theY = 200;
            BundleDesign.toDrawOn = Graphics.FromImage(bmp);
            BundleDesign.toDrawOn.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            BundleDesign.toDrawOn.SmoothingMode = SmoothingMode.HighQuality;
            BundleDesign.myItem = theItem;
            BundleDesign.isBundleLevelup = false;
            BundleDesign.isGrantWithBundle = false;
            BundleDesign.isRequiresBattlePass = false;

            BundleDesign.drawBackground(bmp, theItem);

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
                new UpdateMyConsole(BundleInfos.BundleData[i].questDescr, Color.SteelBlue).AppendToConsole();
                new UpdateMyConsole("\t\tCount: " + BundleInfos.BundleData[i].questCount, Color.DarkRed).AppendToConsole();
                new UpdateMyConsole("\t\t" + BundleInfos.BundleData[i].rewardItemId + ":" + BundleInfos.BundleData[i].rewardItemQuantity, Color.DarkGreen, true).AppendToConsole();

                BundleDesign.theY += 90;

                if (!string.IsNullOrEmpty(BundleInfos.BundleData[i].questUnlockType))
                {
                    BundleDesign.drawSeparator(BundleInfos.BundleData[i].questUnlockType);
                }

                //draw quest description
                BundleDesign.drawQuestBackground(bmp);
                Font goodFont = FontUtilities.FindFont(BundleDesign.toDrawOn, BundleInfos.BundleData[i].questDescr, new Rectangle(57, BundleDesign.theY + 7, bmp.Width - 227, 38).Size, new Font(Settings.Default.IconLanguage == "Japanese" ? FontUtilities.pfc.Families[2] : FontUtilities.pfc.Families[1], 30)); //size in "new Font()" is never check
                BundleDesign.toDrawOn.DrawString(BundleInfos.BundleData[i].questDescr, goodFont, new SolidBrush(Color.White), new Point(55, BundleDesign.theY + 10));

                //draw quest count
                BundleDesign.toDrawOn.DrawString("0 /", new Font(FontUtilities.pfc.Families[1], 12), new SolidBrush(Color.FromArgb(255, 255, 255, 255)), new Point(565, BundleDesign.theY + 42));
                BundleDesign.toDrawOn.DrawString(BundleInfos.BundleData[i].questCount.ToString(), new Font(FontUtilities.pfc.Families[1], 12), new SolidBrush(Color.FromArgb(200, 255, 255, 255)), new Point(587, BundleDesign.theY + 42));

                //draw quest reward
                DrawingRewards.getRewards(BundleInfos.BundleData[i].rewardItemId, BundleInfos.BundleData[i].rewardItemQuantity);
            }
            new UpdateMyConsole("", Color.Black, true).AppendToConsole();

            BundleDesign.drawCompletionReward(bmp, theItem);
            BundleDesign.drawWatermark(bmp);

            //cut if too long and return the bitmap
            using (Bitmap bmp2 = bmp)
            {
                var newImg = bmp2.Clone(
                    new Rectangle { X = 0, Y = 0, Width = bmp.Width, Height = BundleDesign.theY + 105 },
                    bmp2.PixelFormat);

                pictureBox1.Image = newImg;
            }

            AutoSaveImage();
            BundleDesign.toDrawOn.Dispose(); //actually this is the most useful thing in this method
        }

        private void CreateSchematicIcon(JToken theItem)
        {
            SchematicIconDesign.createItemDefinitionIcon(theItem);
            SchematicIconDesign.createIngredientIcon();

            pictureBox1.Image = SchematicIconDesign.schematicBitmap;

            AutoSaveImage();
            SchematicIconDesign.toDrawOn.Dispose();
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
                    LocResSerializer.setLocRes(filePath);
                    scintilla1.Text = JsonConvert.SerializeObject(LocResSerializer.LocResDict, Formatting.Indented);
                }
                else { throw new FileNotFoundException("Error while searching " + listBox1.SelectedItem); }
            }));
        }

        private void ConvertTexture2D()
        {
            new UpdateMyState(ThePak.CurrentUsedItem + " is a Texture2D", "Success").ChangeProcessState();

            JohnWick.MyAsset = new PakAsset(Checking.ExtractedFilePath.Substring(0, Checking.ExtractedFilePath.LastIndexOf(".", StringComparison.Ordinal)));
            JohnWick.MyAsset.SaveTexture(Checking.ExtractedFilePath.Substring(0, Checking.ExtractedFilePath.LastIndexOf(".", StringComparison.Ordinal)) + ".png");
            string imgPath = Checking.ExtractedFilePath.Substring(0, Checking.ExtractedFilePath.LastIndexOf(".", StringComparison.Ordinal)) + ".png";

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
                new UpdateMyConsole(ThePak.CurrentUsedItem, Color.DarkRed).AppendToConsole();
                new UpdateMyConsole(" successfully saved", Color.Black, true).AppendToConsole();
            }
        }
        private void ConvertSoundWave()
        {
            new UpdateMyState(ThePak.CurrentUsedItem + " is a Sound", "Success").ChangeProcessState();

            string soundPathToConvert = Checking.ExtractedFilePath.Substring(0, Checking.ExtractedFilePath.LastIndexOf('\\')) + "\\" + ThePak.CurrentUsedItem + ".uexp";
            string soundPathConverted = UnrealEngineDataToOgg.ConvertToOgg(soundPathToConvert);
            new UpdateMyState("Converting " + ThePak.CurrentUsedItem, "Processing").ChangeProcessState();

            if (File.Exists(soundPathConverted))
            {
                if (Properties.Settings.Default.tryToOpenAssets)
                {
                    Utilities.OpenWithDefaultProgramAndNoFocus(soundPathConverted);
                    new UpdateMyState("Opening " + ThePak.CurrentUsedItem + ".ogg", "Success").ChangeProcessState();
                } else { new UpdateMyState("Extracted " + ThePak.CurrentUsedItem + ".ogg", "Success").ChangeProcessState(); }
            }
            else
                new UpdateMyState("Couldn't convert " + ThePak.CurrentUsedItem, "Error").ChangeProcessState();
        }

        private void uPluginConvertToJson(string file)
        {
            if (File.Exists(Path.ChangeExtension(file, ".json"))) File.Delete(Path.ChangeExtension(file, ".json"));

            File.Move(file, Path.ChangeExtension(file, ".json"));
            Invoke(new Action(() =>
            {
                scintilla1.Text = File.ReadAllText(Path.ChangeExtension(file, ".json"));
            }));
            new UpdateMyState(ThePak.CurrentUsedItem + " successfully converter to JSON", "Success").ChangeProcessState();
        }

        //EVENTS
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            _stopWatch = new Stopwatch();
            _stopWatch.Start();
            Utilities.CreateDefaultFolders();
            RegisterInArray(e);
        }
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _stopWatch.Stop();
            if (e.Cancelled)
            {
                new UpdateMyState("Canceled!", "Error").ChangeProcessState();
            }
            else if (e.Error != null)
            {
                new UpdateMyState(e.Error.Message, "Error").ChangeProcessState();
            }
            else
            {
                TimeSpan ts = _stopWatch.Elapsed;
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                new UpdateMyState("Time elapsed: " + elapsedTime, "Success").ChangeProcessState();
            }

            _selectedItemsArray = null;
            backgroundWorker1.Dispose();
            GC.Collect();
            Invoke(new Action(() =>
            {
                StopButton.Enabled = false;
                OpenImageButton.Enabled = true;
                ExtractButton.Enabled = true;
            }));
        }

        private void ExtractButton_Click(object sender, EventArgs e)
        {
            ExtractProcess();
        }
        private void ListBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listBox1.SelectedItem != null && _selectedItemsArray == null)
            {
                ExtractProcess();
            }
        }

        private void ExtractProcess()
        {
            // Check if watermark exists
            Utilities.CheckWatermark();

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

        #region IMAGES TOOLSTRIP AND OPEN
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
                    new UpdateMyConsole(Path.GetFileNameWithoutExtension(saveTheDialog.FileName), Color.DarkRed).AppendToConsole();
                    new UpdateMyConsole(" successfully saved", Color.Black, true).AppendToConsole();
                }
            }
        }
        private void mergeImagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MergeImages mergeImagesForm = new MergeImages();
            if (Application.OpenForms[mergeImagesForm.Name] == null)
            {
                mergeImagesForm.Show();
            }
            else
                Application.OpenForms[mergeImagesForm.Name].Focus();
        }
        #endregion

        #region FILES TOOLSTRIP
        //METHODS
        private void OpenMe()
        {
            if (SearchFiles.IsClosed)
            {
                treeView1.CollapseAll();

                List<string> pathList = SearchFiles.SfPath.Split('/').ToList();

                foreach (TreeNode node in treeView1.Nodes)
                {
                    if (node.Text == pathList[0])
                    {
                        ExpandMyLitleBoys(node, pathList);
                    }
                }
            }
            else if (SearchFiles.FilesToSearch)
            {
                AddAndSelectAllItems(SearchFiles.myItems);
            }
        }
        private async void ExpandMyLitleBoys(TreeNode node, List<string> path)
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

                    var all = Utilities.GetAncestors(node, x => x.Parent).ToList();
                    all.Reverse();
                    var full = string.Join("/", all.Select(x => x.Text)) + "/" + node.Text + "/";
                    if (string.IsNullOrEmpty(full))
                    {
                        return;
                    }

                    var dirfiles = PakHelper.PakAsTxt.Where(x => x.StartsWith(full) && !x.Replace(full, "").Contains("/"));
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
        private void CopySelectedFilePathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopySelectedFile();
        }
        private void CopySelectedFile(bool isName = false, bool withExtension = true)
        {
            if (listBox1.SelectedItem != null)
            {
                string treeviewPath = treeView1.SelectedNode.FullPath;
                // if loading all paks
                if (treeviewPath.StartsWith("..\\"))
                    treeviewPath = treeviewPath.Substring(3);

                string path = treeviewPath + "\\" + listBox1.SelectedItem;
                // if file uasset/uexp/ubulk
                path = !path.Contains(".") ? (path.Replace("\\", "/") + ".uasset") : path.Replace("\\", "/");
                if (isName)
                    path = Path.GetFileName(path);

                if (!withExtension)
                    path = isName ? Path.GetFileNameWithoutExtension(path) : Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));

                Clipboard.SetText(path);
                new UpdateMyConsole(path + " Copied!", Color.Green, true).AppendToConsole();
            }
        }

        private void SaveAsJSONToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveAsJSON();
        }
        private void SaveAsJSON()
        {
            if (!string.IsNullOrEmpty(scintilla1.Text))
            {
                SaveFileDialog saveTheDialog = new SaveFileDialog();
                saveTheDialog.Title = @"Save Serialized File";
                saveTheDialog.Filter = @"JSON Files (*.json)|*.json";
                saveTheDialog.InitialDirectory = App.DefaultOutputPath + "\\Saved_JSON\\";
                saveTheDialog.FileName = ThePak.CurrentUsedItem.Contains('.') ? ThePak.CurrentUsedItem.Substring(0, ThePak.CurrentUsedItem.LastIndexOf('.')) : ThePak.CurrentUsedItem;
                if (saveTheDialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(saveTheDialog.FileName, scintilla1.Text);
                    if (File.Exists(saveTheDialog.FileName))
                    {
                        new UpdateMyConsole(ThePak.CurrentUsedItem, Color.DarkRed).AppendToConsole();
                        new UpdateMyConsole(" successfully saved", Color.Black, true).AppendToConsole();
                    }
                    else
                    {
                        new UpdateMyConsole("Fail to save ", Color.Black).AppendToConsole();
                        new UpdateMyConsole(ThePak.CurrentUsedItem, Color.DarkRed, true).AppendToConsole();
                    }
                }
            }
        }
        private void AutoSaveAsJSON()
        {
            if (autoSaveAsJSONToolStripMenuItem.Checked)
            {
                string text = string.Empty;
                Invoke(new Action(() =>
                {
                    text = scintilla1.Text;
                }));

                if (!string.IsNullOrEmpty(text))
                {
                    string filename = ThePak.CurrentUsedItem.Contains('.') ? ThePak.CurrentUsedItem.Substring(0, ThePak.CurrentUsedItem.LastIndexOf('.')) : ThePak.CurrentUsedItem;
                    File.WriteAllText(App.DefaultOutputPath + "\\Saved_JSON\\" + filename + ".json", text);

                    if (File.Exists(App.DefaultOutputPath + "\\Saved_JSON\\" + filename + ".json"))
                    {
                        new UpdateMyConsole(ThePak.CurrentUsedItem, Color.DarkRed).AppendToConsole();
                        new UpdateMyConsole("'s properties successfully saved", Color.Black, true).AppendToConsole();
                    }
                    else
                    {
                        new UpdateMyConsole("Fail to save ", Color.Black).AppendToConsole();
                        new UpdateMyConsole(ThePak.CurrentUsedItem, Color.DarkRed, true).AppendToConsole();
                    }
                }
            }
        }
        private void AutoSaveImage()
        {
            if (autoSaveImagesToolStripMenuItem.Checked || Checking.UmWorking)
            {
                Invoke(new Action(() =>
                {
                    pictureBox1.Image.Save(App.DefaultOutputPath + "\\Icons\\" + ThePak.CurrentUsedItem + ".png", ImageFormat.Png);
                }));

                if (File.Exists(App.DefaultOutputPath + "\\Icons\\" + ThePak.CurrentUsedItem + ".png"))
                {
                    new UpdateMyConsole(ThePak.CurrentUsedItem, Color.DarkRed).AppendToConsole();
                    new UpdateMyConsole("'s image successfully saved", Color.Black, true).AppendToConsole();
                }
            }
        }

        private void openExtractionFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(Settings.Default.ExtractOutput))
                Process.Start(@"" + Settings.Default.ExtractOutput);
        }
        #endregion

        #region RIGHT CLICK
        private void copyFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopySelectedFile();
        }
        private void copyFileNameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopySelectedFile(true);
        }
        private void copyFilePathWithoutExtensionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopySelectedFile(false, false);
        }
        private void copyFileNameWithoutExtensionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopySelectedFile(true, false);
        }

        private void extractToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExtractProcess();
        }

        private void saveAsJSONToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            SaveAsJSON();
        }

        private void ExtractFolderContentsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RegisterSettings.updateModeDictionary = new Dictionary<string, string>();

            for (int i = 0; i < PakHelper.PakAsTxt.Length; i++)
            {
                if (PakHelper.PakAsTxt[i].Contains(Checking.currentSelectedNodePartialPath.Replace("\\", "/")))
                {
                    string filename = Path.GetFileName(PakHelper.PakAsTxt[i]);
                    if (filename.Contains(".uasset") || filename.Contains(".uexp") || filename.Contains(".ubulk"))
                    {
                        if (!RegisterSettings.updateModeDictionary.ContainsKey(filename.Substring(0, filename.LastIndexOf(".", StringComparison.Ordinal))))
                            RegisterSettings.updateModeDictionary.Add(filename.Substring(0, filename.LastIndexOf(".", StringComparison.Ordinal)), PakHelper.PakAsTxt[i]);
                    }
                }
            }
            Checking.UmWorking = true;

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
        #endregion
    }
}
