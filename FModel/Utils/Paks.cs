using FModel.Grabber.Paks;
using FModel.Logger;
using Newtonsoft.Json;
using PakReader.Parsers.Objects;
using System.Collections.Generic;
using System.IO;

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
        public static (string, string, string) GetFortnitePakFilesPath()
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
                            if (installationList.AppName.Equals("Fortnite"))
                                return (installationList.AppName, installationList.AppVersion, $"{installationList.InstallLocation}\\FortniteGame\\Content\\Paks");
                        }

                        DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[LauncherInstalled.dat]", "Fortnite not found");
                    }
                }
            }

            DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[LauncherInstalled.dat]", "File not found");
            return (string.Empty, string.Empty, string.Empty);
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
                    if (installsJson?.AssociatedClient.Count > 0)
                    {
                        foreach (var KvP in installsJson.AssociatedClient)
                            if (KvP.Key.Contains("VALORANT/live/"))
                                return $"{KvP.Key.Replace("/", "\\")}ShooterGame\\Content\\Paks";

                        DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[RiotClientInstalls.json]", "Valorant not found");
                    }
                }
            }

            DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[RiotClientInstalls.json]", "File not found");
            return string.Empty;
        }

        public static void Merge(Dictionary<string, FPakEntry> tempFiles, out Dictionary<string, FPakEntry> files, string mount)
        {
            files = new Dictionary<string, FPakEntry>();
            foreach (FPakEntry entry in tempFiles.Values)
            {
                if (files.ContainsKey(mount + entry.GetPathWithoutExtension()) || entry.GetExtension().Equals(".uptnl"))
                    continue;

                if (entry.IsUE4Package()) // if .uasset
                {
                    if (!tempFiles.ContainsKey(Path.ChangeExtension(entry.Name, ".umap"))) // but not including a .umap
                    {
                        string e = Path.ChangeExtension(entry.Name, ".uexp");
                        FPakEntry uexp = tempFiles.ContainsKey(e) ? tempFiles[e] : null; // add its uexp
                        if (uexp != null)
                            entry.Uexp = uexp;

                        string u = Path.ChangeExtension(entry.Name, ".ubulk");
                        FPakEntry ubulk = tempFiles.ContainsKey(u) ? tempFiles[u] : null; // add its ubulk
                        if (ubulk != null)
                            entry.Ubulk = ubulk;
                        else
                        {
                            string f = Path.ChangeExtension(entry.Name, ".ufont");
                            FPakEntry ufont = tempFiles.ContainsKey(f) ? tempFiles[f] : null; // add its ufont
                            if (ufont != null)
                                entry.Ubulk = ufont;
                        }
                    }
                }
                else if (entry.IsUE4Map()) // if .umap
                {
                    string e = Path.ChangeExtension(entry.Name, ".uexp");
                    string u = Path.ChangeExtension(entry.Name, ".ubulk");
                    FPakEntry uexp = tempFiles.ContainsKey(e) ? tempFiles[e] : null; // add its uexp
                    if (uexp != null)
                        entry.Uexp = uexp;
                    FPakEntry ubulk = tempFiles.ContainsKey(u) ? tempFiles[u] : null; // add its ubulk
                    if (ubulk != null)
                        entry.Ubulk = ubulk;
                }

                files[mount + entry.GetPathWithoutExtension()] = entry;
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
                if (stream != null)
                    stream.Close();
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
                if (stream != null)
                    stream.Close();
            }

            return false;
        }
    }
}
