using System;
using System.IO;
using System.Runtime.InteropServices;
using FProp = FModel.Properties.Settings;

namespace PakReader
{
    internal struct FPakInfo
    {
        public const int PAK_FILE_MAGIC = 0x5A6F12E1;
        public const int MAX_PACKAGE_PATH = 512;

        public int Magic;
        public int Version;
        public long IndexOffset;
        public long IndexSize;
        public byte[] IndexHash; // 20 bytes

        public byte bEncryptedIndex;
        public FGuid EncryptionKeyGuid;

        public static readonly int Size = sizeof(int) * 2 + sizeof(long) * 2 + 20 + 1 + Marshal.SizeOf<FGuid>() + (32 * 5);

        public FPakInfo(BinaryReader reader)
        {
            EncryptionKeyGuid = new FGuid(reader);
            bEncryptedIndex = reader.ReadByte();

            Magic = reader.ReadInt32();
            Version = reader.ReadInt32();
            IndexOffset = reader.ReadInt64();
            IndexSize = reader.ReadInt64();

            IndexHash = reader.ReadBytes(20);

            if (Version < (int)PAK_VERSION.PAK_INDEX_ENCRYPTION)
            {
                bEncryptedIndex = 0;
            }
            if (Version < (int)PAK_VERSION.PAK_ENCRYPTION_KEY_GUID)
            {
                EncryptionKeyGuid = new FGuid();
            }
        }
    }

    public struct FPakFile
    {
        public byte[] data;

        const int EncryptionAlign = 16; // AES-specific constant
        const int EncryptedBufferSize = 256; //?? TODO: check - may be value 16 will be better for performance

