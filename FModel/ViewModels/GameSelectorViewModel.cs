using FModel.Extensions;
using FModel.Framework;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace FModel.ViewModels
{
    public class GameSelectorViewModel : ViewModel
    {
        public class DetectedGame
        {
            public string GameName { get; set; }
            public string GameDirectory { get; set; }
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
            AutoDetectedGames = new ReadOnlyObservableCollection<DetectedGame>(_autoDetectedGames);

            if (AutoDetectedGames.FirstOrDefault(x => x.GameDirectory == gameDirectory) is { } detectedGame)
                SelectedDetectedGame = detectedGame;
            else if (!string.IsNullOrEmpty(gameDirectory))
                AddUnknownGame(gameDirectory);
            else
                SelectedDetectedGame = AutoDetectedGames.FirstOrDefault();
        }

        public void AddUnknownGame(string gameDirectory)
        {
            _autoDetectedGames.Add(new DetectedGame {GameName = gameDirectory.SubstringAfterLast('\\'), GameDirectory = gameDirectory});
            SelectedDetectedGame = AutoDetectedGames.Last();
        }

        private IEnumerable<DetectedGame> EnumerateDetectedGames()
        {
            yield return GetUnrealEngineGame("Fortnite", "\\FortniteGame\\Content\\Paks");
            yield return new DetectedGame {GameName = "Fortnite [LIVE]", GameDirectory = Constants._FN_LIVE_TRIGGER};
            yield return GetUnrealEngineGame("Pewee", "\\RogueCompany\\Content\\Paks");
            yield return GetUnrealEngineGame("Rosemallow", "\\Indiana\\Content\\Paks");
            yield return GetUnrealEngineGame("Catnip", "\\OakGame\\Content\\Paks");
            yield return GetUnrealEngineGame("AzaleaAlpha", "\\Prospect\\Content\\Paks");
            yield return GetUnrealEngineGame("WorldExplorersLive", "\\WorldExplorers\\Content\\Paks");
            yield return GetUnrealEngineGame("Newt", "\\g3\\Content\\Paks");
            yield return GetUnrealEngineGame("shoebill", "\\SwGame\\Content\\Paks");
            yield return GetUnrealEngineGame("Snoek", "\\StateOfDecay2\\Content\\Paks");
            yield return GetUnrealEngineGame("a99769d95d8f400baad1f67ab5dfe508", "\\Core\\Platform\\Content\\Paks");
            yield return GetRiotGame("VALORANT", "ShooterGame\\Content\\Paks");
            // yield return new DetectedGame {GameName = "Valorant [LIVE]", GameDirectory = Constants._VAL_LIVE_TRIGGER};
            yield return GetMojangGame("MinecraftDungeons", "\\dungeons\\dungeons\\Dungeons\\Content\\Paks");
        }

        private LauncherInstalled _launcherInstalled;
        private DetectedGame GetUnrealEngineGame(string gameName, string pakDirectory)
        {
            _launcherInstalled ??= GetDrivedLauncherInstalls<LauncherInstalled>("ProgramData\\Epic\\UnrealEngineLauncher\\LauncherInstalled.dat");
            if (_launcherInstalled?.InstallationList != null)
            {
                foreach (var installationList in _launcherInstalled.InstallationList)
                {
                    if (installationList.AppName.Equals(gameName, StringComparison.OrdinalIgnoreCase))
                        return new DetectedGame {GameName = installationList.AppName, GameDirectory = $"{installationList.InstallLocation}{pakDirectory}"};
                }
            }

            Log.Warning("Could not find {GameName} in LauncherInstalled.dat", gameName);
            return null;
        }

        private RiotClientInstalls _riotClientInstalls;
        private DetectedGame GetRiotGame(string gameName, string pakDirectory)
        {
            _riotClientInstalls ??= GetDrivedLauncherInstalls<RiotClientInstalls>("ProgramData\\Riot Games\\RiotClientInstalls.json");
            if (_riotClientInstalls is {AssociatedClient: { }})
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
            _launcherSettings ??= GetDataLauncherInstalls<LauncherSettings>("\\.minecraft_dungeons\\launcher_settings.json");
            if (_launcherSettings is {ProductLibraryDir: { }})
                return new DetectedGame {GameName = gameName, GameDirectory = $"{_launcherSettings.ProductLibraryDir}{pakDirectory}"};

            Log.Warning("Could not find {GameName} in launcher_settings.json", gameName);
            return null;
        }

        private T GetDrivedLauncherInstalls<T>(string jsonFile)
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
    }
}