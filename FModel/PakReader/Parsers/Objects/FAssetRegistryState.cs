using System.Collections.Generic;
using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FAssetRegistryState : IUStruct
    {
        /** When loading a registry from disk, we can allocate all the FAssetData objects in one chunk, to save on 10s of thousands of heap allocations */
        public readonly FAssetData[] PreallocatedAssetDataBuffers;
        public readonly FDependsNode[] PreallocatedDependsNodeDataBuffers;
        public readonly FAssetPackageData[] PreallocatedPackageDataBuffers;

        internal FAssetRegistryState(Stream stream) : this(new BinaryReader(stream)) { }
        internal FAssetRegistryState(BinaryReader reader)
        {
            var Version = FAssetRegistryVersion.DeserializeVersion(reader);
            if (Version < FAssetRegistryVersion.Type.AddedCookedMD5Hash)
            {
                throw new FileLoadException("Cannot read states before this version");
            }

            var nameReader = new FNameTableArchiveReader(reader);

            var LocalNumAssets = reader.ReadInt32();
            PreallocatedAssetDataBuffers = new FAssetData[LocalNumAssets];

            for (int i = 0; i < LocalNumAssets; i++)
            {
                PreallocatedAssetDataBuffers[i] = new FAssetData(nameReader);
            }

            var LocalNumDependsNodes = reader.ReadInt32();
            PreallocatedDependsNodeDataBuffers = new FDependsNode[LocalNumDependsNodes];
            for (int i = 0; i < LocalNumDependsNodes; i++)
            {
                PreallocatedDependsNodeDataBuffers[i] = new FDependsNode();
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

                PreallocatedDependsNodeDataBuffers[i].Identifier = AssetIdentifier;
                PreallocatedDependsNodeDataBuffers[i].Reserve(DepCounts);

                for (int x = 0; x < DepCounts[EAssetRegistryDependencyType.Hard]; ++x)
                {
                    int index = reader.ReadInt32();

                    if (index < 0 || index >= LocalNumDependsNodes)
                        throw new FileLoadException("could you please serialize your assetregistry correctly");

                    PreallocatedDependsNodeDataBuffers[i].Add(index, EAssetRegistryDependencyType.Hard);
                }
                for (int x = 0; x < DepCounts[EAssetRegistryDependencyType.Soft]; ++x)
                {
                    int index = reader.ReadInt32();

                    if (index < 0 || index >= LocalNumDependsNodes)
                        throw new FileLoadException("could you please serialize your assetregistry correctly");

                    PreallocatedDependsNodeDataBuffers[i].Add(index, EAssetRegistryDependencyType.Soft);
                }
                for (int x = 0; x < DepCounts[EAssetRegistryDependencyType.SearchableName]; ++x)
                {
                    int index = reader.ReadInt32();

                    if (index < 0 || index >= LocalNumDependsNodes)
                        throw new FileLoadException("could you please serialize your assetregistry correctly");

                    PreallocatedDependsNodeDataBuffers[i].Add(index, EAssetRegistryDependencyType.SearchableName);
                }
                for (int x = 0; x < DepCounts[EAssetRegistryDependencyType.SoftManage]; ++x)
                {
                    int index = reader.ReadInt32();

                    if (index < 0 || index >= LocalNumDependsNodes)
                        throw new FileLoadException("could you please serialize your assetregistry correctly");

                    PreallocatedDependsNodeDataBuffers[i].Add(index, EAssetRegistryDependencyType.SoftManage);
                }
                for (int x = 0; x < DepCounts[EAssetRegistryDependencyType.HardManage]; ++x)
                {
                    int index = reader.ReadInt32();

                    if (index < 0 || index >= LocalNumDependsNodes)
                        throw new FileLoadException("could you please serialize your assetregistry correctly");

                    PreallocatedDependsNodeDataBuffers[i].Add(index, EAssetRegistryDependencyType.HardManage);
                }
                for (int x = 0; x < DepCounts[0]; ++x)
                {
                    int index = reader.ReadInt32();

                    if (index < 0 || index >= LocalNumDependsNodes)
                        throw new FileLoadException("could you please serialize your assetregistry correctly");

                    PreallocatedDependsNodeDataBuffers[i].Add(index, 0);
                }
            }

            int LocalNumPackageData = reader.ReadInt32();
            PreallocatedPackageDataBuffers = new FAssetPackageData[LocalNumPackageData];

            var bSerializeHash = Version < FAssetRegistryVersion.Type.AddedCookedMD5Hash;
            for (int i = 0; i < LocalNumPackageData; i++)
            {
                PreallocatedPackageDataBuffers[i] = new FAssetPackageData(nameReader, bSerializeHash);
            }
        }
    }
}