        public FPakFile(BinaryReader Reader, FPakEntry Info, byte[] key = null)
        {
            Reader.BaseStream.Seek(Info.Offset + Info.StructSize, SeekOrigin.Begin);
            if (Info.Encrypted)
            {
                long encSize = (Info.Size & 15) == 0 ? Info.Size : ((Info.Size / 16) + 1) * 16;
                byte[] encBuffer = Reader.ReadBytes((int)encSize);
                data = AESDecryptor.DecryptAES(encBuffer, key).SubArray(0, (int)Info.UncompressedSize);
                if (encSize != Info.Size)
                {
                    data = data.SubArray(0, (int)Info.UncompressedSize);
                }
            }
            else
            {
                data = Reader.ReadBytes((int)Info.UncompressedSize);
            }
            //File.WriteAllBytes(Path.GetFileName(info.Name), data);
            /*
            if (info.CompressionMethod != 0)
            {
                Console.WriteLine("compressed");
                while (size > 0)
                {
                    if ((UncompressedBuffer == null) || (ArPos < UncompressedBufferPos) || (ArPos >= UncompressedBufferPos + Info.CompressionBlockSize))
                    {
                        // buffer is not ready
                        if (UncompressedBuffer == null)
                        {
                            UncompressedBuffer = new byte[Info.CompressionBlockSize];
                        }
                        // prepare buffer
                        int BlockIndex = ArPos / Info.CompressionBlockSize;
                        UncompressedBufferPos = Info.CompressionBlockSize * BlockIndex;

                        FPakCompressedBlock Block = Info.CompressionBlocks[BlockIndex];
                        int CompressedBlockSize = (int)(Block.CompressedEnd - Block.CompressedStart);
                        int UncompressedBlockSize = Math.Min(Info.CompressionBlockSize, (int)Info.UncompressedSize - UncompressedBufferPos); // don't pass file end

                        byte[] CompressedData;
                        if (Info.bEncrypted == 0)
                        {
                            Reader.BaseStream.Seek(Block.CompressedStart, SeekOrigin.Begin);
                            CompressedData = Reader.ReadBytes(CompressedBlockSize);
                        }
                        else
                        {
                            int EncryptedSize = Align(CompressedBlockSize, EncryptionAlign);
                            Reader.BaseStream.Seek(Block.CompressedStart, SeekOrigin.Begin);
                            CompressedData = Reader.ReadBytes(EncryptedSize);
                            CompressedData = AESDecryptor.DecryptAES(CompressedData, EncryptedSize, key, key.Length);
                        }
                        // appDecompress(CompressedData, CompressedBlockSize, UncompressedBuffer, UncompressedBlockSize, Info.CompressionMethod);
                        // https://github.com/gildor2/UModel/blob/39641f9e58cb4c286dde5f191e5db9a24b19f503/Unreal/UnCoreCompression.cpp#L183
                        throw new NotImplementedException("Decompressing of files aren't written yet");
                    }

                    // data is in buffer, copy it
                    int BytesToCopy = UncompressedBufferPos + Info.CompressionBlockSize - ArPos; // number of bytes until end of the buffer
                    if (BytesToCopy > size) BytesToCopy = size;
                    if (BytesToCopy <= 0)
                    {
                        throw new ArgumentOutOfRangeException("Bytes to copy is invalid");
                    }

                    // copy uncompressed data
                    int OffsetInBuffer = ArPos - UncompressedBufferPos;
                    Buffer.BlockCopy(UncompressedBuffer, OffsetInBuffer, data, dataOffset, BytesToCopy);

                    // advance pointers

                    // ArPos += BytesToCopy;
                    size -= BytesToCopy;
                    dataOffset += BytesToCopy;
                }
                throw new NotImplementedException("Decompressing of files aren't written yet");
            }
            else if (Info.bEncrypted != 0)
            {
                Console.WriteLine("Encrypted");
                // Uncompressed encrypted data. Reuse compression fields to handle decryption efficiently
                if (UncompressedBuffer == null)
                {
                    UncompressedBuffer = new byte[EncryptedBufferSize];
                    UncompressedBufferPos = 0x40000000; // some invalid value
                }

                while (size > 0)
                {
                    if ((ArPos < UncompressedBufferPos) || (ArPos >= UncompressedBufferPos + EncryptedBufferSize))
                    {
                        // Should fetch block and decrypt it.
                        // Note: AES is block encryption, so we should always align read requests for correct decryption.
                        UncompressedBufferPos = ArPos & ~(EncryptionAlign - 1);
                        Reader.BaseStream.Seek(Info.Pos + Info.StructSize + UncompressedBufferPos, SeekOrigin.Begin);
                        int RemainingSize = (int)Info.Size;
                        if (RemainingSize >= 0)
                        {
                            if (RemainingSize > EncryptedBufferSize)
                                RemainingSize = EncryptedBufferSize;
                            RemainingSize = Align(RemainingSize, EncryptionAlign); // align for AES, pak contains aligned data
                            UncompressedBuffer = Reader.ReadBytes(RemainingSize);
                            UncompressedBuffer = AESDecryptor.DecryptAES(UncompressedBuffer, RemainingSize, key, key.Length);
                        }
                    }

                    // Now copy decrypted data from UncompressedBuffer (code is very similar to those used in decompression above)
                    int BytesToCopy = UncompressedBufferPos + EncryptedBufferSize - ArPos; // number of bytes until end of the buffer
                    if (BytesToCopy > size) BytesToCopy = size;
                    if (BytesToCopy <= 0)
                    {
                        throw new ArgumentOutOfRangeException("Bytes to copy is invalid");
                    }

                    // copy uncompressed data
                    int OffsetInBuffer = ArPos - UncompressedBufferPos;
                    Buffer.BlockCopy(UncompressedBuffer, OffsetInBuffer, data, dataOffset, BytesToCopy);

                    // advance pointers

                    // ArPos += BytesToCopy;
                    size -= BytesToCopy;
                    dataOffset += BytesToCopy;
                }
                throw new NotImplementedException("Decryption of files aren't written yet");
            }
            else
            {
                // Pure data
                // seek every time in a case if the same 'Reader' was used by different FPakFile
                // (this is a lightweight operation for buffered FArchive)
                Reader.BaseStream.Seek(Info.Pos + Info.StructSize, SeekOrigin.Begin);
                data = Reader.ReadBytes(size);
                // ArPos += size;
            }*/
        }

        public Stream GetStream() => new MemoryStream(data);
    }

