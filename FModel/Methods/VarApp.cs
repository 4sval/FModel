using System;
using System.Collections.Generic;
using System.IO;

namespace FModel
{
    static class ThePak
    {
        public static string CurrentUsedPak { get; set; }
        public static string CurrentUsedPakGuid { get; set; }
        public static string CurrentUsedItem { get; set; }

        public static Dictionary<string, string> PaksMountPoint { get; set; }
        public static Dictionary<string, string> AllpaksDictionary { get; set; }

        /// <summary>
        /// read the GUID of a the param, it's basically just reading some bytes at the end of a pak file, but it's useful to tell if the pak is dynamically encrypted
        /// "0-0-0-0" for normal pak
        /// "123456789-9876543210-741085209630-015975306482" for dynamic pak (these numbers are an example)
        /// </summary>
        /// <param name="pakPath"> path to the pak file to get its GUID </param>
        /// <returns> the guid </returns>
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

                string guid = g1 + "-" + g2 + "-" + g3 + "-" + g4;
                return guid;
            }
        }

        /// <summary>
        /// read the pak version (v8 28.05.2019), it's basically just reading one byte at the end of a pak file
        /// </summary>
        /// <param name="pakPath"> path to the pak file to get its version </param>
        /// <returns> the version </returns>
        public static string ReadPakVersion(string pakPath)
        {
            using (BinaryReader reader = new BinaryReader(File.Open(pakPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                reader.BaseStream.Seek(reader.BaseStream.Length - 40 - 160, SeekOrigin.Begin);
                uint version = reader.ReadUInt32();

                return version.ToString();
            }
        }
    }

    static class App
    {
        public static string DefaultOutputPath { get; set; }
    }

    static class Checking
    {
        public static bool WasFeatured { get; set; }
        public static bool UmWorking { get; set; }
    }
    public struct BundleInfoEntry: IEquatable<BundleInfoEntry>
    {
        internal BundleInfoEntry(string QuestDescription, long QuestCount, string RewardId, string RewardQuantity)
        {
            questDescr = QuestDescription;
            questCount = QuestCount;
            rewardItemId = RewardId;
            rewardItemQuantity = RewardQuantity;
        }
        public string questDescr { get; set; }
        public long questCount { get; set; }
        public string rewardItemId { get; set; }
        public string rewardItemQuantity { get; set; }

        bool IEquatable<BundleInfoEntry>.Equals(BundleInfoEntry other)
        {
            throw new NotImplementedException();
        }
    }

    public struct AESEntry : IEquatable<AESEntry>
    {
        internal AESEntry(string myKey)
        {
            theKey = myKey;
        }
        public string theKey { get; set; }

        bool IEquatable<AESEntry>.Equals(AESEntry other)
        {
            throw new NotImplementedException();
        }
    }
}
