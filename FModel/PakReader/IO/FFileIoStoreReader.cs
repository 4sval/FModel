using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using FModel.Logger;
using FModel.PakReader.Parsers;
using FModel.PakReader.Parsers.Objects;
using FModel.Utils;
using Ionic.Zlib;

namespace FModel.PakReader.IO
{
    public class FFileIoStoreReader : IReadOnlyDictionary<string, FIoStoreEntry>
    {
        public readonly string FileName;
        public readonly string Directory;
        public readonly FIoStoreTocResource TocResource;
        public readonly Dictionary<FIoChunkId, FIoOffsetAndLength> Toc;
        public readonly FFileIoStoreContainerFile ContainerFile;
        public readonly FIoContainerId ContainerId;

        public bool IsInitialized => Files != null;

        private byte[] _aesKey;
        public byte[] AesKey
        {
            get => _aesKey;
            set
            {
                if (!HasDirectoryIndex) return;
                if (value != null && !TestAesKey(value)) //if value not null, test but fail, throw not working
                    throw new ArgumentException(string.Format(FModel.Properties.Resources.AesNotWorking, value.ToStringKey(), FileName));
                _aesKey = value; // else, even if value is null, set it
                // setting _aesKey to null will disable the corresponding menu item
            }
        }

        public readonly bool CaseSensitive;

        public bool HasDirectoryIndex => TocResource.DirectoryIndexBuffer != null;

        public FGuid EncryptionKeyGuid => ContainerFile.EncryptionKeyGuid;
        public bool IsEncrypted => ContainerFile.ContainerFlags.HasAnyFlags(EIoContainerFlags.Encrypted);

        public Dictionary<string, FIoStoreEntry> Files;
        public Dictionary<ulong, string> Chunks;

        public FIoDirectoryIndexResource _directoryIndex;
        private byte[] _directoryIndexBuffer;

        public FFileIoStoreReader(string fileName, string dir, Stream tocStream, Stream containerStream, bool caseSensitive = true, EIoStoreTocReadOptions tocReadOptions = EIoStoreTocReadOptions.ReadDirectoryIndex)
        {
            FileName = fileName;
            Directory = dir;
            CaseSensitive = caseSensitive;
            ContainerFile.FileHandle = containerStream;
            var tocResource = new FIoStoreTocResource(tocStream, tocReadOptions);
            TocResource = tocResource;

            var containerUncompressedSize = tocResource.Header.TocCompressedBlockEntryCount > 0
                ? (ulong) tocResource.Header.TocCompressedBlockEntryCount * (ulong) tocResource.Header.CompressionBlockSize
                : (ulong) containerStream.Length;
            
            Toc = new Dictionary<FIoChunkId, FIoOffsetAndLength>((int) tocResource.Header.TocEntryCount);

            for (var chunkIndex = 0; chunkIndex < tocResource.Header.TocEntryCount; chunkIndex++)
            {
                ref var chunkOffsetLength = ref tocResource.ChunkOffsetLengths[chunkIndex];
                if (chunkOffsetLength.Offset + chunkOffsetLength.Length > containerUncompressedSize)
                {
                    throw new FileLoadException("TocEntry out of container bounds");
                }
                Toc[tocResource.ChunkIds[chunkIndex]] = chunkOffsetLength;
            }
            
            for (var compressedBlockIndex = 0; compressedBlockIndex < tocResource.CompressionBlocks.Length; compressedBlockIndex++)
            {
                ref var compressedBlockEntry = ref tocResource.CompressionBlocks[compressedBlockIndex];
                if (compressedBlockEntry.Offset + compressedBlockEntry.CompressedSize > ContainerFile.FileSize)
                {
                    throw new FileLoadException("TocCompressedBlockEntry out of container bounds");
                }
            }

            ContainerFile.CompressionMethods    = tocResource.CompressionMethods;
            ContainerFile.CompressionBlockSize	= tocResource.Header.CompressionBlockSize;
            ContainerFile.CompressionBlocks		= tocResource.CompressionBlocks;
            ContainerFile.ContainerFlags		= tocResource.Header.ContainerFlags;
            ContainerFile.EncryptionKeyGuid		= tocResource.Header.EncryptionKeyGuid;
            ContainerFile.BlockSignatureHashes	= tocResource.ChunkBlockSignatures;

            ContainerId = tocResource.Header.ContainerId;

            _directoryIndexBuffer = tocResource.DirectoryIndexBuffer;
        }