    public enum PAK_VERSION
    {
        PAK_INITIAL = 1,
        PAK_NO_TIMESTAMPS = 2,
        PAK_COMPRESSION_ENCRYPTION = 3,         // UE4.3+
        PAK_INDEX_ENCRYPTION = 4,               // UE4.17+ - encrypts only pak file index data leaving file content as is
        PAK_RELATIVE_CHUNK_OFFSETS = 5,         // UE4.20+
        PAK_DELETE_RECORDS = 6,                 // UE4.21+ - this constant is not used in UE4 code
        PAK_ENCRYPTION_KEY_GUID = 7,            // ... allows to use multiple encryption keys over the single project
        PAK_FNAME_BASED_COMPRESSION_METHOD = 8, // UE4.22+ - use string instead of enum for compression method
        PAK_FROZEN_INDEX = 9,
        PAK_PATH_HASH_INDEX = 10,

        PAK_LATEST_PLUS_ONE,
        PAK_LATEST = PAK_LATEST_PLUS_ONE - 1
    }

    public enum ECompressionFlags : uint
    {
        /** No compression																*/
        COMPRESS_None = 0x00,
        /** Compress with ZLIB - DEPRECATED, USE FNAME									*/
        COMPRESS_ZLIB = 0x01,
        /** Compress with GZIP - DEPRECATED, USE FNAME									*/
        COMPRESS_GZIP = 0x02,
        /** Compress with user defined callbacks - DEPRECATED, USE FNAME                */
        COMPRESS_Custom = 0x04,
        /** Joint of the previous ones to determine if old flags are being used			*/
        COMPRESS_DeprecatedFormatFlagsMask = 0xF,


        /** No flags specified /														*/
        COMPRESS_NoFlags = 0x00,
        /** Prefer compression that compresses smaller (ONLY VALID FOR COMPRESSION)		*/
        COMPRESS_BiasMemory = 0x10,
        /** Prefer compression that compresses faster (ONLY VALID FOR COMPRESSION)		*/
        COMPRESS_BiasSpeed = 0x20,
        /** Is the source buffer padded out	(ONLY VALID FOR UNCOMPRESS)					*/
        COMPRESS_SourceIsPadded = 0x80,

        /** Set of flags that are options are still allowed								*/
        COMPRESS_OptionsFlagsMask = 0xF0,
    }

    public struct FSHAHash
    {
        public readonly byte[] Hash;

        internal FSHAHash(BinaryReader reader)
        {
            Hash = reader.ReadBytes(20);
        }
    }

    public struct FPakEntry : IEquatable<FPakEntry>
    {
        const byte Flag_Encrypted = 0x01;
        const byte Flag_Deleted = 0x02;

        public bool Encrypted => (Flags & Flag_Encrypted) != 0;
        public bool Deleted => (Flags & Flag_Deleted) != 0;

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

