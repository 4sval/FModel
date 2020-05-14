using System.Collections.Generic;

namespace PakReader.Parsers.Objects
{
    public struct FDependsNode : IUStruct
    {
        public FAssetIdentifier Identifier;
        public List<int> HardDependencies;
        public List<int> SoftDependencies;
        public List<int> NameDependencies;
        public List<int> SoftManageDependencies;
        public List<int> HardManageDependencies;
        public List<int> Referencers;

        internal void Reserve(SortedList<EAssetRegistryDependencyType, int> list)
        {
            foreach (var kv in list)
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
}
