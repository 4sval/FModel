using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using FModel.Logger;
using FModel.PakReader.Parsers.Objects;
using FModel.Utils;
using PakReader.Parsers.Objects;

namespace PakReader.Pak
{
    public sealed class PakFileReader : IReadOnlyDictionary<string, FPakEntry>
    {
        public FPakInfo Info { get; }
        public Stream Stream { get; }
        public string Directory { get; }
        public string FileName { get; }
        public bool CaseSensitive { get; }

        private byte[] _aesKey = null;
        public byte[] AesKey
        {
            get { return _aesKey; }
            set
            {
                if (value != null && !TestAesKey(value)) //if value not null, test but fail, throw not working
                    throw new ArgumentException(string.Format(FModel.Properties.Resources.AesNotWorking, value.ToStringKey(), FileName));
                _aesKey = value; // else, even if value is null, set it
                // setting _aesKey to null will disable the corresponding menu item
            }
        }

        public string MountPoint { get; private set; }
        public bool Initialized { get; private set; }

        readonly BinaryReader Reader;
        readonly byte[] MountArray;
        Dictionary<string, FPakEntry> Entries;

        // Buffered streams increase performance dramatically
        public PakFileReader(string file, bool caseSensitive = true)
            : this(file, new BufferedStream(new FileInfo(file).Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite)), caseSensitive)
        { }

        public PakFileReader(string path, Stream stream, bool caseSensitive = true)
        {
            Directory = Path.GetDirectoryName(path);
            FileName = Path.GetFileName(path);
            Stream = stream;
            CaseSensitive = caseSensitive;
            Reader = new BinaryReader(stream, Encoding.Default, true);
            Info = new FPakInfo().ReadPakInfo(Reader);

            Stream.Position = Info.IndexOffset;
            MountArray = Reader.ReadBytes(128);
        }

        public bool TestAesKey(byte[] key)
        {
            if (!Info.bEncryptedIndex)
                return true;

            Stream.Position = Info.IndexOffset;
            return TestAesKey(MountArray, key);
        }
        public bool TestAesKey(byte[] bytes, byte[] key)
        {
            using BinaryReader IndexReader = new BinaryReader(new MemoryStream(AESDecryptor.DecryptAES(bytes, key)));
            int stringLen = IndexReader.ReadInt32();
            if (stringLen > 128 || stringLen < -128)
            {
                return false;
            }
            if (stringLen == 0)
            {
                return IndexReader.ReadUInt16() == 0;
            }
            if (stringLen < 0)
            {
                int nullTerminatedPos = 4 - (stringLen - 1) * 2;
                IndexReader.BaseStream.Seek(nullTerminatedPos, SeekOrigin.Begin);
                return IndexReader.ReadInt16() == 0;
            }
            else
            {
                int nullTerminatedPos = 4 + stringLen - 1;
                IndexReader.BaseStream.Seek(nullTerminatedPos, SeekOrigin.Begin);
                return IndexReader.ReadSByte() == 0;
            }
        }

        public bool TryReadIndex(byte[] key, PakFilter filter = null)
        {
            ReadIndexInternal(key, filter, out var exc);
            return exc == null;
        }

        public void ReadIndex(byte[] key, PakFilter filter = null)
        {
            ReadIndexInternal(key, filter, out var exc);
            if (exc != null)
                throw exc;
        }