        internal FPakEntry(BinaryReader reader, string mountPoint, PAK_VERSION Version)
        {
            CompressionBlocks = null;
            CompressionBlockSize = 0;
            Flags = 0;

            Name = mountPoint + reader.ReadFString(FPakInfo.MAX_PACKAGE_PATH).Replace(".umap", ".uasset");

            var StartOffset = reader.BaseStream.Position;

            Offset = reader.ReadInt64();
            Size = reader.ReadInt64();
            UncompressedSize = reader.ReadInt64();
            if (Version < PAK_VERSION.PAK_FNAME_BASED_COMPRESSION_METHOD)
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
            if (Version <= PAK_VERSION.PAK_INITIAL)
            {
                // Timestamp of type FDateTime, but the serializer only reads to the Ticks property (int64)
                reader.ReadInt64();
            }
            Hash = reader.ReadBytes(20);
            if (Version >= PAK_VERSION.PAK_COMPRESSION_ENCRYPTION)
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

        internal FPakEntry(string name, long offset, long size, long uncompressedSize, byte[] hash, FPakCompressedBlock[] compressionBlocks, uint compressionBlockSize, uint compressionMethodIndex, byte flags)
        {
            Name = name;
            Offset = offset;
            Size = size;
            UncompressedSize = uncompressedSize;
            Hash = hash;
            CompressionBlocks = compressionBlocks;
            CompressionBlockSize = compressionBlockSize;
            CompressionMethodIndex = compressionMethodIndex;
            Flags = flags;
            StructSize = compressionBlocks != null ? (int)GetSize(PAK_VERSION.PAK_LATEST, compressionMethodIndex, (uint)compressionBlocks.Length) : 0;
        }

        public static long GetSize(PAK_VERSION version, uint CompressionMethodIndex = 0, uint CompressionBlocksCount = 0)
        {
            long SerializedSize = sizeof(long) + sizeof(long) + sizeof(long) + 20;

            if (version >= PAK_VERSION.PAK_FNAME_BASED_COMPRESSION_METHOD)
            {
                SerializedSize += sizeof(uint);
            }
            else
            {
                SerializedSize += sizeof(int); // Old CompressedMethod var from pre-fname based compression methods
            }

            if (version >= PAK_VERSION.PAK_COMPRESSION_ENCRYPTION)
            {
                SerializedSize += sizeof(byte) + sizeof(uint);
                if (CompressionMethodIndex != 0)
                {
                    SerializedSize += sizeof(long) * 2 * CompressionBlocksCount + sizeof(int);
                }
            }
            if (version < PAK_VERSION.PAK_NO_TIMESTAMPS)
            {
                // Timestamp
                SerializedSize += sizeof(long);
            }
            return SerializedSize;
        }

        public bool Equals(FPakEntry other)
        {
            if (other.GetType() != typeof(FPakEntry))
                return false;

            return FProp.Default.FDiffFileSize ? this.Name == other.Name && this.UncompressedSize == other.UncompressedSize : this.Name == other.Name;
        }
        public override bool Equals(object obj) => Equals((FPakEntry)obj);

        public override int GetHashCode() => FProp.Default.FDiffFileSize ? (Name, UncompressedSize).GetHashCode() : (Name).GetHashCode();
    }

    public struct FPathHashIndexEntry
    {
        public string Filename { get; }

        public int Location { get; }

        public FPathHashIndexEntry(BinaryReader reader)
        {
            Filename = reader.ReadFString();
            Location = reader.ReadInt32();
        }
    }

    public struct FPakDirectoryEntry
    {
        public string Directory { get; }

        public FPathHashIndexEntry[] Entries { get; }

        public FPakDirectoryEntry(BinaryReader reader)
        {
            Directory = reader.ReadFString();
            Entries = reader.ReadTArray(() => new FPathHashIndexEntry(reader));
        }
    }

    public struct FPakCompressedBlock
    {
        public long CompressedStart;
        public long CompressedEnd;

        public FPakCompressedBlock(BinaryReader reader)
        {
            CompressedStart = reader.ReadInt64();
            CompressedEnd = reader.ReadInt64();
        }
        public FPakCompressedBlock(long compressedStart, long compressedEnd)
        {
            CompressedStart = compressedStart;
            CompressedEnd = compressedEnd;
        }
    }

    internal struct FString
    {
        public string str;

        public FString(string str)
        {
            this.str = str;
        }

        public FString(BinaryReader reader)
        {
            str = AssetReader.read_string(reader);
        }
    }

    internal struct FCustomVersion
    {
        FGuid Key;
        int Version;

        public FCustomVersion(BinaryReader reader)
	    {
            Key = new FGuid(reader);
            Version = reader.ReadInt32();
	    }
    }

    internal struct FEnumCustomVersion
    {
        int Tag;
        int Version;

        public FEnumCustomVersion(BinaryReader reader)
        {
            Tag = reader.ReadInt32();
            Version = reader.ReadInt32();
        }
    }

    internal struct FGuidCustomVersion
    {
        FGuid Key;
        int Version;
        string FriendlyName;

        public FGuidCustomVersion(BinaryReader reader)
        {
            Key = new FGuid(reader);
            Version = reader.ReadInt32();
            FriendlyName = reader.ReadString();
        }
    }

    internal struct FCustomVersionContainer
    {
        FCustomVersion[] Versions;

