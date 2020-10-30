using System;
using System.IO;
using System.Runtime.CompilerServices;
using Ionic.Zlib;

namespace FModel.PakReader.Parsers.Objects
{
    public class FPakEntry : ReaderEntry
    {
        const byte Flag_Encrypted = 0x01;
        const byte Flag_Deleted = 0x02;

        public override bool Encrypted => (Flags & Flag_Encrypted) != 0;
        public bool Deleted => (Flags & Flag_Deleted) != 0;

        public override string ContainerName { get; }
        public override string Name => _name;
        private readonly string _name;
        public readonly long Offset;
        public override long Size => _size;
        private readonly long _size;
        public override long UncompressedSize => _uncompressedSize;
        private readonly long _uncompressedSize;
        public readonly FPakCompressedBlock[] CompressionBlocks;
        public readonly uint CompressionBlockSize;
        public override uint CompressionMethodIndex => _compressionMethodIndex;
        private readonly uint _compressionMethodIndex;
        public readonly byte Flags;

        public override int StructSize => _structSize;
        private readonly int _structSize;

        internal FPakEntry(BinaryReader reader, EPakVersion Version, int SubVersion, bool caseSensitive, string pakName)
        {
            CompressionBlocks = null;
            CompressionBlockSize = 0;
            Flags = 0;

            ContainerName = pakName;
            string name = caseSensitive ? reader.ReadFString() : reader.ReadFString().ToLowerInvariant();
            _name = name.StartsWith("/") ? name[1..] : name;

            var StartOffset = reader.BaseStream.Position;

            Offset = reader.ReadInt64();
            _size = reader.ReadInt64();
            _uncompressedSize = reader.ReadInt64();
            if (Version < EPakVersion.FNAME_BASED_COMPRESSION_METHOD)
            {
                var LegacyCompressionMethod = reader.ReadInt32();
                if (LegacyCompressionMethod == (int)ECompressionFlags.COMPRESS_None)
                {
                    _compressionMethodIndex = 0;
                }
                else if ((LegacyCompressionMethod & (int)ECompressionFlags.COMPRESS_ZLIB) != 0)
                {
                    _compressionMethodIndex = 1;
                }
                else if ((LegacyCompressionMethod & (int)ECompressionFlags.COMPRESS_GZIP) != 0)
                {
                    _compressionMethodIndex = 2;
                }
                else if ((LegacyCompressionMethod & (int)ECompressionFlags.COMPRESS_Custom) != 0)
                {
                    _compressionMethodIndex = 3;
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
                    _compressionMethodIndex = reader.ReadByte();
                else
                    _compressionMethodIndex = reader.ReadUInt32();
            }
            if (Version < EPakVersion.NO_TIMESTAMPS) reader.ReadInt64(); // Timestamp
            reader.ReadBytes(20); // Hash
            if (Version >= EPakVersion.COMPRESSION_ENCRYPTION)
            {
                if (_compressionMethodIndex != 0)
                {
                    CompressionBlocks = reader.ReadTArray(() => new FPakCompressedBlock(reader));
                }

                Flags = reader.ReadByte();
                CompressionBlockSize = reader.ReadUInt32();
            }

            // Used to seek ahead to the file data instead of parsing the entry again
            _structSize = (int)(reader.BaseStream.Position - StartOffset);
        }

        internal FPakEntry(BinaryReader reader, bool caseSensitive, string pakName)
        {
            CompressionBlocks = null;
            CompressionBlockSize = 0;
            Flags = 0;

            ContainerName = pakName;
            _name = caseSensitive ? reader.ReadFString() : reader.ReadFString().ToLowerInvariant();

            var StartOffset = reader.BaseStream.Position;

            Offset = reader.ReadInt64();
            _size = reader.ReadInt64();
            _uncompressedSize = reader.ReadInt64();
            _compressionMethodIndex = reader.ReadUInt32();
            reader.ReadBytes(20); // Hash
            if (_compressionMethodIndex != 0)
            {
                CompressionBlocks = reader.ReadTArray(() => new FPakCompressedBlock(reader));
            }
            Flags = reader.ReadByte();
            CompressionBlockSize = reader.ReadUInt32();

            // Used to seek ahead to the file data instead of parsing the entry again
            _structSize = (int)(reader.BaseStream.Position - StartOffset);
        }

        internal FPakEntry(string pakName, string name, long offset, long size, long uncompressedSize, FPakCompressedBlock[] compressionBlocks, uint compressionBlockSize, uint compressionMethodIndex, byte flags)
        {
            ContainerName = pakName;
            _name = name;
            Offset = offset;
            _size = size;
            _uncompressedSize = uncompressedSize;
            CompressionBlocks = compressionBlocks;
            CompressionBlockSize = compressionBlockSize;
            _compressionMethodIndex = compressionMethodIndex;
            Flags = flags;
            _structSize = (int)GetSize(EPakVersion.LATEST, compressionMethodIndex, compressionBlocks != null ? (uint)compressionBlocks.Length : 0);
        }

        public ArraySegment<byte> GetData(Stream stream, byte[] key, string[] compressionMethods)
        {
            lock (stream)
            {
                if (_compressionMethodIndex == 0U)
                {
                    stream.Position = Offset + _structSize;
                    if (Encrypted)
                    {
                        var data = new byte[(_size & 15) == 0 ? _size : (_size / 16 + 1) * 16];
                        stream.Read(data, 0, data.Length);
                        return new ArraySegment<byte>(AESDecryptor.DecryptAES(data, key), 0, (int)_uncompressedSize);
                    }
                    else
                    {
                        var data = new byte[_uncompressedSize];
                        stream.Read(data, 0, data.Length);
                        return new ArraySegment<byte>(data);
                    }
                }
                else
                {
                    var data = new byte[_uncompressedSize];
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

            string compressionMethod = compressionMethods[_compressionMethodIndex - 1]; // -1 because we dont have 'NAME_None' in the array
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsCompressed() => _uncompressedSize != _size || _compressionMethodIndex != (int)ECompressionFlags.COMPRESS_None;
    }
}
