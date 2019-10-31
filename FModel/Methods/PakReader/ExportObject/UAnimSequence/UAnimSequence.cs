using System;
using System.IO;

namespace PakReader
{
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
}