        public FCustomVersionContainer(BinaryReader reader, int LegacyVersion)
        {
            if (LegacyVersion == -2)
            {
                // Before 4.0 release: ECustomVersionSerializationFormat::Enums in Core/Private/Serialization/CustomVersion.cpp
                FEnumCustomVersion[] VersionsEnum = reader.ReadTArray(() => new FEnumCustomVersion(reader));
                Versions = null;
            }
            else if (LegacyVersion < -2 && LegacyVersion >= -5)
            {
                // 4.0 .. 4.10: ECustomVersionSerializationFormat::Guids
                FGuidCustomVersion[] VersionsGuid = reader.ReadTArray(() => new FGuidCustomVersion(reader));
                Versions = null;
            }
            else
            {
                // Starting with 4.11: ECustomVersionSerializationFormat::Optimized
                Versions = reader.ReadTArray(() => new FCustomVersion(reader));;
            }
        }
    }

    internal struct FEngineVersion
    {
        ushort Major, Minor, Patch;
        int Changelist;
        string Branch;

        public FEngineVersion(BinaryReader reader)
	    {
            Major = reader.ReadUInt16();
            Minor = reader.ReadUInt16();
            Patch = reader.ReadUInt16();
            Changelist = reader.ReadInt32();
            Branch = reader.ReadString();
	    }
    }

    internal struct FGenerationInfo
    {
        int ExportCount, NameCount;
        int NetObjectCount;

        public FGenerationInfo(BinaryReader reader, int Version)
        {
            ExportCount = reader.ReadInt32();
            NameCount = reader.ReadInt32();
            if (Version >= FPackageFileSummary.VER_UE4_REMOVE_NET_INDEX)
            {
                NetObjectCount = 0;
                return;
            }
            if (Version >= 322) // PACKAGE_V3
            {
                NetObjectCount = reader.ReadInt32();
            }
            else
            {
                NetObjectCount = 0;
            }
        }
    }

    internal struct FCompressedChunk
    {
        int UncompressedOffset, UncompressedSize, CompressedOffset, CompressedSize;

        public FCompressedChunk(BinaryReader reader)
        {
            UncompressedOffset = reader.ReadInt32();
            UncompressedSize = reader.ReadInt32();
            CompressedOffset = reader.ReadInt32();
            CompressedSize = reader.ReadInt32();
        }
    }

    /*sealed class CGameFileInfo
    {
        public string RelativeName;
        public string ShortFilename;
        public string Extension;

        public PakReader FileSystem;
        //public UPackage Package;

        public long Size;
        public int SizeInKb;
        public int ExtraSizeInKb;
        public bool IsPackage;

        // content information, valid when PackageScanned is true
        public bool PackageScanned;
        public ushort NumSkeletalMeshes;
        public ushort NumStaticMeshes;
        public ushort NumAnimations;
        public ushort NumTextures;
    }*/

    internal sealed class FPackageFileSummary
    {
        uint Tag;

        int LegacyVersion;
        bool IsUnversioned;
        FCustomVersionContainer CustomVersionContainer;

        ushort FileVersion;
        ushort LicenseeVersion;
        int PackageFlags;
        int NameCount, NameOffset;
        int ExportCount, ExportOffset;
        int ImportCount, ImportOffset;
        FGuid Guid;
        FGenerationInfo[] Generations;

        int HeadersSize;      // used by UE3 for precaching name table
        string PackageGroup;       // "None" or directory name
        int DependsOffset;        // number of items = ExportCount
        /*
        int f38;
        int f3C;
        int f40;
        int EngineVersion;
        int CookerVersion;
        */
        int CompressionFlags;
        FCompressedChunk[] CompressedChunks;
        // int U3unk60;

        long BulkDataStartOffset;

        // bool ReverseBytes;
        int Version;

        static readonly int[] legacyVerToEngineVer =
        {
            -1,		// -1
		    -1,		// -2 -> older than UE4.0, no mapping
		    0,		// -3 -> UE4.0
		    7,		// -4 -> UE4.7
		    7,		// -5 ...
		    11,		// -6
		    14,		// -7
		    // add new versions above this line
		    LATEST_SUPPORTED_UE4_VERSION+1 // this line here is just to make code below simpler
	    };
        static readonly int LatestSupportedLegacyVer = -legacyVerToEngineVer.Length + 1;

