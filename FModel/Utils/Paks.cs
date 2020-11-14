using FModel.Grabber.Paks;
using FModel.Logger;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System;
using Windows.Management.Deployment;
using FModel.PakReader;

namespace FModel.Utils
{
    static class Paks
    {
        /// <summary>
        /// 1. AppName
        /// 2. AppVersion
        /// 3. AppFilesPath
        /// </summary>
        /// <returns></returns>
        public static (string, string, string) GetUEGameFilesPath(string game)
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                string launcher = $"{drive.Name}ProgramData\\Epic\\UnrealEngineLauncher\\LauncherInstalled.dat";
                if (File.Exists(launcher))
                {
                    DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[LauncherInstalled.dat]", launcher);
                    LauncherDat launcherDat = JsonConvert.DeserializeObject<LauncherDat>(File.ReadAllText(launcher));
                    if (launcherDat?.InstallationList != null)
                    {
                        foreach (InstallationList installationList in launcherDat.InstallationList)
                        {
                            if (installationList.AppName.Equals(game))
                                return (installationList.AppName, installationList.AppVersion,
                                    installationList.InstallLocation);
                        }

                        DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[LauncherInstalled.dat]",
                            $"{game} not found");
                        return (string.Empty, string.Empty, string.Empty);
                    }
                }
            }

            DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[LauncherInstalled.dat]", "File not found");
            return (string.Empty, string.Empty, string.Empty);
        }

        // This method is in testing and is not recommended to be used other than for development purposes
        public static string GetUWPPakFilesPath(string game)
        {
            try
            {
                foreach (var pkg in new PackageManager().FindPackagesForUser(string.Empty, game))
                {
                    DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Microsoft Store]", $"UWP Game {game} found at {pkg.EffectiveLocation.Name}");
                    return pkg.EffectivePath;
                }
            }
            catch (UnauthorizedAccessException)
            {
                DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Microsoft Store]",
                    $"The WindowsApps folder can't be accessed without permission changes to the folder.");
            }
            catch
            {
                DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Microsoft Store]", $"An expected error has ocurred.");
            }

            DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Microsoft Store]", $"{game} cannot be found on this system.");
            return string.Empty;
        }

        public static string GetFortnitePakFilesPath()
        {
            (_, string _, string fortniteFilesPath) = GetUEGameFilesPath("Fortnite");
            if (!string.IsNullOrEmpty(fortniteFilesPath))
                return $"{fortniteFilesPath}\\FortniteGame\\Content\\Paks";
            else
                return string.Empty;
        }

        public static string GetValorantPakFilesPath()
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                string installs = $"{drive.Name}ProgramData\\Riot Games\\RiotClientInstalls.json";
                if (File.Exists(installs))
                {
                    DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[RiotClientInstalls.json]", installs);
                    InstallsJson installsJson = JsonConvert.DeserializeObject<InstallsJson>(File.ReadAllText(installs));
                    if (installsJson != null && installsJson.AssociatedClient.Count > 0)
                    {
                        foreach (var KvP in installsJson.AssociatedClient)
                            if (KvP.Key.Contains("VALORANT/live/"))
                                return $"{KvP.Key.Replace("/", "\\")}ShooterGame\\Content\\Paks";

                        DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[RiotClientInstalls.json]",
                            "Valorant not found");
                    }
                }
            }

            DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[RiotClientInstalls.json]", "File not found");
            return string.Empty;
        }

        // public static string GetStateOfDecay2PakFilesPath()
        // {
        //     (_, string _, string sod2PakFilesPath) = GetUEGameFilesPath("<Replace when known with EGL name>");
        //     if (!string.IsNullOrEmpty(sod2PakFilesPath))
        //     {
        //         return $"{sod2PakFilesPath}\\StateOfDecay2\\Content\\Paks";
        //     }
        //
        //     return string.Empty;
        // }

        public static string GetBorderlands3PakFilesPath()
        {
            (_, string _, string borderlands3FilesPath) = GetUEGameFilesPath("Catnip");
            if (!string.IsNullOrEmpty(borderlands3FilesPath))
                return $"{borderlands3FilesPath}\\OakGame\\Content\\Paks";
            else
                return string.Empty;
        }

        public static string GetTheCyclePakFilesPath()
        {
            (_, string _, string theCycleFilesPath) = GetUEGameFilesPath("AzaleaAlpha"); // TODO: Change when out of alpha
            if (!string.IsNullOrEmpty(theCycleFilesPath))
                return $"{theCycleFilesPath}\\Prospect\\Content\\Paks";
            else
                return string.Empty;
        }

        public static string GetMinecraftDungeonsPakFilesPath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var install = $"{appData}/.minecraft_dungeons/launcher_settings.json";
            if (File.Exists(install))
            {
                DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[launcher_settings.json]", install);
                var launcherSettings = JsonConvert.DeserializeObject<LauncherSettings>(File.ReadAllText(install));

                if (launcherSettings.productLibraryDir != null &&
                    !string.IsNullOrEmpty(launcherSettings.productLibraryDir))
                    return $"{launcherSettings.productLibraryDir}\\dungeons\\dungeons\\Dungeons\\Content\\Paks";

                DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[launcher_settings.json]",
                    "Minecraft Dungeons not found");
            }

            return string.Empty;
        }

        public static string GetBattleBreakersPakFilesPath()
        {
            (_, string _, string battlebreakersFilesPath) = GetUEGameFilesPath("WorldExplorersLive");
            if (!string.IsNullOrEmpty(battlebreakersFilesPath))
                return $"{battlebreakersFilesPath}\\WorldExplorers\\Content\\Paks";
            else
                return string.Empty;
        }

        public static string GetSpellbreakPakFilesPath()
        {
            (_, string _, string spellbreakFilesPath) = GetUEGameFilesPath("Newt");
            if (!string.IsNullOrEmpty(spellbreakFilesPath))
                return $"{spellbreakFilesPath}\\g3\\Content\\Paks";
            else
                return string.Empty;
        }

        public static void Merge<T>(Dictionary<string, T> tempFiles, out Dictionary<string, T> files, string mount) where T : ReaderEntry
        {
            files = new Dictionary<string, T>();
            foreach (var entry in tempFiles.Values)
            {
                if (files.ContainsKey(mount + entry.GetPathWithoutExtension()) ||
                    entry.GetExtension().Equals(".uptnl") ||
                    entry.GetExtension().Equals(".uexp") ||
                    entry.GetExtension().Equals(".ubulk"))
                    continue;

                if (entry.IsUE4Package()) // if .uasset
                {
                    if (!tempFiles.ContainsKey(Path.ChangeExtension(entry.Name, ".umap"))) // but not including a .umap
                    {
                        string e = Path.ChangeExtension(entry.Name, ".uexp");
                        var uexp = tempFiles.ContainsKey(e) ? tempFiles[e] : null; // add its uexp
                        if (uexp != null)
                            entry.Uexp = uexp;

                        string u = Path.ChangeExtension(entry.Name, ".ubulk");
                        var ubulk = tempFiles.ContainsKey(u) ? tempFiles[u] : null; // add its ubulk
                        if (ubulk != null)
                            entry.Ubulk = ubulk;
                        else
                        {
                            string f = Path.ChangeExtension(entry.Name, ".ufont");
                            var ufont = tempFiles.ContainsKey(f) ? tempFiles[f] : null; // add its ufont
                            if (ufont != null)
                                entry.Ubulk = ufont;
                        }
                    }
                }
                else if (entry.IsUE4Map()) // if .umap
                {
                    string e = Path.ChangeExtension(entry.Name, ".uexp");
                    string u = Path.ChangeExtension(entry.Name, ".ubulk");
                    var uexp = tempFiles.ContainsKey(e) ? tempFiles[e] : null; // add its uexp
                    if (uexp != null)
                        entry.Uexp = uexp;
                    var ubulk = tempFiles.ContainsKey(u) ? tempFiles[u] : null; // add its ubulk
                    if (ubulk != null)
                        entry.Ubulk = ubulk;
                }

                files[mount + entry.GetPathWithoutExtension()] = entry;
                if (Globals.Game.ActualGame == EGame.Unknown)
                    Folders.SetGameName((mount.Length == 1 ? entry.GetFirstFolder() : mount) + "\\Content\\Paks");
            }
        }

        public static bool IsFileReadLocked(FileInfo PakFileInfo)
        {
            FileStream stream = null;
            try
            {
                stream = PakFileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                stream?.Close();
            }

            return false;
        }

        public static bool IsFileWriteLocked(FileInfo PakFileInfo)
        {
            FileStream stream = null;
            try
            {
                stream = PakFileInfo.Open(FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                stream?.Close();
            }

            return false;
        }
    }
}