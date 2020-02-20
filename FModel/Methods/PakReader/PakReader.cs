using FModel.Methods.Utilities;
using System;
using System.Collections.Generic;
using System.IO;

namespace PakReader
{
    public class PakReader
    {
        readonly Stream Stream;
        readonly BinaryReader Reader;
        readonly byte[] Aes;
        public readonly string MountPoint;
        public FPakEntry[] FileInfos;
        public readonly string Name;

        public PakReader(string file, byte[] aes = null, bool ParseFiles = true) : this(File.OpenRead(file), file, aes, ParseFiles) { }

        public PakReader(Stream stream, string name, byte[] aes = null, bool ParseFiles = true)
        {
            Aes = aes;
            Stream = stream;
            Name = name;
            Reader = new BinaryReader(Stream);

            Stream.Seek(-FPakInfo.Size, SeekOrigin.End);

            FPakInfo info = new FPakInfo(Reader);
            if (info.Magic != FPakInfo.PAK_FILE_MAGIC)
            {
                DebugHelper.WriteLine(".PAKs: The file magic is invalid");
                throw new FileLoadException("The file magic is invalid");
            }

            if (info.Version > (int)PAK_VERSION.PAK_LATEST)
            {
                DebugHelper.WriteLine($".PAKs: WARNING: Pak file \"{Name}\" has unsupported version {info.Version}");
            }

            if (info.bEncryptedIndex != 0)
            {
                if (Aes == null)
                {
                    DebugHelper.WriteLine(".PAKs: The file has an encrypted index");
                    throw new FileLoadException("The file has an encrypted index");
                }
            }

            // Read pak index

            Stream.Seek(info.IndexOffset, SeekOrigin.Begin);

            // Manage pak files with encrypted index
            BinaryReader infoReader = Reader;

            if (info.bEncryptedIndex != 0)
            {
                var InfoBlock = Reader.ReadBytes((int)info.IndexSize);
                InfoBlock = AESDecryptor.DecryptAES(InfoBlock, Aes);

                infoReader = new BinaryReader(new MemoryStream(InfoBlock));
                int stringLen = infoReader.ReadInt32();
                if (stringLen > 512 || stringLen < -512)
                {
                    DebugHelper.WriteLine(".PAKs: The AES key is invalid");
                    throw new FileLoadException("The AES key is invalid");
                }
                if (stringLen < 0)
                {
                    infoReader.BaseStream.Seek((stringLen - 1) * 2, SeekOrigin.Current);
                    ushort c = infoReader.ReadUInt16();
                    if (c != 0)
                    {
                        DebugHelper.WriteLine(".PAKs: The AES key is invalid");
                        throw new FileLoadException("The AES key is invalid");
                    }
                }
                else
                {
                    infoReader.BaseStream.Seek(stringLen - 1, SeekOrigin.Current);
                    byte c = infoReader.ReadByte();
                    if (c != 0)
                    {
                        DebugHelper.WriteLine(".PAKs: The AES key is invalid");
                        throw new FileLoadException("The AES key is invalid");
                    }
                }
            }

            if (!ParseFiles) return;

            // Pak index reading time :)
            infoReader.BaseStream.Seek(0, SeekOrigin.Begin);
            MountPoint = infoReader.ReadFString(FPakInfo.MAX_PACKAGE_PATH);
            bool badMountPoint = false;
            if (!MountPoint.StartsWith("../../.."))
            {
                badMountPoint = true;
            }
            else
            {
                MountPoint = MountPoint.Substring(8);
            }
            if (MountPoint[0] != '/' || ((MountPoint.Length > 1) && (MountPoint[1] == '.')))
            {
                badMountPoint = true;
            }

            if (badMountPoint)
            {
                DebugHelper.WriteLine($".PAKs: WARNING: Pak \"{Name}\" has strange mount point \"{MountPoint}\", mounting to root");
                MountPoint = "/";
            }

            if (info.Version >= (int)PAK_VERSION.PAK_PATH_HASH_INDEX)
            {
                ReadIndexUpdated(infoReader, MountPoint, info, Aes, Stream.Length);
            }
            else
            {
                FileInfos = new FPakEntry[infoReader.ReadInt32()];
                for (int i = 0; i < FileInfos.Length; i++)
                {
                    FileInfos[i] = new FPakEntry(infoReader, MountPoint, (PAK_VERSION)info.Version);
                }
            }
        }

