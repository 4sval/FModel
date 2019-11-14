using FModel.Methods.Assets;
using FModel.Methods.TreeViewModel;
using FModel.Methods.Utilities;
using Microsoft.Win32;
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
        public static bool umIsOk { get; set; }
        private static readonly string PAK_PATH = FProp.Default.FPak_Path;
        private static SortedTreeViewWindowViewModel srt { get; set; }

        public static async Task LoadOnePAK()
        {
            FWindow.FMain.MI_LoadOnePAK.IsEnabled = false;
            FWindow.FMain.MI_LoadAllPAKs.IsEnabled = false;
            FWindow.FMain.MI_BackupPAKs.IsEnabled = false;
            FWindow.FMain.MI_DifferenceMode.IsEnabled = false;
            FWindow.FMain.MI_UpdateMode.IsEnabled = false;
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
            FWindow.FMain.MI_BackupPAKs.IsEnabled = true;
            FWindow.FMain.MI_DifferenceMode.IsEnabled = true;
        }
        public static async Task LoadAllPAKs()
        {
            FWindow.FMain.MI_LoadOnePAK.IsEnabled = false;
            FWindow.FMain.MI_LoadAllPAKs.IsEnabled = false;
            FWindow.FMain.MI_BackupPAKs.IsEnabled = false;
            FWindow.FMain.MI_DifferenceMode.IsEnabled = false;
            FWindow.FMain.MI_UpdateMode.IsEnabled = false;
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
            FWindow.FMain.MI_BackupPAKs.IsEnabled = true;
            FWindow.FMain.MI_DifferenceMode.IsEnabled = true;
        }
        public static async Task LoadDifference(bool updateMode = false)
        {
            FWindow.FMain.MI_LoadOnePAK.IsEnabled = false;
            FWindow.FMain.MI_LoadAllPAKs.IsEnabled = false;
            FWindow.FMain.MI_BackupPAKs.IsEnabled = false;
            FWindow.FMain.MI_DifferenceMode.IsEnabled = false;
            FWindow.FMain.MI_UpdateMode.IsEnabled = false;
            FWindow.FMain.AssetPropertiesBox_Main.Text = string.Empty;
            FWindow.FMain.ViewModel = srt = new SortedTreeViewWindowViewModel();
            FWindow.FMain.ImageBox_Main.Source = null;
            ListBoxUtility.FilesListWithoutPath = null;
            FWindow.FMain.ListBox_Main.Items.Clear();
            umIsOk = false;

            FWindow.FMain.TreeView_Main.IsEnabled = false;
            await Task.Run(async () =>
            {
                PAKEntries.PAKToDisplay = new Dictionary<string, FPakEntry[]>();

                LoadPAKFiles(true);
                await LoadBackupFile(updateMode);

            }).ContinueWith(TheTask =>
            {
                TasksUtility.TaskCompleted(TheTask.Exception);
            });
        }

        private static void LoadPAKFiles(bool bAllPAKs = false)
        {
            if (PAKEntries.PAKEntriesList != null && PAKEntries.PAKEntriesList.Any())
            {
                AssetEntries.ArraySearcher = new Dictionary<string, FPakEntry[]>();
                AssetEntries.AssetEntriesDict = new Dictionary<string, PakReader.PakReader>();
                bool isMainKeyWorking = false;

                //MAIN PAKs LOOP
                foreach (PAKInfosEntry Pak in PAKEntries.PAKEntriesList.Where(x => !x.bTheDynamicPAK))
                {
                    if (!string.IsNullOrEmpty(FProp.Default.FPak_MainAES))
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
                            else { new UpdateMyConsole(ex.Message, CColors.Red, true).Append(); return; }
                            break;
                        }

                        if (reader != null)
                        {
                            isMainKeyWorking = true;
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
                if (isMainKeyWorking) { AssetTranslations.SetAssetTranslation(FProp.Default.FLanguage); }

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
                            else { new UpdateMyConsole(ex.Message, CColors.Red, true).Append(); return; }
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

        private static async Task LoadBackupFile(bool updateMode = false)
        {
            OpenFileDialog openFiledialog = new OpenFileDialog();
            openFiledialog.Title = "Choose your Backup File";
            openFiledialog.InitialDirectory = FProp.Default.FOutput_Path + "\\Backups\\";
            openFiledialog.Multiselect = false;
            openFiledialog.Filter = "FBKP Files (*.fbkp)|*.fbkp|All Files (*.*)|*.*";
            if (openFiledialog.ShowDialog() == true)
            {
                new UpdateMyProcessEvents("Comparing Files", "Waiting").Update();

                FPakEntry[] BackupEntries;
                using (FileStream fileStream = new FileStream(openFiledialog.FileName, FileMode.Open))
                using (BinaryReader reader = new BinaryReader(fileStream))
                {
                    List<FPakEntry> entries = new List<FPakEntry>();
                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        FPakEntry entry = new FPakEntry();
                        entry.Pos = reader.ReadInt64();
                        entry.Size = reader.ReadInt64();
                        entry.UncompressedSize = reader.ReadInt64();
                        entry.Encrypted = reader.ReadBoolean();
                        entry.StructSize = reader.ReadInt32();
                        entry.Name = reader.ReadString();
                        entry.CompressionMethod = reader.ReadInt32();
                        entries.Add(entry);
                    }
                    BackupEntries = entries.ToArray();
                }

                if (BackupEntries.Any())
                {
                    List<FPakEntry> LocalEntries = new List<FPakEntry>();
                    foreach (FPakEntry[] PAKsFileInfos in PAKEntries.PAKToDisplay.Values)
                    {
                        PAKsFileInfos.ToList().ForEach(x => LocalEntries.Add(x));
                    }
                    PAKEntries.PAKToDisplay.Clear();

                    //FILTER WITH THE OVERRIDED EQUALS METHOD (CHECKING FILE NAME AND FILE UNCOMPRESSED SIZE)
                    IEnumerable<FPakEntry> newAssets = LocalEntries.ToArray().Except(BackupEntries);

                    //ADD TO TREE
                    foreach (FPakEntry entry in newAssets)
                    {
                        string onlyFolders = entry.Name.Substring(0, entry.Name.LastIndexOf('/'));
                        await FWindow.FMain.Dispatcher.InvokeAsync(() =>
                        {
                            TreeViewUtility.PopulateTreeView(srt, onlyFolders.Substring(1));
                        });
                    }

                    //ONLY LOAD THE DIFFERENCE WHEN WE CLICK ON A FOLDER
                    FWindow.FCurrentPAK = "ComparedPAK-WindowsClient.pak";
                    PAKEntries.PAKToDisplay.Add("ComparedPAK-WindowsClient.pak", newAssets.ToArray());
                    await FWindow.FMain.Dispatcher.InvokeAsync(() =>
                    {
                        FWindow.FMain.ViewModel = srt;
                    });

                    await FWindow.FMain.Dispatcher.InvokeAsync(() =>
                    {
                        FWindow.FMain.TreeView_Main.IsEnabled = true;
                        FWindow.FMain.MI_LoadAllPAKs.IsEnabled = true;
                        FWindow.FMain.MI_BackupPAKs.IsEnabled = true;
                        FWindow.FMain.MI_DifferenceMode.IsEnabled = true;
                        if (updateMode) { FWindow.FMain.MI_UpdateMode.IsEnabled = true; }
                    });

                    //PRINT REMOVED IF NO FILE SIZE CHECK
                    if (!FProp.Default.FDiffFileSize)
                    {
                        new UpdateMyProcessEvents("Checking deleted items", "Waiting").Update();
                        IEnumerable<FPakEntry> removedAssets = BackupEntries.Except(LocalEntries.ToArray());

                        List<string> removedItems = new List<string>();
                        foreach (FPakEntry entry in removedAssets)
                        {
                            if (entry.Name.StartsWith("/FortniteGame/Content/Athena/Items/Cosmetics/"))
                                removedItems.Add(entry.Name.Substring(0, entry.Name.LastIndexOf(".")));
                        }

                        if (removedItems.Count > 0)
                        {
                            new UpdateMyConsole("Items Removed/Renamed:", CColors.Red, true).Append();
                            removedItems.Distinct().ToList().ForEach(e => new UpdateMyConsole($"    - {e.Substring(1)}", CColors.White, true).Append());
                        }
                    }

                    new UpdateMyProcessEvents("All PAK files have been compared successfully", "Success").Update();
                    umIsOk = true;
                }
            }
            else
            {
                new UpdateMyConsole("You change your mind pretty fast but it's fine ", CColors.White).Append();
                new UpdateMyConsole("all paks have been loaded instead", CColors.Blue, true).Append();
                FillTreeView(true);

                await FWindow.FMain.Dispatcher.InvokeAsync(() =>
                {
                    FWindow.FMain.TreeView_Main.IsEnabled = true;
                    FWindow.FMain.MI_LoadAllPAKs.IsEnabled = true;
                    FWindow.FMain.MI_BackupPAKs.IsEnabled = true;
                    FWindow.FMain.MI_DifferenceMode.IsEnabled = true;
                    FWindow.FMain.MI_DifferenceMode.IsChecked = false;
                    if (updateMode)
                    {
                        FWindow.FMain.MI_UpdateMode.IsEnabled = true;
                        FWindow.FMain.MI_UpdateMode.IsChecked = false;
                    }
                });
            }
        }
    }
}
