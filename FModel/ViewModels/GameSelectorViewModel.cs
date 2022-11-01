using FModel.Extensions;
using FModel.Framework;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CUE4Parse.UE4.Objects.Core.Serialization;
using CUE4Parse.UE4.Versions;
using FModel.Settings;
using FModel.ViewModels.ApiEndpoints.Models;
using Microsoft.Win32;

namespace FModel.ViewModels;

public class GameSelectorViewModel : ViewModel
{
    public class DetectedGame
    {
        public string GameName { get; set; }
        public string GameDirectory { get; set; }
        public bool IsManual { get; set; }

        // the followings are only used when game is manually added
        public AesResponse AesKeys { get; set; }
        public EGame OverridedGame { get; set; }
        public List<FCustomVersion> OverridedCustomVersions { get; set; }
        public Dictionary<string, bool> OverridedOptions { get; set; }
        public IList<CustomDirectory> CustomDirectories { get; set; }
    }

    private DetectedGame _selectedDetectedGame;
    public DetectedGame SelectedDetectedGame
    {
        get => _selectedDetectedGame;
        set => SetProperty(ref _selectedDetectedGame, value);
    }

    private readonly ObservableCollection<DetectedGame> _autoDetectedGames;
    public ReadOnlyObservableCollection<DetectedGame> AutoDetectedGames { get; }

    public GameSelectorViewModel(string gameDirectory)
    {
        _autoDetectedGames = new ObservableCollection<DetectedGame>(EnumerateDetectedGames().Where(x => x != null));
        foreach (var game in UserSettings.Default.ManualGames.Values)
        {
            _autoDetectedGames.Add(game);
        }

        AutoDetectedGames = new ReadOnlyObservableCollection<DetectedGame>(_autoDetectedGames);

        if (AutoDetectedGames.FirstOrDefault(x => x.GameDirectory == gameDirectory) is { } detectedGame)
            SelectedDetectedGame = detectedGame;
        else if (!string.IsNullOrEmpty(gameDirectory))
            AddUnknownGame(gameDirectory);
        else
            SelectedDetectedGame = AutoDetectedGames.FirstOrDefault();
    }

    /// <summary>
    /// dedicated to manual games
    /// </summary>
    public void AddUnknownGame(string gameName, string gameDirectory)
    {
        var game = new DetectedGame
        {
            GameName = gameName,
            GameDirectory = gameDirectory,
            IsManual = true,
            AesKeys = null,
            OverridedGame = EGame.GAME_UE4_LATEST,
            OverridedCustomVersions = null,
            OverridedOptions = null,
            CustomDirectories = new List<CustomDirectory>()
        };

        UserSettings.Default.ManualGames[gameDirectory] = game;
        _autoDetectedGames.Add(game);
        SelectedDetectedGame = AutoDetectedGames.Last();
    }

    public void AddUnknownGame(string gameDirectory)
    {
        _autoDetectedGames.Add(new DetectedGame { GameName = gameDirectory.SubstringAfterLast('\\'), GameDirectory = gameDirectory });
        SelectedDetectedGame = AutoDetectedGames.Last();
    }

    public void DeleteSelectedGame()
    {
        UserSettings.Default.ManualGames.Remove(SelectedDetectedGame.GameDirectory); // should not be a problem
        _autoDetectedGames.Remove(SelectedDetectedGame);
        SelectedDetectedGame = AutoDetectedGames.Last();
    }

    private IEnumerable<DetectedGame> EnumerateDetectedGames()
    {
        yield return GetUnrealEngineGame("Fortnite");
        yield return new DetectedGame { GameName = "Fortnite [LIVE]", GameDirectory = Constants._FN_LIVE_TRIGGER };
        yield return GetUnrealEngineGame("Pewee");
        yield return GetUnrealEngineGame("Rosemallow");
        yield return GetUnrealEngineGame("Catnip");
        yield return GetUnrealEngineGame("AzaleaAlpha");
        yield return GetUnrealEngineGame("WorldExplorersLive");
        yield return GetUnrealEngineGame("Newt");
        yield return GetUnrealEngineGame("shoebill");
        yield return GetUnrealEngineGame("Snoek");
        yield return GetUnrealEngineGame("a99769d95d8f400baad1f67ab5dfe508");
        yield return GetUnrealEngineGame("Nebula");
        yield return GetUnrealEngineGame("711c5e95dc094ca58e5f16bd48e751d6");
        yield return GetRiotGame("VALORANT", "ShooterGame\\Content\\Paks");
        yield return new DetectedGame { GameName = "Valorant [LIVE]", GameDirectory = Constants._VAL_LIVE_TRIGGER };
        yield return GetMojangGame("MinecraftDungeons", "\\dungeons\\dungeons\\Dungeons\\Content\\Paks");
        yield return GetSteamGame(381210, "\\DeadByDaylight\\Content\\Paks"); // Dead By Daylight
        yield return GetSteamGame(578080, "\\TslGame\\Content\\Paks"); // PUBG
        yield return GetSteamGame(677620, "\\PortalWars\\Content\\Paks"); // Splitgate
        yield return GetSteamGame(1172620, "\\Athena\\Content\\Paks"); // Sea of Thieves
        yield return GetRockstarGamesGame("GTA III - Definitive Edition", "\\Gameface\\Content\\Paks");
        yield return GetRockstarGamesGame("GTA San Andreas - Definitive Edition", "\\Gameface\\Content\\Paks");
        yield return GetRockstarGamesGame("GTA Vice City - Definitive Edition", "\\Gameface\\Content\\Paks");
        yield return GetLevelInfiniteGame("tof_launcher", "\\Hotta\\Content\\Paks");
    }

