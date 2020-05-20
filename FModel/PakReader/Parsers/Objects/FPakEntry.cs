using Ionic.Zlib;
using System;
using System.IO;

namespace PakReader.Parsers.Objects
{
    public class FPakEntry
    {
        const byte Flag_Encrypted = 0x01;
        const byte Flag_Deleted = 0x02;

        public bool Encrypted => (Flags & Flag_Encrypted) != 0;
        public bool Deleted => (Flags & Flag_Deleted) != 0;

        public readonly string PakFileName;
        public readonly string Name;
        public readonly long Offset;
        public readonly long Size;
        public readonly long UncompressedSize;
        public readonly byte[] Hash; // why isn't this an FShaHash?
        public readonly FPakCompressedBlock[] CompressionBlocks;
        public readonly uint CompressionBlockSize;
        public readonly uint CompressionMethodIndex;
        public readonly byte Flags;

        public readonly int StructSize;

        internal FPakEntry(BinaryReader reader, EPakVersion Version, bool caseSensitive, string pakName)
        {
            CompressionBlocks = null;
            CompressionBlockSize = 0;
            Flags = 0;

            PakFileName = pakName;
            string name = caseSensitive ? reader.ReadFString() : reader.ReadFString().ToLowerInvariant();
            Name = name.StartsWith("/") ? name.Substring(1) : name;

            var StartOffset = reader.BaseStream.Position;

            Offset = reader.ReadInt64();
            Size = reader.ReadInt64();
            UncompressedSize = reader.ReadInt64();
            if (Version < EPakVersion.FNAME_BASED_COMPRESSION_METHOD)
            {
                var LegacyCompressionMethod = reader.ReadInt32();
                if (LegacyCompressionMethod == (int)ECompressionFlags.COMPRESS_None)
                {
                    CompressionMethodIndex = 0;
                }
                else if ((LegacyCompressionMethod & (int)ECompressionFlags.COMPRESS_ZLIB) != 0)
                {
                    CompressionMethodIndex = 1;
                }
                else if ((LegacyCompressionMethod & (int)ECompressionFlags.COMPRESS_GZIP) != 0)
                {
                    CompressionMethodIndex = 2;
                }
                else if ((LegacyCompressionMethod & (int)ECompressionFlags.COMPRESS_Custom) != 0)
                {
                    CompressionMethodIndex = 3;
                }
                else
                {
                    // https://github.com/EpicGames/UnrealEngine/blob/8b6414ae4bca5f93b878afadcc41ab518b09984f/Engine/Source/Runtime/PakFile/Public/IPlatformFilePak.h#L441
                    throw new FileLoadException(@"Found an unknown compression type in pak file, will need to be supported for legacy files");
                }
            }
            else
            {
                CompressionMethodIndex = reader.ReadUInt32();
            }
            if (Version <= EPakVersion.INITIAL)
            {
                // Timestamp of type FDateTime, but the serializer only reads to the Ticks property (int64)
                reader.ReadInt64();
            }
            Hash = reader.ReadBytes(20);
            if (Version >= EPakVersion.COMPRESSION_ENCRYPTION)
            {
                if (CompressionMethodIndex != 0)
                {
                    CompressionBlocks = reader.ReadTArray(() => new FPakCompressedBlock(reader));
                }

                Flags = reader.ReadByte();
                CompressionBlockSize = reader.ReadUInt32();
            }

            // Used to seek ahead to the file data instead of parsing the entry again
            StructSize = (int)(reader.BaseStream.Position - StartOffset);
        }

        internal FPakEntry(BinaryReader reader, bool caseSensitive, string pakName)
        {
            CompressionBlocks = null;
            CompressionBlockSize = 0;
            Flags = 0;

            PakFileName = pakName;
            Name = caseSensitive ? reader.ReadFString() : reader.ReadFString().ToLowerInvariant();

            var StartOffset = reader.BaseStream.Position;

            Offset = reader.ReadInt64();
            Size = reader.ReadInt64();
            UncompressedSize = reader.ReadInt64();
            CompressionMethodIndex = reader.ReadUInt32();
            Hash = reader.ReadBytes(20);
            if (CompressionMethodIndex != 0)
            {
                CompressionBlocks = reader.ReadTArray(() => new FPakCompressedBlock(reader));
            }
            Flags = reader.ReadByte();
            CompressionBlockSize = reader.ReadUInt32();

            // Used to seek ahead to the file data instead of parsing the entry again
            StructSize = (int)(reader.BaseStream.Position - StartOffset);
        }