        void ReadIndexUpdated(BinaryReader reader, string mountPoint, FPakInfo info, byte[] key, long totalSize)
        {
            int NumEntries = reader.ReadInt32();
            ulong PathHashSeed = reader.ReadUInt64();

            bool bReaderHasPathHashIndex = false;
            long PathHashIndexOffset = -1; // INDEX_NONE
            long PathHashIndexSize = 0;
            FSHAHash PathHashIndexHash = default;
            bReaderHasPathHashIndex = reader.ReadInt32() != 0;
            if (bReaderHasPathHashIndex)
            {
                PathHashIndexOffset = reader.ReadInt64();
                PathHashIndexSize = reader.ReadInt64();
                PathHashIndexHash = new FSHAHash(reader);
                bReaderHasPathHashIndex = bReaderHasPathHashIndex && PathHashIndexOffset != -1;
            }

            bool bReaderHasFullDirectoryIndex = false;
            long FullDirectoryIndexOffset = -1; // INDEX_NONE
            long FullDirectoryIndexSize = 0;
            FSHAHash FullDirectoryIndexHash = default;
            bReaderHasFullDirectoryIndex = reader.ReadInt32() != 0;
            if (bReaderHasFullDirectoryIndex)
            {
                FullDirectoryIndexOffset = reader.ReadInt64();
                FullDirectoryIndexSize = reader.ReadInt64();
                FullDirectoryIndexHash = new FSHAHash(reader);
                bReaderHasFullDirectoryIndex = bReaderHasFullDirectoryIndex && FullDirectoryIndexOffset != -1;
            }

            byte[] EncodedPakEntries = reader.ReadTArray(() => reader.ReadByte());

            int FilesNum = reader.ReadInt32();
            if (FilesNum < 0)
                // Should not be possible for any values in the PrimaryIndex to be invalid, since we verified the index hash
                throw new FileLoadException("Corrupt pak PrimaryIndex detected!");

            FPakEntry[] Files = new FPakEntry[FilesNum]; // from what i can see, there aren't any???
            if (FilesNum > 0)
                for (int FileIndex = 0; FileIndex < FilesNum; ++FileIndex)
                    Files[FileIndex] = new FPakEntry(reader, mountPoint, (PAK_VERSION)info.Version);

            // Decide which SecondaryIndex(es) to load
            bool bWillUseFullDirectoryIndex;
            bool bWillUsePathHashIndex;
            bool bReadFullDirectoryIndex;
            if (bReaderHasPathHashIndex && bReaderHasFullDirectoryIndex)
            {
                bWillUseFullDirectoryIndex = false;
                bWillUsePathHashIndex = !bWillUseFullDirectoryIndex;
                bool bWantToReadFullDirectoryIndex = false;
                bReadFullDirectoryIndex = bReaderHasFullDirectoryIndex && bWantToReadFullDirectoryIndex;
            }
            else if (bReaderHasPathHashIndex)
            {
                bWillUsePathHashIndex = true;
                bWillUseFullDirectoryIndex = false;
                bReadFullDirectoryIndex = false;
            }
            else if (bReaderHasFullDirectoryIndex)
            {
                // We don't support creating the PathHash Index at runtime; we want to move to having only the PathHashIndex, so supporting not having it at all is not useful enough to write
                bWillUsePathHashIndex = false;
                bWillUseFullDirectoryIndex = true;
                bReadFullDirectoryIndex = true;
            }
            else
                // It should not be possible for PrimaryIndexes to be built without a PathHashIndex AND without a FullDirectoryIndex; CreatePakFile in UnrealPak.exe has a check statement for it.
                throw new FileLoadException("Corrupt pak PrimaryIndex detected!");

            // Load the Secondary Index(es)
            byte[] PathHashIndexData;
            Dictionary<ulong, int> PathHashIndex;
            BinaryReader PathHashIndexReader = default;
            if (bWillUsePathHashIndex)
            {
                if (PathHashIndexOffset < 0 || totalSize < (PathHashIndexOffset + PathHashIndexSize))
                    // Should not be possible for these values (which came from the PrimaryIndex) to be invalid, since we verified the index hash of the PrimaryIndex
                    throw new FileLoadException("Corrupt pak PrimaryIndex detected!");

                Reader.BaseStream.Position = PathHashIndexOffset;
                PathHashIndexData = Reader.ReadBytes((int)PathHashIndexSize);
                {
                    if (!DecryptAndValidateIndex(info.bEncryptedIndex != 0, ref PathHashIndexData, key, PathHashIndexHash, out var ComputedHash))
                        throw new FileLoadException("Corrupt pak PrimaryIndex detected!");
                }

                PathHashIndexReader = new BinaryReader(new MemoryStream(PathHashIndexData));
                PathHashIndex = ReadPathHashIndex(PathHashIndexReader);
            }

            var DirectoryIndex = new Dictionary<string, Dictionary<string, int>>();
            if (!bReadFullDirectoryIndex)
            {
                DirectoryIndex = ReadDirectoryIndex(PathHashIndexReader);
            }
            if (DirectoryIndex.Count == 0)
            {
                if (totalSize < (FullDirectoryIndexOffset + FullDirectoryIndexSize) || FullDirectoryIndexOffset < 0)
                    throw new FileLoadException("Corrupt pak PrimaryIndex detected!");
                Reader.BaseStream.Position = FullDirectoryIndexOffset;
                byte[] FullDirectoryIndexData = Reader.ReadBytes((int)FullDirectoryIndexSize);

                {
                    if (!DecryptAndValidateIndex(info.bEncryptedIndex != 0, ref FullDirectoryIndexData, key, FullDirectoryIndexHash, out var ComputedHash))
                        throw new FileLoadException("Corrupt pak PrimaryIndex detected!");
                }

                var SecondaryIndexReader = new BinaryReader(new MemoryStream(FullDirectoryIndexData));
                DirectoryIndex = ReadDirectoryIndex(SecondaryIndexReader);
            }

            var entries = new List<FPakEntry>(NumEntries);
            foreach (var stringDict in DirectoryIndex)
            {
                foreach (var stringInt in stringDict.Value)
                {
                    string path = stringDict.Key + stringInt.Key;
                    FPakEntry entry = GetEntry(mountPoint + path, stringInt.Value, EncodedPakEntries);
                    entries.Add(entry);
                }
            }
            this.FileInfos = entries.ToArray();
        }

