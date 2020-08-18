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
        public readonly FPakCompressedBlock[] CompressionBlocks;
        public readonly uint CompressionBlockSize;
        public readonly uint CompressionMethodIndex;
        public readonly byte Flags;

        public readonly int StructSize;

        internal FPakEntry(BinaryReader reader, EPakVersion Version, int SubVersion, bool caseSensitive, string pakName)
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
                if (Version == EPakVersion.FNAME_BASED_COMPRESSION_METHOD && SubVersion == 0)
                    CompressionMethodIndex = reader.ReadByte();
                else
                    CompressionMethodIndex = reader.ReadUInt32();
            }
            if (Version < EPakVersion.NO_TIMESTAMPS) reader.ReadInt64(); // Timestamp
            reader.ReadBytes(20); // Hash
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
            reader.ReadBytes(20); // Hash
            if (CompressionMethodIndex != 0)
            {
                CompressionBlocks = reader.ReadTArray(() => new FPakCompressedBlock(reader));
            }
            Flags = reader.ReadByte();
            CompressionBlockSize = reader.ReadUInt32();

            // Used to seek ahead to the file data instead of parsing the entry again
            StructSize = (int)(reader.BaseStream.Position - StartOffset);
        }

        internal FPakEntry(string pakName, string name, long offset, long size, long uncompressedSize, FPakCompressedBlock[] compressionBlocks, uint compressionBlockSize, uint compressionMethodIndex, byte flags)
        {
            PakFileName = pakName;
            Name = name;
            Offset = offset;
            Size = size;
            UncompressedSize = uncompressedSize;
            CompressionBlocks = compressionBlocks;
            CompressionBlockSize = compressionBlockSize;
            CompressionMethodIndex = compressionMethodIndex;
            Flags = flags;
            StructSize = (int)GetSize(EPakVersion.LATEST, compressionMethodIndex, compressionBlocks != null ? (uint)compressionBlocks.Length : 0);
        }

        public ArraySegment<byte> GetData(Stream stream, byte[] key, string[] compressionMethods)
        {
            lock (stream)
            {
                if (CompressionMethodIndex == 0U)
                {
                    stream.Position = Offset + StructSize;
                    if (Encrypted)
                    {
                        var data = new byte[(Size & 15) == 0 ? Size : (Size / 16 + 1) * 16];
                        stream.Read(data, 0, data.Length);
                        return new ArraySegment<byte>(AESDecryptor.DecryptAES(data, key), 0, (int)UncompressedSize);
                    }
                    else
                    {
                        var data = new byte[UncompressedSize];
                        stream.Read(data, 0, data.Length);
                        return new ArraySegment<byte>(data);
                    }
                }
                else
                {
                    var data = new byte[UncompressedSize];
                    Decompress(stream, key, compressionMethods, data);
                    return new ArraySegment<byte>(data);
                }
            }
            throw new NotImplementedException("Decompression not yet implemented");
        }

        private void Decompress(Stream stream, byte[] key, string[] compressionMethods, byte[] outData)
        {
            if (compressionMethods == null || compressionMethods.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(compressionMethods), "CompressionMethods are null or empty");

            string compressionMethod = compressionMethods[CompressionMethodIndex - 1]; // -1 because we dont have 'NAME_None' in the array
            int bytesRead = 0;
            for (int i = 0; i < CompressionBlocks.Length; i++)
            {
                stream.Position = Offset + CompressionBlocks[i].CompressedStart;
                int uncompressedSize = (int)Math.Min(CompressionBlockSize, outData.Length - bytesRead);

                byte[] blockBbuffer;
                if (Encrypted)
                {
                    blockBbuffer = new byte[BinaryHelper.Align(CompressionBlocks[i].Size, AESDecryptor.BLOCK_SIZE)];
                    stream.Read(blockBbuffer, 0, blockBbuffer.Length);
                    blockBbuffer = AESDecryptor.DecryptAES(blockBbuffer, key);
                }
                else
                {
                    blockBbuffer = new byte[CompressionBlocks[i].Size];
                    stream.Read(blockBbuffer, 0, blockBbuffer.Length);
                }

                using var blockMs = new MemoryStream(blockBbuffer, false);
                using Stream compressionStream = compressionMethod switch
                {
                    "Zlib" => new ZlibStream(blockMs, CompressionMode.Decompress),
                    "Gzip" => new GZipStream(blockMs, CompressionMode.Decompress),
                    "Oodle" => new OodleStream(blockBbuffer, uncompressedSize),
                    _ => throw new NotImplementedException($"Decompression not yet implemented ({compressionMethod})")
                };

                bytesRead += compressionStream.Read(outData, bytesRead, uncompressedSize);
            }
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
        public string GetFirstFolder() => Name.Substring(Name.StartsWith('/') ? 1 : 0, Name.IndexOf('/'));

        public override string ToString() => Name;
    }
}