        const int GAME_UE4_BASE = 0x1000000;
        const int LATEST_SUPPORTED_UE4_VERSION = 22;

        const int VER_UE4_ADDED_PACKAGE_SUMMARY_LOCALIZATION_ID = 516;
        const int VER_UE4_SERIALIZE_TEXT_IN_PACKAGES = 459;
        const int VER_UE4_ADD_STRING_ASSET_REFERENCES_MAP = 384;
        const int VER_UE4_ADDED_SEARCHABLE_NAMES = 510;
        const int VER_UE4_ENGINE_VERSION_OBJECT = 336;
        const int VER_UE4_PACKAGE_SUMMARY_HAS_COMPATIBLE_ENGINE_VERSION = 444;
        const int VER_UE4_ASSET_REGISTRY_TAGS = 112;
        const int VER_UE4_SUMMARY_HAS_BULKDATA_OFFSET = 212;
        public const int VER_UE4_REMOVE_NET_INDEX = 196;

        const uint PACKAGE_FILE_TAG = 0x9E2A83C1;
        const uint PACKAGE_FILE_TAG_REV = 0xC1832A9E;

        public FPackageFileSummary(BinaryReader reader)
        {
            Tag = reader.ReadUInt32();

            // support reverse byte order
            if (Tag == PACKAGE_FILE_TAG)
            {
                // ReverseBytes = false;
            }
            else
            {
                if (Tag != PACKAGE_FILE_TAG_REV)
                {
                    throw new IOException("Wrong package tag in file. Probably the file is encrypted.");
                }
                // ReverseBytes = true;
                Tag = PACKAGE_FILE_TAG;
            }

            Version = reader.ReadInt32();

            // UE4 has negative version value, growing from -1 towards negative direction. This value is followed
            // by "UE3 Version", "UE4 Version" and "Licensee Version" (parsed in SerializePackageFileSummary4).
            // The value is used as some version for package header, and it's not changed frequently. We can't
            // expect these values to have large values in the future. The code below checks this value for
            // being less than zero, but allows UE1-UE3 LicenseeVersion up to 32767.
            if ((Version & 0xFFFFF000) == 0xFFFFF000)
            {
                LegacyVersion = Version;
                SerializePackageFileSummary4(reader);
                //!! note: UE4 requires different DetectGame way, perhaps it's not possible at all
                //!! (but can use PAK file names for game detection)
            }
        }

