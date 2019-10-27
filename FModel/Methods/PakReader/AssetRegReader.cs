using System;
using System.Collections.Generic;
using System.IO;
using static PakReader.AssetReader;

namespace PakReader
{
    public sealed class AssetRegistryFile
    {
        public readonly FAssetData[] PreallocatedAssetDataBuffer;
        public readonly FDependsNode[] PreallocatedDependsNodeDataBuffer;
        public readonly FAssetPackageData[] PreallocatedPackageDataBuffer;

        public AssetRegistryFile(string path) : this(File.OpenRead(path)) { }

        public AssetRegistryFile(Stream stream) : this(new BinaryReader(stream)) { }

        public AssetRegistryFile(BinaryReader reader)
        {
            var Version = FAssetRegistryVersion.DeserializeVersion(reader);
            if (Version < FAssetRegistryVersion.Type.AddedCookedMD5Hash)
            {
                throw new FileLoadException("Cannot read states before this version");
            }

            var nameReader = new FNameTableArchiveReader(reader);

            var LocalNumAssets = reader.ReadInt32();
            PreallocatedAssetDataBuffer = new FAssetData[LocalNumAssets];

            for (int i = 0; i < LocalNumAssets; i++)
            {
                PreallocatedAssetDataBuffer[i] = new FAssetData(nameReader);
            }

            var LocalNumDependsNodes = reader.ReadInt32();
            PreallocatedDependsNodeDataBuffer = new FDependsNode[LocalNumDependsNodes];
            for (int i = 0; i < LocalNumDependsNodes; i++)
            {
                PreallocatedDependsNodeDataBuffer[i] = new FDependsNode();
            }

            SortedList<EAssetRegistryDependencyType, int> DepCounts = new SortedList<EAssetRegistryDependencyType, int>();

            for (int i = 0; i < LocalNumDependsNodes; i++)
            {
                FAssetIdentifier AssetIdentifier = new FAssetIdentifier(nameReader);

                DepCounts[EAssetRegistryDependencyType.Hard] = reader.ReadInt32();
                DepCounts[EAssetRegistryDependencyType.Soft] = reader.ReadInt32();
                DepCounts[EAssetRegistryDependencyType.SearchableName] = reader.ReadInt32();
                DepCounts[EAssetRegistryDependencyType.SoftManage] = reader.ReadInt32();
                DepCounts[EAssetRegistryDependencyType.HardManage] = Version < FAssetRegistryVersion.Type.AddedHardManage ? 0 : reader.ReadInt32();
                DepCounts[0] = reader.ReadInt32();

                PreallocatedDependsNodeDataBuffer[i].Identifier = AssetIdentifier;
                PreallocatedDependsNodeDataBuffer[i].Reserve(DepCounts);

                SerializeDependencyType(EAssetRegistryDependencyType.Hard, i);
                SerializeDependencyType(EAssetRegistryDependencyType.Soft, i);
                SerializeDependencyType(EAssetRegistryDependencyType.SearchableName, i);
                SerializeDependencyType(EAssetRegistryDependencyType.SoftManage, i);
                SerializeDependencyType(EAssetRegistryDependencyType.HardManage, i);
                SerializeDependencyType(0, i);
            }

            void SerializeDependencyType(EAssetRegistryDependencyType InDependencyType, int AssetIndex)
            {
                for (int i = 0; i < DepCounts[InDependencyType]; ++i)
                {
                    int index = reader.ReadInt32();

                    if (index < 0 || index >= LocalNumDependsNodes)
                    {
                        throw new FileLoadException("could you please serialize your assetregistry correctly");
                    }
                    PreallocatedDependsNodeDataBuffer[AssetIndex].Add(index, InDependencyType);
                }
            }

            int LocalNumPackageData = reader.ReadInt32();
            PreallocatedPackageDataBuffer = new FAssetPackageData[LocalNumPackageData];

            var bSerializeHash = Version < FAssetRegistryVersion.Type.AddedCookedMD5Hash;
            for (int i = 0; i < LocalNumPackageData; i++)
            {
                PreallocatedPackageDataBuffer[i] = new FAssetPackageData(nameReader, bSerializeHash);
            }
        }
    }

    struct FAssetRegistryVersion
    {
        public enum Type
        {
            PreVersioning = 0,      // From before file versioning was implemented
            HardSoftDependencies,   // The first version of the runtime asset registry to include file versioning.
            AddAssetRegistryState,  // Added FAssetRegistryState and support for piecemeal serialization
            ChangedAssetData,       // AssetData serialization format changed, versions before this are not readable
            RemovedMD5Hash,         // Removed MD5 hash from package data
            AddedHardManage,        // Added hard/soft manage references
            AddedCookedMD5Hash,     // Added MD5 hash of cooked package to package data

            // -----<new versions can be added above this line>-------------------------------------------------
            VersionPlusOne,
            LatestVersion = VersionPlusOne - 1
        }

        static readonly FGuid GUID = new FGuid() { A = 0x717F9EE7, B = 0xE9B0493A, C = 0x88B39132, D = 0x1B388107 };

        public static Type DeserializeVersion(BinaryReader reader)
        {
            if (GUID == new FGuid(reader))
            {
                return (Type)reader.ReadInt32();
            }
            return default;
        }
    }

    enum EAssetRegistryDependencyType
    {
        // Dependencies which don't need to be loaded for the object to be used (i.e. soft object paths)
        Soft = 0x01,

        // Dependencies which are required for correct usage of the source asset, and must be loaded at the same time
        Hard = 0x02,

        // References to specific SearchableNames inside a package
        SearchableName = 0x04,

        // Indirect management references, these are set through recursion for Primary Assets that manage packages or other primary assets
        SoftManage = 0x08,

