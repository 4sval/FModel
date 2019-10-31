using System.IO;
using static PakReader.AssetReader;

namespace PakReader
{
    public struct FObjectExport
    {
        public FPackageIndex class_index;
        public FPackageIndex super_index;
        public FPackageIndex template_index;
        public FPackageIndex outer_index;
        public string object_name;
        public uint save;
        public long serial_size;
        public long serial_offset;
        public bool forced_export;
        public bool not_for_client;
        public bool not_for_server;
        public FGuid package_guid;
        public uint package_flags;
        public bool not_always_loaded_for_editor_game;
        public bool is_asset;
        public int first_export_dependency;
        public bool serialization_before_serialization_dependencies;
        public bool create_before_serialization_dependencies;
        public bool serialization_before_create_dependencies;
        public bool create_before_create_dependencies;

        internal FObjectExport(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map)
        {
            class_index = new FPackageIndex(reader, import_map);
            super_index = new FPackageIndex(reader, import_map);
            template_index = new FPackageIndex(reader, import_map);
            outer_index = new FPackageIndex(reader, import_map);
            object_name = read_fname(reader, name_map);
            save = reader.ReadUInt32();
            serial_size = reader.ReadInt64();
            serial_offset = reader.ReadInt64();
            forced_export = reader.ReadInt32() != 0;
            not_for_client = reader.ReadInt32() != 0;
            not_for_server = reader.ReadInt32() != 0;
            package_guid = new FGuid(reader);
            package_flags = reader.ReadUInt32();
            not_always_loaded_for_editor_game = reader.ReadInt32() != 0;
            is_asset = reader.ReadInt32() != 0;
            first_export_dependency = reader.ReadInt32();
            serialization_before_serialization_dependencies = reader.ReadInt32() != 0;
            create_before_serialization_dependencies = reader.ReadInt32() != 0;
            serialization_before_create_dependencies = reader.ReadInt32() != 0;
            create_before_create_dependencies = reader.ReadInt32() != 0;
        }
    }
}
