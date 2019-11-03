using FModel.Methods.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FProp = FModel.Properties.Settings;

namespace FModel.Methods.PAKs
{
    static class RegisterFromPath
    {
        public static string PAK_PATH = FProp.Default.FPak_Path;

        public static void FilterPAKs()
        {
            if (string.IsNullOrEmpty(FProp.Default.FPak_Path) && DatFileExists())
            {
                string AutoPath = GetGameFiles();
                if (!string.IsNullOrEmpty(AutoPath))
                {
                    new UpdateMyConsole(".PAK files path detected at ", CColors.White).Append();
                    new UpdateMyConsole(AutoPath, CColors.Blue, true).Append();

                    FProp.Default.FPak_Path = AutoPath;
                    FProp.Default.Save();

                    PAK_PATH = FProp.Default.FPak_Path;
                }
            }

            if (Directory.Exists(PAK_PATH))
            {
                PAKEntries.PAKEntriesList = new List<PAKInfosEntry>();
                foreach (string Pak in GetPAKsFromPath())
                {
                    if (!PAKsUtility.IsPAKLocked(new FileInfo(Pak)))
                    {
                        if (PAKsUtility.GetPAKVersion(Pak) == 8)
                        {
                            string PAKGuid = PAKsUtility.GetPAKGuid(Pak);
                            PAKEntries.PAKEntriesList.Add(new PAKInfosEntry(Pak, PAKGuid, string.Equals(PAKGuid, "0-0-0-0") ? false : true));
                            FWindow.FMain.Dispatcher.InvokeAsync(() =>
                            {
                                MenuItem MI_Pak = new MenuItem();
                                MI_Pak.Header = Path.GetFileName(Pak);
                                MI_Pak.Click += new RoutedEventHandler(FWindow.FMain.MI_Pak_Click);

                                FWindow.FMain.MI_LoadOnePAK.Items.Add(MI_Pak);
                            });
                        }
                        else { new UpdateMyProcessEvents($"Unsupported .PAK Version for {Path.GetFileName(Pak)}", "Error").Update(); }
                    }
                    else
                    {
                        new UpdateMyConsole(Path.GetFileName(Pak), CColors.Blue).Append();
                        new UpdateMyConsole(" is locked by another process.", CColors.White, true).Append();
                    }
                }

                FWindow.FMain.Dispatcher.InvokeAsync(() =>
                {
                    FWindow.FMain.MI_LoadOnePAK.IsEnabled = true;
                    FWindow.FMain.MI_LoadAllPAKs.IsEnabled = true;
                    FWindow.FMain.MI_BackupPAKs.IsEnabled = true;
                });
            }
            else { new UpdateMyProcessEvents(".PAK Files Input Path is missing", "Error").Update(); }
        }

        private static IEnumerable<string> GetPAKsFromPath()
        {
            return Directory.GetFiles(PAK_PATH, "*.pak", SearchOption.AllDirectories);
        }

        private static string GetEpicDirectory() => Directory.Exists(@"C:\ProgramData\Epic") ? @"C:\ProgramData\Epic" : Directory.Exists(@"D:\ProgramData\Epic") ? @"D:\ProgramData\Epic" : @"E:\ProgramData\Epic";
        private static bool DatFileExists() => File.Exists($@"{GetEpicDirectory()}\UnrealEngineLauncher\LauncherInstalled.dat");
        private static string GetGameFiles()
        {
            if (DatFileExists())
            {
                string jsonData = File.ReadAllText($@"{GetEpicDirectory()}\UnrealEngineLauncher\LauncherInstalled.dat");
                if (AssetsUtility.IsValidJson(jsonData))
                {
                    JToken games = JsonConvert.DeserializeObject<JToken>(jsonData);
                    if (games != null)
                    {
                        JArray installationListArray = games["InstallationList"].Value<JArray>();
                        if (installationListArray != null)
                        {
                            foreach (JToken game in installationListArray)
                            {
                                if (string.Equals(game["AppName"].Value<string>(), "Fortnite"))
                                {
                                    return $@"{game["InstallLocation"].Value<string>()}\FortniteGame\Content\Paks";
                                }
                            }
                        }
                    }
                }
            }
            return string.Empty;
        }
    }
}