    private LauncherInstalled _launcherInstalled;
    private DetectedGame GetUnrealEngineGame(string gameName)
    {
        _launcherInstalled ??= GetDriveLauncherInstalls<LauncherInstalled>("ProgramData\\Epic\\UnrealEngineLauncher\\LauncherInstalled.dat");
        if (_launcherInstalled?.InstallationList != null)
        {
            foreach (var installationList in _launcherInstalled.InstallationList)
            {
                if (installationList.AppName.Equals(gameName, StringComparison.OrdinalIgnoreCase))
                {
                    var pak = Directory.GetDirectories(installationList.InstallLocation, "Paks*", SearchOption.AllDirectories);
                    if (pak.Length > 0) return new DetectedGame { GameName = installationList.AppName, GameDirectory = pak[0] };
                }
            }
        }

        Log.Warning("Could not find {GameName} in LauncherInstalled.dat", gameName);
        return null;
    }

    private RiotClientInstalls _riotClientInstalls;
    private DetectedGame GetRiotGame(string gameName, string pakDirectory)
    {
        _riotClientInstalls ??= GetDriveLauncherInstalls<RiotClientInstalls>("ProgramData\\Riot Games\\RiotClientInstalls.json");
        if (_riotClientInstalls is { AssociatedClient: { } })
        {
            foreach (var (key, _) in _riotClientInstalls.AssociatedClient)
            {
                if (key.Contains(gameName, StringComparison.OrdinalIgnoreCase))
                    return new DetectedGame { GameName = gameName, GameDirectory = $"{key.Replace('/', '\\')}{pakDirectory}" };
            }
        }

        Log.Warning("Could not find {GameName} in RiotClientInstalls.json", gameName);
        return null;
    }

    private LauncherSettings _launcherSettings;
    private DetectedGame GetMojangGame(string gameName, string pakDirectory)
    {
        _launcherSettings ??= GetDataLauncherInstalls<LauncherSettings>("\\.minecraft\\launcher_settings.json");
        if (_launcherSettings is { ProductLibraryDir: { } })
            return new DetectedGame { GameName = gameName, GameDirectory = $"{_launcherSettings.ProductLibraryDir}{pakDirectory}" };

        Log.Warning("Could not find {GameName} in launcher_settings.json", gameName);
        return null;
    }

    private DetectedGame GetSteamGame(int id, string pakDirectory)
    {
        var steamInfo = SteamDetection.GetSteamGameById(id);
        if (steamInfo is not null)
            return new DetectedGame { GameName = steamInfo.Name, GameDirectory = $"{steamInfo.GameRoot}{pakDirectory}" };

        Log.Warning("Could not find {GameId} in steam manifests", id);
        return null;
    }

    private DetectedGame GetRockstarGamesGame(string key, string pakDirectory)
    {
        var installLocation = string.Empty;
        try
        {
            installLocation = App.GetRegistryValue(@$"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{key}", "InstallLocation", RegistryHive.LocalMachine);
        }
        catch
        {
            // ignored
        }

        if (!string.IsNullOrEmpty(installLocation))
            return new DetectedGame { GameName = key, GameDirectory = $"{installLocation}{pakDirectory}" };

        Log.Warning("Could not find {GameName} in the registry", key);
        return null;
    }

    private DetectedGame GetLevelInfiniteGame(string key, string pakDirectory)
    {
        var installLocation = string.Empty;
        var displayName = string.Empty;

        try
        {
            installLocation = App.GetRegistryValue($@"Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{key}", "GameInstallPath", RegistryHive.CurrentUser);
            displayName = App.GetRegistryValue($@"Software\Microsoft\Windows\CurrentVersion\Uninstall\{key}", "DisplayName", RegistryHive.CurrentUser);
        }
        catch
        {
            // ignored
        }

        if (!string.IsNullOrEmpty(installLocation))
            return new DetectedGame { GameName = displayName, GameDirectory = $"{installLocation}{pakDirectory}" };

        Log.Warning("Could not find {GameName} in the registry", key);
        return null;
    }

    private T GetDriveLauncherInstalls<T>(string jsonFile)
    {
        foreach (var drive in DriveInfo.GetDrives())
        {
            var launcher = $"{drive.Name}{jsonFile}";
            if (!File.Exists(launcher)) continue;

            Log.Information("\"{Launcher}\" found in drive \"{DriveName}\"", launcher, drive.Name);
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(launcher));
        }

