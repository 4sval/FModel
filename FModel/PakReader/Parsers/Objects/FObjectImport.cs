using Newtonsoft.Json;

namespace PakReader.Parsers.Objects
{
    public sealed class FObjectImport : FObjectResource
    {
        [JsonIgnore]
        public FName ClassPackage { get; }
        [JsonIgnore]
        public FName ClassName { get; }
        //public bool bImportPackageHandled { get; } unused for serialization
        //public bool bImportSearchedFor { get; }
        //public bool bImportFailed { get; }

        internal FObjectImport(PackageReader reader)
        {
            ClassPackage = reader.ReadFName();
            ClassName = reader.ReadFName();
            OuterIndex = new FPackageIndex(reader);
            ObjectName = reader.ReadFName();
        }
    }
}
