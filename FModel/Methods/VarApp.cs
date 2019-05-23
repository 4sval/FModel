using System.Collections.Generic;

namespace FModel
{
    class ThePak
    {
        public static string CurrentUsedPak { get; set; }
        public static string CurrentUsedPakGuid { get; set; }
        public static string CurrentUsedItem { get; set; }

        public static Dictionary<string, string> PaksMountPoint { get; set; }
        public static Dictionary<string, string> AllpaksDictionary { get; set; }
    }

    class App
    {
        public static string DefaultOutputPath { get; set; }
    }

    class Checking
    {
        public static bool WasFeatured { get; set; }
        public static int YAfterLoop { get; set; }

        public static bool UmWorking { get; set; }
    }
}