        Log.Warning("\"{JsonFile}\" not found in any drives", jsonFile);
        return default;
    }

    private T GetDataLauncherInstalls<T>(string jsonFile)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var launcher = $"{appData}{jsonFile}";
        if (File.Exists(launcher))
        {
            Log.Information("\"{Launcher}\" found in \"{AppData}\"", launcher, appData);
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(launcher));
        }

        Log.Warning("\"{Json}\" not found anywhere", jsonFile);
        return default;
    }

#pragma warning disable 649
    private class LauncherInstalled
    {
        public Installation[] InstallationList;
    }

    private class Installation
    {
        public string InstallLocation;
        public string AppName;
        public string AppVersion;
    }

    private class RiotClientInstalls
    {
        [JsonProperty("associated_client", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> AssociatedClient;

        [JsonProperty("patchlines", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> Patchlines;

        [JsonProperty("rc_default", NullValueHandling = NullValueHandling.Ignore)]
        public string RcDefault;

        [JsonProperty("rc_live", NullValueHandling = NullValueHandling.Ignore)]
        public string RcLive;
    }

    private class LauncherSettings
    {
        [JsonProperty("channel", NullValueHandling = NullValueHandling.Ignore)]
        public string Channel;

        [JsonProperty("customChannels", NullValueHandling = NullValueHandling.Ignore)]
        public object[] CustomChannels;

        [JsonProperty("deviceId", NullValueHandling = NullValueHandling.Ignore)]
        public string DeviceId;

        [JsonProperty("formatVersion", NullValueHandling = NullValueHandling.Ignore)]
        public int FormatVersion;

        [JsonProperty("locale", NullValueHandling = NullValueHandling.Ignore)]
        public string Locale;

        [JsonProperty("productLibraryDir", NullValueHandling = NullValueHandling.Ignore)]
        public string ProductLibraryDir;
    }
#pragma warning restore 649

    // https://stackoverflow.com/questions/54767662/finding-game-launcher-executables-in-directory-c-sharp/67679123#67679123
    public static class SteamDetection
    {
        private static readonly List<AppInfo> _steamApps;

        static SteamDetection()
        {
            _steamApps = GetSteamApps(GetSteamLibs());
        }

        public static AppInfo GetSteamGameById(int id) => _steamApps.FirstOrDefault(app => app.Id == id.ToString());

        private static List<AppInfo> GetSteamApps(IEnumerable<string> steamLibs)
        {
            var apps = new List<AppInfo>();
            foreach (var files in steamLibs.Select(lib => Path.Combine(lib, "SteamApps")).Select(appMetaDataPath => Directory.GetFiles(appMetaDataPath, "*.acf")))
            {
                apps.AddRange(files.Select(GetAppInfo).Where(appInfo => appInfo != null));
            }

            return apps;
        }

        private static AppInfo GetAppInfo(string appMetaFile)
        {
            var fileDataLines = File.ReadAllLines(appMetaFile);
            var dic = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in fileDataLines)
            {
                var match = Regex.Match(line, @"\s*""(?<key>\w+)""\s+""(?<val>.*)""");
                if (!match.Success) continue;
                var key = match.Groups["key"].Value;
                var val = match.Groups["val"].Value;
                dic[key] = val;
            }

            if (dic.Keys.Count <= 0) return null;
            AppInfo appInfo = new();
            var appId = dic["appid"];
            var name = dic["name"];
            var installDir = dic["installDir"];

            var path = Path.GetDirectoryName(appMetaFile);
            var libGameRoot = Path.Combine(path, "common", installDir);

            if (!Directory.Exists(libGameRoot)) return null;

            appInfo.Id = appId;
            appInfo.Name = name;
            appInfo.GameRoot = libGameRoot;

            return appInfo;
        }

        private static List<string> GetSteamLibs()
        {
            var steamPath = GetSteamPath();
            if (steamPath == null) return new List<string>();
            var libraries = new List<string> { steamPath };

            var listFile = Path.Combine(steamPath, @"steamapps\libraryfolders.vdf");
            if (!File.Exists(listFile)) return new List<string>();
            var lines = File.ReadAllLines(listFile);
            foreach (var line in lines)
            {
                var match = Regex.Match(line, @"""(?<path>\w:\\\\.*)""");
                if (!match.Success) continue;
                var path = match.Groups["path"].Value.Replace(@"\\", @"\");
                if (Directory.Exists(path))
                {
                    libraries.Add(path);
                }
            }

            return libraries;
        }

        private static string GetSteamPath() => (string) Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath", ""); // Win64, we don't support Win32

        public class AppInfo
        {
            public string Id { get; internal set; }
            public string Name { get; internal set; }
            public string GameRoot { get; internal set; }

            public override string ToString()
            {
                return $"{Name} ({Id})";
            }
        }
    }
}
