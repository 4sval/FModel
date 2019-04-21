using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScintillaNET_FindReplaceDialog;
using System.Security.Principal;
using System.Security.AccessControl;

namespace FModel
{
    public partial class MainWindow : Form
    {
        #region EVERYTHING WE NEED
        FindReplace MyFindReplace;
        Stopwatch stopWatch;
        private static string[] PAKsArray;
        public static string[] PAKasTXT;
        private static Dictionary<string, string> AllPAKsDictionary;
        private static Dictionary<string, long> questStageDict;
        private static Dictionary<string, string> diffToExtract;
        private static string BackupFileName;
        private static List<string> itemsToDisplay;
        public static string DefaultOutputPath;
        public static string currentUsedPAK;
        public static string currentUsedPAKGUID;
        public static string currentUsedItem;
        public static string extractedFilePath;
        public static string[] SelectedItemsArray;
        public static string[] SelectedChallengesArray;
        public static bool wasFeatured;
        public static string itemIconPath;
        public static int yAfterLoop;
        public static bool umWorking;
        #endregion

        #region FONTS
        PrivateFontCollection pfc = new PrivateFontCollection();
        StringFormat centeredString = new StringFormat();
        StringFormat rightString = new StringFormat();
        StringFormat centeredStringLine = new StringFormat();
        private int fontLength;
        private byte[] fontdata;
        private int fontLength2;
        private byte[] fontdata2;
        #endregion

        #region DLLIMPORT
        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int description, int reservedValue);
        public static bool IsInternetAvailable()
        {
            int description;
            return InternetGetConnectedState(out description, 0);
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

            treeView1.Sort();
            //REMOVE SPACE CAUSED BY SIZING GRIP
            statusStrip1.Padding = new Padding(statusStrip1.Padding.Left, statusStrip1.Padding.Top, statusStrip1.Padding.Left, statusStrip1.Padding.Bottom);

            // Create instance of FindReplace with reference to a ScintillaNET control.
            MyFindReplace = new FindReplace(scintilla1); // For WinForms
            MyFindReplace.Window.StartPosition = FormStartPosition.CenterScreen;
            // Tie in FindReplace event
            MyFindReplace.KeyPressed += MyFindReplace_KeyPressed;
            // Tie in Scintilla event
            scintilla1.KeyDown += scintilla1_KeyDown;
        }

