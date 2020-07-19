using PakReader.Pak;
using PakReader.Parsers.Objects;
using System;
using System.Collections.Generic;
using System.Windows;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;

namespace FModel
{
    static class Globals
    {
        /// <summary>
        /// string is the pak file name
        /// PakFileReader is the reader where you can grab the FPakEntries, MountPoint and more
        /// </summary>
        public static readonly Dictionary<string, PakFileReader> CachedPakFiles = new Dictionary<string, PakFileReader>();
        public static readonly Dictionary<uint, string> ValorantWemToName = new Dictionary<uint, string>
        {
            { 1055663626, "Play_Armor_ShieldBreak" },
            { 1068023171, "Play_Armor_Regen_Start_Heal_Self" },
            { 325810485, "Play_Buff_Nearsight_Base_End" },
            { 653991800, "Play_Buff_Nearsight_Base_Start" },
            { 119316015, "Play_Buff_RevealLocation_Base_Start" },
            { 1012366820, "Play_Mvt_Breach_S0_Arm_Inspect_Med_1P" },
            { 199696126, "Play_Breach_Common_Concuss" },
            { 673248071, "Play_Breach_Common_Concuss_Sweep" },
            { 101067380, "Play_Pandemic_CharacterSelect_Arrive" },
            { 1022095199, "Play_Phoenix_CS_Intro_V1_1033" },
            { 124189940, "Play_Vampire_CharacterSelect_Arrive_01" },
            { 131223731, "Play_Clay_CharacterSelect_Arrive_Abilities" },
            { 156379850, "Play_Hunter_CharacterSelect_Idle_01" },
            { 198127812, "Play_Vampire_CharacterSelect_Idle_Foley" },
            { 216172758, "Play_Wushu_CharacterSelect_Arrive" },
            { 228679376, "Play_Clay_CharacterSelect_Arrive_Explosion2" },
            { 267243372, "Play_Wraith_CharacterSelect_Arrive" },
            { 280046394, "Play_Thorne_CharacterSelect_Idle" },
            { 322197515, "Play_Gumshoe_CharacterSelect_Fidget" },
            { 325029817, "Play_Breach_character_Select_Idle_Loop" },
            { 366056075, "Play_Vampire_CharacterSelect_Idle_Foley" },
            { 488668639, "Play_Clay_CharacterSelect_Arrive_Foley" },
            { 491211431, "Play_Vampire_CharacterSelect_Idle_Abilities" },
            { 523634569, "Play_Gumshoe_CharacterSelect_Arrive" },
            { 641708471, "Play_Hunter_CharacterSelect_Arrive_01" },
            { 679600709, "Play_Breach_CharacterSelect_Arrive" },
            { 849624945, "Play_Sarge_CharacterSelect_Idle_01" },
            { 852951369, "Play_Clay_CharacterSelect_Idle_01" },
            { 893700681, "Play_Thorne_CharacterSelect_Arrive" },
            { 902446960, "Play_Sarge_CharaterSelect_Arrive_01" },
            { 932757179, "Play_Phoenix_CS_Idle_V1" },
            { 975327491, "Play_Clay_CharacterSelect_Arrive_Explosion1" },
            { 746127837, "Play_Ping_Player_Move_On" },
            { 329781885, "Play_Music_Match_End" },
            { 584532814, "Play_Music_OutOfGame" },
            { 146784229, "Play_NPE_M4_Mission6_BombIntro" },
            { 870724556, "Play_music_KillBanner6" },
            { 277987135, "Play_music_KillBanner5" },
            { 69962686, "Play_music_KillBanner4" },
            { 793984097, "Play_music_KillBanner3" },
            { 154454498, "Play_music_KillBanner2" },
            { 896617886, "Play_music_KillBanner1" },
            { 307548551, "Play_MVT_Ascender_Detach" },
            { 952007697, "Play_MVT_Ascender_Attach" },
            { 448253994, "Play_UI_KillBanner_Sovereign_5" },
            { 315838728, "Play_UI_KillBanner_Sovereign_4" },
            { 1064796764, "Play_UI_KillBanner_Sovereign_3" },
            { 15228101, "Play_UI_KillBanner_Sovereign_2" },
            { 703936068, "Play_UI_KillBanner_Sovereign_1" },
            { 991898391, "Play_UI_KillBanner_Sovereign_Appear" },
            { 16798053, "Play_UI_KillBanner_HypeBeast_5" },
            { 771344507, "Play_UI_KillBanner_HypeBeast_4" },
            { 541156431, "Play_UI_KillBanner_HypeBeast_3" },
            { 688688158, "Play_UI_KillBanner_HypeBeast_2" },
            { 848670920, "Play_UI_KillBanner_HypeBeast_1" },
            { 23136954, "Play_UI_KillBanner_Dragon_5" },
            { 880730930, "Play_UI_KillBanner_Dragon_4" },
            { 597747382, "Play_UI_KillBanner_Dragon_3" },
            { 26548691, "Play_UI_KillBanner_Dragon_2" },
            { 555446229, "Play_UI_KillBanner_Dragon_1" },
            { 365155667, "Play_UI_KillBanner_Dragon_Appear" },
            { 435978362, "Play_UI_KillBanner_Dragon_Transition" },
            { 69566023, "Play_UI_KillBanner_Base_6" },
            { 1035341202, "Play_UI_KillBanner_Base_5" },
            { 274787234, "Play_UI_KillBanner_Base_4" },
            { 869943120, "Play_UI_KillBanner_Base_3" },
            { 412643527, "Play_UI_KillBanner_Base_2" },
            { 348954400, "Play_UI_KillBanner_Base_1" },
            { 1005067658, "Play_Rad_Barrier_Amb_Loop" },
            { 942997956, "Play_Rad_Barrier_Dissolve" },
            { 98773689, "Play_Rad_Barrier_Flash" },
            { 731728099, "Play_Wp_Finisher_Sovereign_A_Test" },
            { 177545810, "Play_Wp_Finisher_HypeBeast_1p" },
            { 526065652, "Play_Wp_Finisher_Dragon" },
            { 335415490, "Play_Wp_Shotgun_Pump_Equip_Forestock_Back_3P" },
            { 349926119, "Play_Wp_Shotgun_Pump_Equip_Forestock_Fwd_3P" },
            { 140647853, "Play_Wp_Shotgun_Pump_Equip_Mvt_A_1P" },
            { 781016999, "Play_Wp_Shotgun_Pump_Equip_Mvt_B_1P" },
            { 379491061, "Play_Wp_Shotgun_Pump_Inspect_Forestock_Back_1P" },
            { 236670956, "Play_Wp_Shotgun_Pump_Inspect_Forestock_Fwd_1P" },
            { 883364050, "Play_Wp_Shotgun_Pump_Inspect_Mvt_A_1P" },
            { 284594592, "Play_Wp_Shotgun_Pump_Inspect_Mvt_B_1P" },
            { 890502400, "Play_Wp_Shotgun_Pump_Inspect_Mvt_C_1P" },
            { 851080892, "Play_Wp_Shotgun_Pump_Inspect_Mvt_D_1P" },
            { 424465925, "Play_Wp_Shotgun_Pump_Inspect_Mvt_E_1P" },
            { 1032013312, "Play_Wp_Shotgun_Pump_Inspect_Mvt_F_1P" },
            { 27241452, "Play_Wp_Shotgun_Pump_Reload_CarrierClick_1P" },
            { 1034664301, "Play_Wp_Shotgun_Pump_Reload_End_Mvt_1P" },
            { 234526718, "Play_Wp_Shotgun_Pump_Reload_Grab_1P" },
            { 218060133, "Play_Wp_Shotgun_Pump_Reload_Mvt_A_1P" },
            { 490257873, "Play_Wp_Shotgun_Pump_Reload_Mvt_B_1P" },
            { 921435741, "Play_Wp_Shotgun_Pump_Reload_ShellInsrt_1P" }
        };
        public static readonly Notifier gNotifier = new Notifier(cfg =>
        {
            cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(TimeSpan.FromSeconds(7), MaximumNotificationCount.FromCount(15));
            cfg.PositionProvider = new PrimaryScreenPositionProvider(Corner.BottomRight, 5, 5);
            cfg.Dispatcher = Application.Current.Dispatcher;
        });
        public static bool bSearch = false; // trigger the event to select a file thank to the search window
        public static string sSearch = string.Empty; // this will be the file name triggered
        public static FGame Game = new FGame(EGame.Unknown, EPakVersion.LATEST);
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

        public FGame(EGame game, EPakVersion version)
        {
            ActualGame = game;
            Version = version;
        }

        public string GetName()
        {
            return ActualGame switch
            {
                EGame.Fortnite => "Fortnite",
                EGame.Valorant => "Valorant",
                EGame.DeadByDaylight => "Dead By Daylight",
                EGame.Borderlands3 => "Borderlands 3",
                EGame.MinecraftDungeons => "Minecraft Dungeons",
                EGame.BattleBreakers => "Battle Breakers",
                EGame.Spellbreak => "Spellbreak",
                EGame.StateOfDecay2 => "State of Decay 2",
                EGame.TheCycleEA => "The Cycle (Early Access)",
                EGame.Unknown => "Unknown",
                _ => "Unknown",
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
