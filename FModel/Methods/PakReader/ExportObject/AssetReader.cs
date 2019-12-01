using FModel.Methods.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace PakReader
{
    public class AssetReader
    {
        public readonly ExportObject[] Exports;

        public AssetReader(string path, bool ubulk = false, bool ignoreErrors = true) : this(path + ".uasset", path + ".uexp", ubulk ? path + ".ubulk" : null, ignoreErrors) { }

        public AssetReader(string assetPath, string expPath, string bulkPath, bool ignoreErrors = true) : this(File.OpenRead(assetPath), File.OpenRead(expPath), bulkPath == null ? null : File.OpenRead(bulkPath), ignoreErrors) { }

        public AssetReader(Stream asset, Stream exp, Stream bulk = null, bool ignoreErrors = true)
        {
            BinaryReader reader = new BinaryReader(asset);
            var summary = new AssetSummary(reader);

            reader.BaseStream.Seek(summary.name_offset, SeekOrigin.Begin);
            FNameEntrySerialized[] name_map = new FNameEntrySerialized[summary.name_count];
            for (int i = 0; i < summary.name_count; i++)
            {
                name_map[i] = new FNameEntrySerialized(reader);
            }

            reader.BaseStream.Seek(summary.import_offset, SeekOrigin.Begin);
            FObjectImport[] import_map = new FObjectImport[summary.import_count];
            for (int i = 0; i < summary.import_count; i++)
            {
                import_map[i] = new FObjectImport(reader, name_map, import_map);
            }

            reader.BaseStream.Seek(summary.export_offset, SeekOrigin.Begin);
            FObjectExport[] export_map = new FObjectExport[summary.export_count];
            for (int i = 0; i < summary.export_count; i++)
            {
                export_map[i] = new FObjectExport(reader, name_map, import_map);
            }

            long export_size = export_map.Sum(v => v.serial_size);

            reader = new BinaryReader(exp);

            var bulkReader = bulk == null ? null : new BinaryReader(bulk);

            int asset_length = summary.total_header_size;

            Exports = new ExportObject[summary.export_count];

            int ind = 0;
            foreach (FObjectExport v in export_map)
            {
                string export_type = v.class_index.import;
                long position = v.serial_offset - asset.Length;
                reader.BaseStream.Seek(position, SeekOrigin.Begin);
                try
                {
                    switch (export_type)
                    {
                        case "Texture2D":
                            Exports[ind] = new Texture2D(reader, name_map, import_map, asset_length, export_size, bulkReader);
                            break;
                        case "FontFace":
                            Exports[ind] = new FontFace(reader, name_map, import_map);
                            break;
                        case "DataTable":
                            Exports[ind] = new UDataTable(reader, name_map, import_map);
                            break;
                        case "CurveTable":
                            Exports[ind] = new UCurveTable(reader, name_map, import_map);
                            break;
                        case "SkeletalMesh":
                            Exports[ind] = new USkeletalMesh(reader, name_map, import_map);
                            break;
                        case "AnimSequence":
                            Exports[ind] = new UAnimSequence(reader, name_map, import_map);
                            break;
                        case "Skeleton":
                            Exports[ind] = new USkeleton(reader, name_map, import_map);
                            break;
                        case "SoundWave":
                            Exports[ind] = new USoundWave(reader, name_map, import_map, asset_length, export_size, bulkReader);
                            break;
                        default:
                            Exports[ind] = new UObject(reader, name_map, import_map, export_type, true);
                            break;
                    }
                }
                catch (Exception e)
                {
                    DebugHelper.WriteException(e, "thrown in AssetReader.cs by AssetReader");
                    if (!ignoreErrors)
                        throw e;
                }
                long valid_pos = position + v.serial_size;
                if (reader.BaseStream.Position != valid_pos)
                {
                    reader.BaseStream.Seek(valid_pos, SeekOrigin.Begin);
                }
                ind++;
            }
            //Exports[Exports.Length - 1] = new AssetInfo(name_map, import_map, export_map);
        }

        /*public class AssetInfo : ExportObject
        {
            [JsonProperty]
            FNameEntrySerialized[] name_map;
            [JsonProperty]
            FObjectImport[] import_map;
            [JsonProperty]
            FObjectExport[] export_map;

            internal AssetInfo(FNameEntrySerialized[] name_map, FObjectImport[] import_map, FObjectExport[] export_map)
            {
                this.name_map = name_map;
                this.import_map = import_map;
                this.export_map = export_map;
            }
        }*/

        static readonly uint[] _Lookup32 = Enumerable.Range(0, 256).Select(i => {
            string s = i.ToString("X2");
            return s[0] + ((uint)s[1] << 16);
        }).ToArray();

        internal static string ToHex(params byte[] bytes)
        {
            var result = new char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                var val = _Lookup32[bytes[i]];
                result[2 * i] = (char)val;
                result[2 * i + 1] = (char)(val >> 16);
            }
            return new string(result);
        }

        internal static string read_string(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            if (length > 65536 || length < -65536)
            {
                throw new IOException($"String length too large ({length}), likely a read error.");
            }

            if (length == 0)
            {
                return "";
            }

            if (length < 0)
            {
                length *= -1;
                ushort[] data = new ushort[length];
                for (int i = 0; i < length; i++)
                {
                    data[i] = reader.ReadUInt16();
                }
                unsafe
                {
                    fixed (ushort* dataPtr = &data[0])
                        return new string((char*)dataPtr, 0, data.Length).TrimEnd('\0');
                }
            }
            else
            {
                byte[] bytes = reader.ReadBytes(length);
                if (bytes.Length == 0) return string.Empty;
                var ret = Encoding.UTF8.GetString(bytes).TrimEnd('\0');
                if (ret.Length != length)
                {
                    return ret;
                }
                else
                {
                    return ret.Substring(0, length - 1);
                }
            }
        }

        public static T[] read_tarray<T>(BinaryReader reader, Func<BinaryReader, T> getter)
        {
            uint length = reader.ReadUInt32();
            T[] container = new T[length];
            for (int i = 0; i < length; i++)
            {
                container[i] = getter(reader);
            }
            return container;
        }

        internal static string read_fname(BinaryReader reader, FNameEntrySerialized[] name_map)
        {
            long index_pos = reader.BaseStream.Position;
            int name_index = reader.ReadInt32();
            reader.ReadInt32(); //name number ?
            if (name_index >= 0 && name_index < name_map.Length)
            {
                return name_map[name_index].data;
            }
            else
            {
                throw new IOException(string.Format("FName could not be read at offset {0} Requested Index: {1}, Name Map Size: {2}", index_pos, name_index, name_map.Length));
            }
        }

        static object tag_data_overrides(string name)
        {
            switch (name)
            {
                case "BindingIdToReferences":
                    return ("Guid", "LevelSequenceBindingReferenceArray");
                case "Tracks":
                    return ("MovieSceneTrackIdentifier", "MovieSceneEvaluationTrack");
                case "SubTemplateSerialNumbers":
                    return ("MovieSceneSequenceID", "UInt32Property");
                case "SubSequences":
                    return ("MovieSceneSequenceID", "MovieSceneSubSequenceData");
                case "Hierarchy":
                    return ("MovieSceneSequenceID", "MovieSceneSequenceHierarchyNode");
                case "TrackSignatureToTrackIdentifier":
                    return ("Guid", "MovieSceneTrackIdentifier");
                case "SubSectionRanges":
                    return ("Guid", "MovieSceneFrameRange");
                default:
                    return default;
            }
        }

        internal static FPropertyTag read_property_tag(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map, bool read_data)
        {
            string name = read_fname(reader, name_map);
            if (name == "None")
            {
                return default;
            }

            string property_type = read_fname(reader, name_map).Trim();
            int size = reader.ReadInt32();
            int array_index = reader.ReadInt32();

            object tag_data;
            switch (property_type)
            {
                case "StructProperty":
                    tag_data = (read_fname(reader, name_map), new FGuid(reader));
                    break;
                case "BoolProperty":
                    tag_data = reader.ReadByte() != 0;
                    break;
                case "EnumProperty":
                    tag_data = read_fname(reader, name_map);
                    break;
                case "ByteProperty":
                    tag_data = read_fname(reader, name_map);
                    break;
                case "ArrayProperty":
                    tag_data = read_fname(reader, name_map);
                    break;
                case "MapProperty":
                    tag_data = (read_fname(reader, name_map), read_fname(reader, name_map));
                    break;
                case "SetProperty":
                    tag_data = read_fname(reader, name_map);
                    break;
                default:
                    tag_data = null;
                    break;
            }

            if (property_type == "MapProperty")
            {
                tag_data = tag_data_overrides(name) ?? tag_data;
            }

            bool has_property_guid = reader.ReadByte() != 0;
            FGuid property_guid = has_property_guid ? new FGuid(reader) : default;

            long pos = reader.BaseStream.Position;
            var (type, data) = read_data ? new_property_tag_type(reader, name_map, import_map, property_type, tag_data) : default;
            if ((int)type == 100)
            {
                return default;
            }

            if (read_data)
            {
                reader.BaseStream.Seek(pos + size, SeekOrigin.Begin);
            }
            if (read_data && pos + size != reader.BaseStream.Position)
            {
                throw new IOException($"Could not read entire property: {name} ({property_type})");
            }

            return new FPropertyTag
            {
                array_index = array_index,
                name = name,
                position = pos,
                property_guid = property_guid,
                property_type = property_type,
                size = size,
                tag = type,
                tag_data = read_data ? data : tag_data
            };
        }

        internal static (FPropertyTagType type, object data) new_property_tag_type(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map, string property_type, object tag_data)
        {
            switch (property_type)
            {
                case "BoolProperty":
                    return (FPropertyTagType.BoolProperty, (bool)tag_data);
                case "StructProperty":
                    if (tag_data is UScriptStruct)
                    {
                        return (FPropertyTagType.StructProperty, tag_data);
                    }
                    return (FPropertyTagType.StructProperty, new UScriptStruct(reader, name_map, import_map, ((ValueTuple<string, FGuid>)tag_data).Item1));
                case "ObjectProperty":
                    return (FPropertyTagType.ObjectProperty, new FPackageIndex(reader, import_map));
                case "InterfaceProperty":
                    return (FPropertyTagType.InterfaceProperty, new UInterfaceProperty(reader));
                case "FloatProperty":
                    return (FPropertyTagType.FloatProperty, reader.ReadSingle());
                case "TextProperty":
                    return (FPropertyTagType.TextProperty, new FText(reader));
                case "StrProperty":
                    return (FPropertyTagType.StrProperty, read_string(reader));
                case "NameProperty":
                    return (FPropertyTagType.NameProperty, read_fname(reader, name_map));
                case "IntProperty":
                    return (FPropertyTagType.IntProperty, reader.ReadInt32());
                case "UInt16Property":
                    return (FPropertyTagType.UInt16Property, reader.ReadUInt16());
                case "UInt32Property":
                    return (FPropertyTagType.UInt32Property, reader.ReadUInt32());
                case "UInt64Property":
                    return (FPropertyTagType.UInt64Property, reader.ReadUInt64());
                case "ArrayProperty":
                    return (FPropertyTagType.ArrayProperty, new UScriptArray(reader, (string)tag_data, name_map, import_map));
                case "MapProperty":
                    (string key_type, string value_type) = (ValueTuple<string, string>)tag_data;
                    return (FPropertyTagType.MapProperty, new UScriptMap(reader, name_map, import_map, key_type, value_type));
                case "ByteProperty":
                    return (FPropertyTagType.ByteProperty, (string)tag_data == "None" ? (object)reader.ReadByte() : read_fname(reader, name_map));
                case "EnumProperty":
                    return (FPropertyTagType.EnumProperty, (string)tag_data == "None" ? null : read_fname(reader, name_map));
                case "DelegateProperty":
                    return (FPropertyTagType.DelegateProperty, new FScriptDelegate(reader, name_map));
                case "SoftObjectProperty":
                    return (FPropertyTagType.SoftObjectProperty, new FSoftObjectPath(reader, name_map));
                default:
                    return ((FPropertyTagType)100, null);
                    //throw new NotImplementedException($"Could not read property type: {property_type} at pos {reader.BaseStream.Position}");
            }
        }

        public struct AssetSummary
        {
            internal AssetSummary(BinaryReader reader)
            {
                tag = reader.ReadInt32();
                legacy_file_version = reader.ReadInt32();
                legacy_ue3_version = reader.ReadInt32();
                file_version_u34 = reader.ReadInt32();
                file_version_licensee_ue4 = reader.ReadInt32();
                custom_version_container = reader.ReadTArray(() => new FCustomVersion(reader));
                total_header_size = reader.ReadInt32();
                folder_name = read_string(reader);
                package_flags = reader.ReadUInt32();
                name_count = reader.ReadInt32();
                name_offset = reader.ReadInt32();
                gatherable_text_data_count = reader.ReadInt32();
                gatherable_text_data_offset = reader.ReadInt32();
                export_count = reader.ReadInt32();
                export_offset = reader.ReadInt32();
                import_count = reader.ReadInt32();
                import_offset = reader.ReadInt32();
                depends_offset = reader.ReadInt32();
                string_asset_references_count = reader.ReadInt32();
                string_asset_references_offset = reader.ReadInt32();
                searchable_names_offset = reader.ReadInt32();
                thumbnail_table_offset = reader.ReadInt32();
                guid = new FGuid(reader);
                generations = reader.ReadTArray(() => new FGenerationInfo(reader));
                saved_by_engine_version = new FEngineVersion(reader);
                compatible_with_engine_version = new FEngineVersion(reader);
                compression_flags = reader.ReadUInt32();
                compressed_chunks = reader.ReadTArray(() => new FCompressedChunk(reader));
                package_source = reader.ReadUInt32();
                additional_packages_to_cook = reader.ReadTArray(() => read_string(reader));
                asset_registry_data_offset = reader.ReadInt32();
                buld_data_start_offset = reader.ReadInt32();
                world_tile_info_data_offset = reader.ReadInt32();
                chunk_ids = reader.ReadTArray(() => reader.ReadInt32());
                preload_dependency_count = reader.ReadInt32();
                preload_dependency_offset = reader.ReadInt32();
                var pos = reader.BaseStream.Position;
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
            }

            public int tag;
            public int legacy_file_version;
            public int legacy_ue3_version;
            public int file_version_u34;
            public int file_version_licensee_ue4;
            public FCustomVersion[] custom_version_container;
            public int total_header_size;
            public string folder_name;
            public uint package_flags;
            public int name_count;
            public int name_offset;
            public int gatherable_text_data_count;
            public int gatherable_text_data_offset;
            public int export_count;
            public int export_offset;
            public int import_count;
            public int import_offset;
            public int depends_offset;
            public int string_asset_references_count;
            public int string_asset_references_offset;
            public int searchable_names_offset;
            public int thumbnail_table_offset;
            public FGuid guid;
            public FGenerationInfo[] generations;
            public FEngineVersion saved_by_engine_version;
            public FEngineVersion compatible_with_engine_version;
            public uint compression_flags;
            public FCompressedChunk[] compressed_chunks;
            public uint package_source;
            public string[] additional_packages_to_cook;
            public int asset_registry_data_offset;
            public int buld_data_start_offset;
            public int world_tile_info_data_offset;
            public int[] chunk_ids;
            public int preload_dependency_count;
            public int preload_dependency_offset;
        }

        public struct FCustomVersion
        {
            public FGuid key;
            public int version;

            internal FCustomVersion(BinaryReader reader)
            {
                key = new FGuid(reader);
                version = reader.ReadInt32();
            }
        }

        public struct FGenerationInfo
        {
            public int export_count;
            public int name_count;

            internal FGenerationInfo(BinaryReader reader)
            {
                export_count = reader.ReadInt32();
                name_count = reader.ReadInt32();
            }
        }

        public struct FEngineVersion
        {
            public ushort major;
            public ushort minor;
            public ushort patch;
            public uint changelist;
            public string branch;

            internal FEngineVersion(BinaryReader reader)
            {
                major = reader.ReadUInt16();
                minor = reader.ReadUInt16();
                patch = reader.ReadUInt16();
                changelist = reader.ReadUInt32();
                branch = read_string(reader);
            }
        }

        public struct FCompressedChunk
        {
            public int uncompressed_offset;
            public int uncompressed_size;
            public int compressed_offset;
            public int compressed_size;

            internal FCompressedChunk(BinaryReader reader)
            {
                uncompressed_offset = reader.ReadInt32();
                uncompressed_size = reader.ReadInt32();
                compressed_offset = reader.ReadInt32();
                compressed_size = reader.ReadInt32();
            }
        }

        public struct FSkelMeshChunk
        {
            public int BaseVertexIndex;
            public FVertex[] RigidVerticies;
            public FVertex[] SoftVertices;
            public ushort[] BoneMap;
            public int NumRigidVertices;
            public int NumSoftVertices;
            public int MaxBoneInfluences;
            public bool HasClothData;
        }

        public struct FVertex
        {
            public FVector Pos;
            public FPackedNormal[] Normal; // 3 length
            public FSkinWeightInfo Infs;

            public FMeshUVFloat[] UV; // 4 length
            public FColor Color;

            public static FVertex Soft(BinaryReader reader) => new FVertex
            {
                Pos = new FVector(reader),
                Normal = new FPackedNormal[]
                {
                    new FVector(reader),
                    new FVector(reader),
                    new FVector4(reader)
                },
                UV = new FMeshUVFloat[]
                {
                    new FMeshUVFloat(reader),
                    new FMeshUVFloat(reader),
                    new FMeshUVFloat(reader),
                    new FMeshUVFloat(reader)
                },
                Color = new FColor(reader),
                Infs = new FSkinWeightInfo(reader)
            };

            public static FVertex Rigid(BinaryReader reader) => new FVertex
            {
                Pos = new FVector(reader),
                Normal = new FPackedNormal[]
                {
                    new FVector(reader),
                    new FVector(reader),
                    new FVector4(reader)
                },
                UV = new FMeshUVFloat[]
                {
                    new FMeshUVFloat(reader),
                    new FMeshUVFloat(reader),
                    new FMeshUVFloat(reader),
                    new FMeshUVFloat(reader)
                },
                Color = new FColor(reader),
                Infs = new FSkinWeightInfo()
                {
                    bone_index = new byte[] { reader.ReadByte(), 0, 0, 0 },
                    bone_weight = new byte[] { 255, 0, 0, 0 }
                }
            };
        }

        public struct FMeshToMeshVertData
        {
            public FQuat position_bary_coords;
            public FQuat normal_bary_coords;
            public FQuat tangent_bary_coords;
            public ushort[] source_mesh_vert_indices;
            public uint[] padding;

            public FMeshToMeshVertData(BinaryReader reader)
            {
                position_bary_coords = new FQuat(reader);
                normal_bary_coords = new FQuat(reader);
                tangent_bary_coords = new FQuat(reader);

                source_mesh_vert_indices = new ushort[4];
                for (int i = 0; i < 4; i++)
                {
                    source_mesh_vert_indices[i] = reader.ReadUInt16();
                }
                padding = new uint[2];
                for (int i = 0; i < 4; i++)
                {
                    padding[i] = reader.ReadUInt32();
                }
            }
        }

        internal struct FLevelSequenceObjectReferenceMap
        {
            public FLevelSequenceLegacyObjectReference[] map_data;

            internal FLevelSequenceObjectReferenceMap(BinaryReader reader)
            {
                int element_count = reader.ReadInt32();
                map_data = new FLevelSequenceLegacyObjectReference[element_count];
                for (int i = 0; i < element_count; i++)
                {
                    map_data[i] = new FLevelSequenceLegacyObjectReference(reader);
                }
            }
        }

        internal struct FLevelSequenceLegacyObjectReference
        {
            public FGuid key_guid;
            public FGuid object_id;
            public string object_path;

            internal FLevelSequenceLegacyObjectReference(BinaryReader reader)
            {
                key_guid = new FGuid(reader);
                object_id = new FGuid(reader);
                object_path = read_string(reader);
            }
        }

        internal struct FSectionEvaluationDataTree
        {
            public TMovieSceneEvaluationTree<FStructFallback> tree;

            internal FSectionEvaluationDataTree(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map)
            {
                tree = new TMovieSceneEvaluationTree<FStructFallback>(reader, name_map, import_map);
            }
        }

        internal struct TMovieSceneEvaluationTree<T>
        {
            public FMovieSceneEvaluationTree base_tree;
            public TEvaluationTreeEntryContainer<T> data;

            internal TMovieSceneEvaluationTree(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map)
            {
                base_tree = new FMovieSceneEvaluationTree(reader);
                data = new TEvaluationTreeEntryContainer<T>(reader);
            }
        }

        internal struct FMovieSceneEvaluationTree
        {
            public FMovieSceneEvaluationTreeNode root_node;
            public TEvaluationTreeEntryContainer<FMovieSceneEvaluationTreeNode> child_nodes;

            internal FMovieSceneEvaluationTree(BinaryReader reader)
            {
                root_node = new FMovieSceneEvaluationTreeNode(reader);
                child_nodes = new TEvaluationTreeEntryContainer<FMovieSceneEvaluationTreeNode>(reader);
            }
        }

        internal struct FMovieSceneEvaluationTreeNode
        {
            internal FMovieSceneEvaluationTreeNode(BinaryReader reader)
            {
                // holy shit this goes on forever
                throw new NotImplementedException("Not implemented yet.");
            }
        }

        internal struct TEvaluationTreeEntryContainer<T>
        {
            public FEntry[] entries;
            public T[] items;

            internal TEvaluationTreeEntryContainer(BinaryReader reader)
            {
                entries = reader.ReadTArray(() => new FEntry(reader));
                items = null;
                throw new NotImplementedException("Not implemented yet.");
            }
        }

        internal struct FEntry
        {
            public int start_index;
            public int size;
            public int capacity;

            internal FEntry(BinaryReader reader)
            {
                start_index = reader.ReadInt32();
                size = reader.ReadInt32();
                capacity = reader.ReadInt32();
            }
        }

        internal struct UInterfaceProperty
        {
            public uint interface_number;

            internal UInterfaceProperty(BinaryReader reader)
            {
                interface_number = reader.ReadUInt32();
            }
        }

        internal static object read_map_value(BinaryReader reader, string inner_type, string struct_type, FNameEntrySerialized[] name_map, FObjectImport[] import_map)
        {
            switch (inner_type)
            {
                case "BoolProperty":
                    return reader.ReadByte() != 1;
                case "ByteProperty":
                    return reader.ReadByte();
                case "EnumProperty":
                    return read_fname(reader, name_map);
                case "IntProperty":
                    return reader.ReadInt32();
                case "UInt32Property":
                    return reader.ReadUInt32();
                case "StructProperty":
                    return new UScriptStruct(reader, name_map, import_map, struct_type);
                case "NameProperty":
                    return read_fname(reader, name_map);
                case "ObjectProperty":
                    return new FPackageIndex(reader, import_map);
                case "SoftObjectProperty":
                    return (FPropertyTagType.SoftObjectPropertyMap, new FGuid(reader));
                case "StrProperty":
                    return read_string(reader);
                case "TextProperty":
                    return new FText(reader);
                default:
                    return (FPropertyTagType.StructProperty, new UScriptStruct(reader, name_map, import_map, inner_type));
            }
        }
    }
}
