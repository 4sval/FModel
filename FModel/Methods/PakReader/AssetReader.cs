using Newtonsoft.Json;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static PakReader.AssetReader;

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
                        default:
                            Exports[ind] = new UObject(reader, name_map, import_map, export_type, true);
                            break;
                    }
                }
                catch (Exception e)
                {
                    if (!ignoreErrors)
                        throw e;
                }
                long valid_pos = position + v.serial_size;
                if (reader.BaseStream.Position != valid_pos)
                {
                    Console.WriteLine($"Did not read {export_type} correctly. Current Position: {reader.BaseStream.Position}, Bytes Remaining: {valid_pos - reader.BaseStream.Position}");
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
                        return new string((char*)dataPtr, 0, data.Length);
                }
            }
            else
            {
                byte[] bytes = reader.ReadBytes(length);
                if (bytes.Length == 0) return string.Empty;
                var ret = Encoding.UTF8.GetString(bytes);
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
            //long index_pos = reader.BaseStream.Position;
            int name_index = reader.ReadInt32();
            reader.ReadInt32();
            return name_map[name_index].data;
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

        public struct FSmartName
        {
            public string display_name;

            internal FSmartName(BinaryReader reader, FNameEntrySerialized[] name_map)
            {
                display_name = read_fname(reader, name_map);
            }
        }

        public struct FCompressedOffsetData
        {
            public int[] offset_data;
            public int strip_size;
        }

        public struct FCompressedSegment
        {
            public int start_frame;
            public int num_frames;
            public int byte_stream_offset;
            public byte translation_compression_format;
            public byte rotation_compression_format;
            public byte scale_compression_format;

            public FCompressedSegment(BinaryReader reader)
            {
                start_frame = reader.ReadInt32();
                num_frames = reader.ReadInt32();
                byte_stream_offset = reader.ReadInt32();
                translation_compression_format = reader.ReadByte();
                rotation_compression_format = reader.ReadByte();
                scale_compression_format = reader.ReadByte();
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

        internal struct FNameEntrySerialized
        {
            public string data;
            public ushort non_case_preserving_hash;
            public ushort case_preserving_hash;

            internal FNameEntrySerialized(BinaryReader reader)
            {
                data = read_string(reader);
                non_case_preserving_hash = reader.ReadUInt16();
                case_preserving_hash = reader.ReadUInt16();
            }
        }

        internal struct FStripDataFlags
        {
            byte global_strip_flags;
            byte class_strip_flags;

            public bool editor_data_stripped => (global_strip_flags & 1) != 0;
            public bool server_data_stripped => (global_strip_flags & 2) != 0;
            public bool class_data_stripped(byte flag) => (class_strip_flags & flag) != 0;

            internal FStripDataFlags(BinaryReader reader)
            {
                global_strip_flags = reader.ReadByte();
                class_strip_flags = reader.ReadByte();
            }
        }

        public struct FTexturePlatformData
        {
            public int size_x;
            public int size_y;
            public int num_slices;
            public string pixel_format;
            public int first_mip;
            public FTexture2DMipMap[] mips;

            internal FTexturePlatformData(BinaryReader reader, BinaryReader ubulk, long bulk_offset)
            {
                size_x = reader.ReadInt32();
                size_y = reader.ReadInt32();
                num_slices = reader.ReadInt32();
                pixel_format = read_string(reader);
                first_mip = reader.ReadInt32();
                mips = new FTexture2DMipMap[reader.ReadUInt32()];
                for (int i = 0; i < mips.Length; i++)
                {
                    mips[i] = new FTexture2DMipMap(reader, ubulk, bulk_offset);
                }
            }
        }

        public struct FTexture2DMipMap
        {
            [JsonIgnore]
            public FByteBulkData data;
            public int size_x;
            public int size_y;
            public int size_z;

            internal FTexture2DMipMap(BinaryReader reader, BinaryReader ubulk, long bulk_offset)
            {
                int cooked = reader.ReadInt32();
                data = new FByteBulkData(reader, ubulk, bulk_offset);
                size_x = reader.ReadInt32();
                size_y = reader.ReadInt32();
                size_z = reader.ReadInt32();
                if (cooked != 1)
                {
                    read_string(reader);
                }
            }
        }

        public struct FByteBulkData
        {
            public FByteBulkDataHeader header;
            public byte[] data;

            internal FByteBulkData(BinaryReader reader, BinaryReader ubulk, long bulk_offset)
            {
                header = new FByteBulkDataHeader(reader);

                data = null;
                if ((header.bulk_data_flags & 0x0040) != 0)
                {
                    data = reader.ReadBytes(header.element_count);
                }
                if ((header.bulk_data_flags & 0x0100) != 0)
                {
                    if (ubulk == null)
                    {
                        throw new IOException("No ubulk specified for texture");
                    }
                    // Archive seems "kind of" appended.
                    ubulk.BaseStream.Seek(header.offset_in_file + bulk_offset, SeekOrigin.Begin);
                    data = ubulk.ReadBytes(header.element_count);
                }

                if (data == null)
                {
                    throw new IOException("Could not read texture");
                }
            }
        }

        public struct FByteBulkDataHeader
        {
            public int bulk_data_flags;
            public int element_count;
            public int size_on_disk;
            public long offset_in_file;

            internal FByteBulkDataHeader(BinaryReader reader)
            {
                bulk_data_flags = reader.ReadInt32();
                element_count = reader.ReadInt32();
                size_on_disk = reader.ReadInt32();
                offset_in_file = reader.ReadInt64();
            }
        }

        public enum AnimationCompressionFormat : uint
        {
            None,
            Float96NoW,
            Fixed48NoW,
            IntervalFixed32NoW,
            Fixed32NoW,
            Float32NoW,
            Identity,
        }

        public struct FAnimKeyHeader
        {
            public AnimationCompressionFormat key_format;
            public uint component_mask;
            public uint num_keys;
            public bool has_time_tracks;

            public FAnimKeyHeader(BinaryReader reader)
            {
                var packed = reader.ReadUInt32();
                key_format = (AnimationCompressionFormat)(packed >> 28);
                component_mask = (packed >> 24) & 0xF;
                num_keys = packed & 0xFFFFFF;
                has_time_tracks = (component_mask & 8) != 0;
            }
        }

        public struct FTrack
        {
            public FVector[] translation;
            public FQuat[] rotation;
            public FVector[] scale;
            public float[] translation_times;
            public float[] rotation_times;
            public float[] scale_times;
        }

        public struct FReferenceSkeleton
        {
            public FMeshBoneInfo[] ref_bone_info;
            public FTransform[] ref_bone_pose;
            public (string, int)[] name_to_index;

            internal FReferenceSkeleton(BinaryReader reader, FNameEntrySerialized[] name_map)
            {
                ref_bone_info = reader.ReadTArray(() => new FMeshBoneInfo(reader, name_map));
                ref_bone_pose = reader.ReadTArray(() => new FTransform(reader));

                name_to_index = new (string, int)[reader.ReadUInt32()];
                for (int i = 0; i < name_to_index.Length; i++)
                {
                    name_to_index[i] = (read_fname(reader, name_map), reader.ReadInt32());
                }
            }
        }

        public struct FMeshBoneInfo
        {
            public string name;
            public int parent_index;

            internal FMeshBoneInfo(BinaryReader reader, FNameEntrySerialized[] name_map)
            {
                name = read_fname(reader, name_map);
                parent_index = reader.ReadInt32();
            }
        }

        public struct FTransform
        {
            public FQuat rotation;
            public FVector translation;
            public FVector scale_3d;

            internal FTransform(BinaryReader reader)
            {
                rotation = new FQuat(reader);
                translation = new FVector(reader);
                scale_3d = new FVector(reader);
            }
        }

        public struct FReferencePose
        {
            public string pose_name;
            public FTransform[] reference_pose;

            internal FReferencePose(BinaryReader reader, FNameEntrySerialized[] name_map)
            {
                pose_name = read_fname(reader, name_map);
                reference_pose = reader.ReadTArray(() => new FTransform(reader));
            }
        }

        public struct FBoxSphereBounds
        {
            public FVector origin;
            public FVector box_extend;
            public float sphere_radius;

            internal FBoxSphereBounds(BinaryReader reader)
            {
                origin = new FVector(reader);
                box_extend = new FVector(reader);
                sphere_radius = reader.ReadSingle();
            }
        }

        public struct FSkeletalMaterial
        {
            public FPackageIndex Material;
            public string MaterialSlotName;
            public FMeshUVChannelInfo UVChannelData;

            internal FSkeletalMaterial(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map)
            {
                Material = new FPackageIndex(reader, import_map);

                MaterialSlotName = read_fname(reader, name_map);
                bool bSerializeImportedMaterialSlotName = reader.ReadUInt32() != 0;
                if (bSerializeImportedMaterialSlotName)
                {
                    var ImportedMaterialSlotName = read_fname(reader, name_map);
                }
                UVChannelData = new FMeshUVChannelInfo(reader);
            }
        }

        public struct FMeshUVChannelInfo
        {
            public bool initialized;
            public bool override_densities;
            public float[] local_uv_densities;

            internal FMeshUVChannelInfo(BinaryReader reader)
            {
                initialized = reader.ReadUInt32() != 0;
                override_densities = reader.ReadUInt32() != 0;
                local_uv_densities = new float[4];
                for (int i = 0; i < 4; i++)
                {
                    local_uv_densities[i] = reader.ReadSingle();
                }
            }
        }

        public struct FSkeletalMeshLODInfo
        {
            public float DisplayFactor;
            public float LODHysteresis;
            public int[] LODMaterialMap;
            public bool[] bEnableShadowCasting;
        }

        public struct FStaticLODModel
        {
            public FSkelMeshSection[] Sections;
            public FMultisizeIndexContainer Indices;
            public FMultisizeIndexContainer AdjacencyIndexBuffer;
            public short[] ActiveBoneIndices;
            public short[] RequiredBones;
            //public FSkelMeshChunk Chunks;
            //public int Size;
            public int NumVertices;
            public int NumTexCoords;
            //public FIntBulkData RawPointIndices;
            //public int[] MeshToImportVertexMap;
            //public int MaxImportVertex;
            public FSkeletalMeshVertexBuffer VertexBufferGPUSkin;
            //public FSkeletalMeshVertexClothBuffer ColorVertexBuffer;
            public FSkeletalMeshVertexClothBuffer ClothVertexBuffer;
            public FSkinWeightProfilesData SkinWeightProfilesData;

            internal FStaticLODModel(BinaryReader reader, FNameEntrySerialized[] name_map, bool has_vertex_colors)
            {
                var flags = new FStripDataFlags(reader);
                Sections = reader.ReadTArray(() => new FSkelMeshSection(reader, name_map));
                Indices = new FMultisizeIndexContainer(reader);
                ActiveBoneIndices = reader.ReadTArray(() => reader.ReadInt16());
                RequiredBones = reader.ReadTArray(() => reader.ReadInt16());

                if (flags.server_data_stripped || flags.class_data_stripped(2))
                {
                    throw new FileLoadException("Could not read FSkelMesh, no renderable data");
                }

                var position_vertex_buffer = new FPositionVertexBuffer(reader);
                var static_mesh_vertex_buffer = new FStaticMeshVertexBuffer(reader);
                var skin_weight_vertex_buffer = new FSkinWeightVertexBuffer(reader);

                if (has_vertex_colors)
                {
                    var colour_vertex_buffer = new FColorVertexBuffer(reader);
                }

                AdjacencyIndexBuffer = default;
                if (!flags.class_data_stripped(1))
                {
                    AdjacencyIndexBuffer = new FMultisizeIndexContainer(reader);
                }

                ClothVertexBuffer = default;
                if (HasClothData(Sections))
                {
                    ClothVertexBuffer = new FSkeletalMeshVertexClothBuffer(reader);
                }

                SkinWeightProfilesData = new FSkinWeightProfilesData(reader, name_map);

                VertexBufferGPUSkin = new FSkeletalMeshVertexBuffer();
                VertexBufferGPUSkin.bUseFullPrecisionUVs = true;
                NumVertices = position_vertex_buffer.num_verts;
                NumTexCoords = static_mesh_vertex_buffer.num_tex_coords;

                VertexBufferGPUSkin.VertsFloat = new FGPUVert4Float[NumVertices];
                for (int i = 0; i < NumVertices; i++)
                {
                    var V = new FGPUVert4Float();
                    var SV = static_mesh_vertex_buffer.uv[i];
                    V.Pos = position_vertex_buffer.verts[i];
                    V.Infs = skin_weight_vertex_buffer.weights[i];
                    V.Normal = SV.Normal; // i mean, we're not using it for anything else, are we?
                    V.UV = SV.UV;
                    VertexBufferGPUSkin.VertsFloat[i] = V;
                }
            }

            static bool HasClothData(FSkelMeshSection[] sections)
            {
                foreach (var s in sections)
                    if (s.cloth_mapping_data.Length > 0)
                        return true;
                return false;
            }
        }

        public struct FGPUVert4Half
        {
            public FVector Pos;
            public FPackedNormal[] Normal; // 3 length
            public FSkinWeightInfo Infs;

            public FMeshUVHalf[] UV; // 4 length
        }

        public struct FGPUVert4Float
        {
            public FVector Pos;
            public FPackedNormal[] Normal; // 3 length
            public FSkinWeightInfo Infs;

            public FMeshUVFloat[] UV; // 4 length
        }

        public struct FSkeletalMeshVertexBuffer
        {
            public int NumTexCoords;
            public FVector MeshExtension;
            public FVector MeshOrigin;
            public bool bUseFullPrecisionUVs;
            public bool bExtraBoneInfluences;
            public FGPUVert4Half[] VertsHalf;
            public FGPUVert4Float[] VertsFloat;

            public int GetVertexCount()
            {
                if (VertsHalf != null && VertsHalf.Length != 0)
                {
                    return VertsHalf.Length;
                }
                else if (VertsFloat != null && VertsFloat.Length != 0)
                {
                    return VertsFloat.Length;
                }
                return 0;
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

        public struct FSkelMeshSection
        {
            public ushort material_index;
            public uint base_index;
            public uint num_triangles;
            public uint base_vertex_index;
            public FApexClothPhysToRenderVertData[] cloth_mapping_data;
            public ushort[] bone_map;
            public int num_vertices;
            public int max_bone_influences;
            public FClothingSectionData clothing_data;
            public bool disabled;

            internal FSkelMeshSection(BinaryReader reader, FNameEntrySerialized[] name_map)
            {
                var flags = new FStripDataFlags(reader);
                material_index = reader.ReadUInt16();
                base_index = reader.ReadUInt32();
                num_triangles = reader.ReadUInt32();

                var _recompute_tangent = reader.ReadUInt32() != 0;
                var _cast_shadow = reader.ReadUInt32() != 0;
                base_vertex_index = reader.ReadUInt32();
                cloth_mapping_data = reader.ReadTArray(() => new FApexClothPhysToRenderVertData(reader));
                bool HasClothData = cloth_mapping_data.Length > 0;

                bone_map = reader.ReadTArray(() => reader.ReadUInt16());
                num_vertices = reader.ReadInt32();
                max_bone_influences = reader.ReadInt32();
                var _correspond_cloth_asset_index = reader.ReadInt16();
                clothing_data = new FClothingSectionData(reader);
                var _vertex_buffer = new FDuplicatedVerticesBuffer(reader);
                disabled = reader.ReadUInt32() != 0;
            }
        }

        public struct FApexClothPhysToRenderVertData
        {
            public FVector4 PositionBaryCoordsAndDist;
            public FVector4 NormalBaryCoordsAndDist;
            public FVector4 TangentBaryCoordsAndDist;
            public short[] SimulMeshVertIndices;
            public int[] Padding;

            public FApexClothPhysToRenderVertData(BinaryReader reader)
            {
                PositionBaryCoordsAndDist = new FVector4(reader);
                NormalBaryCoordsAndDist = new FVector4(reader);
                TangentBaryCoordsAndDist = new FVector4(reader);
                SimulMeshVertIndices = new short[] { reader.ReadInt16(), reader.ReadInt16(), reader.ReadInt16(), reader.ReadInt16() };
                Padding = new int[] { reader.ReadInt32(), reader.ReadInt32() };
            }
        }

        public struct FVector4
        {
            public float X, Y, Z, W;

            public FVector4(BinaryReader reader)
            {
                X = reader.ReadSingle();
                Y = reader.ReadSingle();
                Z = reader.ReadSingle();
                W = reader.ReadSingle();
            }
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

        public struct FClothingSectionData
        {
            public FGuid asset_guid;
            public int asset_lod_index;

            public FClothingSectionData(BinaryReader reader)
            {
                asset_guid = new FGuid(reader);
                asset_lod_index = reader.ReadInt32();
            }
        }

        public struct FDuplicatedVerticesBuffer
        {
            public int[] dup_vert;
            public FIndexLengthPair[] dup_vert_index;

            public FDuplicatedVerticesBuffer(BinaryReader reader)
            {
                dup_vert = reader.ReadTArray(() => reader.ReadInt32());
                dup_vert_index = reader.ReadTArray(() => new FIndexLengthPair(reader));
            }
        }

        public struct FSkinWeightProfilesData
        {
            public (string, FRuntimeSkinWeightProfileData)[] override_data;

            internal FSkinWeightProfilesData(BinaryReader reader, FNameEntrySerialized[] name_map)
            {
                override_data = new (string, FRuntimeSkinWeightProfileData)[reader.ReadInt32()];
                for (int i = 0; i < override_data.Length; i++)
                {
                    override_data[i] = (read_fname(reader, name_map), new FRuntimeSkinWeightProfileData(reader));
                }
            }
        }

        public struct FRuntimeSkinWeightProfileData
        {
            public FSkinWeightOverrideInfo[] overrides_info;
            public ushort[] weights;
            public (uint, uint)[] vertex_index_override_index;

            public FRuntimeSkinWeightProfileData(BinaryReader reader)
            {
                overrides_info = reader.ReadTArray(() => new FSkinWeightOverrideInfo(reader));
                weights = reader.ReadTArray(() => reader.ReadUInt16());
                vertex_index_override_index = new (uint, uint)[reader.ReadInt32()];
                for (int i = 0; i < vertex_index_override_index.Length; i++)
                {
                    vertex_index_override_index[i] = (reader.ReadUInt32(), reader.ReadUInt32());
                }
            }
        }

        public struct FSkinWeightOverrideInfo
        {
            public uint influences_offset;
            public byte num_influences;

            public FSkinWeightOverrideInfo(BinaryReader reader)
            {
                influences_offset = reader.ReadUInt32();
                num_influences = reader.ReadByte();
            }
        }

        public struct FIndexLengthPair
        {
            public uint word1;
            public uint word2;

            public FIndexLengthPair(BinaryReader reader)
            {
                word1 = reader.ReadUInt32();
                word2 = reader.ReadUInt32();
            }
        }

        public struct FMultisizeIndexContainer
        {
            public ushort[] Indices16;
            public uint[] Indices32;

            public FMultisizeIndexContainer(BinaryReader reader)
            {
                var data_size = reader.ReadByte();
                var _element_size = reader.ReadInt32();
                switch (data_size)
                {
                    case 2:
                        Indices16 = reader.ReadTArray(() => reader.ReadUInt16());
                        Indices32 = null;
                        return;
                    case 4:
                        Indices32 = reader.ReadTArray(() => reader.ReadUInt32());
                        Indices16 = null;
                        return;
                    default:
                        throw new FileLoadException("No format size");
                }
            }
        }

        public struct FPositionVertexBuffer
        {
            public FVector[] verts;
            public int stride;
            public int num_verts;

            public FPositionVertexBuffer(BinaryReader reader)
            {
                stride = reader.ReadInt32();
                num_verts = reader.ReadInt32();
                var _element_size = reader.ReadInt32();
                verts = reader.ReadTArray(() => new FVector(reader));
            }
        }

        public struct FStaticMeshVertexBuffer
        {
            public int num_tex_coords;
            public int num_vertices;
            bool full_precision_uvs;
            bool high_precision_tangent_basis;
            public FStaticMeshUVItem4[] uv;
            //public FStaticMeshVertexDataUV? uvs;

            public FStaticMeshVertexBuffer(BinaryReader reader)
            {
                high_precision_tangent_basis = false;

                var flags = new FStripDataFlags(reader);

                num_tex_coords = reader.ReadInt32();
                num_vertices = reader.ReadInt32();
                full_precision_uvs = reader.ReadInt32() != 0;
                high_precision_tangent_basis = reader.ReadInt32() != 0;

                if (!flags.server_data_stripped)
                {
                    int ItemSize, ItemCount;
                    uv = new FStaticMeshUVItem4[num_vertices];

                    // Tangents
                    ItemSize = reader.ReadInt32();
                    ItemCount = reader.ReadInt32();
                    if (ItemCount != num_vertices)
                    {
                        throw new FileLoadException("Invalid item count/num_vertices at pos " + reader.BaseStream.Position);
                    }
                    var pos = reader.BaseStream.Position;
                    for (int i = 0; i < num_vertices; i++)
                    {
                        uv[i].SerializeTangents(reader, high_precision_tangent_basis);
                    }
                    if (reader.BaseStream.Position - pos != ItemCount * ItemSize)
                    {
                        throw new FileLoadException("Didn't read static mesh uvs correctly at pos " + reader.BaseStream.Position);
                    }

                    // Texture coordinates
                    ItemSize = reader.ReadInt32();
                    ItemCount = reader.ReadInt32();
                    if (ItemCount != num_vertices * num_tex_coords)
                    {
                        throw new FileLoadException("Invalid item count/num_vertices at pos " + reader.BaseStream.Position);
                    }
                    pos = reader.BaseStream.Position;
                    for (int i = 0; i < num_vertices; i++)
                    {
                        uv[i].SerializeTexcoords(reader, num_tex_coords, full_precision_uvs);
                    }
                    if (reader.BaseStream.Position - pos != ItemCount * ItemSize)
                    {
                        throw new FileLoadException("Didn't read static mesh texcoords correctly at pos " + reader.BaseStream.Position);
                    }
                }
                else
                {
                    uv = null;
                }
            }
        }

        public struct FStaticMeshUVItem4
        {
            public FPackedNormal[] Normal;
            public FMeshUVFloat[] UV;

            public void SerializeTangents(BinaryReader reader, bool useHighPrecisionTangents)
            {
                Normal = new FPackedNormal[3];
                if (!useHighPrecisionTangents)
                {
                    Normal[0] = new FPackedNormal(reader);
                    Normal[2] = new FPackedNormal(reader);
                }
                else
                {
                    FPackedRGBA16N Normal, Tangent;
                    Normal = new FPackedRGBA16N(reader);
                    Tangent = new FPackedRGBA16N(reader);
                    this.Normal[0] = Normal.ToPackedNormal();
                    this.Normal[2] = Tangent.ToPackedNormal();
                }
            }

            public void SerializeTexcoords(BinaryReader reader, int uvSets, bool useStaticFloatUVs)
            {
                UV = new FMeshUVFloat[8];
                if (useStaticFloatUVs)
                {
                    for (int i = 0; i < uvSets; i++)
                        UV[i] = new FMeshUVFloat(reader);
                }
                else
                {
                    for (int i = 0; i < uvSets; i++)
                    {
                        UV[i] = (FMeshUVFloat)new FMeshUVHalf(reader);
                    }
                }
            }
        }

        public struct FMeshUVFloat
        {
            public float U;
            public float V;

            public FMeshUVFloat(BinaryReader reader)
            {
                U = reader.ReadSingle();
                V = reader.ReadSingle();
            }

            public static implicit operator CMeshUVFloat(FMeshUVFloat me)
            {
                return new CMeshUVFloat
                {
                    U = me.U,
                    V = me.V
                };
            }
        }

        public struct FMeshUVHalf
        {
            public ushort U;
            public ushort V;

            public FMeshUVHalf(BinaryReader reader)
            {
                U = reader.ReadUInt16();
                V = reader.ReadUInt16();
            }

            public static explicit operator FMeshUVFloat(FMeshUVHalf me)
            {
                return new FMeshUVFloat
                {
                    U = Extensions.HalfToFloat(me.U),
                    V = Extensions.HalfToFloat(me.V)
                };
            }
        }

        public struct FPackedRGBA16N
        {
            public ushort X;
            public ushort Y;
            public ushort Z;
            public ushort W;

            public FPackedRGBA16N(BinaryReader reader)
            {
                X = reader.ReadUInt16();
                Y = reader.ReadUInt16();
                Z = reader.ReadUInt16();
                W = reader.ReadUInt16();

                X ^= 0x8000; // 4.20+: https://github.com/gildor2/UModel/blob/dcdb92c987c15f0a5d3366247667a8fb9fd8008b/Unreal/UnCore.h#L1290
                Y ^= 0x8000;
                Z ^= 0x8000;
                W ^= 0x8000;
            }

            public FPackedNormal ToPackedNormal() => new FVector
            {
                X = (X - 32767.5f) / 32767.5f,
                Y = (Y - 32767.5f) / 32767.5f,
                Z = (Z - 32767.5f) / 32767.5f
            };
        }

        public struct FPackedNormal
        {
            public uint Data;

            public FPackedNormal(BinaryReader reader)
            {
                Data = reader.ReadUInt32();
                Data ^= 0x80808080; // 4.20+: https://github.com/gildor2/UModel/blob/dcdb92c987c15f0a5d3366247667a8fb9fd8008b/Unreal/UnCore.h#L1216
            }

            public static implicit operator FPackedNormal(FVector V) => new FPackedNormal
            {
                Data = (uint)((int)((V.X + 1) * 127.5f)
                         + ((int)((V.Y + 1) * 127.5f) << 8)
                         + ((int)((V.Z + 1) * 127.5f) << 16))
            };

            public static implicit operator FPackedNormal(FVector4 V) => new FPackedNormal
            {
                Data = (uint)((int)((V.X + 1) * 127.5f)
                         + ((int)((V.Y + 1) * 127.5f) << 8)
                         + ((int)((V.Z + 1) * 127.5f) << 16)
                         + ((int)((V.W + 1) * 127.5f) << 24))
            };

            public static implicit operator FPackedNormal(CPackedNormal me) => new FPackedNormal
            {
                Data = me.Data ^ 0x80808080
            };
        }

        public struct FSkinWeightVertexBuffer
        {
            public FSkinWeightInfo[] weights;

            public FSkinWeightVertexBuffer(BinaryReader reader)
            {
                var flags = new FStripDataFlags(reader);

                var bExtraBoneInfluences = reader.ReadInt32() != 0;
                var num_vertices = reader.ReadInt32();

                if (flags.server_data_stripped)
                {
                    weights = null;
                    return;
                }

                var _element_size = reader.ReadInt32();
                var num_influences = bExtraBoneInfluences ? 8 : 4;
                weights = reader.ReadTArray(() => new FSkinWeightInfo(reader, num_influences));
            }
        }

        public struct FSkinWeightInfo
        {
            public byte[] bone_index;
            public byte[] bone_weight;

            public FSkinWeightInfo(BinaryReader reader, int influences = 4) // NUM_INFLUENCES_UE4 = 4
            {
                bone_index = reader.ReadBytes(influences);
                bone_weight = reader.ReadBytes(influences);
            }
        }

        public struct FSkeletalMeshVertexClothBuffer
        {
            public ulong[] cloth_index_mapping;

            public FSkeletalMeshVertexClothBuffer(BinaryReader reader)
            {
                var flags = new FStripDataFlags(reader);

                if (!flags.server_data_stripped)
                {
                    // umodel: https://github.com/gildor2/UModel/blob/9a1fe8c77d136f018ba18c9e5c445fdcc5f374ae/Unreal/UnMesh4.cpp#L924
                    //         https://github.com/gildor2/UModel/blob/39c635c13d61616297fb3e47f33e3fc20259626e/Unreal/UnCoreSerialize.cpp#L320
                    // ue4: https://github.com/EpicGames/UnrealEngine/blob/master/Engine/Source/Runtime/Engine/Private/SkeletalMeshLODRenderData.cpp#L758
                    //      https://github.com/EpicGames/UnrealEngine/blob/master/Engine/Source/Runtime/Engine/Public/Rendering/SkeletalMeshLODRenderData.h#L119

                    int elem_size = reader.ReadInt32(); // umodel has this, might want to actually serialize this like how ue4 has it
                    int count = reader.ReadInt32();
                    reader.BaseStream.Seek(elem_size * count, SeekOrigin.Current);

                    cloth_index_mapping = reader.ReadTArray(() => reader.ReadUInt64());
                }

                cloth_index_mapping = null;
            }
        }

        public struct FColorVertexBuffer
        {
            public int stride;
            public int num_verts;
            public FColor[] colors;

            public FColorVertexBuffer(BinaryReader reader)
            {
                var flags = new FStripDataFlags(reader);
                stride = reader.ReadInt32();
                num_verts = reader.ReadInt32();
                colors = null;
                if (!flags.server_data_stripped && num_verts > 0)
                {
                    var _element_size = reader.ReadInt32();
                    colors = reader.ReadTArray(() => new FColor(reader));
                }
            }
        }

        public struct FObjectImport
        {
            public string class_package;
            public string class_name;
            public FPackageIndex outer_index;
            public string object_name;

            internal FObjectImport(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map)
            {
                class_package = read_fname(reader, name_map);
                class_name = read_fname(reader, name_map);
                outer_index = new FPackageIndex(reader, import_map);
                object_name = read_fname(reader, name_map);
            }
        }

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

        internal struct FRichCurveKey
        {
            public byte interp_mode;
            public byte tangent_mode;
            public byte tangent_weight_mode;
            public float time;
            public float arrive_tangent;
            public float arrive_tangent_weight;
            public float leave_tangent;
            public float leave_tangent_weight;

            public FRichCurveKey(BinaryReader reader)
            {
                interp_mode = reader.ReadByte();
                tangent_mode = reader.ReadByte();
                tangent_weight_mode = reader.ReadByte();
                time = reader.ReadSingle();
                arrive_tangent = reader.ReadSingle();
                arrive_tangent_weight = reader.ReadSingle();
                leave_tangent = reader.ReadSingle();
                leave_tangent_weight = reader.ReadSingle();
            }
        }

        internal struct FSimpleCurveKey
        {
            public float time;
            public float value;

            public FSimpleCurveKey(BinaryReader reader)
            {
                time = reader.ReadSingle();
                value = reader.ReadSingle();
            }
        }

        internal struct FDateTime
        {
            public long date;

            public FDateTime(BinaryReader reader)
            {
                date = reader.ReadInt64();
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

    public enum FPropertyTagType
    {
        BoolProperty,
        StructProperty,
        ObjectProperty,
        InterfaceProperty,
        FloatProperty,
        TextProperty,
        StrProperty,
        NameProperty,
        IntProperty,
        UInt16Property,
        UInt32Property,
        UInt64Property,
        ArrayProperty,
        MapProperty,
        ByteProperty,
        EnumProperty,
        DelegateProperty,
        SoftObjectProperty,
        SoftObjectPropertyMap,
    }

    public struct FPropertyTag
    {
        public string name;
        public long position;
        [JsonIgnore]
        public string property_type;
        public object tag_data;
        [JsonIgnore]
        public int size;
        [JsonIgnore]
        public int array_index;
        [JsonIgnore]
        public FGuid property_guid;
        [JsonIgnore]
        public FPropertyTagType tag;

        public bool Equals(FPropertyTag b)
        {
            return name == b.name &&
                position == b.position && 
                property_type == b.property_type &&
                size == b.size &&
                array_index == b.array_index &&
                tag == b.tag &&
                tag_data == b.tag_data;
        }
    }

    public struct UScriptStruct
    {
        public string struct_name;
        public object struct_type;

        internal UScriptStruct(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map, string struct_name)
        {
            this.struct_name = struct_name;
            switch (struct_name)
            {
                case "Vector2D":
                    struct_type = new FVector2D(reader);
                    break;
                case "LinearColor":
                    struct_type = new FLinearColor(reader);
                    break;
                case "Color":
                    struct_type = new FColor(reader);
                    break;
                case "GameplayTagContainer":
                    struct_type = new FGameplayTagContainer(reader, name_map);
                    break;
                case "IntPoint":
                    struct_type = new FIntPoint(reader);
                    break;
                case "Guid":
                    struct_type = new FGuid(reader);
                    break;
                case "Quat":
                    struct_type = new FQuat(reader);
                    break;
                case "Vector":
                    struct_type = new FVector(reader);
                    break;
                case "Rotator":
                    struct_type = new FRotator(reader);
                    break;
                case "SoftObjectPath":
                    struct_type = new FSoftObjectPath(reader, name_map);
                    break;
                case "LevelSequenceObjectReferenceMap":
                    struct_type = new FLevelSequenceObjectReferenceMap(reader);
                    break;
                case "FrameNumber":
                    struct_type = reader.ReadSingle();
                    break;/*
                case "SectionEvaluationDataTree":
                    struct_type = new FSectionEvaluationDataTree(reader, name_map, import_map);
                    break;
                case "MovieSceneTrackIdentifier":
                    struct_type = reader.ReadSingle();
                    break;
                case "MovieSceneSegment":
                    struct_type = new FMovieSceneSegment(reader, name_map, import_map);
                    break;
                case "MovieSceneEvalTemplatePtr":
                    struct_type = new InlineUStruct(reader, name_map, import_map);
                    break;
                case "MovieSceneTrackImplementationPtr":
                    struct_type = new InlineUStruct(reader, name_map, import_map);
                    break;
                case "MovieSceneSequenceInstanceDataPtr":
                    struct_type = new InlineUStruct(reader, name_map, import_map);
                    break;
                case "MovieSceneFrameRange":
                    struct_type = new FMovieSceneFrameRange(reader, name_map, import_map);
                    break;
                case "MovieSceneSegmentIdentifier":
                    struct_type = reader.ReadSingle();
                    break;
                case "MovieSceneSequenceID":
                    struct_type = reader.ReadSingle();
                    break;
                case "MovieSceneEvaluationKey":
                    struct_type = new FMovieSceneEvaluationKey(reader, name_map, import_map);
                    break;*/
                case "SmartName":
                    struct_type = new FSmartName(reader, name_map);
                    break;
                case "RichCurveKey":
                    struct_type = new FRichCurveKey(reader);
                    break;
                case "SimpleCurveKey":
                    struct_type = new FSimpleCurveKey(reader);
                    break;
                case "DateTime":
                    struct_type = new FDateTime(reader);
                    break;
                case "Timespan":
                    struct_type = new FDateTime(reader);
                    break;
                default:
                    struct_type = new FStructFallback(reader, name_map, import_map);
                    break;
            }
        }
    }

    public struct FStructFallback
    {
        public FPropertyTag[] properties;

        internal FStructFallback(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map)
        {
            var properties_ = new List<FPropertyTag>();
            int i = 0;
            while (true)
            {
                var tag = read_property_tag(reader, name_map, import_map, true);
                if (tag.Equals(default))
                {
                    break;
                }

                properties_.Add(tag);
                i++;
            }
            properties = properties_.ToArray();
        }
    }

    public struct FVector2D
    {
        public float x;
        public float y;

        internal FVector2D(BinaryReader reader)
        {
            x = reader.ReadSingle();
            y = reader.ReadSingle();
        }
    }

    public struct FLinearColor
    {
        public float r;
        public float g;
        public float b;
        public float a;

        [JsonProperty]
        public string Hex => a == 1 || a == 0 ?
            ToHex((byte)Math.Round(r * 256), (byte)Math.Round(g * 256), (byte)Math.Round(b * 256)) :
            ToHex((byte)Math.Round(r * 256), (byte)Math.Round(g * 256), (byte)Math.Round(b * 256), (byte)Math.Round(a * 256));

        internal FLinearColor(BinaryReader reader)
        {
            r = reader.ReadSingle();
            g = reader.ReadSingle();
            b = reader.ReadSingle();
            a = reader.ReadSingle();
        }
    }

    public struct FColor
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a;

        [JsonProperty]
        public string Hex => a == 0 || a == 255 ?
            ToHex(r, g, b) :
            ToHex(r, g, b, a);

        internal FColor(BinaryReader reader)
        {
            r = reader.ReadByte();
            g = reader.ReadByte();
            b = reader.ReadByte();
            a = reader.ReadByte();
        }
    }

    public struct FGameplayTagContainer
    {
        public string[] gameplay_tags;

        internal FGameplayTagContainer(BinaryReader reader, FNameEntrySerialized[] name_map)
        {
            uint length = reader.ReadUInt32();
            gameplay_tags = new string[length];

            for (int i = 0; i < length; i++)
            {
                gameplay_tags[i] = read_fname(reader, name_map);
            }
        }
    }

    public struct FIntPoint
    {
        public uint x;
        public uint y;

        internal FIntPoint(BinaryReader reader)
        {
            x = reader.ReadUInt32();
            y = reader.ReadUInt32();
        }
    }

    public struct FQuat
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        internal FQuat(BinaryReader reader)
        {
            X = reader.ReadSingle();
            Y = reader.ReadSingle();
            Z = reader.ReadSingle();
            W = reader.ReadSingle();
        }

        public void rebuild_w()
        {
            var ww = 1f - (X * X + Y * Y + Z * Z);
            W = ww > 0 ? (float)Math.Sqrt(ww) : 0;
        }

        public static implicit operator CQuat(FQuat me) => new CQuat
        {
            x = me.X,
            y = me.Y,
            z = me.Z,
            w = me.W
        };

        public static implicit operator FQuat(CQuat me) => new FQuat
        {
            X = me.x,
            Y = me.y,
            Z = me.z,
            W = me.w
        };

        public void Write(BinaryWriter writer)
        {
            writer.Write(X);
            writer.Write(Y);
            writer.Write(Z);
            writer.Write(W);
        }
    }

    public struct FVector : IEquatable<FVector>
    {
        public float X;
        public float Y;
        public float Z;

        internal FVector(BinaryReader reader)
        {
            X = reader.ReadSingle();
            Y = reader.ReadSingle();
            Z = reader.ReadSingle();
        }

        public static FVector operator -(FVector a, FVector b)
        {
            return new FVector
            {
                X = a.X - b.X,
                Y = a.Y - b.Y,
                Z = a.Z - b.Z
            };
        }

        public static FVector operator +(FVector a, FVector b)
        {
            return new FVector
            {
                X = a.X + b.X,
                Y = a.Y + b.Y,
                Z = a.Z + b.Z
            };
        }

        public bool Equals(FVector other) => other.X == X && other.Y == Y && other.Z == Z;

        public static bool operator ==(FVector a, FVector b) => a.Equals(b);

        public static bool operator !=(FVector a, FVector b) => !a.Equals(b);

        public static implicit operator CVec3(FVector me) => new CVec3
        {
            v = new float[] { me.X, me.Y, me.Z }
        };

        public static implicit operator FVector(CVec3 me) => new FVector
        {
            X = me.v[0],
            Y = me.v[1],
            Z = me.v[2]
        };

        public static implicit operator CVec4(FVector me) => new CVec4
        {
            v = new float[] { me.X, me.Y, me.Z, 0 }
        };

        public static implicit operator FVector(FPackedNormal V) => new FVector
        {
            X = ((V.Data & 0xFF) / 127.5f) - 1,
            Y = ((V.Data >> 8 & 0xFF) / 127.5f) - 1,
            Z = ((V.Data >> 16 & 0xFF) / 127.5f) - 1
        };

        public void Write(BinaryWriter writer)
        {
            writer.Write(X);
            writer.Write(Y);
            writer.Write(Z);
        }
    }

    public struct FRotator
    {
        public float pitch;
        public float yaw;
        public float roll;

        internal FRotator(BinaryReader reader)
        {
            pitch = reader.ReadSingle();
            yaw = reader.ReadSingle();
            roll = reader.ReadSingle();
        }
    }

    public struct FScriptDelegate
    {
        public int obj;
        public string name;

        internal FScriptDelegate(BinaryReader reader, FNameEntrySerialized[] name_map)
        {
            obj = reader.ReadInt32();
            name = read_fname(reader, name_map);
        }
    }

    public struct FSoftObjectPathMap
    {
        public string asset_path_name;
        public string sub_path_string;

        internal FSoftObjectPathMap(BinaryReader reader, FNameEntrySerialized[] name_map)
        {
            asset_path_name = read_fname(reader, name_map);
            sub_path_string = read_string(reader);
        }
    }

    public struct FSoftObjectPath
    {
        public string asset_path_name;
        public string sub_path_string;

        internal FSoftObjectPath(BinaryReader reader, FNameEntrySerialized[] name_map)
        {
            asset_path_name = read_fname(reader, name_map);
            sub_path_string = read_string(reader);
        }
    }

    public struct FLevelSequenceObjectReferenceMap
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

    public struct FLevelSequenceLegacyObjectReference
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

    public struct UScriptMap
    {
        public Dictionary<object, object> map_data;

        internal UScriptMap(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map, string key_type, string value_type)
        {
            int num_keys_to_remove = reader.ReadInt32();
            if (num_keys_to_remove != 0)
            {
                throw new NotSupportedException($"Could not read MapProperty with types: {key_type} {value_type}");
            }

            int num = reader.ReadInt32();
            map_data = new Dictionary<object, object>(num);
            for (int i = 0; i < num; i++)
            {
                map_data[read_map_value(reader, key_type, "StructProperty", name_map, import_map)] = read_map_value(reader, value_type, "StructProperty", name_map, import_map);
            }
        }
    }

    public abstract class ExportObject { }

    public sealed class Texture2D : ExportObject, IDisposable
    {
        public UObject base_object;
        public bool cooked;
        internal FTexturePlatformData[] textures;

        internal Texture2D(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map, int asset_file_size, long export_size, BinaryReader ubulk)
        {
            var uobj = new UObject(reader, name_map, import_map, "Texture2D", true);

            new FStripDataFlags(reader); // no idea
            new FStripDataFlags(reader); // why are there two

            List<FTexturePlatformData> texs = new List<FTexturePlatformData>();
            cooked = reader.ReadUInt32() == 1;
            if (cooked)
            {
                string pixel_format = read_fname(reader, name_map);
                while (pixel_format != "None")
                {
                    long skipOffset = reader.ReadInt64();
                    var texture = new FTexturePlatformData(reader, ubulk, export_size + asset_file_size);
                    if (reader.BaseStream.Position + asset_file_size != skipOffset)
                    {
                        throw new IOException("Texture read incorrectly");
                    }
                    texs.Add(texture);
                    pixel_format = read_fname(reader, name_map);
                }
            }

            textures = texs.ToArray();
        }

        public SKImage GetImage() => ImageExporter.GetImage(textures[0].mips[0], textures[0].pixel_format);

        public void Dispose()
        {
            textures = null;
        }
    }

    public sealed class FontFace : ExportObject
    {
        public UObject base_object;
        [JsonIgnore]
        public uint data;

        internal FontFace(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map)
        {
            base_object = new UObject(reader, name_map, import_map, "FontFace", true);

            new FStripDataFlags(reader); // no idea
            new FStripDataFlags(reader); // why are there two

            data = reader.ReadUInt32();
        }
    }

    public sealed class UDataTable : ExportObject
    {
        public UObject super_object;
        public (string Name, UObject Object)[] rows;

        internal UDataTable(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map)
        {
            super_object = new UObject(reader, name_map, import_map, "RowStruct", true);

            rows = new (string Name, UObject Object)[reader.ReadInt32()];

            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = (read_fname(reader, name_map), new UObject(reader, name_map, import_map, "RowStruct", false));
            }
        }
    }

    public enum ECurveTableMode : byte
    {
        Empty,
        SimpleCurves,
        RichCurves,
    }

    public sealed class UCurveTable : ExportObject
    {
        public UObject super_object;
        public ECurveTableMode curve_table_mode;
        public (string Name, UObject Object)[] row_map;

        internal UCurveTable(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map)
        {
            super_object = new UObject(reader, name_map, import_map, "CurveTable", true);

            row_map = new (string Name, UObject Object)[reader.ReadInt32()];

            curve_table_mode = (ECurveTableMode)reader.ReadByte();

            string row_type;
            switch (curve_table_mode)
            {
                case ECurveTableMode.Empty:
                    row_type = "Empty";
                    break;
                case ECurveTableMode.SimpleCurves:
                    row_type = "SimpleCurveKey";
                    break;
                case ECurveTableMode.RichCurves:
                    row_type = "RichCurveKey";
                    break;
                default:
                    throw new InvalidOperationException("Unsupported curve mode " + (byte)curve_table_mode);
            }
            for (int i = 0; i < row_map.Length; i++)
            {
                row_map[i] = (read_fname(reader, name_map), new UObject(reader, name_map, import_map, row_type, false));
            }
        }
    }

    public sealed class UAnimSequence : ExportObject
    {
        public UObject super_object;
        public FGuid skeleton_guid;
        public byte key_encoding_format;
        public byte translation_compression_format;
        public byte rotation_compression_format;
        public byte scale_compression_format;
        public int[] compressed_track_offsets;
        public FCompressedOffsetData compressed_scale_offsets;
        public FCompressedSegment[] compressed_segments;
        public int[] compressed_track_to_skeleton_table;
        public FSmartName[] compressed_curve_names;
        public int compressed_raw_data_size;
        public int compressed_num_frames;
        public FTrack[] tracks;

        internal UAnimSequence(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map)
        {
            super_object = new UObject(reader, name_map, import_map, "AnimSequence", true);
            skeleton_guid = new FGuid(reader);
            new FStripDataFlags(reader);
            if (reader.ReadUInt32() == 0)
            {
                throw new FileLoadException("Could not decode AnimSequence (must be compressed)");
            }
            key_encoding_format = reader.ReadByte();
            translation_compression_format = reader.ReadByte();
            rotation_compression_format = reader.ReadByte();
            scale_compression_format = reader.ReadByte();
            compressed_track_offsets = reader.ReadTArray(() => reader.ReadInt32());
            compressed_scale_offsets = new FCompressedOffsetData
            {
                offset_data = reader.ReadTArray(() => reader.ReadInt32()),
                strip_size = reader.ReadInt32()
            };

            compressed_segments = reader.ReadTArray(() => new FCompressedSegment(reader));
            compressed_track_to_skeleton_table = reader.ReadTArray(() => reader.ReadInt32());
            compressed_curve_names = reader.ReadTArray(() => new FSmartName(reader, name_map));

            compressed_raw_data_size = reader.ReadInt32();
            compressed_num_frames = reader.ReadInt32();
            int num_bytes = reader.ReadInt32();
            if (reader.ReadUInt32() != 0)
            {
                throw new NotImplementedException("Bulk data for animations not supported yet");
            }
            tracks = read_tracks(new MemoryStream(reader.ReadBytes(num_bytes)));
        }

        static float read_fixed48(ushort val) => val - 255f;

        static float read_fixed48_q(ushort val) => (val / 32767f) - 1;

        const uint X_MASK = 0x000003ff;
        const uint Y_MASK = 0x001ffc00;

        static FVector read_fixed32_vec(uint val, FVector min, FVector max)
        {
            return new FVector
            {
                X = ((val & X_MASK) - 511) / 511f * max.X + min.X,
                Y = (((val & Y_MASK) >> 10) - 1023) / 1023f * max.Y + min.Y,
                Z = ((val >> 21) - 1023) / 1023f * max.Z + min.Z
            };
        }

        static FQuat read_fixed32_quat(uint val, FVector min, FVector max)
        {
            var q = new FQuat
            {
                X = ((val >> 21) - 1023) / 1023f * max.X + min.X,
                Y = (((val & Y_MASK) >> 10) - 1023) / 1023f * max.Y + min.Y,
                Z = ((val & X_MASK) - 511) / 511f * max.Z + min.Z,
                W = 1
            };
            q.rebuild_w();
            return q;
        }

        static void align_reader(BinaryReader reader)
        {
            var pos = reader.BaseStream.Position % 4;
            if (pos != 0) reader.BaseStream.Seek(4 - pos, SeekOrigin.Current);
        }

        static float[] read_times(BinaryReader reader, uint num_keys, uint num_frames)
        {
            if (num_keys <= 1) return new float[0];
            align_reader(reader);

            var ret = new float[num_keys];
            if (num_frames < 256)
            {
                for (int i = 0; i < num_keys; i++)
                {
                    ret[i] = reader.ReadByte();
                }
            }
            else
            {
                for (int i = 0; i < num_keys; i++)
                {
                    ret[i] = reader.ReadUInt16();
                }
            }
            return ret;
        }

        private FTrack[] read_tracks(MemoryStream stream)
        {
            if (key_encoding_format != 2)
            {
                throw new NotImplementedException("Can only parse PerTrackCompression");
            }
            using (stream)
            using (var reader = new BinaryReader(stream))
            {
                var num_tracks = compressed_track_offsets.Length / 2;
                var num_frames = compressed_num_frames;

                FTrack[] ret = new FTrack[num_tracks];
                for (int i = 0; i < ret.Length; i++)
                {
                    FTrack track = new FTrack();

                    // translation
                    var offset = compressed_track_offsets[i * 2];
                    if (offset != -1)
                    {
                        var header = new FAnimKeyHeader(reader);
                        var min = new FVector();
                        var max = new FVector();

                        if (header.key_format == AnimationCompressionFormat.IntervalFixed32NoW)
                        {
                            if ((header.component_mask & 1) != 0)
                            {
                                min.X = reader.ReadSingle();
                                max.X = reader.ReadSingle();
                            }
                            if ((header.component_mask & 2) != 0)
                            {
                                min.Y = reader.ReadSingle();
                                max.Y = reader.ReadSingle();
                            }
                            if ((header.component_mask & 4) != 0)
                            {
                                min.Z = reader.ReadSingle();
                                max.Z = reader.ReadSingle();
                            }
                        }

                        track.translation = new FVector[header.num_keys];
                        for (int j = 0; j < track.translation.Length; j++)
                        {
                            switch (header.key_format)
                            {
                                case AnimationCompressionFormat.None:
                                case AnimationCompressionFormat.Float96NoW:
                                    var fvec = new FVector();
                                    if ((header.component_mask & 7) != 0)
                                    {
                                        if ((header.component_mask & 1) != 0) fvec.X = reader.ReadSingle();
                                        if ((header.component_mask & 2) != 0) fvec.Y = reader.ReadSingle();
                                        if ((header.component_mask & 4) != 0) fvec.Z = reader.ReadSingle();
                                    }
                                    else
                                    {
                                        fvec = new FVector(reader);
                                    }
                                    track.translation[j] = fvec;
                                    break;
                                case AnimationCompressionFormat.Fixed48NoW:
                                    fvec = new FVector();
                                    if ((header.component_mask & 1) != 0) fvec.X = read_fixed48(reader.ReadUInt16());
                                    if ((header.component_mask & 2) != 0) fvec.Y = read_fixed48(reader.ReadUInt16());
                                    if ((header.component_mask & 4) != 0) fvec.Z = read_fixed48(reader.ReadUInt16());
                                    track.translation[j] = fvec;
                                    break;
                                case AnimationCompressionFormat.IntervalFixed32NoW:
                                    track.translation[j] = read_fixed32_vec(reader.ReadUInt32(), min, max);
                                    break;
                            }
                        }
                        if (header.has_time_tracks)
                        {
                            track.translation_times = read_times(reader, header.num_keys, (uint)num_frames);
                        }
                        align_reader(reader);
                    }

                    // rotation
                    offset = compressed_track_offsets[(i * 2) + 1];
                    if (offset != -1)
                    {
                        var header = new FAnimKeyHeader(reader);
                        var min = new FVector();
                        var max = new FVector();

                        if (header.key_format == AnimationCompressionFormat.IntervalFixed32NoW)
                        {
                            if ((header.component_mask & 1) != 0)
                            {
                                min.X = reader.ReadSingle();
                                max.X = reader.ReadSingle();
                            }
                            if ((header.component_mask & 2) != 0)
                            {
                                min.Y = reader.ReadSingle();
                                max.Y = reader.ReadSingle();
                            }
                            if ((header.component_mask & 4) != 0)
                            {
                                min.Z = reader.ReadSingle();
                                max.Z = reader.ReadSingle();
                            }
                        }

                        track.rotation = new FQuat[header.num_keys];
                        for (int j = 0; j < track.rotation.Length; j++)
                        {
                            switch (header.key_format)
                            {
                                case AnimationCompressionFormat.None:
                                case AnimationCompressionFormat.Float96NoW:
                                    var fvec = new FVector();
                                    if ((header.component_mask & 7) != 0)
                                    {
                                        if ((header.component_mask & 1) != 0) fvec.X = reader.ReadSingle();
                                        if ((header.component_mask & 2) != 0) fvec.Y = reader.ReadSingle();
                                        if ((header.component_mask & 4) != 0) fvec.Z = reader.ReadSingle();
                                    }
                                    else
                                    {
                                        fvec = new FVector(reader);
                                    }
                                    var fquat = new FQuat()
                                    {
                                        X = fvec.X,
                                        Y = fvec.Y,
                                        Z = fvec.Z
                                    };
                                    fquat.rebuild_w();
                                    track.rotation[j] = fquat;
                                    break;
                                case AnimationCompressionFormat.Fixed48NoW:
                                    fquat = new FQuat();
                                    if ((header.component_mask & 1) != 0) fquat.X = read_fixed48_q(reader.ReadUInt16());
                                    if ((header.component_mask & 2) != 0) fquat.Y = read_fixed48_q(reader.ReadUInt16());
                                    if ((header.component_mask & 4) != 0) fquat.Z = read_fixed48_q(reader.ReadUInt16());
                                    fquat.rebuild_w();
                                    track.rotation[j] = fquat;
                                    break;
                                case AnimationCompressionFormat.IntervalFixed32NoW:
                                    track.rotation[j] = read_fixed32_quat(reader.ReadUInt32(), min, max);
                                    break;
                            }
                        }
                        if (header.has_time_tracks)
                        {
                            track.rotation_times = read_times(reader, header.num_keys, (uint)num_frames);
                        }
                        align_reader(reader);
                    }

                    // scale
                    offset = compressed_scale_offsets.offset_data[i * compressed_scale_offsets.strip_size];
                    if (offset != -1)
                    {
                        var header = new FAnimKeyHeader(reader);
                        var min = new FVector();
                        var max = new FVector();

                        if (header.key_format == AnimationCompressionFormat.IntervalFixed32NoW)
                        {
                            if ((header.component_mask & 1) != 0)
                            {
                                min.X = reader.ReadSingle();
                                max.X = reader.ReadSingle();
                            }
                            if ((header.component_mask & 2) != 0)
                            {
                                min.Y = reader.ReadSingle();
                                max.Y = reader.ReadSingle();
                            }
                            if ((header.component_mask & 4) != 0)
                            {
                                min.Z = reader.ReadSingle();
                                max.Z = reader.ReadSingle();
                            }
                        }

                        track.scale = new FVector[header.num_keys];
                        for (int j = 0; j < track.scale.Length; j++)
                        {
                            switch (header.key_format)
                            {
                                case AnimationCompressionFormat.None:
                                case AnimationCompressionFormat.Float96NoW:
                                    var fvec = new FVector();
                                    if ((header.component_mask & 7) != 0)
                                    {
                                        if ((header.component_mask & 1) != 0) fvec.X = reader.ReadSingle();
                                        if ((header.component_mask & 2) != 0) fvec.Y = reader.ReadSingle();
                                        if ((header.component_mask & 4) != 0) fvec.Z = reader.ReadSingle();
                                    }
                                    else
                                    {
                                        fvec = new FVector(reader);
                                    }
                                    track.scale[j] = fvec;
                                    break;
                                case AnimationCompressionFormat.Fixed48NoW:
                                    fvec = new FVector();
                                    if ((header.component_mask & 1) != 0) fvec.X = read_fixed48(reader.ReadUInt16());
                                    if ((header.component_mask & 2) != 0) fvec.Y = read_fixed48(reader.ReadUInt16());
                                    if ((header.component_mask & 4) != 0) fvec.Z = read_fixed48(reader.ReadUInt16());
                                    track.scale[j] = fvec;
                                    break;
                                case AnimationCompressionFormat.IntervalFixed32NoW:
                                    track.scale[j] = read_fixed32_vec(reader.ReadUInt32(), min, max);
                                    break;
                            }
                        }
                        if (header.has_time_tracks)
                        {
                            track.scale_times = read_times(reader, header.num_keys, (uint)num_frames);
                        }
                        align_reader(reader);
                    }

                    ret[i] = track;
                }

                if (reader.BaseStream.Position != stream.Length)
                {
                    throw new FileLoadException($"Could not read tracks correctly, {reader.BaseStream.Position - stream.Length} bytes remaining");
                }
                return ret;
            }
        }
    }

    public sealed class USkeleton : ExportObject
    {
        public UObject super_object;
        public FReferenceSkeleton reference_skeleton;
        public (string, FReferencePose)[] anim_retarget_sources;

        internal USkeleton(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map)
        {
            super_object = new UObject(reader, name_map, import_map, "Skeleton", true);
            reference_skeleton = new FReferenceSkeleton(reader, name_map);

            anim_retarget_sources = new (string, FReferencePose)[reader.ReadUInt32()];
            for (int i = 0; i < anim_retarget_sources.Length; i++)
            {
                anim_retarget_sources[i] = (read_fname(reader, name_map), new FReferencePose(reader, name_map));
            }
        }
    }

    public sealed class USkeletalMesh : ExportObject
    {
        public UObject BaseObject;
        public FBoxSphereBounds Bounds;
        public FSkeletalMaterial[] Materials;
        public FReferenceSkeleton RefSkeleton;
        public FStaticLODModel[] LODModels;
        public FSkeletalMeshLODInfo[] LODInfo;

        public string[] MaterialAssets;

        internal USkeletalMesh(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map)
        {
            BaseObject = new UObject(reader, name_map, import_map, "SkeletalMesh", true);
            bool has_vertex_colors = false;
            foreach (var prop in BaseObject.properties)
            {
                if (prop.name == "bHasVertexColors" && prop.tag == FPropertyTagType.BoolProperty)
                {
                    has_vertex_colors = (bool)prop.tag_data;
                }
                else if (prop.name == "LODInfo")
                {
                    var data = ((UScriptArray)prop.tag_data).data;
                    LODInfo = new FSkeletalMeshLODInfo[data.Length];
                    for (int i = 0; i < data.Length; i++)
                    {
                        var info = (UScriptStruct)data[i];
                        if (info.struct_name != "SkeletalMeshLODInfo")
                        {
                            throw new FileLoadException("Invalid lod info type");
                        }
                        var props = ((FStructFallback)info.struct_type).properties;
                        var newInfo = new FSkeletalMeshLODInfo();
                        foreach (var lodProp in props)
                        {
                            switch (lodProp.name)
                            {
                                case "DisplayFactor":
                                    newInfo.DisplayFactor = (float)lodProp.tag_data;
                                    break;
                                case "LODHysteresis":
                                    newInfo.LODHysteresis = (float)lodProp.tag_data;
                                    break;
                                case "LODMaterialMap":
                                    newInfo.LODMaterialMap = ((UScriptArray)lodProp.tag_data).data.Cast<int>().ToArray();
                                    break;
                                case "bEnableShadowCasting":
                                    newInfo.bEnableShadowCasting = ((UScriptArray)lodProp.tag_data).data.Cast<bool>().ToArray();
                                    break;
                            }
                        }
                        LODInfo[i] = newInfo;
                    }
                }
            }
            var flags = new FStripDataFlags(reader);
            Bounds = new FBoxSphereBounds(reader);
            Materials = reader.ReadTArray(() => new FSkeletalMaterial(reader, name_map, import_map));
            RefSkeleton = new FReferenceSkeleton(reader, name_map);

            if (!flags.editor_data_stripped)
            {
                Console.WriteLine("Editor data still present!");
            }

            if (reader.ReadUInt32() == 0)
            {
                throw new FileLoadException("No cooked data");
            }
            LODModels = reader.ReadTArray(() => new FStaticLODModel(reader, name_map, has_vertex_colors));

            uint serialize_guid = reader.ReadUInt32();

            MaterialAssets = new string[Materials.Length];
            for (int i = 0; i < Materials.Length; i++)
            {
                if (Materials[i].Material.import == null) continue;
                for (int j = 0; j < import_map.Length; j++)
                {
                    if (import_map[j].class_name != "MaterialInstanceConstant" && import_map[j].object_name.EndsWith(Materials[i].Material.import))
                    {
                        MaterialAssets[i] = import_map[j].object_name;
                        break;
                    }
                }
            }
        }
    }

    public class FPackageIndex
    {
        [JsonIgnore]
        public int index;
        public string import;
        public string outer_import;

        internal FPackageIndex(BinaryReader reader, FObjectImport[] import_map)
        {
            index = reader.ReadInt32();
            if (index < 0) index *= -1;
            index -= 1;
            if (index < 0 || index >= import_map.Length)
            {
                import = index.ToString();
                outer_import = default;
            }
            else
            {
                var imp = import_map[index];
                import = imp.object_name;
                outer_import = imp.outer_index?.import;
            }
        }
    }

    public class UObject : ExportObject
    {
        public string export_type;
        public FPropertyTag[] properties;

        internal UObject(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map, string export_type, bool read_guid)
        {
            this.export_type = export_type;
            var properties_ = new List<FPropertyTag>();
            while (true)
            {
                var tag = read_property_tag(reader, name_map, import_map, export_type != "FontFace");
                if (tag.Equals(default))
                {
                    break;
                }
                properties_.Add(tag);
            }

            if (read_guid && reader.ReadUInt32() != 0)
            {
                if (reader.BaseStream.Position + 16 <= reader.BaseStream.Length)
                    new FGuid(reader);
            }

            properties = properties_.ToArray();
        }
    }

    public struct FText
    {
        [JsonIgnore]
        public uint flags;
        [JsonIgnore]
        public byte history_type;
        public string @namespace;
        public string key;
        public string source_string;

        internal FText(BinaryReader reader)
        {
            flags = reader.ReadUInt32();
            history_type = reader.ReadByte();

            if (history_type == 255)
            {
                @namespace = "";
                key = "";
                source_string = "";
            }
            else if (history_type == 0)
            {
                @namespace = read_string(reader);
                key = read_string(reader);
                source_string = read_string(reader);
            }
            else
            {
                throw new NotImplementedException($"Could not read history type: {history_type}");
            }
        }
    }
    public struct UScriptArray
    {
        public FPropertyTag tag;
        public object[] data;

        internal UScriptArray(BinaryReader reader, string inner_type, FNameEntrySerialized[] name_map, FObjectImport[] import_map)
        {
            uint element_count = reader.ReadUInt32();
            tag = default;

            if (inner_type == "StructProperty" || inner_type == "ArrayProperty")
            {
                tag = read_property_tag(reader, name_map, import_map, false);
                if (tag.Equals(default))
                {
                    throw new IOException("Could not read file");
                }
            }
            object inner_tag_data = tag.Equals(default) ? null : tag.tag_data;

            data = new object[element_count];
            for (int i = 0; i < element_count; i++)
            {
                if (inner_type == "BoolProperty")
                {
                    data[i] = reader.ReadByte() != 0;
                }
                else if (inner_type == "ByteProperty")
                {
                    data[i] = reader.ReadByte();
                }
                else
                {
                    var tag = new_property_tag_type(reader, name_map, import_map, inner_type, inner_tag_data);
                    if ((int)tag.type != 100)
                    {
                        data[i] = tag.data;
                    }
                }
            }
        }
    }
}