        public bool ReadDirectoryIndex()
        {
            try
            {
                if (HasDirectoryIndex)
                {
                    using Stream indexStream = IsEncrypted
                        ? new MemoryStream(AESDecryptor.DecryptAES(_directoryIndexBuffer, _aesKey))
                        : new MemoryStream(_directoryIndexBuffer);
                    _directoryIndex = new FIoDirectoryIndexResource(indexStream, CaseSensitive);

                    var firstEntry = GetChildDirectory(FIoDirectoryIndexHandle.Root);

                    var tempFiles = new Dictionary<string, FIoStoreEntry>();
                    Chunks = new Dictionary<ulong, string>();
                    ReadIndex("", firstEntry, tempFiles, Chunks);
                    Paks.Merge(tempFiles, out Files, _directoryIndex.MountPoint);
                    DebugHelper.WriteLine("{0} {1} {2} {3}", "[FModel]", "[FFileIoStoreReader]", "[ReadDirectoryIndex]", $"{FileName} contains {Files.Count} files, mount point: \"{MountPoint}\", version: {(int)TocResource.Header.Version}");
                
                    return true;
                }
            }
            catch (Exception e)
            {
                DebugHelper.WriteLine(e.ToString());
            }
            return false;
        }

        public string MountPoint => _directoryIndex.MountPoint;

        public bool TestAesKey(byte[] key)
        {
            if (!HasDirectoryIndex)
                return false;
            if (!IsEncrypted)
                return true;
            return TestAesKey(_directoryIndexBuffer, key);
        }
        
        public static bool TestAesKey(byte[] bytes, byte[] key)
        {
            using BinaryReader indexReader = new BinaryReader(new MemoryStream(AESDecryptor.DecryptAES(bytes, key)));
            var stringLen = indexReader.ReadInt32();
            if (stringLen > 128 || stringLen < -128)
            {
                return false;
            }
            if (stringLen == 0)
            {
                return indexReader.ReadUInt16() == 0;
            }
            if (stringLen < 0)
            {
                var nullTerminatedPos = 4 - (stringLen - 1) * 2;
                indexReader.BaseStream.Seek(nullTerminatedPos, SeekOrigin.Begin);
                return indexReader.ReadInt16() == 0;
            }
            else
            {
                var nullTerminatedPos = 4 + stringLen - 1;
                indexReader.BaseStream.Seek(nullTerminatedPos, SeekOrigin.Begin);
                return indexReader.ReadSByte() == 0;
            }
        }

        public bool DoesChunkExist(FIoChunkId chunkId) => Toc.ContainsKey(chunkId);