        internal FPakEntry(string pakName, string name, long offset, long size, long uncompressedSize, byte[] hash, FPakCompressedBlock[] compressionBlocks, uint compressionBlockSize, uint compressionMethodIndex, byte flags)
        {
            PakFileName = pakName;
            Name = name;
            Offset = offset;
            Size = size;
            UncompressedSize = uncompressedSize;
            Hash = hash;
            CompressionBlocks = compressionBlocks;
            CompressionBlockSize = compressionBlockSize;
            CompressionMethodIndex = compressionMethodIndex;
            Flags = flags;
            StructSize = (int)GetSize(EPakVersion.LATEST, compressionMethodIndex, compressionBlocks != null ? (uint)compressionBlocks.Length : 0);
        }

        public ArraySegment<byte> GetData(Stream stream, byte[] key)
        {
            lock (stream)
            {
                stream.Position = Offset + StructSize;
                if (Encrypted)
                {
                    var data = new byte[(Size & 15) == 0 ? Size : ((Size / 16) + 1) * 16];
                    stream.Read(data);
                    byte[] decrypted = AESDecryptor.DecryptAES(data, key);

                    if ((ECompressionFlags)CompressionMethodIndex == ECompressionFlags.COMPRESS_ZLIB) // zlib and pray
                        decrypted = ZlibStream.UncompressBuffer(decrypted);
                    return new ArraySegment<byte>(decrypted, 0, (int)UncompressedSize);
                }
                else
                {
                    var data = new byte[UncompressedSize];
                    stream.Read(data);
                    
                    if ((ECompressionFlags)CompressionMethodIndex == ECompressionFlags.COMPRESS_ZLIB) // zlib and pray
                        return new ArraySegment<byte>(ZlibStream.UncompressBuffer(data));
                    else if ((ECompressionFlags)CompressionMethodIndex == ECompressionFlags.COMPRESS_None)
                        return new ArraySegment<byte>(data);
                }
            }
            throw new NotImplementedException("Decompression not yet implemented");
        }

        public static long GetSize(EPakVersion version, uint CompressionMethodIndex = 0, uint CompressionBlocksCount = 0)
        {
            long SerializedSize = sizeof(long) + sizeof(long) + sizeof(long) + 20;

            if (version >= EPakVersion.FNAME_BASED_COMPRESSION_METHOD)
            {
                SerializedSize += sizeof(uint);
            }
            else
            {
                SerializedSize += sizeof(int); // Old CompressedMethod var from pre-fname based compression methods
            }

            if (version >= EPakVersion.COMPRESSION_ENCRYPTION)
            {
                SerializedSize += sizeof(byte) + sizeof(uint);
                if (CompressionMethodIndex != 0)
                {
                    SerializedSize += sizeof(long) * 2 * CompressionBlocksCount + sizeof(int);
                }
            }
            if (version < EPakVersion.NO_TIMESTAMPS)
            {
                // Timestamp
                SerializedSize += sizeof(long);
            }
            return SerializedSize;
        }

        public FPakEntry Uexp = null;
        public FPakEntry Ubulk = null;

        public bool IsUE4Package() => Name.Substring(Name.LastIndexOf(".")).Equals(".uasset");
        public bool IsLocres() => Name.Substring(Name.LastIndexOf(".")).Equals(".locres");
        public bool IsUE4Map() => Name.Substring(Name.LastIndexOf(".")).Equals(".umap");
        public bool IsUE4Font() => Name.Substring(Name.LastIndexOf(".")).Equals(".ufont");

        public bool HasUexp() => Uexp != null;
        public bool HasUbulk() => Ubulk != null;

        public bool IsCompressed() => UncompressedSize != Size || CompressionMethodIndex != (int)ECompressionFlags.COMPRESS_None;
        public string GetExtension() => Name.Substring(Name.LastIndexOf("."));
        public string GetPathWithoutFile()
        {
            int stop = Name.LastIndexOf("/");
            if (stop <= -1)
                stop = 0;
            return Name.Substring(0, stop);
        }
        public string GetPathWithoutExtension() => Name.Substring(0, Name.LastIndexOf("."));
        public string GetNameWithExtension() => Name.Substring(Name.LastIndexOf("/") + 1);
        public string GetNameWithoutExtension()
        {
            int start = Name.LastIndexOf("/") + 1;
            int stop = Name.LastIndexOf(".") - start;
            return Name.Substring(start, stop);
        }

        public override string ToString() => Name;
    }
}
