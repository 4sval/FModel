using System;
using System.Collections.Generic;
using System.Windows;
using FModel.PakReader.IO;
using FModel.PakReader.Pak;
using FModel.PakReader.Parsers.Objects;
using FModel.Properties;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;
using UsmapNET.Classes;

namespace FModel
{
    static class Globals
    {
        /// <summary>
        /// string is the pak file name
        /// PakFileReader is the reader where you can grab the FPakEntries, MountPoint and more
        /// </summary>
        public static readonly Dictionary<string, PakFileReader> CachedPakFiles = new Dictionary<string, PakFileReader>();
        public static readonly Dictionary<string, FFileIoStoreReader> CachedIoStores = new Dictionary<string, FFileIoStoreReader>();
        public static readonly Dictionary<string, FUnversionedType> CachedSchemas = new Dictionary<string, FUnversionedType>();
        public static Usmap Usmap = null;
        public static FIoGlobalData GlobalData = null;
        public static readonly Notifier gNotifier = new Notifier(cfg =>
        {
            cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(TimeSpan.FromSeconds(7), MaximumNotificationCount.FromCount(15));
            cfg.PositionProvider = new PrimaryScreenPositionProvider(Corner.BottomRight, 5, 5);
            cfg.Dispatcher = Application.Current.Dispatcher;
        });
        public static bool bSearch = false; // trigger the event to select a file thank to the search window
        public static string sSearch = string.Empty; // this will be the file name triggered
        public static FGame Game = new FGame(EGame.Unknown, EPakVersion.LATEST, 0);
        public const EFModel Build =
#if RELEASE
            EFModel.Release;
#elif DEBUG
            EFModel.Debug;
#else
            EFModel.Unknown;
#endif
    }

    public class FGame
    {
        public EGame ActualGame;
        public EPakVersion Version;
        public int SubVersion;

        public FGame(EGame game, EPakVersion version, int subVersion)
        {
            ActualGame = game;
            Version = version;
            SubVersion = subVersion;
        }

        public string GetName()
        {
            return ActualGame switch
            {
                EGame.Fortnite => Resources.GameName_Fortnite,
                EGame.Valorant => Resources.GameName_Valorant,
                EGame.DeadByDaylight => Resources.GameName_DeadByDaylight,
                EGame.Borderlands3 => Resources.GameName_Borderlands3,
                EGame.MinecraftDungeons => Resources.GameName_MinecraftDungeons,
                EGame.BattleBreakers => Resources.GameName_BattleBreakers,
                EGame.Spellbreak => Resources.GameName_Spellbreak,
                EGame.StateOfDecay2 => Resources.GameName_StateofDecay2,
                EGame.TheCycle => Resources.GameName_TheCycle,
                EGame.TheOuterWorlds => Resources.GameName_TheOuterWorlds,
                EGame.RogueCompany => Resources.GameName_RogueCompany,
                EGame.Unknown => "Unknown",
                _ => "Unknown"
            };
        }
    }

    static class FColors
    {
        public const string Red = "#E06C75";
        public const string Orange = "#D19A66";
        public const string Yellow = "#E5C07B";
        public const string Purple = "#C678DD";
        public const string Blue = "#61AFEF";
        public const string Discord = "#8B9BD4";
        public const string Green = "#98C379";
        public const string LightGray = "#BBBBBB";
        public const string DarkGray = "#9B9B9B";
        public const string White = "#EFEFEF";
    }
}
