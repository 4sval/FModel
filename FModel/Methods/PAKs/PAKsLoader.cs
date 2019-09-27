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
    static class PAKsLoader
    {
        private static readonly string PAK_PATH = FProp.Default.FPak_Path;
        private static SortedTreeViewWindowViewModel srt { get; set; }

        public static async Task LoadOnePAK()
        {
            FWindow.FMain.MI_LoadOnePAK.IsEnabled = false;
            FWindow.FMain.MI_LoadAllPAKs.IsEnabled = false;
            FWindow.FMain.AssetPropertiesBox_Main.Text = string.Empty;
            FWindow.FMain.ViewModel = srt = new SortedTreeViewWindowViewModel();
            FWindow.FMain.ImageBox_Main.Source = null;
            ListBoxUtility.FilesListWithoutPath = null;
            FWindow.FMain.ListBox_Main.Items.Clear();

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

            FWindow.FMain.MI_LoadOnePAK.IsEnabled = true;
            FWindow.FMain.MI_LoadAllPAKs.IsEnabled = true;
        }
        public static async Task LoadAllPAKs()
        {
            FWindow.FMain.MI_LoadOnePAK.IsEnabled = false;
            FWindow.FMain.MI_LoadAllPAKs.IsEnabled = false;
            FWindow.FMain.AssetPropertiesBox_Main.Text = string.Empty;
            FWindow.FMain.ViewModel = srt = new SortedTreeViewWindowViewModel();
            FWindow.FMain.ImageBox_Main.Source = null;
            ListBoxUtility.FilesListWithoutPath = null;
            FWindow.FMain.ListBox_Main.Items.Clear();

            await Task.Run(() =>
            {
                PAKEntries.PAKToDisplay = new Dictionary<string, FPakEntry[]>();

                LoadPAKFiles(true);
                FillTreeView(true);

            }).ContinueWith(TheTask =>
            {
                TasksUtility.TaskCompleted(TheTask.Exception);
            });

            FWindow.FMain.MI_LoadOnePAK.IsEnabled = true;
            FWindow.FMain.MI_LoadAllPAKs.IsEnabled = true;
        }

        private static void LoadPAKFiles(bool bAllPAKs = false)
        {
            if (PAKEntries.PAKEntriesList != null && PAKEntries.PAKEntriesList.Any())
            {
                AssetEntries.ArraySearcher = new Dictionary<string, FPakEntry[]>();
                AssetEntries.AssetEntriesDict = new Dictionary<string, PakReader.PakReader>();

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
                            AssetEntries.AssetEntriesDict[entry.Name] = reader;
                            AssetEntries.ArraySearcher[entry.Name] = reader.FileInfos;
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

                    if (AESKey != null)
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
                                AssetEntries.AssetEntriesDict[entry.Name] = reader;
                                AssetEntries.ArraySearcher[entry.Name] = reader.FileInfos;
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
