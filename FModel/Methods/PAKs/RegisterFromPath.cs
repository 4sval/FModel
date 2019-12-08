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
        private static string datFilePath = string.Empty;

        public static void FilterPAKs()
        {
            if (string.IsNullOrEmpty(FProp.Default.FPak_Path) && !string.IsNullOrEmpty(datFilePath))
            {
                string AutoPath = GetGameInstallLocation();
                if (!string.IsNullOrEmpty(AutoPath))
                {
                    DebugHelper.WriteLine("Auto .PAK files detection at " + AutoPath);
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
                        string PAKGuid = PAKsUtility.GetPAKGuid(Pak);
                        DebugHelper.WriteLine("Registering " + Pak + " with GUID " + PAKGuid + " (" + PAKsUtility.GetEpicGuid(PAKGuid) + ")");

                        PAKEntries.PAKEntriesList.Add(new PAKInfosEntry(Pak, PAKGuid, string.Equals(PAKGuid, "0-0-0-0") ? false : true));
                        FWindow.FMain.Dispatcher.InvokeAsync(() =>
                        {
                            MenuItem MI_Pak = new MenuItem();
                            MI_Pak.Header = Path.GetFileName(Pak);
                            MI_Pak.Click += new RoutedEventHandler(FWindow.FMain.MI_Pak_Click);

                            FWindow.FMain.MI_LoadOnePAK.Items.Add(MI_Pak);
                        });
                    }
                    else
                    {
                        DebugHelper.WriteLine(Path.GetFileName(Pak) + " is locked by another process.");

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
        private static string GetDatFile()
        {
            if (!string.IsNullOrEmpty(datFilePath))
                return datFilePath;

            if (File.Exists($@"{GetEpicDirectory()}\UnrealEngineLauncher\LauncherInstalled.dat"))
            {
                datFilePath = $@"{GetEpicDirectory()}\UnrealEngineLauncher\LauncherInstalled.dat";
                DebugHelper.WriteLine("EPIC .dat file at " + datFilePath);
            }
            else
                DebugHelper.WriteLine("EPIC .dat file not found");

            return datFilePath;
        }

        private static string GetGameInstallLocation()
        {
            JToken game = GetGameData();
            if (game != null)
                return $@"{game["InstallLocation"].Value<string>()}\FortniteGame\Content\Paks";

            return string.Empty;
        }

        private static JToken GetGameData()
        {
            GetDatFile();
            if (!string.IsNullOrEmpty(datFilePath))
            {
                string jsonData = File.ReadAllText(datFilePath);
                if (AssetsUtility.IsValidJson(jsonData))
                {
                    JToken games = JsonConvert.DeserializeObject<JToken>(jsonData);
                    if (games != null)
                    {
                        JArray installationListArray = games["InstallationList"].Value<JArray>();
                        if (installationListArray != null)
                            return installationListArray.Where(game => string.Equals(game["AppName"].Value<string>(), "Fortnite")).FirstOrDefault();

                        DebugHelper.WriteLine("Fortnite not found in .dat file");
                    }
                }
            }

            return null;
        }

        public static void CheckFortniteVersion()
        {
            JToken game = GetGameData();
            if (game != null)
                DebugHelper.WriteLine("Fortnite version: " + game["AppVersion"] + " found in .dat file");
        }
    }
}