        void SerializePackageFileSummary4(BinaryReader reader)
        {
            if (LegacyVersion < LatestSupportedLegacyVer || LegacyVersion >= -1) // -2 is supported
            {
                throw new NotImplementedException("Unsuported version: " + LegacyVersion);
            }

            IsUnversioned = false;

            // read versions
            int VersionUE3, Version, LicenseeVersion; // note: using int32 instead of uint16 as in UE1-UE3
            if (LegacyVersion != -4) // UE4 had some changes for version -4, but these changes were reverted in -5 due to some problems
            {
                VersionUE3 = reader.ReadInt32();
            }
            Version = reader.ReadInt32();
            LicenseeVersion = reader.ReadInt32();
            // VersionUE3 is ignored
            if ((Version & ~0xFFFF) != 0 || (LicenseeVersion & ~0xFFFF) != 0)
            {
                throw new IOException("Invalid Version or Licensee Version.");
            }
            FileVersion = unchecked((ushort)(Version & 0xFFFF));
            this.LicenseeVersion = unchecked((ushort)(LicenseeVersion & 0xFFFF));

            if (FileVersion == 0 && LicenseeVersion == 0)
            {
                IsUnversioned = true;
            }

            if (IsUnversioned)
            {
                int ver = -LegacyVersion - 1;
                int verMin = legacyVerToEngineVer[ver];
                int verMax = legacyVerToEngineVer[ver + 1] - 1;
                int selectedVersion;
                if (verMax < verMin)
                {
                    // if LegacyVersion exactly matches single engine version, don't show any UI
                    selectedVersion = verMin;
                }
                else
                {
                    // display UI if it is supported
                    selectedVersion = GAME_UE4_BASE + (22 << 4); // 4.22
                    if (!(selectedVersion >= 0 && selectedVersion <= LATEST_SUPPORTED_UE4_VERSION))
                    {
                        throw new InvalidOperationException("Invalid version");
                    }
                }
            }

            if (LegacyVersion <= -2)
            {
                // CustomVersions array - not serialized to unversioned packages, and UE4 always consider
                // all custom versions to use highest available value. However this is used for versioned
                // packages: engine starts to use custom versions heavily starting with 4.12.
                CustomVersionContainer = new FCustomVersionContainer(reader, LegacyVersion);
            }

            HeadersSize = reader.ReadInt32();
            PackageGroup = reader.ReadString();
            PackageFlags = reader.ReadInt32();

            NameCount = reader.ReadInt32();
            NameOffset = reader.ReadInt32();

            if (this.Version >= VER_UE4_ADDED_PACKAGE_SUMMARY_LOCALIZATION_ID) // also contains editor data but idk
            {
                string LocalizationId = reader.ReadString();
            }

            if (this.Version >= VER_UE4_SERIALIZE_TEXT_IN_PACKAGES)
            {
                int GatherableTextDataCount, GatherableTextDataOffset;
                GatherableTextDataCount = reader.ReadInt32();
                GatherableTextDataOffset = reader.ReadInt32();
            }

            ExportCount = reader.ReadInt32();
            ExportOffset = reader.ReadInt32();
            ImportCount = reader.ReadInt32();
            ImportOffset = reader.ReadInt32();
            DependsOffset = reader.ReadInt32();

            if (this.Version >= VER_UE4_ADD_STRING_ASSET_REFERENCES_MAP)
            {
                int StringAssetReferencesCount, StringAssetReferencesOffset;
                StringAssetReferencesCount = reader.ReadInt32();
                StringAssetReferencesOffset = reader.ReadInt32();
            }

            if (this.Version >= VER_UE4_ADDED_SEARCHABLE_NAMES)
            {
                int SearchableNamesoffset;
                SearchableNamesoffset = reader.ReadInt32();
            }

            // there's a thumbnail table in source packages with following layout
            // * package headers
            // * thumbnail data - sequence of FObjectThumbnail
            // * thumbnail metadata - array of small headers pointed to 'thumbnail data' objects
            //   (this metadata is what FPackageFileSummary::ThumbnailTableOffset points to)
            int ThumbnailTableOffset = reader.ReadInt32();

            // guid and generations
            Guid = new FGuid(reader);
            int Count = reader.ReadInt32();

            Generations = new FGenerationInfo[Count];
            for(int i = 0; i < Count; i++)
            {
                Generations[i] = new FGenerationInfo(reader, this.Version);
            }

            // engine version
            if (this.Version >= VER_UE4_ENGINE_VERSION_OBJECT)
            {
                FEngineVersion engineVersion; // empty for cooked packages, so don't store it anywhere ...
                engineVersion = new FEngineVersion(reader);
            }
            else
            {
                int changelist = reader.ReadInt32();
            }

            if (this.Version >= VER_UE4_PACKAGE_SUMMARY_HAS_COMPATIBLE_ENGINE_VERSION)
            {
                FEngineVersion CompatibleVersion = new FEngineVersion(reader);
            }

            // compression structures
            CompressionFlags = reader.ReadInt32();
            CompressedChunks = reader.ReadTArray(() => new FCompressedChunk(reader));

            int PackageSource = reader.ReadInt32();

            string[] AdditionalPackagesToCook = reader.ReadTArray(() => reader.ReadString());

            if (LegacyVersion > -7)
            {
                int NumTextureAllocations = reader.ReadInt32();
                if (NumTextureAllocations != 0) // actually this was an array before
                {
                    throw new IOException("Array length must be 0");
                }
            }

            if (this.Version >= VER_UE4_ASSET_REGISTRY_TAGS)
            {
                int AssetRegistryDataOffset = reader.ReadInt32();
            }

            if (this.Version >= VER_UE4_SUMMARY_HAS_BULKDATA_OFFSET)
            {
                BulkDataStartOffset = reader.ReadInt64();
            }

            //!! other fields - useless for now
        }
    }
}