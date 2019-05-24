using System.Collections.Generic;
using System.IO;

namespace FModel
{
    class ThePak
    {
        public static string CurrentUsedPak { get; set; }
        public static string CurrentUsedPakGuid { get; set; }
        public static string CurrentUsedItem { get; set; }

        public static Dictionary<string, string> PaksMountPoint { get; set; }
        public static Dictionary<string, string> AllpaksDictionary { get; set; }

        public static string ReadPakGuid(string pakPath)
        {
            using (BinaryReader reader = new BinaryReader(File.Open(pakPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                reader.BaseStream.Seek(reader.BaseStream.Length - 61 - 160, SeekOrigin.Begin);
                uint g1 = reader.ReadUInt32();
                reader.BaseStream.Seek(reader.BaseStream.Length - 57 - 160, SeekOrigin.Begin);
                uint g2 = reader.ReadUInt32();
                reader.BaseStream.Seek(reader.BaseStream.Length - 53 - 160, SeekOrigin.Begin);
                uint g3 = reader.ReadUInt32();
                reader.BaseStream.Seek(reader.BaseStream.Length - 49 - 160, SeekOrigin.Begin);
                uint g4 = reader.ReadUInt32();

                var guid = g1 + "-" + g2 + "-" + g3 + "-" + g4;
                return guid;
            }
        }
        public static string ReadPakVersion(string pakPath)
        {
            using (BinaryReader reader = new BinaryReader(File.Open(pakPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                reader.BaseStream.Seek(reader.BaseStream.Length - 40 - 160, SeekOrigin.Begin);
                uint guid = reader.ReadUInt32();

                return guid.ToString();
            }
        }
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
