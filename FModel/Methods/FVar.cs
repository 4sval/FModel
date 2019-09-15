using PakReader;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace FModel.Methods
{
    static class FWindow
    {
        public static MainWindow FMain = null;
        public static string FCurrentPAK { get; set; }
        public static string FCurrentAssetParentPath { get; set; }
    }

    #region PAKs
    static class PAKEntries
    {
        public static List<PAKInfosEntry> PAKEntriesList { get; set; }
        public static Dictionary<string, FPakEntry[]> PAKToDisplay { get; set; }
    }
    public struct PAKInfosEntry : IEquatable<PAKInfosEntry>
    {
        public string ThePAKPath { get; set; }
        public string ThePAKGuid { get; set; }
        public bool bTheDynamicPAK { get; set; }

        internal PAKInfosEntry(string MyPAKPath, string MyPAKGuid, bool isDynamicPAK)
        {
            ThePAKPath = MyPAKPath;
            ThePAKGuid = MyPAKGuid;
            bTheDynamicPAK = isDynamicPAK;
        }

        bool IEquatable<PAKInfosEntry>.Equals(PAKInfosEntry other)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    #region AESs
    static class AESEntries
    {
        public static List<AESInfosEntry> AESEntriesList { get; set; }
    }
    public struct AESInfosEntry : IEquatable<AESInfosEntry>
    {
        public string ThePAKName { get; set; }
        public string ThePAKKey { get; set; }

        internal AESInfosEntry(string MyPAKName, string MyPAKKey)
        {
            ThePAKName = MyPAKName;
            ThePAKKey = MyPAKKey;
        }

        bool IEquatable<AESInfosEntry>.Equals(AESInfosEntry other)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    #region BACKUPS
    public struct BackupInfosEntry : IEquatable<BackupInfosEntry>
    {
        public string TheFileName { get; set; }
        public string TheFileDownload { get; set; }

        internal BackupInfosEntry(string MyFileName, string MyFileDownlaod)
        {
            TheFileName = MyFileName;
            TheFileDownload = MyFileDownlaod;
        }

        bool IEquatable<BackupInfosEntry>.Equals(BackupInfosEntry other)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    #region ASSETS
    static class AssetEntries
    {
        public static Dictionary<FPakEntry, PakReader.PakReader> AssetEntriesDict { get; set; }
        public static ConcurrentDictionary<string, PakPackage> AssetIndexes { get; set; }
    }
    public struct AssetInfosEntry : IEquatable<AssetInfosEntry>
    {
        public string TheAssetPath { get; set; }
        public FPakEntry TheAssetEntry { get; set; }
        public PakReader.PakReader TheAssetPakReader { get; set; }

        internal AssetInfosEntry(string MyAssetPath, FPakEntry MyAssetEntry, PakReader.PakReader MyAssetPakReader)
        {
            TheAssetPath = MyAssetPath;
            TheAssetEntry = MyAssetEntry;
            TheAssetPakReader = MyAssetPakReader;
        }

        bool IEquatable<AssetInfosEntry>.Equals(AssetInfosEntry other)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    static class DLLImport
    {
        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int description, int reservedValue);
        public static bool IsInternetAvailable()
        {
            return InternetGetConnectedState(description: out _, reservedValue: 0);
        }
    }
}
