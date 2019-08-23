using csharp_wick;
using FModel.Properties;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FModel
{
    static class PakHelper
    {
        public static string[] PakAsTxt { get; set; }
        private static StringBuilder _sb { get; set; }

        /// <summary>
        /// for each main paks we generate a file list
        /// if the main key used to generate the file list for each pak is working, we also load the localization file to get the translated version of all strings
        /// for the dynamic paks we check if the key exist in the AES Manager, if so, we do the same steps as a main pak
        /// </summary>
        /// <param name="theSinglePak"></param>
        /// <param name="loadAllPaKs"></param>
        public static void RegisterPaKsinDict(ToolStripItemClickedEventArgs theSinglePak = null, bool loadAllPaKs = false)
        {
            _sb = new StringBuilder();
            bool bMainKeyWorking = false;

            for (int i = 0; i < ThePak.mainPaksList.Count; i++)
            {
                PakExtractor theExtractor = null;
                try
                {
                    if (!string.IsNullOrWhiteSpace(Settings.Default.AESKey))
                    {
                        theExtractor = new PakExtractor(Settings.Default.PAKsPath + "\\" + ThePak.mainPaksList[i].thePak, Settings.Default.AESKey);
                    }
                    else { if (theExtractor != null) { theExtractor.Dispose(); } break; }
                }
                catch (Exception ex)
                {
                    if (string.Equals(ex.Message, "Extraction failed")) { DisplayError(); }
                    else { DisplayEmergencyError(ex); return; }

                    if (theExtractor != null) { theExtractor.Dispose(); }
                    break; //if one of the main pak file doesn't work, all the other doesn't work either
                }

                if (theExtractor.GetFileList().ToArray() != null)
                {
                    bMainKeyWorking = true;
                    string mountPoint = theExtractor.GetMountPoint();
                    string[] fileListWithMountPoint = theExtractor.GetFileList().Select(s => s.Replace(s, mountPoint.Substring(9) + s)).ToArray();

                    ThePak.PaksExtractorDictionary.Add(ThePak.mainPaksList[i].thePak, theExtractor);
                    ThePak.PaksFileArrayDictionary.Add(theExtractor, fileListWithMountPoint);

                    RegisterInDict(ThePak.mainPaksList[i].thePak, fileListWithMountPoint, mountPoint, theSinglePak, loadAllPaKs);
                }
            }
            if (bMainKeyWorking) { LoadLocRes.LoadMySelectedLocRes(Settings.Default.IconLanguage); }

            for (int i = 0; i < ThePak.dynamicPaksList.Count; i++)
            {
                PakExtractor theExtractor = null;
                string pakName = DynamicKeysManager.AESEntries.Where(x => x.thePak == ThePak.dynamicPaksList[i].thePak).Select(x => x.thePak).FirstOrDefault();
                string pakKey = DynamicKeysManager.AESEntries.Where(x => x.thePak == ThePak.dynamicPaksList[i].thePak).Select(x => x.theKey).FirstOrDefault();

                if (!string.IsNullOrEmpty(pakName) && !string.IsNullOrEmpty(pakKey))
                {
                    try
                    {
                        theExtractor = new PakExtractor(Settings.Default.PAKsPath + "\\" + pakName, pakKey);
                    }
                    catch (Exception ex)
                    {
                        if (string.Equals(ex.Message, "Extraction failed")) { DisplayError(pakName, pakKey); }
                        else { DisplayEmergencyError(ex); return; }

                        if (theExtractor != null) { theExtractor.Dispose(); }
                        continue;
                    }

                    if (theExtractor.GetFileList().ToArray() != null)
                    {
                        string mountPoint = theExtractor.GetMountPoint();
                        string[] fileListWithMountPoint = theExtractor.GetFileList().Select(s => s.Replace(s, mountPoint.Substring(9) + s)).ToArray();

                        ThePak.PaksExtractorDictionary.Add(ThePak.dynamicPaksList[i].thePak, theExtractor);
                        ThePak.PaksFileArrayDictionary.Add(theExtractor, fileListWithMountPoint);

                        RegisterInDict(ThePak.dynamicPaksList[i].thePak, fileListWithMountPoint, mountPoint, theSinglePak, loadAllPaKs);
                    }
                }
            }

            if (loadAllPaKs)
            {
                File.WriteAllText(App.DefaultOutputPath + "\\FortnitePAKs.txt", _sb.ToString()); //File will always exist
            }

            new UpdateMyState("Building tree, please wait...", "Loading").ChangeProcessState();
            GC.Collect();
        }

        /// <summary>
        /// 1. we get the pak mount point and add it to the PaksMountPoint dictionary
        /// 2. for each file in a pak, we add the file name as the key and its pak name as the value (used later by SearchFiles form)
        /// </summary>
        /// <param name="thePakName"></param>
        /// <param name="thePakLines"></param>
        /// <param name="theSinglePak"></param>
        /// <param name="weLoadAll"></param>
        private static void RegisterInDict(string thePakName, string[] thePakLines, string mountPoint, ToolStripItemClickedEventArgs theSinglePak = null, bool weLoadAll = false)
        {
            for (int i = 0; i < thePakLines.Length; i++)
            {
                string CurrentUsedPakFileName = thePakLines[i].Substring(thePakLines[i].LastIndexOf("/", StringComparison.Ordinal) + 1);
                if (CurrentUsedPakFileName.Contains(".uasset") || CurrentUsedPakFileName.Contains(".uexp") || CurrentUsedPakFileName.Contains(".ubulk"))
                {
                    if (!ThePak.AllpaksDictionary.ContainsKey(CurrentUsedPakFileName.Substring(0, CurrentUsedPakFileName.LastIndexOf(".", StringComparison.Ordinal))))
                    {
                        ThePak.AllpaksDictionary.Add(CurrentUsedPakFileName.Substring(0, CurrentUsedPakFileName.LastIndexOf(".", StringComparison.Ordinal)), thePakName);
                    }
                }
                else
                {
                    if (!ThePak.AllpaksDictionary.ContainsKey(CurrentUsedPakFileName))
                    {
                        ThePak.AllpaksDictionary.Add(CurrentUsedPakFileName, thePakName);
                    }
                }

                if (weLoadAll)
                {
                    _sb.Append(thePakLines[i] + "\n");
                }
            }

            if (weLoadAll) { new UpdateMyState(".PAK mount point: " + mountPoint.Substring(9), "Waiting").ChangeProcessState(); }
            if (theSinglePak != null && thePakName == theSinglePak.ClickedItem.Text) { PakAsTxt = thePakLines; }
        }

        public static void DisplayError(string pak = null, string key = null)
        {
            if (string.IsNullOrEmpty(pak) && string.IsNullOrEmpty(key))
            {
                new UpdateMyConsole("0x" + Settings.Default.AESKey, Color.Crimson).AppendToConsole();
                new UpdateMyConsole(" doesn't work with the main pak files", Color.Black, true).AppendToConsole();
            }
            else
            {
                new UpdateMyConsole("0x" + key, Color.Crimson).AppendToConsole();
                new UpdateMyConsole(" doesn't work with ", Color.Black).AppendToConsole();
                new UpdateMyConsole(pak, Color.Crimson, true).AppendToConsole();
            }
        }
        public static void DisplayEmergencyError(Exception ex)
        {
            new UpdateMyConsole("Message: ", Color.Crimson).AppendToConsole();
            new UpdateMyConsole(ex.Message, Color.Black, true).AppendToConsole();

            new UpdateMyConsole("Source: ", Color.Crimson).AppendToConsole();
            new UpdateMyConsole(ex.Source, Color.Black, true).AppendToConsole();

            new UpdateMyConsole("Target: ", Color.Crimson).AppendToConsole();
            new UpdateMyConsole(ex.TargetSite.ToString(), Color.Black, true).AppendToConsole();

            new UpdateMyConsole("\nContact me: @AsvalFN on Twitter or open an issue on GitHub", Color.Crimson, true).AppendToConsole();
        }
    }
}