        public byte[] Read(FIoChunkId chunkId)
        {
            var offsetAndLength = Toc[chunkId];
            var tocResource = TocResource;
            var compressionBlockSize = tocResource.Header.CompressionBlockSize;
            var dst = new byte[offsetAndLength.Length];
            var firstBlockIndex = (int) (offsetAndLength.Offset / compressionBlockSize);
            var lastBlockIndex = (int) ((BinaryHelper.Align((long) offsetAndLength.Offset + dst.Length, compressionBlockSize) - 1) / compressionBlockSize);
            var offsetInBlock = (int) offsetAndLength.Offset % compressionBlockSize;
            var remainingSize = dst.Length;
            var dstOffset = 0;

            for (int i = firstBlockIndex; i <= lastBlockIndex; i++)
            {
                var compressionBlock = tocResource.CompressionBlocks[i];

                var rawSize = BinaryHelper.Align(compressionBlock.CompressedSize, AESDecryptor.ALIGN);
                var compressedBuffer = new byte[rawSize];
                
                var uncompressedSize = compressionBlock.UncompressedSize;
                var uncompressedBuffer = new byte[uncompressedSize];
                
                var containerStream = ContainerFile.FileHandle;
                containerStream.Position = compressionBlock.Offset;
                containerStream.Read(compressedBuffer, 0, (int) rawSize);
                if (TocResource.Header.ContainerFlags.HasAnyFlags(EIoContainerFlags.Encrypted))
                {
                    compressedBuffer = AESDecryptor.DecryptAES(compressedBuffer, _aesKey);
                }

                byte[] src;

                if (compressionBlock.CompressionMethodIndex == 0)
                {
                    src = compressedBuffer;
                }
                else
                {
                    var compressionMethod = tocResource.CompressionMethods[compressionBlock.CompressionMethodIndex - 1];
                    Decompress(compressedBuffer, uncompressedBuffer, compressionMethod);
                    src = uncompressedBuffer;
                }

                var sizeInBlock = (int)Math.Min(compressionBlockSize - offsetInBlock, remainingSize);
                Buffer.BlockCopy(src, (int) offsetInBlock, dst, dstOffset, sizeInBlock);
                offsetInBlock = 0;
                remainingSize -= sizeInBlock;
                dstOffset += sizeInBlock;
            }

            return dst;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FIoDirectoryIndexHandle GetChildDirectory(FIoDirectoryIndexHandle directory) =>
            directory.IsValid() && IsValidIndex()
                ? FIoDirectoryIndexHandle.FromIndex(GetDirectoryEntry(directory).FirstChildEntry)
                : FIoDirectoryIndexHandle.InvalidHandle;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FIoDirectoryIndexHandle GetNextDirectory(FIoDirectoryIndexHandle directory) =>
            directory.IsValid() && IsValidIndex()
                ? FIoDirectoryIndexHandle.FromIndex(GetDirectoryEntry(directory).NextSiblingEntry)
                : FIoDirectoryIndexHandle.InvalidHandle;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FIoDirectoryIndexHandle GetFile(FIoDirectoryIndexHandle directory) =>
            directory.IsValid() && IsValidIndex()
                ? FIoDirectoryIndexHandle.FromIndex(GetDirectoryEntry(directory).FirstFileEntry)
                : FIoDirectoryIndexHandle.InvalidHandle;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FIoDirectoryIndexHandle GetNextFile(FIoDirectoryIndexHandle file) => file.IsValid() && IsValidIndex()
            ? FIoDirectoryIndexHandle.FromIndex(GetFileEntry(file).NextFileEntry)
            : FIoDirectoryIndexHandle.InvalidHandle; 

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetDirectoryName(FIoDirectoryIndexHandle directory)
        {
            if (directory.IsValid() && IsValidIndex())
            {
                var nameIndex = GetDirectoryEntry(directory).Name;
                return _directoryIndex.StringTable[nameIndex];
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetFileName(FIoDirectoryIndexHandle file)
        {
            if (file.IsValid() && IsValidIndex())
            {
                var nameIndex = GetFileEntry(file).Name;
                return _directoryIndex.StringTable[nameIndex];
            }

            return null;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetFileData(FIoDirectoryIndexHandle file) => file.IsValid() && IsValidIndex()
            ? _directoryIndex.FileEntries[file.ToIndex()].UserData
            : 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref FIoDirectoryIndexEntry GetDirectoryEntry(FIoDirectoryIndexHandle directory) =>
            ref _directoryIndex.DirectoryEntries[directory.ToIndex()];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref FIoFileIndexEntry GetFileEntry(FIoDirectoryIndexHandle file) =>
            ref _directoryIndex.FileEntries[file.ToIndex()];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsValidIndex() => _directoryIndex.DirectoryEntries.Length > 0;
        
        
        private void Decompress(byte[] src, byte[] outData, string compressionMethod)
        {
            using var blockMs = new MemoryStream(src, false);
            using Stream compressionStream = compressionMethod switch
            {
                "Zlib" => new ZlibStream(blockMs, CompressionMode.Decompress),
                "Gzip" => new GZipStream(blockMs, CompressionMode.Decompress),
                "Oodle" => new OodleStream(src, outData.Length),
                _ => throw new NotImplementedException($"Decompression not yet implemented ({compressionMethod})")
            };
            compressionStream.Read(outData, 0, outData.Length);
        }

        private void ReadIndex(string directoryName, FIoDirectoryIndexHandle dir, IDictionary<string, FIoStoreEntry> outFiles, Dictionary<ulong, string> outChunks)
        {
            while (dir.IsValid())
            {
                var subDirectoryName = string.Concat(directoryName, GetDirectoryName(dir), "/");
            
                var file = GetFile(dir);
                while (file.IsValid())
                {
                    var name = GetFileName(file);
                    var path = string.Concat(subDirectoryName, name);
                    var data = GetFileData(file);
                    var entry = new FIoStoreEntry(this, data, path, CaseSensitive);
                    outChunks[entry.ChunkId.ChunkId] = path;
                    outFiles[path] = entry;
                    file = GetNextFile(file);
                }
            
                ReadIndex(subDirectoryName, GetChildDirectory(dir), outFiles, outChunks);

                dir = GetNextDirectory(dir);
            }
        }
        
        public bool TryGetFile(string path, out ArraySegment<byte> ret1, out ArraySegment<byte> ret2, out ArraySegment<byte> ret3)
        {
            if (!string.IsNullOrEmpty(path) && Files.TryGetValue(CaseSensitive ? path : path.ToLowerInvariant(), out var entry))
            {
                ret1 = entry.GetData();
                if (entry.HasUexp())
                {
                    ret2 = (entry.Uexp as FIoStoreEntry)?.GetData();
                    ret3 = (entry.Ubulk as FIoStoreEntry)?.GetData();
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
        public bool TryGetPartialKey(string partialKey, out string key)
        {
            foreach (string path in Files.Keys)
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetCaseInsensiteveValue(string key, out FIoStoreEntry value)
        {
            foreach (var r in Files)
            {
                if (r.Key.Equals(key, StringComparison.CurrentCultureIgnoreCase))
                {
                    value = r.Value;
                    return true;
                }
            }
            value = null;
            return false;
        }

        public IEnumerator<KeyValuePair<string, FIoStoreEntry>> GetEnumerator()
        {
            return Files.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) Files).GetEnumerator();
        }

        public int Count => Files.Count;

        public bool ContainsKey(string key)
        {
            return Files.ContainsKey(key);
        }

        public bool TryGetValue(string key, out FIoStoreEntry value)
        {
            return Files.TryGetValue(key, out value);
        }

        public FIoStoreEntry this[string key] => Files[key];

        public IEnumerable<string> Keys => Files.Keys;

        public IEnumerable<FIoStoreEntry> Values => Files.Values;
    }
}