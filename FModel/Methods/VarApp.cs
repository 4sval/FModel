using System.Collections.Generic;

namespace FModel
{
    class ThePak
    {
        public static string CurrentUsedPak;
        public static string CurrentUsedPakGuid;
        public static string CurrentUsedItem;

        public static Dictionary<string, string> PaksMountPoint;
        public static Dictionary<string, string> AllpaksDictionary;
    }

    class App
    {
        public static string DefaultOutputPath;
    }

    class Checking
    {
        public static bool WasFeatured;
        public static int YAfterLoop;

        public static bool UmWorking;
    }
}