        // Reference that says one object directly manages another object, set when Primary Assets manage things explicitly
        HardManage = 0x10,
    }

    public struct FAssetData
    {
        public string ObjectPath, PackageName, PackagePath, AssetName, AssetClass;
        public FAssetDataTagMapSharedView TagsAndValues;
        public int[] ChunkIDs;
        public uint PackageFlags;

        internal FAssetData(FNameTableArchiveReader reader)
        {
            ObjectPath = reader.ReadFName();
            PackagePath = reader.ReadFName();
            AssetClass = reader.ReadFName();

            PackageName = reader.ReadFName();
            AssetName = reader.ReadFName();

            TagsAndValues = new FAssetDataTagMapSharedView(reader);
            ChunkIDs = reader.Reader.ReadTArray(() => reader.Reader.ReadInt32());
            PackageFlags = reader.Reader.ReadUInt32();
        }
    }

    public class FDependsNode
    {
        public FAssetIdentifier Identifier { get; set; }
        public List<int> HardDependencies;
        public List<int> SoftDependencies;
        public List<int> NameDependencies;
        public List<int> SoftManageDependencies;
        public List<int> HardManageDependencies;
        public List<int> Referencers;

        internal void Reserve(SortedList<EAssetRegistryDependencyType, int> list)
        {
            foreach(var kv in list)
            {
                switch (kv.Key)
                {
                    case EAssetRegistryDependencyType.Soft:
                        SoftDependencies = new List<int>(kv.Value);
                        break;
                    case EAssetRegistryDependencyType.Hard:
                        HardDependencies = new List<int>(kv.Value);
                        break;
                    case EAssetRegistryDependencyType.SearchableName:
                        NameDependencies = new List<int>(kv.Value);
                        break;
                    case EAssetRegistryDependencyType.SoftManage:
                        SoftManageDependencies = new List<int>(kv.Value);
                        break;
                    case EAssetRegistryDependencyType.HardManage:
                        HardManageDependencies = new List<int>(kv.Value);
                        break;
                    case 0:
                        Referencers = new List<int>(kv.Value);
                        break;
                }
            }
        }

        internal void Add(int node, EAssetRegistryDependencyType type)
        {
            switch (type)
            {
                case EAssetRegistryDependencyType.Soft:
                    SoftDependencies.Add(node);
                    break;
                case EAssetRegistryDependencyType.Hard:
                    HardDependencies.Add(node);
                    break;
                case EAssetRegistryDependencyType.SearchableName:
                    NameDependencies.Add(node);
                    break;
                case EAssetRegistryDependencyType.SoftManage:
                    SoftManageDependencies.Add(node);
                    break;
                case EAssetRegistryDependencyType.HardManage:
                    HardManageDependencies.Add(node);
                    break;
                case 0:
                    Referencers.Add(node);
                    break;
            }
        }
    }

    public class FAssetPackageData
    {
        public readonly string PackageName;
        public readonly long DiskSize;
        public readonly FGuid PackageGuid;
        public readonly FMD5Hash CookedHash;

        internal FAssetPackageData(FNameTableArchiveReader reader, bool bSerializeHash)
        {
            PackageName = reader.ReadFName();
            DiskSize = reader.Reader.ReadInt64();
            PackageGuid = new FGuid(reader.Reader);
            if (bSerializeHash)
                CookedHash = new FMD5Hash(reader.Reader);
        }
    }

    public struct FMD5Hash
    {
        public byte[] Hash;

        public FMD5Hash(BinaryReader reader)
        {
            Hash = reader.ReadUInt32() != 0 ? reader.ReadBytes(16) : null;
        }
    }

    public class FAssetIdentifier
    {
        public readonly string PackageName;
        public readonly string PrimaryAssetType;
        public readonly string ObjectName;
        public readonly string ValueName;

        internal FAssetIdentifier(FNameTableArchiveReader reader)
        {
            byte FieldBits = reader.Reader.ReadByte();

            if ((FieldBits & (1 << 0)) != 0)
            {
                PackageName = reader.ReadFName();
            }
            if ((FieldBits & (1 << 1)) != 0)
            {
                PrimaryAssetType = reader.ReadFName();
            }
            if ((FieldBits & (1 << 2)) != 0)
            {
                ObjectName = reader.ReadFName();
            }
            if ((FieldBits & (1 << 3)) != 0)
            {
                ValueName = reader.ReadFName();
            }
        }
    }

    public class FAssetDataTagMapSharedView
    {
        public readonly SortedList<string, string> Map = new SortedList<string, string>();

        internal FAssetDataTagMapSharedView(FNameTableArchiveReader reader)
        {
            int l = reader.Reader.ReadInt32();
            for(int i = 0; i < l; i++)
            {
                Map.Add(reader.ReadFName(), reader.Reader.ReadString(-1));
            }
        }
    }

    class FNameTableArchiveReader
    {
        readonly FNameEntrySerialized[] NameMap;
        public readonly BinaryReader Reader;

        public FNameTableArchiveReader(BinaryReader reader)
        {
            Reader = reader;
            long NameOffset = reader.ReadInt64();
            if (NameOffset > reader.BaseStream.Length)
            {
                throw new FileLoadException("NameOffset is larger than original file size");
            }

            if (NameOffset <= 0)
            {
                throw new FileLoadException("NameOffset is not positive");
            }

            long OriginalOffset = reader.BaseStream.Position;
            reader.BaseStream.Seek(NameOffset, SeekOrigin.Begin);

            NameMap = reader.ReadTArray(() => new FNameEntrySerialized(reader));

            reader.BaseStream.Seek(OriginalOffset, SeekOrigin.Begin);
        }

        public string ReadFName() => read_fname(Reader, NameMap);
    }
}