        FPakEntry GetEntry(string name, int pakLocation, byte[] encodedPakEntries)
        {
            if (pakLocation >= 0)
            {
                // Grab the big bitfield value:
                // Bit 31 = Offset 32-bit safe?
                // Bit 30 = Uncompressed size 32-bit safe?
                // Bit 29 = Size 32-bit safe?
                // Bits 28-23 = Compression method
                // Bit 22 = Encrypted
                // Bits 21-6 = Compression blocks count
                // Bits 5-0 = Compression block size

                // Filter out the CompressionMethod.

                long Offset, UncompressedSize, Size;
                uint CompressionMethodIndex, CompressionBlockSize;
                bool Encrypted, Deleted;

                uint Value = BitConverter.ToUInt32(encodedPakEntries, pakLocation);
                pakLocation += sizeof(uint);

                CompressionMethodIndex = ((Value >> 23) & 0x3f);

                // Test for 32-bit safe values. Grab it, or memcpy the 64-bit value
                // to avoid alignment exceptions on platforms requiring 64-bit alignment
                // for 64-bit variables.
                //
                // Read the Offset.
                bool bIsOffset32BitSafe = (Value & (1 << 31)) != 0;
                if (bIsOffset32BitSafe)
                {
                    Offset = BitConverter.ToUInt32(encodedPakEntries, pakLocation);
                    pakLocation += sizeof(uint);
                }
                else
                {
                    Offset = BitConverter.ToInt64(encodedPakEntries, pakLocation);
                    pakLocation += sizeof(long);
                }

                // Read the UncompressedSize.
                bool bIsUncompressedSize32BitSafe = (Value & (1 << 30)) != 0;
                if (bIsUncompressedSize32BitSafe)
                {
                    UncompressedSize = BitConverter.ToUInt32(encodedPakEntries, pakLocation);
                    pakLocation += sizeof(uint);
                }
                else
                {
                    UncompressedSize = BitConverter.ToInt64(encodedPakEntries, pakLocation);
                    pakLocation += sizeof(long);
                }

                // Fill in the Size.
                if (CompressionMethodIndex != 0)
                {
                    // Size is only present if compression is applied.
                    bool bIsSize32BitSafe = (Value & (1 << 29)) != 0;
                    if (bIsSize32BitSafe)
                    {
                        Size = BitConverter.ToUInt32(encodedPakEntries, pakLocation);
                        pakLocation += sizeof(uint);
                    }
                    else
                    {
                        Size = BitConverter.ToInt64(encodedPakEntries, pakLocation);
                        pakLocation += sizeof(long);
                    }
                }
                else
                {
                    // The Size is the same thing as the UncompressedSize when
                    // CompressionMethod == COMPRESS_None.
                    Size = UncompressedSize;
                }

                // Filter the encrypted flag.
                Encrypted = (Value & (1 << 22)) != 0;

                // This should clear out any excess CompressionBlocks that may be valid in the user's
                // passed in entry.
                var CompressionBlocksCount = (Value >> 6) & 0xffff;
                FPakCompressedBlock[] CompressionBlocks = new FPakCompressedBlock[CompressionBlocksCount];

                // Filter the compression block size or use the UncompressedSize if less that 64k.
                CompressionBlockSize = 0;
                if (CompressionBlocksCount > 0)
                {
                    CompressionBlockSize = UncompressedSize < 65536 ? (uint)UncompressedSize : ((Value & 0x3f) << 11);
                }

                // Set bDeleteRecord to false, because it obviously isn't deleted if we are here.
                Deleted = false;

                // Base offset to the compressed data
                long BaseOffset = true ? 0 : Offset; // HasRelativeCompressedChunkOffsets -> Version >= PakFile_Version_RelativeChunkOffsets

                // Handle building of the CompressionBlocks array.
                if (CompressionBlocks.Length == 1 && !Encrypted)
                {
                    // If the number of CompressionBlocks is 1, we didn't store any extra information.
                    // Derive what we can from the entry's file offset and size.
                    var start = BaseOffset + FPakEntry.GetSize(PAK_VERSION.PAK_LATEST, CompressionMethodIndex, CompressionBlocksCount);
                    CompressionBlocks[0] = new FPakCompressedBlock(start, start + Size);
                }
                else if (CompressionBlocks.Length > 0)
                {
                    // Get the right pointer to start copying the CompressionBlocks information from.

                    // Alignment of the compressed blocks
                    var CompressedBlockAlignment = Encrypted ? AESDecryptor.BLOCK_SIZE : 1;

                    // CompressedBlockOffset is the starting offset. Everything else can be derived from there.
                    long CompressedBlockOffset = BaseOffset + FPakEntry.GetSize(PAK_VERSION.PAK_LATEST, CompressionMethodIndex, CompressionBlocksCount);
                    for (int CompressionBlockIndex = 0; CompressionBlockIndex < CompressionBlocks.Length; ++CompressionBlockIndex)
                    {
                        CompressionBlocks[CompressionBlockIndex] = new FPakCompressedBlock(CompressedBlockOffset, CompressedBlockOffset + BitConverter.ToUInt32(encodedPakEntries, pakLocation));
                        pakLocation += sizeof(uint);
                        {
                            var toAlign = CompressionBlocks[CompressionBlockIndex].CompressedEnd - CompressionBlocks[CompressionBlockIndex].CompressedStart;
                            CompressedBlockOffset += toAlign + CompressedBlockAlignment - (toAlign % CompressedBlockAlignment);
                        }
                    }
                }
                return new FPakEntry(name, Offset, Size, UncompressedSize, new byte[20], CompressionBlocks, CompressionBlockSize, CompressionMethodIndex, (byte)((Encrypted ? 0x01 : 0x00) | (Deleted ? 0x02 : 0x00)));
            }
            else
            {
                pakLocation = -(pakLocation + 1);
                throw new FileLoadException("list indexes aren't supported");
            }
        }

