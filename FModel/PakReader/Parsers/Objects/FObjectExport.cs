using Newtonsoft.Json;

namespace PakReader.Parsers.Objects
{
    public sealed class FObjectExport : FObjectResource
    {
        public FPackageIndex ClassIndex { get; }
        //public FPackageIndex ThisIndex { get; } unused for serialization
        public FPackageIndex SuperIndex { get; }
        public FPackageIndex TemplateIndex { get; }
        public EObjectFlags ObjectFlags { get; }
        public long SerialSize { get; }
        public long SerialOffset { get; }
        //public long ScriptSerializationStartOffset { get; }
        //public long ScriptSerializationEndOffset { get; }
        //public UObject Object { get; }
        //public int HashNext { get; }
        [JsonIgnore]
        public bool bForcedExport { get; }
        [JsonIgnore]
        public bool bNotForClient { get; }
        [JsonIgnore]
        public bool bNotForServer { get; }
        [JsonIgnore]
        public bool bNotAlwaysLoadedForEditorGame { get; }
        [JsonIgnore]
        public bool bIsAsset { get; }
        //public bool bExportLoadFailed { get; }
        //public EDynamicType DynamicType { get; }
        //public bool bWasFiltered { get; }
        [JsonIgnore]
        public FGuid PackageGuid { get; }
        [JsonIgnore]
        public uint PackageFlags { get; }
        [JsonIgnore]
        public int FirstExportDependency { get; }
        [JsonIgnore]
        public int SerializationBeforeSerializationDependencies { get; }
        [JsonIgnore]
        public int CreateBeforeSerializationDependencies { get; }
        [JsonIgnore]
        public int SerializationBeforeCreateDependencies { get; }
        [JsonIgnore]
        public int CreateBeforeCreateDependencies { get; }

        internal FObjectExport(PackageReader reader)
        {
            ClassIndex = new FPackageIndex(reader);
            SuperIndex = new FPackageIndex(reader);

            // only serialize when file version is past VER_UE4_TemplateIndex_IN_COOKED_EXPORTS
            TemplateIndex = new FPackageIndex(reader);

            OuterIndex = new FPackageIndex(reader);
            ObjectName = reader.ReadFName();

            ObjectFlags = (EObjectFlags)reader.ReadUInt32() & EObjectFlags.RF_Load;

            // only serialize when file version is past VER_UE4_64BIT_EXPORTMAP_SERIALSIZES
            SerialSize = reader.ReadInt64();
            SerialOffset = reader.ReadInt64();

            bForcedExport = reader.ReadInt32() != 0;
            bNotForClient = reader.ReadInt32() != 0;
            bNotForServer = reader.ReadInt32() != 0;

            PackageGuid = new FGuid(reader);
            PackageFlags = reader.ReadUInt32();

            // only serialize when file version is past VER_UE4_LOAD_FOR_EDITOR_GAME
            bNotAlwaysLoadedForEditorGame = reader.ReadInt32() != 0;

            // only serialize when file version is past VER_UE4_COOKED_ASSETS_IN_EDITOR_SUPPORT
            bIsAsset = reader.ReadInt32() != 0;

            // only serialize when file version is past VER_UE4_PRELOAD_DEPENDENCIES_IN_COOKED_EXPORTS
            FirstExportDependency = reader.ReadInt32();
            SerializationBeforeSerializationDependencies = reader.ReadInt32();
            CreateBeforeSerializationDependencies = reader.ReadInt32();
            SerializationBeforeCreateDependencies = reader.ReadInt32() ;
            CreateBeforeCreateDependencies = reader.ReadInt32();
        }

        public enum EDynamicType : byte
        {
            NotDynamicExport,
		    DynamicType,
		    ClassDefaultObject,
	    };
    }
}