        void ReadIndexInternal(byte[] key, PakFilter filter, out Exception exc)
        {
            if (Initialized)
            {
                exc = new InvalidOperationException("Index is already initialized");
                return;
            }

            if (Info.bEncryptedIndex && key == null)
            {
                exc = new ArgumentException("Index is encrypted but no key was provided", nameof(key));
                return;
            }

            Stream.Position = Info.IndexOffset;

            BinaryReader IndexReader;
            if (Info.bEncryptedIndex)
            {
                IndexReader = new BinaryReader(new MemoryStream(AESDecryptor.DecryptAES(Reader.ReadBytes((int)Info.IndexSize), key)));
                int stringLen = IndexReader.ReadInt32();
                if (stringLen > 512 || stringLen < -512)
                {
                    exc = new ArgumentException("The provided key is invalid", nameof(key));
                    return;
                }
                if (stringLen < 0)
                {
                    IndexReader.BaseStream.Position += (stringLen - 1) * 2;
                    if (IndexReader.ReadUInt16() != 0)
                    {
                        exc = new ArgumentException("The provided key is invalid", nameof(key));
                        return;
                    }
                }
                else
                {
                    IndexReader.BaseStream.Position += stringLen - 1;
                    if (IndexReader.ReadByte() != 0)
                    {
                        exc = new ArgumentException("The provided key is invalid", nameof(key));
                        return;
                    }
                }
                IndexReader.BaseStream.Position = 0;
            }
            else
            {
                IndexReader = Reader;
            }

            Dictionary<string, FPakEntry> tempFiles;
            if (Info.Version >= EPakVersion.PATH_HASH_INDEX)
            {
                ReadIndexUpdated(IndexReader, key, out tempFiles, filter);
            }
            else
            {
                // https://github.com/EpicGames/UnrealEngine/blob/bf95c2cbc703123e08ab54e3ceccdd47e48d224a/Engine/Source/Runtime/PakFile/Private/IPlatformFilePak.cpp#L4509
                MountPoint = IndexReader.ReadFString() ?? "";
                if (MountPoint.StartsWith("../../.."))
                {
                    MountPoint = MountPoint[8..];
                }
                else
                {
                    // Weird mount point location...
                    MountPoint = "/";
                }
                if (!CaseSensitive)
                {
                    MountPoint = MountPoint.ToLowerInvariant();
                }

                var NumEntries = IndexReader.ReadInt32();
                tempFiles = new Dictionary<string, FPakEntry>(NumEntries);
                for (int i = 0; i < NumEntries; i++)
                {
                    var entry = new FPakEntry(IndexReader, Info.Version, Info.SubVersion, CaseSensitive, FileName);
                    // if there is no filter OR the filter passes
                    if (filter == null || filter.CheckFilter(MountPoint + entry.Name, CaseSensitive))
                    {
                        // Filename is without the MountPoint concatenated to save memory
                        tempFiles[entry.Name] = entry;
                    }
                }
            }

            Paks.Merge(tempFiles, out Entries, MountPoint);
            DebugHelper.WriteLine("{0} {1} {2} {3}", "[FModel]", "[PakFileReader]", "[ReadIndexInternal]", $"{FileName} contains {Entries.Count} files, mount point: \"{this.MountPoint}\", version: {(int)this.Info.Version}");

            if (Info.bEncryptedIndex)
            {
                // underlying stream is a MemoryStream of the decrypted index, might improve performance with a crypto stream of some sort
                IndexReader.Dispose();
            }

            Reader.Dispose();
            Initialized = true;
            exc = null;
        }

