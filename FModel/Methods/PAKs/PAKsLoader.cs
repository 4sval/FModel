using FModel.Methods.TreeViewModel;
using FModel.Methods.Utilities;
using PakReader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FProp = FModel.Properties.Settings;

namespace FModel.Methods.PAKs
{
    class PAKsLoader
    {
        private static readonly string PAK_PATH = FProp.Default.FPak_Path;
        private static SortedTreeViewWindowViewModel srt { get; set; }

        public static async void LoadOnePAK()
        {
            FWindow.FMain.ViewModel = srt = new SortedTreeViewWindowViewModel();
            FWindow.FMain.ListBox_Main.Items.Clear();
            FWindow.FMain.FilterTextBox_Main.Text = string.Empty;
            FWindow.FMain.AssetPropertiesBox_Main.Text = string.Empty;
            FWindow.FMain.ImageBox_Main.Source = null;

            await Task.Run(() =>
            {
                PAKEntries.PAKToDisplay = new Dictionary<string, FPakEntry[]>();
                new UpdateMyProcessEvents($"{PAK_PATH}\\{FWindow.FCurrentPAK}", "Loading").Update();

                LoadPAKFiles();
                FillTreeView();

            }).ContinueWith(TheTask =>
            {
                TasksUtility.TaskCompleted(TheTask.Exception);
            });
        }
        public static async void LoadAllPAKs()
        {
            FWindow.FMain.ViewModel = srt = new SortedTreeViewWindowViewModel();
            FWindow.FMain.ListBox_Main.Items.Clear();
            FWindow.FMain.FilterTextBox_Main.Text = string.Empty;
            FWindow.FMain.AssetPropertiesBox_Main.Text = string.Empty;
            FWindow.FMain.ImageBox_Main.Source = null;

            await Task.Run(() =>
            {
                PAKEntries.PAKToDisplay = new Dictionary<string, FPakEntry[]>();

                LoadPAKFiles(true);
                FillTreeView(true);

            }).ContinueWith(TheTask =>
            {
                TasksUtility.TaskCompleted(TheTask.Exception);
            });
        }

        private static void LoadPAKFiles(bool bAllPAKs = false)
        {
            if (PAKEntries.PAKEntriesList != null && PAKEntries.PAKEntriesList.Any())
            {
                AssetEntries.AssetEntriesDict = new Dictionary<FPakEntry, PakReader.PakReader>();

                //MAIN PAKs LOOP
                foreach (PAKInfosEntry Pak in PAKEntries.PAKEntriesList.Where(x => !x.bTheDynamicPAK))
                {
                    byte[] AESKey = AESUtility.StringToByteArray(FProp.Default.FPak_MainAES);
                    PakReader.PakReader reader = null;
                    try
                    {
                        reader = new PakReader.PakReader(Pak.ThePAKPath, AESKey);
                    }
                    catch (Exception ex)
                    {
                        if (string.Equals(ex.Message, "The AES key is invalid")) { UIHelper.DisplayError(); }
                        else { UIHelper.DisplayEmergencyError(ex); return; }
                        break;
                    }

                    if (reader != null)
                    {
                        PAKEntries.PAKToDisplay.Add(Path.GetFileName(Pak.ThePAKPath), reader.FileInfos);

                        if (bAllPAKs) { new UpdateMyProcessEvents($"{Path.GetFileNameWithoutExtension(Pak.ThePAKPath)} mount point: {reader.MountPoint}", "Loading").Update(); }
                        foreach (FPakEntry entry in reader.FileInfos)
                        {
                            if (!AssetEntries.AssetEntriesDict.ContainsKey(entry))
                            {
                                AssetEntries.AssetEntriesDict.Add(entry, reader);
                            }
                        }
                    }
                }

                //DYNAMIC PAKs LOOP
                foreach (PAKInfosEntry Pak in PAKEntries.PAKEntriesList.Where(x => x.bTheDynamicPAK))
                {
                    byte[] AESKey = null;
                    string AESFromManager = string.Empty;
                    if (AESEntries.AESEntriesList != null && AESEntries.AESEntriesList.Any())
                    {
                        AESFromManager = AESEntries.AESEntriesList.Where(x => string.Equals(x.ThePAKName, Path.GetFileNameWithoutExtension(Pak.ThePAKPath))).Select(x => x.ThePAKKey).FirstOrDefault();
                        if (!string.IsNullOrEmpty(AESFromManager))
                        {
                            AESKey = AESUtility.StringToByteArray(AESFromManager);
                        }
                    }

                    if (AESKey != null && !string.IsNullOrEmpty(AESFromManager))
                    {
                        PakReader.PakReader reader = null;
                        try
                        {
                            reader = new PakReader.PakReader(Pak.ThePAKPath, AESKey);
                        }
                        catch (Exception ex)
                        {
                            if (string.Equals(ex.Message, "The AES key is invalid")) { UIHelper.DisplayError(Path.GetFileNameWithoutExtension(Pak.ThePAKPath), AESFromManager); }
                            else { UIHelper.DisplayEmergencyError(ex); return; }
                            continue;
                        }

                        if (reader != null)
                        {
                            PAKEntries.PAKToDisplay.Add(Path.GetFileName(Pak.ThePAKPath), reader.FileInfos);

                            if (bAllPAKs) { new UpdateMyProcessEvents($"{Path.GetFileNameWithoutExtension(Pak.ThePAKPath)} mount point: {reader.MountPoint}", "Loading").Update(); }
                            foreach (FPakEntry entry in reader.FileInfos)
                            {
                                if (!AssetEntries.AssetEntriesDict.ContainsKey(entry))
                                {
                                    AssetEntries.AssetEntriesDict.Add(entry, reader);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void FillTreeView(bool bAllPAKs = false)
        {
            if (!bAllPAKs)
            {
                FPakEntry[] PAKFileInfos = PAKEntries.PAKToDisplay.Where(x => x.Key == FWindow.FCurrentPAK).Select(x => x.Value).FirstOrDefault();
                if (PAKFileInfos == null) { throw new ArgumentException($"Please, provide a working key in the AES Manager for {FWindow.FCurrentPAK}"); }

                FWindow.FMain.Dispatcher.InvokeAsync(() =>
                {
                    foreach (FPakEntry entry in PAKFileInfos)
                    {
                        string onlyFolders = entry.Name.Substring(0, entry.Name.LastIndexOf('/'));
                        TreeViewUtility.PopulateTreeView(srt, onlyFolders.Substring(1));
                    }
                });
            }
            else
            {
                foreach (FPakEntry[] PAKsFileInfos in PAKEntries.PAKToDisplay.Values)
                {
                    FWindow.FMain.Dispatcher.InvokeAsync(() =>
                    {
                        foreach (FPakEntry entry in PAKsFileInfos)
                        {
                            string onlyFolders = entry.Name.Substring(0, entry.Name.LastIndexOf('/'));
                            TreeViewUtility.PopulateTreeView(srt, onlyFolders.Substring(1));
                        }
                    });
                }
            }

            FWindow.FMain.Dispatcher.InvokeAsync(() =>
            {
                FWindow.FMain.ViewModel = srt;
            });
            new UpdateMyProcessEvents(!bAllPAKs ? PAK_PATH + "\\" + FWindow.FCurrentPAK : PAK_PATH, "Success").Update();
        }
    }
}