        Dictionary<ulong, int> ReadPathHashIndex(BinaryReader reader)
        {
            var ret = new Dictionary<ulong, int>();
            var keys = reader.ReadTArray(() => (reader.ReadUInt64(), reader.ReadInt32()));
            foreach (var (k, v) in keys)
            {
                ret[k] = v;
            }
            return ret;
        }

        Dictionary<string, Dictionary<string, int>> ReadDirectoryIndex(BinaryReader reader)
        {
            var ret = new Dictionary<string, Dictionary<string, int>>();
            var keys = reader.ReadTArray(() => (reader.ReadFString(), ReadFPakDirectory(reader)));
            foreach (var (k, v) in keys)
            {
                ret[k] = v;
            }
            return ret;
        }

        Dictionary<string, int> ReadFPakDirectory(BinaryReader reader)
        {
            var ret = new Dictionary<string, int>();
            var keys = reader.ReadTArray(() => (reader.ReadFString(), reader.ReadInt32()));
            foreach (var (k, v) in keys)
            {
                ret[k] = v;
            }
            return ret;
        }

        bool DecryptAndValidateIndex(bool bEncryptedIndex, ref byte[] IndexData, byte[] aesKey, FSHAHash ExpectedHash, out FSHAHash OutHash)
        {
            if (bEncryptedIndex)
            {
                IndexData = AESDecryptor.DecryptAES(IndexData, aesKey);
            }
            OutHash = ExpectedHash;
            return true;
        }

        public Stream GetPackageStream(FPakEntry entry)
        {
            lock (Reader)
            {
                return new FPakFile(Reader, entry, Aes).GetStream();
            }
        }

        public void Export(FPakEntry uasset, FPakEntry uexp, FPakEntry ubulk)
        {
            if (uasset.GetType() != typeof(FPakEntry) || uexp.GetType() != typeof(FPakEntry)) return;
            var assetStream = new FPakFile(Reader, uasset, Aes).GetStream();
            var expStream = new FPakFile(Reader, uexp, Aes).GetStream();
            var bulkStream = ubulk.GetType() != typeof(FPakEntry) ? null : new FPakFile(Reader, ubulk, Aes).GetStream();

            try
            {
                var exports = new AssetReader(assetStream, expStream, bulkStream).Exports;
                if (exports[0] is Texture2D)
                {
                    var tex = exports[0] as Texture2D;
                    tex.GetImage();
                }
            }
            catch (IndexOutOfRangeException) { }
            catch (NotImplementedException) { }
            catch (IOException) { }
            catch (Exception e)
            {
                DebugHelper.WriteException(e, "thrown in PakReader.cs by Export");
            }
        }
    }
}