        #region USEFUL METHODS
        private void jwpmProcess(string args)
        {
            using (Process p = new Process())
            {
                p.StartInfo.FileName = DefaultOutputPath + "/john-wick-parse_custom.exe";
                p.StartInfo.Arguments = args;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.UseShellExecute = false;
                p.Start();
                p.WaitForExit();
            }
        }
        private void updateConsole(string textToDisplay, Color SEColor, string SEText)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, Color, string>(updateConsole), new object[] { textToDisplay, SEColor, SEText });
                return;
            }

            toolStripStatusLabel2.Text = textToDisplay;
            toolStripStatusLabel3.BackColor = SEColor;
            toolStripStatusLabel3.Text = SEText;
        }
        private void AppendText(string text, Color color, bool addNewLine = false, HorizontalAlignment align = HorizontalAlignment.Left)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, Color, bool, HorizontalAlignment>(AppendText), new object[] { text, color, addNewLine, align });
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
        private void createDir()
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
        private void addPAKs(IEnumerable<string> thePaks, int index)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<IEnumerable<string>, int>(addPAKs), new object[] { thePaks, index });
                return;
            }
            loadOneToolStripMenuItem.DropDownItems.Add(Path.GetFileName(thePaks.ElementAt(index)));
        }
        private void fillWithPAKs()
        {
            if (!Directory.Exists(Properties.Settings.Default.PAKsPath))
            {
                loadOneToolStripMenuItem.Enabled = false;
                loadAllToolStripMenuItem.Enabled = false;
                backupPAKsToolStripMenuItem.Enabled = false;

                updateConsole(".PAK Files Path is missing", Color.FromArgb(255, 244, 66, 66), "Error");
            }
            else
            {
                IEnumerable<string> yourPAKs = Directory.GetFiles(Properties.Settings.Default.PAKsPath).Where(x => x.EndsWith(".pak"));
                PAKsArray = new string[yourPAKs.Count()];
                for (int i = 0; i < yourPAKs.Count(); i++)
                {
                    addPAKs(yourPAKs, i);
                    PAKsArray[i] = Path.GetFileName(yourPAKs.ElementAt(i));
                }
            }
        }
        private void setOutput()
        {
            DefaultOutputPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).ToString() + "\\FModel"; //DOCUMENTS FOLDER BY DEFAULT
            if (string.IsNullOrEmpty(Properties.Settings.Default.ExtractOutput))
            {
                Properties.Settings.Default.ExtractOutput = DefaultOutputPath;
                Properties.Settings.Default.Save();
            }
            else
            {
                DefaultOutputPath = Properties.Settings.Default.ExtractOutput;
            }

            if (!Directory.Exists(DefaultOutputPath))
                Directory.CreateDirectory(DefaultOutputPath);
        }
        private void johnWickCheck()
        {
            bool connection = IsInternetAvailable();
            FileInfo parserInfo;

            if (!File.Exists(DefaultOutputPath + "\\john-wick-parse_custom.exe") && connection == true)
            {
                WebClient Client = new WebClient();
                Client.DownloadFile("https://dl.dropbox.com/s/af5n0wr3wyb5n1u/john-wick-parse_custom.exe?dl=0", DefaultOutputPath + "\\john-wick-parse_custom.exe");
                parserInfo = new FileInfo(DefaultOutputPath + "\\john-wick-parse_custom.exe");

                updateConsole("john-wick-parse_custom.exe downloaded successfully", Color.FromArgb(255, 66, 244, 66), "Success");
            }
            else if (!File.Exists(DefaultOutputPath + "\\john-wick-parse_custom.exe") && connection == false)
            {
                updateConsole("Can't download john-wick-parse_custom.exe, no internet connection", Color.FromArgb(255, 244, 66, 66), "Error");
            }

            if (File.Exists(DefaultOutputPath + "\\john-wick-parse_custom.exe"))
            {
                parserInfo = new FileInfo(DefaultOutputPath + "\\john-wick-parse_custom.exe");

                string url = "https://pastebin.com/raw/d7BCj2NH";
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
                    if (parserInfo.Length != fileSize)
                    {
                        WebClient Client = new WebClient();
                        Client.DownloadFile("https://dl.dropbox.com/s/af5n0wr3wyb5n1u/john-wick-parse_custom.exe?dl=0", DefaultOutputPath + "\\john-wick-parse_custom.exe");

                        updateConsole("john-wick-parse_custom.exe updated successfully", Color.FromArgb(255, 66, 244, 66), "Success");
                    }
                }
                else
                {
                    updateConsole("Can't check if john-wick-parse_custom.exe needs to be updated", Color.FromArgb(255, 244, 66, 66), "Error");
                }
            }
        }
        private void keyCheck()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(keyCheck));
                return;
            }
            AESKeyTextBox.Text = "0x" + Properties.Settings.Default.AESKey;
        }
        private void setScintillaStyle()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(setScintillaStyle));
                return;
            }

            scintilla1.Styles[ScintillaNET.Style.Json.Default].ForeColor = Color.Silver;
            scintilla1.Styles[ScintillaNET.Style.Json.BlockComment].ForeColor = Color.FromArgb(0, 128, 0);
            scintilla1.Styles[ScintillaNET.Style.Json.LineComment].ForeColor = Color.FromArgb(0, 128, 0);
            scintilla1.Styles[ScintillaNET.Style.Json.Number].ForeColor = Color.Green;
            scintilla1.Styles[ScintillaNET.Style.Json.PropertyName].ForeColor = Color.SteelBlue; ;
            scintilla1.Styles[ScintillaNET.Style.Json.String].ForeColor = Color.OrangeRed;
            scintilla1.Styles[ScintillaNET.Style.Json.StringEol].BackColor = Color.OrangeRed;
            scintilla1.Styles[ScintillaNET.Style.Json.Operator].ForeColor = Color.Black;
            scintilla1.Styles[ScintillaNET.Style.LineNumber].ForeColor = Color.DarkGray;
            var nums = scintilla1.Margins[1];
            nums.Width = 30;
            nums.Type = ScintillaNET.MarginType.Number;
            nums.Sensitive = true;
            nums.Mask = 0;

            scintilla1.ClearCmdKey(Keys.Control | Keys.F);
            scintilla1.ClearCmdKey(Keys.Control | Keys.Z);
            scintilla1.Lexer = ScintillaNET.Lexer.Json;
        }
        private void setFont()
        {
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
        }

        //EVENTS
        private async void MainWindow_Load(object sender, EventArgs e)
        {
            SetTreeViewTheme(treeView1.Handle);
            BackupFileName = "\\FortniteGame_" + DateTime.Now.ToString("MMddyyyy") + ".txt";

            await Task.Run(() => {
                setScintillaStyle();
                setFont();
                setOutput();
                SetFolderPermission(DefaultOutputPath);
                createDir();
                keyCheck();
                johnWickCheck();
                fillWithPAKs();
            });
        }
        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            Environment.Exit(Environment.ExitCode);
        }
        private void differenceModeToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (differenceModeToolStripMenuItem.Checked == true)
            {
                loadAllToolStripMenuItem.Text = "Load Difference";
                loadOneToolStripMenuItem.Enabled = false;
                updateModeToolStripMenuItem.Enabled = true;
            }
            if (differenceModeToolStripMenuItem.Checked == false)
            {
                loadAllToolStripMenuItem.Text = "Load All PAKs";
                loadOneToolStripMenuItem.Enabled = true;
                updateModeToolStripMenuItem.Enabled = false;
                if (updateModeToolStripMenuItem.Checked == true)
                    updateModeToolStripMenuItem.Checked = false;
            }
            if (updateModeToolStripMenuItem.Checked == false && differenceModeToolStripMenuItem.Checked == false)
            {
                loadAllToolStripMenuItem.Text = "Load All PAKs";
            }
        }
        private void updateModeToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (updateModeToolStripMenuItem.Checked == true)
            {
                loadAllToolStripMenuItem.Text = "Load And Extract Difference";
                var updateModeForm = new Forms.UpdateModeSettings();
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
                loadAllToolStripMenuItem.Text = "Load Difference";
            }
        }
        private void scintilla1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.F)
            {
                MyFindReplace.ShowFind();
                e.SuppressKeyPress = true;
            }
            else if (e.Shift && e.KeyCode == Keys.F3)
            {
                MyFindReplace.Window.FindPrevious();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.F3)
            {
                MyFindReplace.Window.FindNext();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.H)
            {
                MyFindReplace.ShowReplace();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.I)
            {
                MyFindReplace.ShowIncrementalSearch();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.G)
            {
                GoTo MyGoTo = new GoTo((ScintillaNET.Scintilla)sender);
                MyGoTo.ShowGoToDialog();
                e.SuppressKeyPress = true;
            }
        }
        private void MyFindReplace_KeyPressed(object sender, System.Windows.Forms.KeyEventArgs e)
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
            var aboutForm = new Forms.About();
            if (Application.OpenForms[aboutForm.Name] == null)
            {
                aboutForm.Show();
            }
            else
            {
                Application.OpenForms[aboutForm.Name].Focus();
            }
        }
        #endregion

        #region PAKLIST & FILL TREE
        //METHODS
        private string readPAKGuid(string pakPath)
        {
            using (BinaryReader reader = new BinaryReader(File.Open(pakPath, FileMode.Open)))
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
        private void registerPAKsinDict(string[] allYourPAKs, ToolStripItemClickedEventArgs theSinglePAK = null, bool loadAllPAKs = false)
        {
            for (int i = 0; i < allYourPAKs.Length; i++)
            {
                string arCurrentUsedPAK = allYourPAKs[i]; //SET CURRENT PAK
                string arCurrentUsedPAKGUID = readPAKGuid(Properties.Settings.Default.PAKsPath + "\\" + arCurrentUsedPAK); //SET CURRENT PAK GUID

                if (arCurrentUsedPAKGUID == "0-0-0-0") //NO DYNAMIC PAK IN DICTIONARY
                {
                    jwpmProcess("filelist \"" + Properties.Settings.Default.PAKsPath + "\\" + arCurrentUsedPAK + "\" \"" + DefaultOutputPath + "\" " +  Properties.Settings.Default.AESKey);
                    if (File.Exists(DefaultOutputPath + "\\" + arCurrentUsedPAK + ".txt"))
                    {
                        if (loadAllPAKs == true)
                            if (!File.Exists(DefaultOutputPath + "\\FortnitePAKs.txt"))
                                File.Create(DefaultOutputPath + "\\FortnitePAKs.txt").Dispose();

                        string[] currentUsedPAKLines = File.ReadAllLines(DefaultOutputPath + "\\" + arCurrentUsedPAK + ".txt");
                        for (int ii = 0; ii < currentUsedPAKLines.Length; ii++)
                        {
                            if (arCurrentUsedPAK == "pakchunk10_s1-WindowsClient.pak" || arCurrentUsedPAK == "pakchunk10_s4-WindowsClient.pak" || arCurrentUsedPAK == "pakchunk10_s5-WindowsClient.pak" || arCurrentUsedPAK == "pakchunk10_s6-WindowsClient.pak" || arCurrentUsedPAK == "pakchunk10_s7-WindowsClient.pak" || arCurrentUsedPAK == "pakchunk10_s8-WindowsClient.pak" || arCurrentUsedPAK == "pakchunk11-WindowsClient.pak" || arCurrentUsedPAK == "pakchunk11_s1-WindowsClient.pak" || arCurrentUsedPAK == "pakchunk1-WindowsClient.pak")
                                currentUsedPAKLines[ii] = "FortniteGame/Content/" + currentUsedPAKLines[ii];
                            if (arCurrentUsedPAK == "pakchunk10_s2-WindowsClient.pak")
                                currentUsedPAKLines[ii] = "FortniteGame/Content/Characters/Player/" + currentUsedPAKLines[ii];
                            if (arCurrentUsedPAK == "pakchunk10_s3-WindowsClient.pak")
                                currentUsedPAKLines[ii] = "FortniteGame/Content/Characters/Player/Male/" + currentUsedPAKLines[ii];
                            if (arCurrentUsedPAK == "pakchunk5-WindowsClient.pak")
                                currentUsedPAKLines[ii] = "FortniteGame/Content/L10N/fr/" + currentUsedPAKLines[ii];
                            if (arCurrentUsedPAK == "pakchunk2-WindowsClient.pak")
                                currentUsedPAKLines[ii] = "FortniteGame/Content/L10N/de/" + currentUsedPAKLines[ii];
                            if (arCurrentUsedPAK == "pakchunk7-WindowsClient.pak")
                                currentUsedPAKLines[ii] = "FortniteGame/Content/L10N/pl/" + currentUsedPAKLines[ii];
                            if (arCurrentUsedPAK == "pakchunk8-WindowsClient.pak")
                                currentUsedPAKLines[ii] = "FortniteGame/Content/L10N/ru/" + currentUsedPAKLines[ii];
                            if (arCurrentUsedPAK == "pakchunk9-WindowsClient.pak")
                                currentUsedPAKLines[ii] = "FortniteGame/Content/L10N/zh-CN/" + currentUsedPAKLines[ii];

                            string currentUsedPAKFileName = currentUsedPAKLines[ii].Substring(currentUsedPAKLines[ii].LastIndexOf("/") + 1);
                            if (currentUsedPAKFileName.Contains(".uasset") || currentUsedPAKFileName.Contains(".uexp") || currentUsedPAKFileName.Contains(".ubulk"))
                            {
                                if (!AllPAKsDictionary.ContainsKey(currentUsedPAKFileName.Substring(0, currentUsedPAKFileName.LastIndexOf("."))))
                                {
                                    AllPAKsDictionary.Add(currentUsedPAKFileName.Substring(0, currentUsedPAKFileName.LastIndexOf(".")), arCurrentUsedPAK);
                                }
                            }
                            else
                            {
                                if (!AllPAKsDictionary.ContainsKey(currentUsedPAKFileName))
                                {
                                    AllPAKsDictionary.Add(currentUsedPAKFileName, arCurrentUsedPAK);
                                }
                            }
                        }
                        if (loadAllPAKs == true)
                        {
                            if (arCurrentUsedPAK == "pakchunk10_s1-WindowsClient.pak" || arCurrentUsedPAK == "pakchunk10_s4-WindowsClient.pak" || arCurrentUsedPAK == "pakchunk10_s5-WindowsClient.pak" || arCurrentUsedPAK == "pakchunk10_s6-WindowsClient.pak" || arCurrentUsedPAK == "pakchunk10_s7-WindowsClient.pak" || arCurrentUsedPAK == "pakchunk10_s8-WindowsClient.pak" || arCurrentUsedPAK == "pakchunk11-WindowsClient.pak" || arCurrentUsedPAK == "pakchunk11_s1-WindowsClient.pak" || arCurrentUsedPAK == "pakchunk1-WindowsClient.pak")
                                updateConsole(".PAK mount point: \"/FortniteGame/Content/\"", Color.FromArgb(255, 244, 132, 66), "Waiting");
                            if (arCurrentUsedPAK == "pakchunk10_s2-WindowsClient.pak")
                                updateConsole(".PAK mount point: \"/FortniteGame/Content/Characters/Player/\"", Color.FromArgb(255, 244, 132, 66), "Waiting");
                            if (arCurrentUsedPAK == "pakchunk10_s3-WindowsClient.pak")
                                updateConsole(".PAK mount point: \"/FortniteGame/Content/Characters/Player/Male/\"", Color.FromArgb(255, 244, 132, 66), "Waiting");
                            if (arCurrentUsedPAK == "pakchunk5-WindowsClient.pak")
                                updateConsole(".PAK mount point: \"/FortniteGame/Content/L10N/fr/\"", Color.FromArgb(255, 244, 132, 66), "Waiting");
                            if (arCurrentUsedPAK == "pakchunk2-WindowsClient.pak")
                                updateConsole(".PAK mount point: \"/FortniteGame/Content/L10N/de/\"", Color.FromArgb(255, 244, 132, 66), "Waiting");
                            if (arCurrentUsedPAK == "pakchunk7-WindowsClient.pak")
                                updateConsole(".PAK mount point: \"/FortniteGame/Content/L10N/pl/\"", Color.FromArgb(255, 244, 132, 66), "Waiting");
                            if (arCurrentUsedPAK == "pakchunk8-WindowsClient.pak")
                                updateConsole(".PAK mount point: \"/FortniteGame/Content/L10N/ru/\"", Color.FromArgb(255, 244, 132, 66), "Waiting");
                            if (arCurrentUsedPAK == "pakchunk9-WindowsClient.pak")
                                updateConsole(".PAK mount point: \"/FortniteGame/Content/L10N/zh-CN/\"", Color.FromArgb(255, 244, 132, 66), "Waiting");

                            File.AppendAllLines(DefaultOutputPath + "\\FortnitePAKs.txt", currentUsedPAKLines);
                            File.Delete(DefaultOutputPath + "\\" + arCurrentUsedPAK + ".txt");

                            currentUsedPAK = null;
                            currentUsedPAKGUID = null;
                        }
                    }
                }
                if (theSinglePAK != null)
                {
                    currentUsedPAK = theSinglePAK.ClickedItem.Text;
                    currentUsedPAKGUID = readPAKGuid(Properties.Settings.Default.PAKsPath + "\\" + currentUsedPAK);

                    if (arCurrentUsedPAK != theSinglePAK.ClickedItem.Text) //DELETE EVERYTHING EXCEPT SELECTED PAK
                        File.Delete(DefaultOutputPath + "\\" + arCurrentUsedPAK + ".txt");
                }
            }
            if (theSinglePAK != null && readPAKGuid(Properties.Settings.Default.PAKsPath + "\\" + theSinglePAK.ClickedItem.Text) != "0-0-0-0") //LOADING DYNAMIC PAK
            {
                jwpmProcess("filelist \"" + Properties.Settings.Default.PAKsPath + "\\" + theSinglePAK.ClickedItem.Text + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
            }
            updateConsole("Building tree, please wait...", Color.FromArgb(255, 244, 132, 66), "Loading");
        }
        private void treeParsePath(TreeNodeCollection nodeList, string path) //https://social.msdn.microsoft.com/Forums/en-US/c75c1804-6933-40ba-b17a-0e36ae8bcbb5/how-to-create-a-tree-view-with-full-paths?forum=csharplanguage
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
                Invoke(new Action(() =>
                {
                    nodeList.Add(node);
                }));
            }
            if (path != "")
            {
                treeParsePath(node.Nodes, path);
            }
        }
        private void comparePAKs()
        {
            PAKasTXT = File.ReadAllLines(DefaultOutputPath + "\\FortnitePAKs.txt");
            File.Delete(DefaultOutputPath + "\\FortnitePAKs.txt");

            //ASK DIFFERENCE FILE AND COMPARE
            OpenFileDialog theDialog = new OpenFileDialog();
            theDialog.Title = "Choose your Backup PAK File";
            theDialog.InitialDirectory = DefaultOutputPath;
            theDialog.Multiselect = false;
            theDialog.Filter = "TXT Files (*.txt)|*.txt|All Files (*.*)|*.*";
            Invoke(new Action(() =>
            {
                if (theDialog.ShowDialog() == DialogResult.OK)
                {
                    String[] linesA = File.ReadAllLines(theDialog.FileName);
                    IEnumerable<String> onlyB = PAKasTXT.Except(linesA);
                    IEnumerable<String> removed = linesA.Except(PAKasTXT);

                    File.WriteAllLines(DefaultOutputPath + "\\Result.txt", onlyB);
                    File.WriteAllLines(DefaultOutputPath + "\\Removed.txt", removed);
                }
            }));

            //GET REMOVED FILES
            var removedTXT = File.ReadAllLines(DefaultOutputPath + "\\Removed.txt");
            File.Delete(DefaultOutputPath + "\\Removed.txt");

            List<string> removedItems = new List<string>();
            for (int i = 0; i < removedTXT.Length; i++)
            {
                if (removedTXT[i].Contains("FortniteGame/Content/Athena/Items/Cosmetics/"))
                    removedItems.Add(removedTXT[i].Substring(0, removedTXT[i].LastIndexOf(".")));
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

            PAKasTXT = File.ReadAllLines(DefaultOutputPath + "\\Result.txt");
            File.Delete(DefaultOutputPath + "\\Result.txt");
        }
        private void createPAKList(ToolStripItemClickedEventArgs selectedPAK = null, bool loadAllPAKs = false, bool getDiff = false, bool updateMode = false)
        {
            AllPAKsDictionary = new Dictionary<string, string>();
            diffToExtract = new Dictionary<string, string>();
            Properties.Settings.Default.AESKey = AESKeyTextBox.Text.Substring(2);
            Properties.Settings.Default.Save();

            if (selectedPAK != null)
            {
                updateConsole(Properties.Settings.Default.PAKsPath + "\\" + selectedPAK.ClickedItem.Text, Color.FromArgb(255, 244, 132, 66), "Loading");

                //ADD TO DICTIONNARY
                registerPAKsinDict(PAKsArray, selectedPAK);

                if (!File.Exists(DefaultOutputPath + "\\" + selectedPAK.ClickedItem.Text + ".txt"))
                {
                    updateConsole("Can't read " + selectedPAK.ClickedItem.Text + " with this key", Color.FromArgb(255, 244, 66, 66), "Error");
                }
                else
                {
                    PAKasTXT = File.ReadAllLines(DefaultOutputPath + "\\" + selectedPAK.ClickedItem.Text + ".txt");
                    File.Delete(DefaultOutputPath + "\\" + selectedPAK.ClickedItem.Text + ".txt");

                    Invoke(new Action(() =>
                    {
                        treeView1.BeginUpdate();
                        for (int i = 0; i < PAKasTXT.Length; i++)
                        {
                            treeParsePath(treeView1.Nodes, PAKasTXT[i].Replace(PAKasTXT[i].Split('/').Last(), ""));
                        }
                        treeView1.EndUpdate();
                    }));
                    updateConsole(Properties.Settings.Default.PAKsPath + "\\" + selectedPAK.ClickedItem.Text, Color.FromArgb(255, 66, 244, 66), "Success");
                }
            }
            if (loadAllPAKs == true)
            {
                //ADD TO DICTIONNARY
                registerPAKsinDict(PAKsArray, null, true);

                if (!File.Exists(DefaultOutputPath + "\\FortnitePAKs.txt"))
                {
                    updateConsole("Can't read .PAK files with this key", Color.FromArgb(255, 244, 66, 66), "Error");
                }
                else
                {
                    PAKasTXT = File.ReadAllLines(DefaultOutputPath + "\\FortnitePAKs.txt");
                    File.Delete(DefaultOutputPath + "\\FortnitePAKs.txt");

                    Invoke(new Action(() =>
                    {
                        treeView1.BeginUpdate();
                        for (int i = 0; i < PAKasTXT.Length; i++)
                        {
                            treeParsePath(treeView1.Nodes, PAKasTXT[i].Replace(PAKasTXT[i].Split('/').Last(), ""));
                        }
                        treeView1.EndUpdate();
                    }));
                    updateConsole(Properties.Settings.Default.PAKsPath, Color.FromArgb(255, 66, 244, 66), "Success");
                }
            }
            if (getDiff == true)
            {
                //ADD TO DICTIONNARY
                registerPAKsinDict(PAKsArray, null, true);

                if (!File.Exists(DefaultOutputPath + "\\FortnitePAKs.txt"))
                {
                    updateConsole("Can't read .PAK files with this key", Color.FromArgb(255, 244, 66, 66), "Error");
                }
                else
                {
                    updateConsole("Comparing files...", Color.FromArgb(255, 244, 132, 66), "Loading");
                    comparePAKs();
                    if (updateMode == true)
                    {
                        umFilter(PAKasTXT, diffToExtract);
                        umWorking = true;
                    }

                    Invoke(new Action(() =>
                    {
                        treeView1.BeginUpdate();
                        for (int i = 0; i < PAKasTXT.Length; i++)
                        {
                            treeParsePath(treeView1.Nodes, PAKasTXT[i].Replace(PAKasTXT[i].Split('/').Last(), ""));
                        }
                        treeView1.EndUpdate();
                    }));
                    updateConsole("Files compared", Color.FromArgb(255, 66, 244, 66), "Success");
                }
            }
        }
        private void createBackupList(string[] allYourPAKs)
        {
            for (int i = 0; i < allYourPAKs.Length; i++)
            {
                string arCurrentUsedPAK = allYourPAKs[i]; //SET CURRENT PAK
                string arCurrentUsedPAKGUID = readPAKGuid(Properties.Settings.Default.PAKsPath + "\\" + arCurrentUsedPAK); //SET CURRENT PAK GUID

                if (arCurrentUsedPAKGUID == "0-0-0-0") //NO DYNAMIC PAK IN DICTIONARY
                {
                    jwpmProcess("filelist \"" + Properties.Settings.Default.PAKsPath + "\\" + arCurrentUsedPAK + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
                    if (File.Exists(DefaultOutputPath + "\\" + arCurrentUsedPAK + ".txt"))
                    {
                        if (!File.Exists(DefaultOutputPath + "\\Backup" + BackupFileName))
                            File.Create(DefaultOutputPath + "\\Backup" + BackupFileName).Dispose();

                        string[] currentUsedPAKLines = File.ReadAllLines(DefaultOutputPath + "\\" + arCurrentUsedPAK + ".txt");
                        for (int ii = 0; ii < currentUsedPAKLines.Length; ii++)
                        {
                            if (arCurrentUsedPAK == "pakchunk10_s1-WindowsClient.pak" || arCurrentUsedPAK == "pakchunk10_s4-WindowsClient.pak" || arCurrentUsedPAK == "pakchunk10_s5-WindowsClient.pak" || arCurrentUsedPAK == "pakchunk10_s6-WindowsClient.pak" || arCurrentUsedPAK == "pakchunk10_s7-WindowsClient.pak" || arCurrentUsedPAK == "pakchunk10_s8-WindowsClient.pak" || arCurrentUsedPAK == "pakchunk11-WindowsClient.pak" || arCurrentUsedPAK == "pakchunk11_s1-WindowsClient.pak" || arCurrentUsedPAK == "pakchunk1-WindowsClient.pak")
                                currentUsedPAKLines[ii] = "FortniteGame/Content/" + currentUsedPAKLines[ii];
                            if (arCurrentUsedPAK == "pakchunk10_s2-WindowsClient.pak")
                                currentUsedPAKLines[ii] = "FortniteGame/Content/Characters/Player/" + currentUsedPAKLines[ii];
                            if (arCurrentUsedPAK == "pakchunk10_s3-WindowsClient.pak")
                                currentUsedPAKLines[ii] = "FortniteGame/Content/Characters/Player/Male/" + currentUsedPAKLines[ii];
                            if (arCurrentUsedPAK == "pakchunk5-WindowsClient.pak")
                                currentUsedPAKLines[ii] = "FortniteGame/Content/L10N/fr/" + currentUsedPAKLines[ii];
                            if (arCurrentUsedPAK == "pakchunk2-WindowsClient.pak")
                                currentUsedPAKLines[ii] = "FortniteGame/Content/L10N/de/" + currentUsedPAKLines[ii];
                            if (arCurrentUsedPAK == "pakchunk7-WindowsClient.pak")
                                currentUsedPAKLines[ii] = "FortniteGame/Content/L10N/pl/" + currentUsedPAKLines[ii];
                            if (arCurrentUsedPAK == "pakchunk8-WindowsClient.pak")
                                currentUsedPAKLines[ii] = "FortniteGame/Content/L10N/ru/" + currentUsedPAKLines[ii];
                            if (arCurrentUsedPAK == "pakchunk9-WindowsClient.pak")
                                currentUsedPAKLines[ii] = "FortniteGame/Content/L10N/zh-CN/" + currentUsedPAKLines[ii];
                        }
                        if (arCurrentUsedPAK == "pakchunk10_s1-WindowsClient.pak" || arCurrentUsedPAK == "pakchunk10_s4-WindowsClient.pak" || arCurrentUsedPAK == "pakchunk10_s5-WindowsClient.pak" || arCurrentUsedPAK == "pakchunk10_s6-WindowsClient.pak" || arCurrentUsedPAK == "pakchunk10_s7-WindowsClient.pak" || arCurrentUsedPAK == "pakchunk10_s8-WindowsClient.pak" || arCurrentUsedPAK == "pakchunk11-WindowsClient.pak" || arCurrentUsedPAK == "pakchunk11_s1-WindowsClient.pak" || arCurrentUsedPAK == "pakchunk1-WindowsClient.pak")
                            updateConsole(".PAK mount point: \"/FortniteGame/Content/\"", Color.FromArgb(255, 244, 132, 66), "Waiting");
                        if (arCurrentUsedPAK == "pakchunk10_s2-WindowsClient.pak")
                            updateConsole(".PAK mount point: \"/FortniteGame/Content/Characters/Player/\"", Color.FromArgb(255, 244, 132, 66), "Waiting");
                        if (arCurrentUsedPAK == "pakchunk10_s3-WindowsClient.pak")
                            updateConsole(".PAK mount point: \"/FortniteGame/Content/Characters/Player/Male/\"", Color.FromArgb(255, 244, 132, 66), "Waiting");
                        if (arCurrentUsedPAK == "pakchunk5-WindowsClient.pak")
                            updateConsole(".PAK mount point: \"/FortniteGame/Content/L10N/fr/\"", Color.FromArgb(255, 244, 132, 66), "Waiting");
                        if (arCurrentUsedPAK == "pakchunk2-WindowsClient.pak")
                            updateConsole(".PAK mount point: \"/FortniteGame/Content/L10N/de/\"", Color.FromArgb(255, 244, 132, 66), "Waiting");
                        if (arCurrentUsedPAK == "pakchunk7-WindowsClient.pak")
                            updateConsole(".PAK mount point: \"/FortniteGame/Content/L10N/pl/\"", Color.FromArgb(255, 244, 132, 66), "Waiting");
                        if (arCurrentUsedPAK == "pakchunk8-WindowsClient.pak")
                            updateConsole(".PAK mount point: \"/FortniteGame/Content/L10N/ru/\"", Color.FromArgb(255, 244, 132, 66), "Waiting");
                        if (arCurrentUsedPAK == "pakchunk9-WindowsClient.pak")
                            updateConsole(".PAK mount point: \"/FortniteGame/Content/L10N/zh-CN/\"", Color.FromArgb(255, 244, 132, 66), "Waiting");

                        File.AppendAllLines(DefaultOutputPath + "\\Backup" + BackupFileName, currentUsedPAKLines);
                        File.Delete(DefaultOutputPath + "\\" + arCurrentUsedPAK + ".txt");
                    }
                }
            }
            if (File.Exists(DefaultOutputPath + "\\Backup" + BackupFileName))
                updateConsole("\\Backup" + BackupFileName + " successfully created", Color.FromArgb(255, 66, 244, 66), "Success");
            else
                updateConsole("Can't create " + BackupFileName.Substring(1), Color.FromArgb(255, 244, 66, 66), "Error");
        }
        private void updateModeExtractSave()
        {
            createPAKList(null, false, true, true);

            questStageDict = new Dictionary<string, long>();
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
        private void umFilter(String[] theFile, Dictionary<string, string> diffToExtract)
        {
            List<string> searchResults = new List<string>();

            if (Properties.Settings.Default.UMCosmetics == true)
                searchResults.Add("Athena/Items/Cosmetics/");
            if (Properties.Settings.Default.UMVariants == true)
                searchResults.Add("Athena/Items/CosmeticVariantTokens/");
            if (Properties.Settings.Default.UMConsumablesWeapons == true)
            {
                searchResults.Add("Athena/Items/Consumables/");
                searchResults.Add("Athena/Items/Weapons/");
            }
            if (Properties.Settings.Default.UMTraps == true)
                searchResults.Add("Athena/Items/Traps/");
            if (Properties.Settings.Default.UMChallenges == true)
                searchResults.Add("Athena/Items/ChallengeBundles/");

            if (Properties.Settings.Default.UMTCosmeticsVariants == true)
            {
                searchResults.Add("UI/Foundation/Textures/Icons/Backpacks/");
                searchResults.Add("UI/Foundation/Textures/Icons/Emotes/");
                searchResults.Add("UI/Foundation/Textures/Icons/Heroes/Athena/Soldier/");
                searchResults.Add("UI/Foundation/Textures/Icons/Heroes/Variants/");
                searchResults.Add("UI/Foundation/Textures/Icons/Skydiving/");
                searchResults.Add("UI/Foundation/Textures/Icons/Pets/");
                searchResults.Add("UI/Foundation/Textures/Icons/Wraps/");
            }
            if (Properties.Settings.Default.UMTLoading == true)
            {
                searchResults.Add("FortniteGame/Content/2dAssets/Loadingscreens/");
                searchResults.Add("UI/Foundation/Textures/LoadingScreens/");
            }
            if (Properties.Settings.Default.UMTWeapons == true)
                searchResults.Add("UI/Foundation/Textures/Icons/Weapons/Items/");
            if (Properties.Settings.Default.UMTBanners == true)
            {
                searchResults.Add("FortniteGame/Content/2dAssets/Banners/");
                searchResults.Add("UI/Foundation/Textures/Banner/");
                searchResults.Add("FortniteGame/Content/2dAssets/Sprays/");
                searchResults.Add("FortniteGame/Content/2dAssets/Emoji/");
                searchResults.Add("FortniteGame/Content/2dAssets/Music/");
                searchResults.Add("FortniteGame/Content/2dAssets/Toys/");
            }
            if (Properties.Settings.Default.UMTFeaturedIMGs == true)
                searchResults.Add("UI/Foundation/Textures/BattleRoyale/");
            if (Properties.Settings.Default.UMTAthena == true)
                searchResults.Add("UI/Foundation/Textures/Icons/Athena/");
            if (Properties.Settings.Default.UMTAthena == true)
                searchResults.Add("UI/Foundation/Textures/Icons/Athena/");
            if (Properties.Settings.Default.UMTDevices == true)
                searchResults.Add("UI/Foundation/Textures/Icons/Devices/");
            if (Properties.Settings.Default.UMTVehicles == true)
                searchResults.Add("UI/Foundation/Textures/Icons/Vehicles/");

            for (int i = 0; i < theFile.Length; i++)
            {
                bool b = searchResults.Any(s => theFile[i].Contains(s));
                if (b == true)
                {
                    string filename = theFile[i].Substring(theFile[i].LastIndexOf("/") + 1);
                    if (filename.Contains(".uasset") || filename.Contains(".uexp") || filename.Contains(".ubulk"))
                    {
                        if (!diffToExtract.ContainsKey(filename.Substring(0, filename.LastIndexOf("."))))
                            diffToExtract.Add(filename.Substring(0, filename.LastIndexOf(".")), theFile[i]);
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

                createPAKList(e);
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
                    createPAKList(null, true);
                });
            }
            if (differenceModeToolStripMenuItem.Checked == true && updateModeToolStripMenuItem.Checked == false)
            {
                await Task.Run(() => {
                    createPAKList(null, false, true);
                });
            }
            if (differenceModeToolStripMenuItem.Checked == true && updateModeToolStripMenuItem.Checked == true)
            {
                await Task.Run(() => {
                    updateModeExtractSave();
                });
            }
        }
        private async void backupPAKsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await Task.Run(() => {
                createBackupList(PAKsArray);
            });
        }
        //UPDATE MODE
        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            stopWatch = new Stopwatch();
            stopWatch.Start();
            createDir();
            extractAndSerializeItems(e, true);
        }
        private void backgroundWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            stopWatch.Stop();
            if (e.Cancelled == true)
            {
                updateConsole("Canceled!", Color.FromArgb(255, 244, 66, 66), "Error");
            }
            else if (e.Error != null)
            {
                updateConsole(e.Error.Message, Color.FromArgb(255, 244, 66, 66), "Error");
            }
            else if (umWorking == false)
            {
                updateConsole("Can't read .PAK files with this key", Color.FromArgb(255, 244, 66, 66), "Error");
            }
            else
            {
                TimeSpan ts = stopWatch.Elapsed;
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                updateConsole("Time elapsed: " + elapsedTime, Color.FromArgb(255, 66, 244, 66), "Success");
            }

            SelectedItemsArray = null;
            umWorking = false;
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
        private void getFilesAndFill(TreeNodeMouseClickEventArgs selectedNode)
        {
            List<string> itemsNotToDisplay = new List<string>();
            itemsToDisplay = new List<string>();

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
            var dircount = full.Count(f => f == '/');
            var dirfiles = PAKasTXT.Where(x => x.StartsWith(full) && !x.Replace(full, "").Contains("/"));
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
                itemsNotToDisplay.Add(v.Replace(full, ""));
            }
            itemsToDisplay = itemsNotToDisplay.Distinct().ToList(); //NO DUPLICATION + NO EXTENSION = EASY TO FIND WHAT WE WANT
            Invoke(new Action(() =>
            {
                for (int i = 0; i < itemsToDisplay.Count; i++)
                {
                    listBox1.Items.Add(itemsToDisplay[i]);
                }
                ExtractButton.Enabled = listBox1.SelectedIndex >= 0; //DISABLE EXTRACT BUTTON IF NOTHING IS SELECTED IN LISTBOX
            }));
        }
        private bool CaseInsensitiveContains(string text, string value, StringComparison stringComparison = StringComparison.CurrentCultureIgnoreCase)
        {
            return text.IndexOf(value, stringComparison) >= 0;
        } //FILTER INSENSITIVE
        private void filterItems()
        {
            if (listBox1.InvokeRequired)
            {
                listBox1.Invoke(new Action(filterItems));
                return;
            }

            listBox1.BeginUpdate();
            listBox1.Items.Clear();

            if (!string.IsNullOrEmpty(FilterTextBox.Text))
            {
                for (int i = 0; i < itemsToDisplay.Count; i++)
                {
                    if (CaseInsensitiveContains(itemsToDisplay[i], FilterTextBox.Text))
                    {
                        listBox1.Items.Add(itemsToDisplay[i]);
                    }
                }
            }
            else
            {
                for (int i = 0; i < itemsToDisplay.Count; i++)
                {
                    listBox1.Items.Add(itemsToDisplay[i]);
                }
            }

            listBox1.EndUpdate();
        }
        private void filterTree()
        {
            if (FilterTextBox.Text == "") return;
            if (FilterTextBox.Text.Length <= 2) return;

            for (int i = 0; i < treeView1.Nodes.Count; i++)
            {
                loopNode(treeView1.Nodes[i]);
            }
        }
        private void loopNode(TreeNode theNodes)
        {
            if (theNodes.Text.IndexOf(FilterTextBox.Text) >= 0)
            {
                theNodes.ForeColor = Color.Blue;
                Invoke(new Action(() =>
                {
                    theNodes.Expand();
                    ExpandParentNodes(theNodes);
                }));
            }
            else
                theNodes.ForeColor = Color.Black;

            for (int i = 0; i < theNodes.Nodes.Count; i++)
            {
                loopNode(theNodes.Nodes[i]);
            }
        }
        private void ExpandParentNodes(TreeNode node)
        {
            TreeNode parent = node.Parent;
            if (parent != null)
            {
                parent.Expand();
                parent = parent.Parent;
            }
            else
                parent.Collapse();
        }

        //EVENTS
        private async void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            await Task.Run(() => {
                getFilesAndFill(e);
            });
        }
        private async void FilterTextBox_TextChanged(object sender, EventArgs e)
        {
            await Task.Run(() => {
                filterItems();
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
        private void extractAndSerializeItems(DoWorkEventArgs e, bool updateMode = false)
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
                    SelectedItemsArray = new string[diffToExtract.Count];
                    for (int i = 0; i < diffToExtract.Count; i++) //ADD DICT ITEM TO ARRAY
                    {
                        SelectedItemsArray[i] = diffToExtract.Keys.ElementAt(i);
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

                currentUsedItem = SelectedItemsArray[i].ToString();

                if (currentUsedPAKGUID != null && currentUsedPAKGUID != "0-0-0-0")
                    jwpmProcess("extract \"" + Properties.Settings.Default.PAKsPath + "\\" + currentUsedPAK + "\" \"" + currentUsedItem + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
                else
                    jwpmProcess("extract \"" + Properties.Settings.Default.PAKsPath + "\\" + AllPAKsDictionary[currentUsedItem] + "\" \"" + currentUsedItem + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
                extractedFilePath = Directory.GetFiles(DefaultOutputPath + "\\Extracted", currentUsedItem + ".*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".png")).FirstOrDefault();

                if (extractedFilePath != null)
                {
                    updateConsole(currentUsedItem + " successfully extracted", Color.FromArgb(255, 66, 244, 66), "Success");
                    if (extractedFilePath.Contains(".uasset") || extractedFilePath.Contains(".uexp") || extractedFilePath.Contains(".ubulk"))
                    {
                        jwpmProcess("serialize \"" + extractedFilePath.Substring(0, extractedFilePath.LastIndexOf('.')) + "\"");
                        jsonParseFile();
                    }
                    if (extractedFilePath.Contains(".ufont"))
                        convertToOTF(extractedFilePath);
                    if (extractedFilePath.Contains(".ini"))
                    {
                        Invoke(new Action(() =>
                        {
                            scintilla1.Text = File.ReadAllText(extractedFilePath);
                        }));
                    }
                }
                else
                    updateConsole("Error while extracting " + currentUsedItem, Color.FromArgb(255, 244, 66, 66), "Error");
            }
        }
        private void jsonParseFile()
        {
            try
            {
                string jsonExtractedFilePath = Directory.GetFiles(DefaultOutputPath, currentUsedItem + ".json", SearchOption.AllDirectories).FirstOrDefault();
                if (jsonExtractedFilePath != null)
                {
                    updateConsole(currentUsedItem + " successfully serialized", Color.FromArgb(255, 66, 244, 66), "Success");

                    string parsedJson = JToken.Parse(File.ReadAllText(jsonExtractedFilePath)).ToString();
                    File.Delete(jsonExtractedFilePath);
                    Invoke(new Action(() =>
                    {
                        scintilla1.Text = parsedJson;
                    }));
                    navigateThroughJSON(parsedJson, jsonExtractedFilePath);
                }
                else
                    updateConsole("No serialized file found", Color.FromArgb(255, 244, 66, 66), "Error");
            }
            catch (JsonSerializationException)
            {
                updateConsole(".JSON file too large to be fully displayed", Color.FromArgb(255, 244, 66, 66), "Error");
            }
        }
        private void navigateThroughJSON(string theParsedJSON, string questJSON = null)
        {
            try
            {
                var ItemID = Parser.Items.ItemsIDParser.FromJson(theParsedJSON);
                updateConsole("Parsing " + currentUsedItem + "...", Color.FromArgb(255, 244, 132, 66), "Waiting");
                for (int i = 0; i < ItemID.Length; i++)
                {
                    if (Properties.Settings.Default.createIconForCosmetics == true && (ItemID[i].ExportType.Contains("Athena") && ItemID[i].ExportType.Contains("Item") && ItemID[i].ExportType.Contains("Definition")))
                        createItemIcon(ItemID[i], true);
                    else if (Properties.Settings.Default.createIconForConsumablesWeapons == true && (ItemID[i].ExportType.Contains("FortWeaponRangedItemDefinition") || ItemID[i].ExportType.Contains("FortWeaponMeleeItemDefinition")))
                        createItemIcon(ItemID[i], false, true);
                    else if (Properties.Settings.Default.createIconForTraps == true && (ItemID[i].ExportType.Contains("FortTrapItemDefinition") || ItemID[i].ExportType.Contains("FortContextTrapItemDefinition")))
                        createItemIcon(ItemID[i]);
                    else if (Properties.Settings.Default.createIconForVariants == true && (ItemID[i].ExportType == "FortVariantTokenType"))
                        createItemIcon(ItemID[i], false, false, true);
                    else if (ItemID[i].ExportType == "FortChallengeBundleItemDefinition")
                        createChallengesIcon(ItemID[i], theParsedJSON, questJSON);
                    else if (ItemID[i].ExportType == "Texture2D")
                        convertTexture2D();
                    else if (ItemID[i].ExportType == "SoundWave")
                        convertSoundWave();
                    else
                        updateConsole(currentUsedItem + " successfully extracted", Color.FromArgb(255, 66, 244, 66), "Success");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private void createItemIcon(Parser.Items.ItemsIDParser theItem, bool athIteDef = false, bool consAndWeap = false, bool variant = false)
        {
            updateConsole(currentUsedItem + " is a Cosmetic ID", Color.FromArgb(255, 66, 244, 66), "Success");

            Bitmap bmp = new Bitmap(522, 522);
            Graphics g = Graphics.FromImage(bmp);
            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            getItemRarity(theItem, g);

            itemIconPath = string.Empty;
            if (Properties.Settings.Default.loadFeaturedImage == false)
            {
                getItemIcon(theItem, false);
            }
            if (Properties.Settings.Default.loadFeaturedImage == true)
            {
                getItemIcon(theItem, true);
            }

            #region DRAW ICON
            if (File.Exists(itemIconPath))
            {
                Image ItemIcon = Image.FromFile(itemIconPath);
                g.DrawImage(Forms.Settings.ResizeImage(ItemIcon, 512, 512), new Point(5, 5));
            }
            else
            {
                Image ItemIcon = Properties.Resources.unknown512;
                g.DrawImage(ItemIcon, new Point(0, 0));
            }
            #endregion

            #region WATERMARK
            if (umWorking == false && (Properties.Settings.Default.isWatermark == true && !string.IsNullOrEmpty(Properties.Settings.Default.wFilename)))
            {
                Image watermark = Image.FromFile(Properties.Settings.Default.wFilename);
                var opacityImage = SetImageOpacity(watermark, (float)Properties.Settings.Default.wOpacity / 100);
                g.DrawImage(Forms.Settings.ResizeImage(opacityImage, Properties.Settings.Default.wSize, Properties.Settings.Default.wSize), (522 - Properties.Settings.Default.wSize) / 2, (522 - Properties.Settings.Default.wSize) / 2, Properties.Settings.Default.wSize, Properties.Settings.Default.wSize);
            }
            if (umWorking == true && (Properties.Settings.Default.UMWatermark == true && !string.IsNullOrEmpty(Properties.Settings.Default.UMFilename)))
            {
                Image watermark = Image.FromFile(Properties.Settings.Default.UMFilename);
                var opacityImage = SetImageOpacity(watermark, (float)Properties.Settings.Default.UMOpacity / 100);
                g.DrawImage(Forms.Settings.ResizeImage(opacityImage, Properties.Settings.Default.UMSize, Properties.Settings.Default.UMSize), (522 - Properties.Settings.Default.UMSize) / 2, (522 - Properties.Settings.Default.UMSize) / 2, Properties.Settings.Default.UMSize, Properties.Settings.Default.UMSize);
            }
            #endregion

            Image bg512 = Properties.Resources.BG512;
            g.DrawImage(bg512, new Point(5, 383));

            #region DRAW TEXT
            try
            {
                g.DrawString(theItem.DisplayName, new Font(pfc.Families[0], 35), new SolidBrush(Color.White), new Point(522 / 2, 395), centeredString);
            }
            catch (NullReferenceException)
            {
                AppendText(currentUsedItem + " ", Color.Red);
                AppendText("No ", Color.Black);
                AppendText("DisplayName ", Color.SteelBlue);
                AppendText("found", Color.Black, true);
            } //NAME
            try
            {
                g.DrawString(theItem.Description, new Font("Arial", 10), new SolidBrush(Color.White), new Point(522 / 2, 465), centeredStringLine);
            }
            catch (NullReferenceException)
            {
                AppendText(currentUsedItem + " ", Color.Red);
                AppendText("No ", Color.Black);
                AppendText("Description ", Color.SteelBlue);
                AppendText("found", Color.Black, true);
            } //DESCRIPTION
            if (athIteDef == true)
            {
                try
                {
                    g.DrawString(theItem.ShortDescription, new Font(pfc.Families[0], 13), new SolidBrush(Color.White), new Point(5, 500));
                }
                catch (NullReferenceException)
                {
                    AppendText(currentUsedItem + " ", Color.Red);
                    AppendText("No ", Color.Black);
                    AppendText("ShortDescription ", Color.SteelBlue);
                    AppendText("found", Color.Black, true);
                } //TYPE
                try
                {
                    g.DrawString(theItem.GameplayTags.GameplayTagsGameplayTags[Array.FindIndex(theItem.GameplayTags.GameplayTagsGameplayTags, x => x.StartsWith("Cosmetics.Source."))].Substring(17), new Font(pfc.Families[0], 13), new SolidBrush(Color.White), new Point(522 - 5, 500), rightString);
                }
                catch (NullReferenceException)
                {
                    AppendText(currentUsedItem + " ", Color.Red);
                    AppendText("No ", Color.Black);
                    AppendText("GameplayTags ", Color.SteelBlue);
                    AppendText("found", Color.Black, true);
                }
                catch (IndexOutOfRangeException)
                {
                    AppendText(currentUsedItem + " ", Color.Red);
                    AppendText("No ", Color.Black);
                    AppendText("GameplayTags ", Color.SteelBlue);
                    AppendText("as ", Color.Black);
                    AppendText("Cosmetics.Source ", Color.SteelBlue);
                    AppendText("found", Color.Black, true);
                } //COSMETIC SOURCE
            }
            if (consAndWeap == true)
                try
                {
                    g.DrawString(theItem.GameplayTags.GameplayTagsGameplayTags[Array.FindIndex(theItem.GameplayTags.GameplayTagsGameplayTags, x => x.StartsWith("Athena.ItemAction."))].Substring(18), new Font(pfc.Families[0], 13), new SolidBrush(Color.White), new Point(522 - 5, 500), rightString);
                }
                catch (NullReferenceException)
                {
                    AppendText(currentUsedItem + " ", Color.Red);
                    AppendText("No ", Color.Black);
                    AppendText("GameplayTags ", Color.SteelBlue);
                    AppendText("found", Color.Black, true);
                }
                catch (IndexOutOfRangeException)
                {
                    try
                    {
                        g.DrawString(theItem.GameplayTags.GameplayTagsGameplayTags[Array.FindIndex(theItem.GameplayTags.GameplayTagsGameplayTags, x => x.StartsWith("Weapon."))].Substring(7), new Font(pfc.Families[0], 13), new SolidBrush(Color.White), new Point(522 - 5, 500), rightString);
                    }
                    catch (NullReferenceException)
                    {
                        AppendText(currentUsedItem + " ", Color.Red);
                        AppendText("No ", Color.Black);
                        AppendText("GameplayTags ", Color.SteelBlue);
                        AppendText("found", Color.Black, true);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        AppendText(currentUsedItem + " ", Color.Red);
                        AppendText("No ", Color.Black);
                        AppendText("GameplayTags ", Color.SteelBlue);
                        AppendText("as ", Color.Black);
                        AppendText("Athena.ItemAction ", Color.SteelBlue);
                        AppendText("or ", Color.Black);
                        AppendText("Weapon ", Color.SteelBlue);
                        AppendText("found", Color.Black, true);
                    }
                } //ACTION
            if (variant == true)
            {
                try
                {
                    g.DrawString(theItem.ShortDescription, new Font(pfc.Families[0], 13), new SolidBrush(Color.White), new Point(5, 500));
                }
                catch (NullReferenceException)
                {
                    AppendText(currentUsedItem + " ", Color.Red);
                    AppendText("No ", Color.Black);
                    AppendText("ShortDescription ", Color.SteelBlue);
                    AppendText("found", Color.Black, true);
                } //TYPE
                try
                {
                    g.DrawString(theItem.cosmetic_item, new Font(pfc.Families[0], 13), new SolidBrush(Color.White), new Point(522 - 5, 500), rightString);
                }
                catch (NullReferenceException)
                {
                    AppendText(currentUsedItem + " ", Color.Red);
                    AppendText("No ", Color.Black);
                    AppendText("Cosmetic Item ", Color.SteelBlue);
                    AppendText("found", Color.Black, true);
                } //COSMETIC ITEM
            }
            try
            {
                if (theItem.GameplayTags.GameplayTagsGameplayTags[Array.FindIndex(theItem.GameplayTags.GameplayTagsGameplayTags, x => x.StartsWith("Cosmetics.UserFacingFlags."))].Contains("Animated"))
                {
                    Image animatedLogo = Properties.Resources.T_Icon_Animated_64;
                    g.DrawImage(Forms.Settings.ResizeImage(animatedLogo, 32, 32), new Point(6, -2));
                }
                else if (theItem.GameplayTags.GameplayTagsGameplayTags[Array.FindIndex(theItem.GameplayTags.GameplayTagsGameplayTags, x => x.StartsWith("Cosmetics.UserFacingFlags."))].Contains("HasUpgradeQuests"))
                {
                    Image questLogo = Properties.Resources.T_Icon_Quests_64;
                    g.DrawImage(Forms.Settings.ResizeImage(questLogo, 32, 40), new Point(6, 6));
                }
                else if (theItem.GameplayTags.GameplayTagsGameplayTags[Array.FindIndex(theItem.GameplayTags.GameplayTagsGameplayTags, x => x.StartsWith("Cosmetics.UserFacingFlags."))].Contains("HasVariants"))
                {
                    Image variantsLogo = Properties.Resources.T_Icon_Variant_64;
                    g.DrawImage(Forms.Settings.ResizeImage(variantsLogo, 32, 32), new Point(6, 6));
                }
                else if (theItem.GameplayTags.GameplayTagsGameplayTags[Array.FindIndex(theItem.GameplayTags.GameplayTagsGameplayTags, x => x.StartsWith("Cosmetics.UserFacingFlags."))].Contains("Reactive"))
                {
                    Image reactiveLogo = Properties.Resources.T_Icon_Adaptive_64;
                    g.DrawImage(Forms.Settings.ResizeImage(reactiveLogo, 32, 32), new Point(6, 6));
                }
                else if (theItem.GameplayTags.GameplayTagsGameplayTags[Array.FindIndex(theItem.GameplayTags.GameplayTagsGameplayTags, x => x.StartsWith("Cosmetics.UserFacingFlags."))].Contains("Traversal"))
                {
                    Image traversalLogo = Properties.Resources.T_Icon_Traversal_64;
                    g.DrawImage(Forms.Settings.ResizeImage(traversalLogo, 32, 32), new Point(6, 3));
                }
            }
            catch (Exception)
            {

            } //COSMETIC USER FACING FLAGS
            #endregion

            pictureBox1.Image = bmp;
            updateConsole(theItem.DisplayName, Color.FromArgb(255, 66, 244, 66), "Success");
            if (autoSaveImagesToolStripMenuItem.Checked == true || updateModeToolStripMenuItem.Checked == true)
            {
                Invoke(new Action(() =>
                {
                    pictureBox1.Image.Save(DefaultOutputPath + "\\Icons\\" + currentUsedItem + ".png", ImageFormat.Png);
                }));
                AppendText(currentUsedItem, Color.DarkRed);
                AppendText(" successfully saved", Color.Black, true);
            }
        }
        private void getItemRarity(Parser.Items.ItemsIDParser theItem, Graphics toDrawOn)
        {
            if (theItem.Rarity == "EFortRarity::Legendary")
            {
                Image RarityBG = Properties.Resources.I512;
                toDrawOn.DrawImage(RarityBG, new Point(0, 0));
            }
            if (theItem.Rarity == "EFortRarity::Masterwork")
            {
                Image RarityBG = Properties.Resources.T512;
                toDrawOn.DrawImage(RarityBG, new Point(0, 0));
            }
            if (theItem.Rarity == "EFortRarity::Elegant")
            {
                Image RarityBG = Properties.Resources.M512;
                toDrawOn.DrawImage(RarityBG, new Point(0, 0));
            }
            if (theItem.Rarity == "EFortRarity::Fine")
            {
                Image RarityBG = Properties.Resources.L512;
                toDrawOn.DrawImage(RarityBG, new Point(0, 0));
            }
            if (theItem.Rarity == "EFortRarity::Quality")
            {
                Image RarityBG = Properties.Resources.E512;
                toDrawOn.DrawImage(RarityBG, new Point(0, 0));
            }
            if (theItem.Rarity == "EFortRarity::Sturdy")
            {
                Image RarityBG = Properties.Resources.R512;
                toDrawOn.DrawImage(RarityBG, new Point(0, 0));
            }
            if (theItem.Rarity == "EFortRarity::Handmade")
            {
                Image RarityBG = Properties.Resources.C512;
                toDrawOn.DrawImage(RarityBG, new Point(0, 0));
            }
            if (theItem.Rarity == null)
            {
                Image RarityBG = Properties.Resources.U512;
                toDrawOn.DrawImage(RarityBG, new Point(0, 0));
            }
        }
        private void getItemIcon(Parser.Items.ItemsIDParser theItem, bool featured = false)
        {
            if (featured == false)
            {
                wasFeatured = false;
                searchAthIteDefIcon(theItem);
            }
            if (featured == true)
            {
                if (theItem.DisplayAssetPath != null && theItem.DisplayAssetPath.AssetPathName.Contains("/Game/Catalog/DisplayAssets/") && theItem.ExportType != "AthenaItemWrapDefinition")
                {
                    string catalogName = theItem.DisplayAssetPath.AssetPathName;
                    searchFeaturedCharacterIcon(theItem, catalogName);
                }
                else if (theItem.DisplayAssetPath == null && theItem.ExportType != "AthenaItemWrapDefinition")
                {
                    searchFeaturedCharacterIcon(theItem, "DA_Featured_" + currentUsedItem, true);
                }
                else
                {
                    getItemIcon(theItem, false);
                }
            }
        }
        private void searchAthIteDefIcon(Parser.Items.ItemsIDParser theItem)
        {
            if (theItem.HeroDefinition != null)
            {
                if (currentUsedPAKGUID != null && currentUsedPAKGUID != "0-0-0-0")
                    jwpmProcess("extract \"" + Properties.Settings.Default.PAKsPath + "\\" + currentUsedPAK + "\" \"" + theItem.HeroDefinition + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
                else
                    jwpmProcess("extract \"" + Properties.Settings.Default.PAKsPath + "\\" + AllPAKsDictionary[theItem.HeroDefinition] + "\" \"" + theItem.HeroDefinition + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
                string HeroFilePath = Directory.GetFiles(DefaultOutputPath + "\\Extracted", theItem.HeroDefinition + ".*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".png")).FirstOrDefault();

                if (HeroFilePath != null)
                {
                    updateConsole(theItem.HeroDefinition + " successfully extracted", Color.FromArgb(255, 66, 244, 66), "Success");
                    if (HeroFilePath.Contains(".uasset") || HeroFilePath.Contains(".uexp") || HeroFilePath.Contains(".ubulk"))
                    {
                        jwpmProcess("serialize \"" + HeroFilePath.Substring(0, HeroFilePath.LastIndexOf('.')) + "\"");
                        try
                        {
                            string jsonExtractedFilePath = Directory.GetFiles(DefaultOutputPath, theItem.HeroDefinition + ".json", SearchOption.AllDirectories).FirstOrDefault();
                            if (jsonExtractedFilePath != null)
                            {
                                updateConsole(theItem.HeroDefinition + " successfully serialized", Color.FromArgb(255, 66, 244, 66), "Success");

                                string parsedJson = JToken.Parse(File.ReadAllText(jsonExtractedFilePath)).ToString();
                                File.Delete(jsonExtractedFilePath);
                                var ItemID = Parser.Items.ItemsIDParser.FromJson(parsedJson);
                                updateConsole("Parsing " + theItem.HeroDefinition + "...", Color.FromArgb(255, 244, 132, 66), "Waiting");
                                for (int i = 0; i < ItemID.Length; i++)
                                {
                                    if (ItemID[i].LargePreviewImage != null)
                                    {
                                        string textureFile = Path.GetFileName(ItemID[i].LargePreviewImage.AssetPathName).Substring(0, Path.GetFileName(ItemID[i].LargePreviewImage.AssetPathName).LastIndexOf('.'));

                                        if (currentUsedPAKGUID != null && currentUsedPAKGUID != "0-0-0-0")
                                            jwpmProcess("extract \"" + Properties.Settings.Default.PAKsPath + "\\" + currentUsedPAK + "\" \"" + textureFile + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
                                        else
                                            jwpmProcess("extract \"" + Properties.Settings.Default.PAKsPath + "\\" + AllPAKsDictionary[textureFile] + "\" \"" + textureFile + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
                                        string textureFilePath = Directory.GetFiles(DefaultOutputPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".png")).FirstOrDefault();

                                        if (textureFilePath != null)
                                        {
                                            jwpmProcess("texture \"" + textureFilePath.Substring(0, textureFilePath.LastIndexOf('\\')) + "\\" + textureFile + "\"");
                                            itemIconPath = textureFilePath.Substring(0, textureFilePath.LastIndexOf('\\')) + "\\" + textureFile + ".png";
                                            updateConsole(textureFile + " successfully converted to .PNG", Color.FromArgb(255, 66, 244, 66), "Success");
                                        }
                                        else
                                            updateConsole("Error while extracting " + textureFile, Color.FromArgb(255, 244, 66, 66), "Error");
                                    }
                                }
                            }
                            else
                                updateConsole("No serialized file found", Color.FromArgb(255, 244, 66, 66), "Error");
                        }
                        catch (JsonSerializationException)
                        {
                            updateConsole(".JSON file too large to be fully displayed", Color.FromArgb(255, 244, 66, 66), "Error");
                        }
                    }
                }
                else
                    updateConsole("Error while extracting " + theItem.HeroDefinition, Color.FromArgb(255, 244, 66, 66), "Error");
            }
            else if (theItem.WeaponDefinition != null)
            {
                //MANUAL FIX
                if (theItem.WeaponDefinition == "WID_Harvest_Pickaxe_NutCracker")
                    theItem.WeaponDefinition = "WID_Harvest_Pickaxe_Nutcracker";
                if (theItem.WeaponDefinition == "WID_Harvest_Pickaxe_Wukong")
                    theItem.WeaponDefinition = "WID_Harvest_Pickaxe_WuKong";

                if (currentUsedPAKGUID != null && currentUsedPAKGUID != "0-0-0-0")
                    jwpmProcess("extract \"" + Properties.Settings.Default.PAKsPath + "\\" + currentUsedPAK + "\" \"" + theItem.WeaponDefinition + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
                else
                    jwpmProcess("extract \"" + Properties.Settings.Default.PAKsPath + "\\" + AllPAKsDictionary[theItem.WeaponDefinition] + "\" \"" + theItem.WeaponDefinition + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
                string WeaponFilePath = Directory.GetFiles(DefaultOutputPath + "\\Extracted", theItem.WeaponDefinition + ".*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".png")).FirstOrDefault();

                if (WeaponFilePath != null)
                {
                    updateConsole(theItem.WeaponDefinition + " successfully extracted", Color.FromArgb(255, 66, 244, 66), "Success");
                    if (WeaponFilePath.Contains(".uasset") || WeaponFilePath.Contains(".uexp") || WeaponFilePath.Contains(".ubulk"))
                    {
                        jwpmProcess("serialize \"" + WeaponFilePath.Substring(0, WeaponFilePath.LastIndexOf('.')) + "\"");
                        try
                        {
                            string jsonExtractedFilePath = Directory.GetFiles(DefaultOutputPath, theItem.WeaponDefinition + ".json", SearchOption.AllDirectories).FirstOrDefault();
                            if (jsonExtractedFilePath != null)
                            {
                                updateConsole(theItem.WeaponDefinition + " successfully serialized", Color.FromArgb(255, 66, 244, 66), "Success");

                                string parsedJson = JToken.Parse(File.ReadAllText(jsonExtractedFilePath)).ToString();
                                File.Delete(jsonExtractedFilePath);
                                var ItemID = Parser.Items.ItemsIDParser.FromJson(parsedJson);
                                updateConsole("Parsing " + theItem.WeaponDefinition + "...", Color.FromArgb(255, 244, 132, 66), "Waiting");
                                for (int i = 0; i < ItemID.Length; i++)
                                {
                                    if (ItemID[i].LargePreviewImage != null)
                                    {
                                        string textureFile = Path.GetFileName(ItemID[i].LargePreviewImage.AssetPathName).Substring(0, Path.GetFileName(ItemID[i].LargePreviewImage.AssetPathName).LastIndexOf('.'));

                                        if (currentUsedPAKGUID != null && currentUsedPAKGUID != "0-0-0-0")
                                            jwpmProcess("extract \"" + Properties.Settings.Default.PAKsPath + "\\" + currentUsedPAK + "\" \"" + textureFile + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
                                        else
                                            jwpmProcess("extract \"" + Properties.Settings.Default.PAKsPath + "\\" + AllPAKsDictionary[textureFile] + "\" \"" + textureFile + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
                                        string textureFilePath = Directory.GetFiles(DefaultOutputPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".png")).FirstOrDefault();

                                        if (textureFilePath != null)
                                        {
                                            jwpmProcess("texture \"" + textureFilePath.Substring(0, textureFilePath.LastIndexOf('\\')) + "\\" + textureFile + "\"");
                                            itemIconPath = textureFilePath.Substring(0, textureFilePath.LastIndexOf('\\')) + "\\" + textureFile + ".png";
                                            updateConsole(textureFile + " successfully converted to .PNG", Color.FromArgb(255, 66, 244, 66), "Success");
                                        }
                                        else
                                            updateConsole("Error while extracting " + textureFile, Color.FromArgb(255, 244, 66, 66), "Error");
                                    }
                                }
                            }
                            else
                                updateConsole("No serialized file found", Color.FromArgb(255, 244, 66, 66), "Error");
                        }
                        catch (JsonSerializationException)
                        {
                            updateConsole(".JSON file too large to be fully displayed", Color.FromArgb(255, 244, 66, 66), "Error");
                        }
                    }
                }
                else
                    updateConsole("Error while extracting " + theItem.WeaponDefinition, Color.FromArgb(255, 244, 66, 66), "Error");
            }
            else
                searchLargeSmallIcon(theItem);
        }
        private void searchLargeSmallIcon(Parser.Items.ItemsIDParser theItem)
        {
            if (theItem.LargePreviewImage != null)
            {
                string textureFile = Path.GetFileName(theItem.LargePreviewImage.AssetPathName).Substring(0, Path.GetFileName(theItem.LargePreviewImage.AssetPathName).LastIndexOf('.'));

                if (currentUsedPAKGUID != null && currentUsedPAKGUID != "0-0-0-0")
                    jwpmProcess("extract \"" + Properties.Settings.Default.PAKsPath + "\\" + currentUsedPAK + "\" \"" + textureFile + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
                else
                    jwpmProcess("extract \"" + Properties.Settings.Default.PAKsPath + "\\" + AllPAKsDictionary[textureFile] + "\" \"" + textureFile + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
                string textureFilePath = Directory.GetFiles(DefaultOutputPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".png")).FirstOrDefault();

                if (textureFilePath != null)
                {
                    jwpmProcess("texture \"" + textureFilePath.Substring(0, textureFilePath.LastIndexOf('\\')) + "\\" + textureFile + "\"");
                    itemIconPath = textureFilePath.Substring(0, textureFilePath.LastIndexOf('\\')) + "\\" + textureFile + ".png";
                    updateConsole(textureFile + " successfully converted to .PNG", Color.FromArgb(255, 66, 244, 66), "Success");
                }
                else
                    updateConsole("Error while extracting " + textureFile, Color.FromArgb(255, 244, 66, 66), "Error");
            }
            else if (theItem.SmallPreviewImage != null)
            {
                string textureFile = Path.GetFileName(theItem.SmallPreviewImage.AssetPathName).Substring(0, Path.GetFileName(theItem.SmallPreviewImage.AssetPathName).LastIndexOf('.'));

                if (currentUsedPAKGUID != null && currentUsedPAKGUID != "0-0-0-0")
                    jwpmProcess("extract \"" + Properties.Settings.Default.PAKsPath + "\\" + currentUsedPAK + "\" \"" + textureFile + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
                else
                    jwpmProcess("extract \"" + Properties.Settings.Default.PAKsPath + "\\" + AllPAKsDictionary[textureFile] + "\" \"" + textureFile + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
                string textureFilePath = Directory.GetFiles(DefaultOutputPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".png")).FirstOrDefault();

                if (textureFilePath != null)
                {
                    jwpmProcess("texture \"" + textureFilePath.Substring(0, textureFilePath.LastIndexOf('\\')) + "\\" + textureFile + "\"");
                    itemIconPath = textureFilePath.Substring(0, textureFilePath.LastIndexOf('\\')) + "\\" + textureFile + ".png";
                    updateConsole(textureFile + " successfully converted to .PNG", Color.FromArgb(255, 66, 244, 66), "Success");
                }
                else
                    updateConsole("Error while extracting " + textureFile, Color.FromArgb(255, 244, 66, 66), "Error");
            }
        }
        private void searchFeaturedCharacterIcon(Parser.Items.ItemsIDParser theItem, string catName, bool manualSearch = false)
        {
            if (manualSearch == false)
            {
                currentUsedItem = catName.Substring(catName.LastIndexOf('.') + 1);

                if (currentUsedPAKGUID != null && currentUsedPAKGUID != "0-0-0-0")
                    jwpmProcess("extract \"" + Properties.Settings.Default.PAKsPath + "\\" + currentUsedPAK + "\" \"" + catName.Substring(catName.LastIndexOf('.') + 1) + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
                else
                    jwpmProcess("extract \"" + Properties.Settings.Default.PAKsPath + "\\" + AllPAKsDictionary[catName.Substring(catName.LastIndexOf('.') + 1)] + "\" \"" + catName.Substring(catName.LastIndexOf('.') + 1) + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
                string CatalogFilePath = Directory.GetFiles(DefaultOutputPath + "\\Extracted", catName.Substring(catName.LastIndexOf('.') + 1) + ".*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".png")).FirstOrDefault();

                if (CatalogFilePath != null)
                {
                    wasFeatured = true;
                    updateConsole(catName.Substring(catName.LastIndexOf('.') + 1) + " successfully extracted", Color.FromArgb(255, 66, 244, 66), "Success");
                    if (CatalogFilePath.Contains(".uasset") || CatalogFilePath.Contains(".uexp") || CatalogFilePath.Contains(".ubulk"))
                    {
                        jwpmProcess("serialize \"" + CatalogFilePath.Substring(0, CatalogFilePath.LastIndexOf('.')) + "\"");
                        try
                        {
                            string jsonExtractedFilePath = Directory.GetFiles(DefaultOutputPath, catName.Substring(catName.LastIndexOf('.') + 1) + ".json", SearchOption.AllDirectories).FirstOrDefault();
                            if (jsonExtractedFilePath != null)
                            {
                                updateConsole(catName.Substring(catName.LastIndexOf('.') + 1) + " successfully serialized", Color.FromArgb(255, 66, 244, 66), "Success");
                                string parsedJson = JToken.Parse(File.ReadAllText(jsonExtractedFilePath)).ToString();
                                File.Delete(jsonExtractedFilePath);
                                var FeaturedID = Parser.Featured.FeaturedParser.FromJson(parsedJson);
                                updateConsole("Parsing " + catName.Substring(catName.LastIndexOf('.') + 1) + "...", Color.FromArgb(255, 244, 132, 66), "Waiting");
                                for (int i = 0; i < FeaturedID.Length; i++)
                                {
                                    if (FeaturedID[i].DetailsImage != null)
                                    {
                                        string textureFile = FeaturedID[i].DetailsImage.ResourceObject;

                                        if (currentUsedPAKGUID != null && currentUsedPAKGUID != "0-0-0-0")
                                            jwpmProcess("extract \"" + Properties.Settings.Default.PAKsPath + "\\" + currentUsedPAK + "\" \"" + textureFile + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
                                        else
                                            jwpmProcess("extract \"" + Properties.Settings.Default.PAKsPath + "\\" + AllPAKsDictionary[textureFile] + "\" \"" + textureFile + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
                                        string textureFilePath = Directory.GetFiles(DefaultOutputPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".png")).FirstOrDefault();

                                        if (textureFilePath != null && textureFilePath.Contains("MI_UI_FeaturedRenderSwitch_"))
                                        {
                                            updateConsole(textureFile + " successfully extracted", Color.FromArgb(255, 66, 244, 66), "Success");
                                            if (textureFilePath.Contains(".uasset") || textureFilePath.Contains(".uexp") || textureFilePath.Contains(".ubulk"))
                                            {
                                                jwpmProcess("serialize \"" + textureFilePath.Substring(0, textureFilePath.LastIndexOf('.')) + "\"");
                                                try
                                                {
                                                    string jsonRSMExtractedFilePath = Directory.GetFiles(DefaultOutputPath, textureFile + ".json", SearchOption.AllDirectories).FirstOrDefault();
                                                    if (jsonRSMExtractedFilePath != null)
                                                    {
                                                        updateConsole(textureFile + " successfully serialized", Color.FromArgb(255, 66, 244, 66), "Success");
                                                        string parsedRSMJson = JToken.Parse(File.ReadAllText(jsonRSMExtractedFilePath)).ToString();
                                                        File.Delete(jsonRSMExtractedFilePath);
                                                        var RSMID = Parser.RenderMat.RenderSwitchMaterial.FromJson(parsedRSMJson);
                                                        updateConsole("Parsing " + textureFile + "...", Color.FromArgb(255, 244, 132, 66), "Waiting");
                                                        for (int ii = 0; ii < RSMID.Length; ii++)
                                                        {
                                                            if (RSMID[ii].TextureParameterValues.FirstOrDefault().ParameterValue != null)
                                                            {
                                                                string textureFile2 = RSMID[ii].TextureParameterValues.FirstOrDefault().ParameterValue;

                                                                if (currentUsedPAKGUID != null && currentUsedPAKGUID != "0-0-0-0")
                                                                    jwpmProcess("extract \"" + Properties.Settings.Default.PAKsPath + "\\" + currentUsedPAK + "\" \"" + textureFile2 + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
                                                                else
                                                                    jwpmProcess("extract \"" + Properties.Settings.Default.PAKsPath + "\\" + AllPAKsDictionary[textureFile2] + "\" \"" + textureFile2 + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
                                                                string textureFilePath2 = Directory.GetFiles(DefaultOutputPath + "\\Extracted", textureFile2 + ".*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".png")).FirstOrDefault();

                                                                if (textureFilePath2 != null)
                                                                {
                                                                    jwpmProcess("texture \"" + textureFilePath2.Substring(0, textureFilePath2.LastIndexOf('\\')) + "\\" + textureFile2 + "\"");
                                                                    itemIconPath = textureFilePath2.Substring(0, textureFilePath2.LastIndexOf('\\')) + "\\" + textureFile2 + ".png";
                                                                    updateConsole(textureFile2 + " successfully converted to .PNG", Color.FromArgb(255, 66, 244, 66), "Success");
                                                                }
                                                                else
                                                                    updateConsole("Error while extracting " + textureFile2, Color.FromArgb(255, 244, 66, 66), "Error");
                                                            }
                                                        }
                                                    }
                                                }
                                                catch (JsonSerializationException)
                                                {
                                                    updateConsole(".JSON file too large to be fully displayed", Color.FromArgb(255, 244, 66, 66), "Error");
                                                }
                                            }
                                        }
                                        else if (textureFilePath != null && !textureFilePath.Contains("MI_UI_FeaturedRenderSwitch_"))
                                        {
                                            jwpmProcess("texture \"" + textureFilePath.Substring(0, textureFilePath.LastIndexOf('\\')) + "\\" + textureFile + "\"");
                                            itemIconPath = textureFilePath.Substring(0, textureFilePath.LastIndexOf('\\')) + "\\" + textureFile + ".png";
                                            updateConsole(textureFile + " successfully converted to .PNG", Color.FromArgb(255, 66, 244, 66), "Success");
                                        }
                                        else
                                            updateConsole("Error while extracting " + textureFile, Color.FromArgb(255, 244, 66, 66), "Error");
                                    }
                                }
                            }
                        }
                        catch (JsonSerializationException)
                        {
                            updateConsole(".JSON file too large to be fully displayed", Color.FromArgb(255, 244, 66, 66), "Error");
                        }
                    }
                }
                else
                    updateConsole("Error while extracting " + catName.Substring(catName.LastIndexOf('.') + 1), Color.FromArgb(255, 244, 66, 66), "Error");
            }
            if (manualSearch == true)
            {
                if (AllPAKsDictionary.ContainsKey(catName))
                {
                    currentUsedItem = catName;

                    if (currentUsedPAKGUID != null && currentUsedPAKGUID != "0-0-0-0")
                        jwpmProcess("extract \"" + Properties.Settings.Default.PAKsPath + "\\" + currentUsedPAK + "\" \"" + catName + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
                    else
                        jwpmProcess("extract \"" + Properties.Settings.Default.PAKsPath + "\\" + AllPAKsDictionary[catName] + "\" \"" + catName + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
                    string CatalogFilePath = Directory.GetFiles(DefaultOutputPath + "\\Extracted", catName + ".*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".png")).FirstOrDefault();

                    if (CatalogFilePath != null)
                    {
                        wasFeatured = true;
                        updateConsole(catName + " successfully extracted", Color.FromArgb(255, 66, 244, 66), "Success");
                        if (CatalogFilePath.Contains(".uasset") || CatalogFilePath.Contains(".uexp") || CatalogFilePath.Contains(".ubulk"))
                        {
                            jwpmProcess("serialize \"" + CatalogFilePath.Substring(0, CatalogFilePath.LastIndexOf('.')) + "\"");
                            try
                            {
                                string jsonExtractedFilePath = Directory.GetFiles(DefaultOutputPath, catName + ".json", SearchOption.AllDirectories).FirstOrDefault();
                                if (jsonExtractedFilePath != null)
                                {
                                    updateConsole(catName + " successfully serialized", Color.FromArgb(255, 66, 244, 66), "Success");
                                    string parsedJson = JToken.Parse(File.ReadAllText(jsonExtractedFilePath)).ToString();
                                    File.Delete(jsonExtractedFilePath);
                                    var FeaturedID = Parser.Featured.FeaturedParser.FromJson(parsedJson);
                                    updateConsole("Parsing " + catName + "...", Color.FromArgb(255, 244, 132, 66), "Waiting");
                                    for (int i = 0; i < FeaturedID.Length; i++)
                                    {
                                        if (FeaturedID[i].DetailsImage != null)
                                        {
                                            string textureFile = FeaturedID[i].DetailsImage.ResourceObject;

                                            if (currentUsedPAKGUID != null && currentUsedPAKGUID != "0-0-0-0")
                                                jwpmProcess("extract \"" + Properties.Settings.Default.PAKsPath + "\\" + currentUsedPAK + "\" \"" + textureFile + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
                                            else
                                                jwpmProcess("extract \"" + Properties.Settings.Default.PAKsPath + "\\" + AllPAKsDictionary[textureFile] + "\" \"" + textureFile + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
                                            string textureFilePath = Directory.GetFiles(DefaultOutputPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".png")).FirstOrDefault();

                                            if (textureFilePath != null && textureFilePath.Contains("MI_UI_FeaturedRenderSwitch_"))
                                            {
                                                updateConsole(textureFile + " successfully extracted", Color.FromArgb(255, 66, 244, 66), "Success");
                                                if (textureFilePath.Contains(".uasset") || textureFilePath.Contains(".uexp") || textureFilePath.Contains(".ubulk"))
                                                {
                                                    jwpmProcess("serialize \"" + textureFilePath.Substring(0, textureFilePath.LastIndexOf('.')) + "\"");
                                                    try
                                                    {
                                                        string jsonRSMExtractedFilePath = Directory.GetFiles(DefaultOutputPath, textureFile + ".json", SearchOption.AllDirectories).FirstOrDefault();
                                                        if (jsonRSMExtractedFilePath != null)
                                                        {
                                                            updateConsole(textureFile + " successfully serialized", Color.FromArgb(255, 66, 244, 66), "Success");
                                                            string parsedRSMJson = JToken.Parse(File.ReadAllText(jsonRSMExtractedFilePath)).ToString();
                                                            File.Delete(jsonRSMExtractedFilePath);
                                                            var RSMID = Parser.RenderMat.RenderSwitchMaterial.FromJson(parsedRSMJson);
                                                            updateConsole("Parsing " + textureFile + "...", Color.FromArgb(255, 244, 132, 66), "Waiting");
                                                            for (int ii = 0; ii < RSMID.Length; ii++)
                                                            {
                                                                if (RSMID[ii].TextureParameterValues.FirstOrDefault().ParameterValue != null)
                                                                {
                                                                    string textureFile2 = RSMID[ii].TextureParameterValues.FirstOrDefault().ParameterValue;

                                                                    if (currentUsedPAKGUID != null && currentUsedPAKGUID != "0-0-0-0")
                                                                        jwpmProcess("extract \"" + Properties.Settings.Default.PAKsPath + "\\" + currentUsedPAK + "\" \"" + textureFile2 + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
                                                                    else
                                                                        jwpmProcess("extract \"" + Properties.Settings.Default.PAKsPath + "\\" + AllPAKsDictionary[textureFile2] + "\" \"" + textureFile2 + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
                                                                    string textureFilePath2 = Directory.GetFiles(DefaultOutputPath + "\\Extracted", textureFile2 + ".*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".png")).FirstOrDefault();

                                                                    if (textureFilePath2 != null)
                                                                    {
                                                                        jwpmProcess("texture \"" + textureFilePath2.Substring(0, textureFilePath2.LastIndexOf('\\')) + "\\" + textureFile2 + "\"");
                                                                        itemIconPath = textureFilePath2.Substring(0, textureFilePath2.LastIndexOf('\\')) + "\\" + textureFile2 + ".png";
                                                                        updateConsole(textureFile2 + " successfully converted to .PNG", Color.FromArgb(255, 66, 244, 66), "Success");
                                                                    }
                                                                    else
                                                                        updateConsole("Error while extracting " + textureFile2, Color.FromArgb(255, 244, 66, 66), "Error");
                                                                }
                                                            }
                                                        }
                                                    }
                                                    catch (JsonSerializationException)
                                                    {
                                                        updateConsole(".JSON file too large to be fully displayed", Color.FromArgb(255, 244, 66, 66), "Error");
                                                    }
                                                }
                                            }
                                            else if (textureFilePath != null && !textureFilePath.Contains("MI_UI_FeaturedRenderSwitch_"))
                                            {
                                                jwpmProcess("texture \"" + textureFilePath.Substring(0, textureFilePath.LastIndexOf('\\')) + "\\" + textureFile + "\"");
                                                itemIconPath = textureFilePath.Substring(0, textureFilePath.LastIndexOf('\\')) + "\\" + textureFile + ".png";
                                                updateConsole(textureFile + " successfully converted to .PNG", Color.FromArgb(255, 66, 244, 66), "Success");
                                            }
                                            else
                                                updateConsole("Error while extracting " + textureFile, Color.FromArgb(255, 244, 66, 66), "Error");
                                        }
                                    }
                                }
                            }
                            catch (JsonSerializationException)
                            {
                                updateConsole(".JSON file too large to be fully displayed", Color.FromArgb(255, 244, 66, 66), "Error");
                            }
                        }
                    }
                }
                else
                {
                    getItemIcon(theItem, false);
                }
            }
        }

        private void createChallengesIcon(Parser.Items.ItemsIDParser theItem, string theParsedJSON, string questJSON = null)
        {
            if (theItem.ExportType == "FortChallengeBundleItemDefinition")
            {
                Bitmap bmp = new Bitmap(Properties.Resources.Quest);
                Graphics g = Graphics.FromImage(bmp);
                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                int iamY = 275;
                int justSkip = 0;
                yAfterLoop = 0;
                bool v2 = false;

                var BundleParser = Parser.Challenges.ChallengeBundleIdParser.FromJson(theParsedJSON);
                for (int i = 0; i < BundleParser.Length; i++)
                {
                    #region DRAW BUNDLE ICON
                    try
                    {
                        if (Properties.Settings.Default.createIconForChallenges == true)
                        {
                            if (BundleParser[i].DisplayStyle.DisplayImage != null)
                            {
                                v2 = true;
                                #region COLORS + IMAGE
                                int sOpacity = unchecked((int)BundleParser[i].DisplayStyle.AccentColor.A);
                                int sRed = (int)(BundleParser[i].DisplayStyle.AccentColor.R * 255);
                                int sGreen = (int)(BundleParser[i].DisplayStyle.AccentColor.G * 255);
                                int sBlue = (int)(BundleParser[i].DisplayStyle.AccentColor.B * 255);

                                int seasonRed = (int)Convert.ToInt32(sRed / 1.5);
                                int seasonGreen = (int)Convert.ToInt32(sGreen / 1.5);
                                int seasonBlue = (int)Convert.ToInt32(sBlue / 1.5);

                                g.FillRectangle(new SolidBrush(Color.FromArgb(sOpacity * 255, sRed, sGreen, sBlue)), new Rectangle(0, 0, bmp.Width, 271));
                                g.FillRectangle(new SolidBrush(Color.FromArgb(255, seasonRed, seasonGreen, seasonBlue)), new Rectangle(0, 271, bmp.Width, bmp.Height - 271));

                                try
                                {
                                    string seasonFolder = questJSON.Substring(questJSON.Substring(0, questJSON.LastIndexOf("\\")).LastIndexOf("\\") + 1).ToUpper();
                                    g.DrawString(seasonFolder.Substring(0, seasonFolder.LastIndexOf("\\")), new Font(pfc.Families[1], 42), new SolidBrush(Color.FromArgb(255, seasonRed, seasonGreen, seasonBlue)), new Point(340, 40));
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
                                    g.DrawString(theItem.DisplayName.ToUpper(), new Font(pfc.Families[1], 115), new SolidBrush(Color.White), new Point(325, 70));
                                }
                                catch (NullReferenceException)
                                {
                                    AppendText("[NullReferenceException] ", Color.Red);
                                    AppendText("No ", Color.Black);
                                    AppendText("DisplayName ", Color.SteelBlue);
                                    AppendText("found", Color.Black, true);
                                } //NAME

                                string pngPATH = string.Empty;
                                string textureFile = Path.GetFileName(BundleParser[i].DisplayStyle.DisplayImage.AssetPathName).Substring(0, Path.GetFileName(BundleParser[i].DisplayStyle.DisplayImage.AssetPathName).LastIndexOf('.'));

                                if (currentUsedPAKGUID != null && currentUsedPAKGUID != "0-0-0-0")
                                    jwpmProcess("extract \"" + Properties.Settings.Default.PAKsPath + "\\" + currentUsedPAK + "\" \"" + textureFile + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
                                else
                                    jwpmProcess("extract \"" + Properties.Settings.Default.PAKsPath + "\\" + AllPAKsDictionary[textureFile] + "\" \"" + textureFile + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
                                string textureFilePath = Directory.GetFiles(DefaultOutputPath + "\\Extracted", textureFile + ".*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".png")).FirstOrDefault();

                                if (textureFilePath != null)
                                {
                                    jwpmProcess("texture \"" + textureFilePath.Substring(0, textureFilePath.LastIndexOf('\\')) + "\\" + textureFile + "\"");
                                    pngPATH = textureFilePath.Substring(0, textureFilePath.LastIndexOf('\\')) + "\\" + textureFile + ".png";
                                    updateConsole(textureFile + " successfully converted to .PNG", Color.FromArgb(255, 66, 244, 66), "Success");

                                    Image challengeIcon = Image.FromFile(pngPATH);
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

                    SelectedChallengesArray = new string[BundleParser[i].QuestInfos.Length];
                    for (int i2 = 0; i2 < BundleParser[i].QuestInfos.Length; i2++)
                    {
                        string cName = Path.GetFileName(BundleParser[i].QuestInfos[i2].QuestDefinition.AssetPathName);
                        SelectedChallengesArray[i2] = cName.Substring(cName.LastIndexOf('.') + 1);
                    }

                    for (int i2 = 0; i2 < SelectedChallengesArray.Length; i2++)
                    {
                        try
                        {
                            //MANUAL FIX
                            if (SelectedChallengesArray[i2] == "Quest_BR_LevelUp_SeasonLevel")
                                SelectedChallengesArray[i2] = "Quest_BR_LevelUp_SeasonLevel_25";

                            if (currentUsedPAKGUID != null && currentUsedPAKGUID != "0-0-0-0")
                                jwpmProcess("extract \"" + Properties.Settings.Default.PAKsPath + "\\" + currentUsedPAK + "\" \"" + SelectedChallengesArray[i2] + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
                            else
                                jwpmProcess("extract \"" + Properties.Settings.Default.PAKsPath + "\\" + AllPAKsDictionary[SelectedChallengesArray[i2]] + "\" \"" + SelectedChallengesArray[i2] + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
                            string ChallengeFilePath = Directory.GetFiles(DefaultOutputPath + "\\Extracted", SelectedChallengesArray[i2] + ".*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".png")).FirstOrDefault();

                            if (ChallengeFilePath != null)
                            {
                                updateConsole(SelectedChallengesArray[i2] + " successfully extracted", Color.FromArgb(255, 66, 244, 66), "Success");
                                if (ChallengeFilePath.Contains(".uasset") || ChallengeFilePath.Contains(".uexp") || ChallengeFilePath.Contains(".ubulk"))
                                {
                                    jwpmProcess("serialize \"" + ChallengeFilePath.Substring(0, ChallengeFilePath.LastIndexOf('.')) + "\"");
                                    try
                                    {
                                        string jsonExtractedFilePath = Directory.GetFiles(DefaultOutputPath, SelectedChallengesArray[i2] + ".json", SearchOption.AllDirectories).FirstOrDefault();
                                        if (jsonExtractedFilePath != null)
                                        {
                                            updateConsole(SelectedChallengesArray[i2] + " successfully serialized", Color.FromArgb(255, 66, 244, 66), "Success");

                                            string parsedJson = JToken.Parse(File.ReadAllText(jsonExtractedFilePath)).ToString();
                                            File.Delete(jsonExtractedFilePath);
                                            var questParser = Parser.Quest.QuestParser.FromJson(parsedJson);
                                            updateConsole("Parsing " + SelectedChallengesArray[i2] + "...", Color.FromArgb(255, 244, 132, 66), "Waiting");
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
                                                        if (Properties.Settings.Default.createIconForChallenges == true)
                                                        {
                                                            g.TextRenderingHint = TextRenderingHint.AntiAlias;
                                                            justSkip += 1;
                                                            iamY += 140;
                                                            g.DrawString(questParser[ii].Objectives[ii2].Description, new Font(pfc.Families[1], 50), new SolidBrush(Color.White), new Point(100, iamY));
                                                            g.DrawString("/" + questParser[ii].Objectives[ii2].Count.ToString(), new Font(pfc.Families[1], 50), new SolidBrush(Color.FromArgb(200, 255, 255, 255)), new Point(2410, iamY + 22), rightString);
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
                                                                if (Properties.Settings.Default.createIconForChallenges == true)
                                                                {
                                                                    g.DrawString(questParser[ii].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest")
                                                                    .Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token")
                                                                    .FirstOrDefault().ItemPrimaryAssetId.PrimaryAssetType.Name + ":"
                                                                    + questParser[ii].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest")
                                                                    .Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token")
                                                                    .FirstOrDefault().ItemPrimaryAssetId.PrimaryAssetName + ":"
                                                                    + questParser[ii].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest")
                                                                    .Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token")
                                                                    .FirstOrDefault().Quantity.ToString(), new Font(pfc.Families[0], 25), new SolidBrush(Color.FromArgb(75, 255, 255, 255)), new Point(108, iamY + 80));
                                                                }

                                                                AppendText("\t\t" + questParser[ii].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest")
                                                                    .Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token")
                                                                    .FirstOrDefault().ItemPrimaryAssetId.PrimaryAssetType.Name + ":"
                                                                    + questParser[ii].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest")
                                                                    .Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token")
                                                                    .FirstOrDefault().ItemPrimaryAssetId.PrimaryAssetName + ":"
                                                                    + questParser[ii].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest")
                                                                    .Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token")
                                                                    .FirstOrDefault().Quantity.ToString(), Color.DarkRed, true);
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                if (questParser[ii].HiddenRewards != null)
                                                                {
                                                                    if (Properties.Settings.Default.createIconForChallenges == true)
                                                                    {
                                                                        g.DrawString(questParser[ii].HiddenRewards.FirstOrDefault().TemplateId + ":"
                                                                        + questParser[ii].HiddenRewards.FirstOrDefault().Quantity.ToString(), new Font(pfc.Families[0], 25), new SolidBrush(Color.FromArgb(75, 255, 255, 255)), new Point(108, iamY + 80));
                                                                    }

                                                                    AppendText("\t\t" + questParser[ii].HiddenRewards.FirstOrDefault().TemplateId + ":"
                                                                        + questParser[ii].HiddenRewards.FirstOrDefault().Quantity.ToString(), Color.DarkRed, true);
                                                                }
                                                                else
                                                                {
                                                                    Console.WriteLine(ex.Message);
                                                                }
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
                                                            loopStageQuest(questParser[ii].Rewards[ii3].ItemPrimaryAssetId.PrimaryAssetType.Name, questParser[ii].Rewards[ii3].ItemPrimaryAssetId.PrimaryAssetName, g, iamY, justSkip);
                                                            iamY = yAfterLoop;
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
                                            updateConsole("No serialized file found", Color.FromArgb(255, 244, 66, 66), "Error");
                                    }
                                    catch (JsonSerializationException)
                                    {
                                        updateConsole(".JSON file too large to be fully displayed", Color.FromArgb(255, 244, 66, 66), "Error");
                                    }
                                }
                            }
                            else
                                updateConsole("Error while extracting " + SelectedChallengesArray[i2], Color.FromArgb(255, 244, 66, 66), "Error");
                        }
                        catch (KeyNotFoundException)
                        {
                            AppendText("Can't extract ", Color.Black);
                            AppendText(SelectedChallengesArray[i2], Color.SteelBlue, true);
                        }
                    }
                }

                if (Properties.Settings.Default.createIconForChallenges == true)
                {
                    #region WATERMARK
                    g.FillRectangle(new SolidBrush(Color.FromArgb(100, 0, 0, 0)), new Rectangle(0, iamY + 240, bmp.Width, 40));
                    g.DrawString(theItem.DisplayName + " Generated using FModel & JohnWickParse - " + DateTime.Now, new Font(pfc.Families[0], 20), new SolidBrush(Color.FromArgb(150, 255, 255, 255)), new Point(bmp.Width / 2, iamY + 250), centeredString);
                    #endregion
                    if (v2 == false)
                    {
                        #region DRAW TEXT
                        try
                        {
                            string seasonFolder = questJSON.Substring(questJSON.Substring(0, questJSON.LastIndexOf("\\")).LastIndexOf("\\") + 1).ToUpper();
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
                            g.DrawString(theItem.DisplayName.ToUpper(), new Font(pfc.Families[1], 115), new SolidBrush(Color.White), new Point(325, 70));
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

                updateConsole(theItem.DisplayName, Color.FromArgb(255, 66, 244, 66), "Success");
                if (autoSaveImagesToolStripMenuItem.Checked == true || updateModeToolStripMenuItem.Checked == true)
                {
                    Invoke(new Action(() =>
                    {
                        pictureBox1.Image.Save(DefaultOutputPath + "\\Icons\\" + currentUsedItem + ".png", ImageFormat.Png);
                    }));
                    AppendText(currentUsedItem, Color.DarkRed);
                    AppendText(" successfully saved", Color.Black, true);
                }

                AppendText("", Color.Black, true);
            }
        }
        private void loopStageQuest(string qAssetType, string qAssetName, Graphics toDrawOn, int yeay, int line)
        {
            Graphics toDrawOnLoop = toDrawOn;
            int yeayLoop = yeay;
            int lineLoop = line;

            if (qAssetType == "Quest")
            {
                try
                {
                    jwpmProcess("extract \"" + Properties.Settings.Default.PAKsPath + "\\" + AllPAKsDictionary[qAssetName] + "\" \"" + qAssetName + "\" \"" + DefaultOutputPath + "\" " + Properties.Settings.Default.AESKey);
                    string ChallengeFilePathLoop = Directory.GetFiles(DefaultOutputPath + "\\Extracted", qAssetName + ".*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".png")).FirstOrDefault();

                    if (ChallengeFilePathLoop != null)
                    {
                        updateConsole(qAssetName + " successfully extracted", Color.FromArgb(255, 66, 244, 66), "Success");
                        if (ChallengeFilePathLoop.Contains(".uasset") || ChallengeFilePathLoop.Contains(".uexp") || ChallengeFilePathLoop.Contains(".ubulk"))
                        {
                            jwpmProcess("serialize \"" + ChallengeFilePathLoop.Substring(0, ChallengeFilePathLoop.LastIndexOf('.')) + "\"");
                            try
                            {
                                string jsonExtractedFilePath = Directory.GetFiles(DefaultOutputPath, qAssetName + ".json", SearchOption.AllDirectories).FirstOrDefault();
                                if (jsonExtractedFilePath != null)
                                {
                                    updateConsole(qAssetName + " successfully serialized", Color.FromArgb(255, 66, 244, 66), "Success");

                                    string parsedJson = JToken.Parse(File.ReadAllText(jsonExtractedFilePath)).ToString();
                                    File.Delete(jsonExtractedFilePath);
                                    var questParser = Parser.Quest.QuestParser.FromJson(parsedJson);
                                    updateConsole("Parsing " + qAssetName + "...", Color.FromArgb(255, 244, 132, 66), "Waiting");
                                    for (int i = 0; i < questParser.Length; i++)
                                    {
                                        string oldQuest = string.Empty;
                                        string oldCount = string.Empty;
                                        for (int ii = 0; ii < questParser[i].Objectives.Length; ii++)
                                        {
                                            if (!questStageDict.ContainsKey(questParser[i].Objectives[ii].Description))
                                            {
                                                string newQuest = questParser[i].Objectives[ii].Description;
                                                string newCount = questParser[i].Objectives[ii].Count.ToString();
                                                questStageDict.Add(questParser[i].Objectives[ii].Description, questParser[i].Objectives[ii].Count);

                                                if (newQuest != oldQuest && newCount != oldCount)
                                                {
                                                    if (Properties.Settings.Default.createIconForChallenges == true)
                                                    {
                                                        toDrawOnLoop.TextRenderingHint = TextRenderingHint.AntiAlias;
                                                        lineLoop += 1;
                                                        yeayLoop += 140;
                                                        toDrawOnLoop.DrawString(questParser[i].Objectives[ii].Description, new Font(pfc.Families[1], 50), new SolidBrush(Color.White), new Point(100, yeayLoop));
                                                        toDrawOnLoop.DrawString("/" + questParser[i].Objectives[ii].Count.ToString(), new Font(pfc.Families[1], 50), new SolidBrush(Color.FromArgb(200, 255, 255, 255)), new Point(2410, yeayLoop + 22), rightString);
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
                                                            if (Properties.Settings.Default.createIconForChallenges == true)
                                                            {
                                                                toDrawOnLoop.DrawString(questParser[i].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest")
                                                                .Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token")
                                                                .FirstOrDefault().ItemPrimaryAssetId.PrimaryAssetType.Name + ":"
                                                                + questParser[i].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest")
                                                                .Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token")
                                                                .FirstOrDefault().ItemPrimaryAssetId.PrimaryAssetName + ":"
                                                                + questParser[i].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest")
                                                                .Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token")
                                                                .FirstOrDefault().Quantity.ToString(), new Font(pfc.Families[0], 25), new SolidBrush(Color.FromArgb(75, 255, 255, 255)), new Point(108, yeayLoop + 80));
                                                            }

                                                            AppendText("\t\t" + questParser[i].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest")
                                                                .Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token")
                                                                .FirstOrDefault().ItemPrimaryAssetId.PrimaryAssetType.Name + ":"
                                                                + questParser[i].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest")
                                                                .Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token")
                                                                .FirstOrDefault().ItemPrimaryAssetId.PrimaryAssetName + ":"
                                                                + questParser[i].Rewards.Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest")
                                                                .Where(x => x.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token")
                                                                .FirstOrDefault().Quantity.ToString(), Color.DarkRed, true);
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            if (questParser[i].HiddenRewards != null)
                                                            {
                                                                if (Properties.Settings.Default.createIconForChallenges == true)
                                                                {
                                                                    toDrawOnLoop.DrawString(questParser[i].HiddenRewards.FirstOrDefault().TemplateId + ":"
                                                                    + questParser[i].HiddenRewards.FirstOrDefault().Quantity.ToString(), new Font(pfc.Families[0], 25), new SolidBrush(Color.FromArgb(75, 255, 255, 255)), new Point(108, yeayLoop + 80));
                                                                }

                                                                AppendText("\t\t" + questParser[i].HiddenRewards.FirstOrDefault().TemplateId + ":"
                                                                    + questParser[i].HiddenRewards.FirstOrDefault().Quantity.ToString(), Color.DarkRed, true);
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
                                                    loopStageQuest(questParser[i].Rewards[iii].ItemPrimaryAssetId.PrimaryAssetType.Name, questParser[i].Rewards[iii].ItemPrimaryAssetId.PrimaryAssetName, toDrawOnLoop, yeayLoop, lineLoop);
                                                    yeayLoop = yAfterLoop;
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                    updateConsole("No serialized file found", Color.FromArgb(255, 244, 66, 66), "Error");
                            }
                            catch (JsonSerializationException)
                            {
                                updateConsole(".JSON file too large to be fully displayed", Color.FromArgb(255, 244, 66, 66), "Error");
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
            yAfterLoop = yeayLoop;
        }

        private void convertTexture2D()
        {
            updateConsole(currentUsedItem + " is a Texture2D", Color.FromArgb(255, 66, 244, 66), "Success");

            jwpmProcess("texture \"" + extractedFilePath.Substring(0, extractedFilePath.LastIndexOf('\\')) + "\\" + currentUsedItem + "\"");
            string IMGPath = extractedFilePath.Substring(0, extractedFilePath.LastIndexOf('\\')) + "\\" + currentUsedItem + ".png";

            if (File.Exists(IMGPath))
            {
                pictureBox1.Image = Image.FromFile(IMGPath);
            }

            if (autoSaveImagesToolStripMenuItem.Checked == true || updateModeToolStripMenuItem.Checked == true)
            {
                Invoke(new Action(() =>
                {
                    pictureBox1.Image.Save(DefaultOutputPath + "\\Icons\\" + currentUsedItem + ".png", ImageFormat.Png);
                }));
                AppendText(currentUsedItem, Color.DarkRed);
                AppendText(" successfully saved", Color.Black, true);
            }
        }
        private void convertSoundWave()
        {
            updateConsole(currentUsedItem + " is a Sound", Color.FromArgb(255, 66, 244, 66), "Success");

            string SoundPathToConvert = extractedFilePath.Substring(0, extractedFilePath.LastIndexOf('\\')) + "\\" + currentUsedItem + ".uexp";
            updateConsole("Converting " + currentUsedItem, Color.FromArgb(255, 244, 132, 66), "Processing");
            OpenWithDefaultProgramAndNoFocus(Converter.UnrealEngineDataToOGG.convertToOGG(SoundPathToConvert));
            updateConsole("Opening " + currentUsedItem + ".ogg", Color.FromArgb(255, 66, 244, 66), "Success");
        }
        private void convertToOTF(string file)
        {
            File.Move(file, Path.ChangeExtension(file, ".otf"));
            updateConsole(currentUsedItem + " successfully converter to a font", Color.FromArgb(255, 66, 244, 66), "Success");
        }

        //EVENTS
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            stopWatch = new Stopwatch();
            stopWatch.Start();
            createDir();
            extractAndSerializeItems(e);
        }
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            stopWatch.Stop();
            if (e.Cancelled == true)
            {
                updateConsole("Canceled!", Color.FromArgb(255, 244, 66, 66), "Error");
            }
            else if (e.Error != null)
            {
                updateConsole(e.Error.Message, Color.FromArgb(255, 244, 66, 66), "Error");
            }
            else
            {
                TimeSpan ts = stopWatch.Elapsed;
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                updateConsole("Time elapsed: " + elapsedTime, Color.FromArgb(255, 66, 244, 66), "Success");
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
            questStageDict = new Dictionary<string, long>();
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
            if (backgroundWorker1.WorkerSupportsCancellation == true)
            {
                backgroundWorker1.CancelAsync();
            }
            if (backgroundWorker2.WorkerSupportsCancellation == true)
            {
                backgroundWorker2.CancelAsync();
            }
        }
        #endregion

        #region IMAGES SAVE & MERGE
        //METHODS
        private void askMergeImages()
        {
            if (string.IsNullOrEmpty(Properties.Settings.Default.mergerFileName))
            {
                MessageBox.Show("Please, set a name to your Merger file before trying to merge images\n\nSteps:\n\t- Load\n\t- Settings", "Merger File Name Missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                OpenFileDialog theDialog = new OpenFileDialog();
                theDialog.Multiselect = true;
                theDialog.InitialDirectory = DefaultOutputPath + "\\Icons\\";
                theDialog.Title = "Choose your images";
                theDialog.Filter = "PNG Files (*.png)|*.png|JPEG Files (*.jpg)|*.jpg|BMP Files (*.bmp)|*.bmp|All Files (*.*)|*.*";

                Invoke(new Action(() =>
                {
                    if (theDialog.ShowDialog() == DialogResult.OK)
                    {
                        List<Image> selectedImages = new List<Image>();
                        foreach (var files in theDialog.FileNames)
                        {
                            selectedImages.Add(Image.FromFile(files));
                        }

                        mergeSelected(selectedImages);
                    }
                }));
            }
        }
        private void mergeSelected(List<Image> mySelectedImages)
        {
            if (Properties.Settings.Default.mergerImagesRow == 0)
            {
                Properties.Settings.Default.mergerImagesRow = 7;
                Properties.Settings.Default.Save();
            }

            int numperrow = Properties.Settings.Default.mergerImagesRow;
            var w = 530 * numperrow;
            if (mySelectedImages.Count * 530 < 530 * numperrow)
            {
                w = mySelectedImages.Count * 530;
            }

            int h = int.Parse(Math.Ceiling(double.Parse(mySelectedImages.Count.ToString()) / numperrow).ToString()) * 530;
            Bitmap bmp = new Bitmap(w - 8, h - 8);

            var num = 1;
            var cur_w = 0;
            var cur_h = 0;

            for (int i = 0; i < mySelectedImages.Count; i++)
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.DrawImage(Forms.Settings.ResizeImage(mySelectedImages[i], 522, 522), new PointF(cur_w, cur_h));
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
            bmp.Save(DefaultOutputPath + "\\" + Properties.Settings.Default.mergerFileName + ".png", ImageFormat.Png);

            openMerged(bmp);
        }
        private void openMerged(Bitmap mergedImage)
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
                newForm.Icon = Properties.Resources.FModel;
                newForm.Text = DefaultOutputPath + "\\" + Properties.Settings.Default.mergerFileName + ".png";
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
                newForm.Icon = Properties.Resources.FModel;
                newForm.Text = currentUsedItem;
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
                saveTheDialog.Title = "Save Icon";
                saveTheDialog.Filter = "PNG Files (*.png)|*.png";
                saveTheDialog.InitialDirectory = DefaultOutputPath + "\\Icons\\";
                saveTheDialog.FileName = currentUsedItem;
                if (saveTheDialog.ShowDialog() == DialogResult.OK)
                {
                    pictureBox1.Image.Save(saveTheDialog.FileName, ImageFormat.Png);
                    AppendText(currentUsedItem, Color.DarkRed);
                    AppendText(" successfully saved", Color.Black, true);
                }
            }
        }
        private async void mergeImagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await Task.Run(() => {
                askMergeImages();
            });
        }
        #endregion
    }
}