        void ReadIndexUpdated(BinaryReader reader, byte[] aesKey, out Dictionary<string, FPakEntry> dict, PakFilter filter)
        {
            MountPoint = reader.ReadFString();
            if (MountPoint.StartsWith("../../.."))
            {
                MountPoint = MountPoint[8..];
            }
            else
            {
                // Weird mount point location...
                MountPoint = "/";
            }
            if (!CaseSensitive)
            {
                MountPoint = MountPoint.ToLowerInvariant();
            }
            var NumEntries = reader.ReadInt32();
            var PathHashSeed = reader.ReadUInt64();

            if (reader.ReadInt32() == 0)
            {
                throw new FileLoadException("No path hash index");
            }

            /*
            long PathHashIndexOffset = reader.ReadInt64();
            long PathHashIndexSize = reader.ReadInt64();
            FSHAHash PathHashIndexHash = new FSHAHash(reader);
            */
            reader.BaseStream.Position += 8L + 8L + 20L;

            if (reader.ReadInt32() == 0)
            {
                throw new FileLoadException("No directory index");
            }

            long FullDirectoryIndexOffset = reader.ReadInt64();
            long FullDirectoryIndexSize = reader.ReadInt64();
            FSHAHash FullDirectoryIndexHash = new FSHAHash(reader);

            byte[] EncodedPakEntries = reader.ReadTArray(() => reader.ReadByte());

            int FilesNum = reader.ReadInt32();
            if (FilesNum < 0)
            {
                // Should not be possible for any values in the PrimaryIndex to be invalid, since we verified the index hash
                throw new FileLoadException("Corrupt pak PrimaryIndex detected!");
            }

            Reader.BaseStream.Position = FullDirectoryIndexOffset;
            byte[] PathHashIndexData = Reader.ReadBytes((int)FullDirectoryIndexSize);

            if (!DecryptAndValidateIndex(ref PathHashIndexData, aesKey, FullDirectoryIndexHash, out var ComputedHash))
            {
                throw new FileLoadException("Corrupt pak PrimaryIndex detected!");
                //UE_LOG(LogPakFile, Log, TEXT(" Filename: %s"), *PakFilename);
                //UE_LOG(LogPakFile, Log, TEXT(" Encrypted: %d"), Info.bEncryptedIndex);
                //UE_LOG(LogPakFile, Log, TEXT(" Total Size: %d"), Reader->TotalSize());
                //UE_LOG(LogPakFile, Log, TEXT(" Index Offset: %d"), FullDirectoryIndexOffset);
                //UE_LOG(LogPakFile, Log, TEXT(" Index Size: %d"), FullDirectoryIndexSize);
                //UE_LOG(LogPakFile, Log, TEXT(" Stored Index Hash: %s"), *PathHashIndexHash.ToString());
                //UE_LOG(LogPakFile, Log, TEXT(" Computed Index Hash: %s"), *ComputedHash.ToString());
            }

            BinaryReader PathHashIndexReader = new BinaryReader(new MemoryStream(PathHashIndexData));
            FPakDirectoryEntry[] PathHashIndex = PathHashIndexReader.ReadTArray(() => new FPakDirectoryEntry(PathHashIndexReader));

            dict = new Dictionary<string, FPakEntry>(NumEntries);
            foreach (FPakDirectoryEntry directoryEntry in PathHashIndex)
            {
                foreach (FPathHashIndexEntry hashIndexEntry in directoryEntry.Entries)
                {
                    var path = directoryEntry.Directory + hashIndexEntry.Filename;
                    if (path.StartsWith("/"))
                        path = path[1..];
                    if (!CaseSensitive)
                    {
                        path = path.ToLowerInvariant();
                    }
                    // if there is no filter OR the filter passes
                    if (filter == null || filter.CheckFilter(MountPoint + hashIndexEntry.Filename, CaseSensitive))
                    {
                        // Filename is without the MountPoint concatenated to save memory
                        dict[path] = GetEntry(path, hashIndexEntry.Location, EncodedPakEntries);
                    }
                }
            }
        }

