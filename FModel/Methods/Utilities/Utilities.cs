using FModel.Properties;
using System;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows.Forms;
using System.Collections.Generic;
using AutoUpdaterDotNET;

namespace FModel
{
    static class Utilities
    {
        /// <summary>
        /// OpenWithDefaultProgramAndNoFocus is used to automatically open sound file once they are converted
        /// </summary>
        /// <param name="path"> path is the path to the converted sound file (in "Sounds" subfolder) </param>
        public static void OpenWithDefaultProgramAndNoFocus(string path)
        {
            try
            {
                Process fileopener = new Process();
                fileopener.StartInfo.FileName = "explorer";
                fileopener.StartInfo.Arguments = "\"" + path + "\"";
                fileopener.Start();
            }
            catch (Exception)
            {
                throw new ArgumentException("Error while trying to open " + ThePak.CurrentUsedItem);
            }
        }

        /// <summary>
        /// just create the default folders FModel needs to work
        /// </summary>
        public static void CreateDefaultFolders()
        {
            Directory.CreateDirectory(App.DefaultOutputPath + "\\Backup\\");
            Directory.CreateDirectory(App.DefaultOutputPath + "\\Extracted\\");
            Directory.CreateDirectory(App.DefaultOutputPath + "\\Icons\\");
            Directory.CreateDirectory(App.DefaultOutputPath + "\\Sounds\\");
            Directory.CreateDirectory(App.DefaultOutputPath + "\\Saved_JSON\\");
        }

        /// <summary>
        /// actually idk if it's useful, i added this for Admin windows account, when FModel can't even use CreateDefaultFolders 
        /// </summary>
        /// <param name="folderPath"></param>
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

        /// <summary>
        /// By default the output folder is the Documents folder, however if user wanna change, once FModel restart SetOutputFolder is called
        /// </summary>
        public static void SetOutputFolder()
        {
            App.DefaultOutputPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\FModel"; //DOCUMENTS FOLDER BY DEFAULT

            if (string.IsNullOrEmpty(Settings.Default.ExtractOutput))
            {
                Settings.Default.ExtractOutput = App.DefaultOutputPath;
                Settings.Default.Save();
            }
            else
            {
                App.DefaultOutputPath = Settings.Default.ExtractOutput;
            }

            Directory.CreateDirectory(App.DefaultOutputPath);
        }

        /// <summary>
        /// this should tell me if i can read the file, to avoid crash when a pak is being written by the launcher
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;
            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }

        /// <summary>
        /// filter text with case insensitive support
        /// </summary>
        /// <param name="text"></param>
        /// <param name="value"></param>
        /// <param name="stringComparison"></param>
        /// <returns></returns>
        public static bool CaseInsensitiveContains(string text, string value, StringComparison stringComparison = StringComparison.CurrentCultureIgnoreCase)
        {
            return text.IndexOf(value, stringComparison) >= 0;
        }

        /// <summary>
        /// in the ToolStripMenuItem, color the paks that have a key (working or not working)
        /// result: improve readability when there's a lot of paks
        /// </summary>
        /// <param name="myToolStrip"></param>
        public static void colorMyPaks(ToolStripMenuItem myToolStrip)
        {
            foreach (ToolStripItem toolstripmenuitem in myToolStrip.DropDownItems)
            {
                toolstripmenuitem.Enabled = false;
                toolstripmenuitem.BackColor = Color.FromArgb(255, 255, 255, 255);

                if (!string.IsNullOrEmpty(Settings.Default.AESKey))
                {
                    string pak = ThePak.mainPaksList.Where(i => i.thePak == toolstripmenuitem.Text).Select(i => i.thePak).FirstOrDefault();

                    if (pak == toolstripmenuitem.Text)
                    {
                        toolstripmenuitem.Enabled = true;
                        toolstripmenuitem.BackColor = Color.FromArgb(25, 50, 92, 219);
                    }
                }

                if (DynamicKeysManager.AESEntries != null)
                {
                    string pak = DynamicKeysManager.AESEntries.Where(i => i.thePak == toolstripmenuitem.Text).Select(i => i.thePak).FirstOrDefault();
                    string key = DynamicKeysManager.AESEntries.Where(i => i.thePak == toolstripmenuitem.Text).Select(i => i.theKey).FirstOrDefault();

                    if (!string.IsNullOrEmpty(key) && pak == toolstripmenuitem.Text)
                    {
                        toolstripmenuitem.Enabled = true;
                        toolstripmenuitem.BackColor = Color.FromArgb(50, 50, 92, 219);
                    }
                }
            }
        }

        /// <summary>
        /// something to expand nodes to a given level
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="level"></param>
        public static void ExpandToLevel(TreeNodeCollection nodes, int level)
        {
            if (level > 0)
            {
                foreach (TreeNode node in nodes)
                {
                    node.Expand();
                    ExpandToLevel(node.Nodes, level - 1);
                }
            }
        }

        public static void CheckWatermark()
        {
            if (!string.IsNullOrEmpty(Settings.Default.wFilename) &&
                !File.Exists(Settings.Default.wFilename))
            {
                Settings.Default.wFilename = string.Empty;
                Settings.Default.isWatermark = false;
                new UpdateMyConsole("Watermark file not found, watermarking disabled.", Color.CornflowerBlue, true).AppendToConsole();
            }

            if (!string.IsNullOrEmpty(Settings.Default.UMFilename) &&
                !File.Exists(Settings.Default.UMFilename))
            {
                Settings.Default.UMFilename = string.Empty;
                Settings.Default.UMWatermark = false;
                new UpdateMyConsole("Watermark file not found, watermarking in Update Mode disabled.", Color.CornflowerBlue, true).AppendToConsole();
            }

            if (!string.IsNullOrEmpty(Settings.Default.challengesBannerFileName) &&
                !File.Exists(Settings.Default.challengesBannerFileName))
            {
                Settings.Default.challengesBannerFileName = string.Empty;
                Settings.Default.isChallengesTheme = false;
                new UpdateMyConsole("Banner file not found, challenges custom theme disabled.", Color.CornflowerBlue, true).AppendToConsole();
            }

            Settings.Default.Save();
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

        public static Color ChangeLightness(this Color color, float coef)
        {
            return Color.FromArgb((int)(color.R * coef), (int)(color.G * coef),
                (int)(color.B * coef));
        }

        public static void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            if (args != null)
            {
                if (args.IsUpdateAvailable)
                {
                    DialogResult dialogResult;
                    if (args.Mandatory)
                    {
                        dialogResult =
                            MessageBox.Show(
                                $@"There is new version {args.CurrentVersion} available. You are using version {args.InstalledVersion}. This is required update. Press Ok to begin updating the application.", @"Update Available",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                    }
                    else
                    {
                        dialogResult =
                            MessageBox.Show(
                                $@"There is new version {args.CurrentVersion} available. You are using version {
                                        args.InstalledVersion
                                    }. Do you want to update the application now?", @"Update Available",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Information);
                    }

                    if (dialogResult.Equals(DialogResult.Yes) || dialogResult.Equals(DialogResult.OK))
                    {
                        try
                        {
                            Process.Start(args.ChangelogURL);
                            if (AutoUpdater.DownloadUpdate())
                            {
                                Application.Exit();
                            }
                        }
                        catch (Exception exception)
                        {
                            MessageBox.Show(exception.Message, exception.GetType().ToString(), MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show(
                        @"There is a problem reaching update server please check your internet connection and try again later.",
                        @"Update check failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