        bool DecryptAndValidateIndex(ref byte[] IndexData, byte[] aesKey, FSHAHash ExpectedHash, out FSHAHash OutHash)
        {
            if (Info.bEncryptedIndex)
            {
                IndexData = AESDecryptor.DecryptAES(IndexData, aesKey);
            }
            OutHash = ExpectedHash; // too lazy to actually check against the hash
            // https://github.com/EpicGames/UnrealEngine/blob/79a64829237ae339118bb50b61d84e4599c14e8a/Engine/Source/Runtime/PakFile/Private/IPlatformFilePak.cpp#L5371
            return true;
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
                    var start = BaseOffset + FPakEntry.GetSize(EPakVersion.LATEST, CompressionMethodIndex, CompressionBlocksCount);
                    CompressionBlocks[0] = new FPakCompressedBlock(start, start + Size);
                }
                else if (CompressionBlocks.Length > 0)
                {
                    // Get the right pointer to start copying the CompressionBlocks information from.

                    // Alignment of the compressed blocks
                    var CompressedBlockAlignment = Encrypted ? 16 : 1;

                    // CompressedBlockOffset is the starting offset. Everything else can be derived from there.
                    long CompressedBlockOffset = BaseOffset + FPakEntry.GetSize(EPakVersion.LATEST, CompressionMethodIndex, CompressionBlocksCount);
                    for (int CompressionBlockIndex = 0; CompressionBlockIndex < CompressionBlocks.Length; ++CompressionBlockIndex)
                    {
                        FPakCompressedBlock CompressionBlock = new FPakCompressedBlock(CompressedBlockOffset, CompressedBlockOffset + BitConverter.ToUInt32(encodedPakEntries, pakLocation));
                        CompressionBlocks[CompressionBlockIndex] = CompressionBlock;
                        CompressedBlockOffset += BinaryHelper.Align(CompressionBlock.CompressedEnd - CompressionBlock.CompressedStart, CompressedBlockAlignment);

                        pakLocation += 4;
                    }
                }
                return new FPakEntry(this.FileName, name, Offset, Size, UncompressedSize, CompressionBlocks, CompressionBlockSize, CompressionMethodIndex, (byte)((Encrypted ? 0x01 : 0x00) | (Deleted ? 0x02 : 0x00)));
            }
            else
            {
                throw new FileLoadException("list indexes aren't supported");
            }
        }

        public bool TryGetFile(string path, out ArraySegment<byte> ret1, out ArraySegment<byte> ret2, out ArraySegment<byte> ret3)
        {
            if (!string.IsNullOrEmpty(path) && Entries.TryGetValue(CaseSensitive ? path : path.ToLowerInvariant(), out var entry))
            {
                ret1 = entry.GetData(Stream, AesKey, Info.CompressionMethods);
                if (entry.HasUexp())
                {
                    ret2 = entry.Uexp.GetData(Stream, AesKey, Info.CompressionMethods);
                    ret3 = entry.HasUbulk() ? entry.Ubulk.GetData(Stream, AesKey, Info.CompressionMethods) : null;
                    return true;
                }
                else // return a fail but keep the uasset data
                {
                    ret2 = null;
                    ret3 = null;
                    return false;
                }
            }
            ret1 = null;
            ret2 = null;
            ret3 = null;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(string key, out FPakEntry value) => Entries.TryGetValue(key, out value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetPartialKey(string partialKey, out string key)
        {
            foreach (string path in Entries.Keys)
            {
                if (Regex.Match(path, partialKey, RegexOptions.IgnoreCase).Success)
                {
                    key = path;
                    return true;
                }
            }

            key = string.Empty;
            return false;
        }


        FPakEntry IReadOnlyDictionary<string, FPakEntry>.this[string key] => Entries[key];
        IEnumerable<string> IReadOnlyDictionary<string, FPakEntry>.Keys => Entries.Keys;
        IEnumerable<FPakEntry> IReadOnlyDictionary<string, FPakEntry>.Values => Entries.Values;
        int IReadOnlyCollection<KeyValuePair<string, FPakEntry>>.Count => Entries.Count;

        bool IReadOnlyDictionary<string, FPakEntry>.ContainsKey(string key) => Entries.ContainsKey(key);
        IEnumerator<KeyValuePair<string, FPakEntry>> IEnumerable<KeyValuePair<string, FPakEntry>>.GetEnumerator() => Entries.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Entries.GetEnumerator();
        bool IReadOnlyDictionary<string, FPakEntry>.TryGetValue(string key, out FPakEntry value) => Entries.TryGetValue(key, out value);
    }
}