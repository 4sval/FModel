using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using FModel.Utils;
using Ionic.Zlib;
using PakReader.Parsers;
using PakReader.Parsers.Objects;

namespace PakReader.Pak.IO
{
    public class FFileIoStoreReader
    {
        public readonly FIoStoreTocResource TocResource;
        public readonly Dictionary<FIoChunkId, FIoOffsetAndLength> Toc;
        public readonly FFileIoStoreContainerFile ContainerFile;
        public readonly FIoContainerId ContainerId;

        private byte[] _aesKey;
        public byte[] AesKey
        {
            get => _aesKey;
            set
            {
                if (value != null && !TestAesKey(value)) //if value not null, test but fail, throw not working
                    throw new ArgumentException(string.Format(FModel.Properties.Resources.AesNotWorking, value.ToStringKey(), ContainerFile.FileName));
                _aesKey = value; // else, even if value is null, set it
                // setting _aesKey to null will disable the corresponding menu item
            }
        }

        public FGuid EncryptionKeyGuid => ContainerFile.EncryptionKeyGuid;
        public bool IsEncrypted => ContainerFile.ContainerFlags.HasAnyFlags(EIoContainerFlags.Encrypted);
        

        public FIoDirectoryIndexResource _directoryIndex;
        private byte[] _directoryIndexBuffer;

        public FFileIoStoreReader(Stream tocStream, Stream containerStream, EIoStoreTocReadOptions tocReadOptions = EIoStoreTocReadOptions.ReadDirectoryIndex)
        {
            ContainerFile.FileHandle = containerStream;
            var tocResource = new FIoStoreTocResource(tocStream, tocReadOptions);
            TocResource = tocResource;

            var containerUncompressedSize = tocResource.Header.TocCompressedBlockEntryCount > 0
                ? tocResource.Header.TocCompressedBlockEntryCount * tocResource.Header.CompressionBlockSize
                : containerStream.Length;
            
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

        public void ReadIndex()
        {
            using Stream indexStream = IsEncrypted
                ? new MemoryStream(AESDecryptor.DecryptAES(_directoryIndexBuffer, _aesKey))
                : new MemoryStream(_directoryIndexBuffer);
            _directoryIndex = new FIoDirectoryIndexResource(indexStream);
        }

        public string MountPoint => _directoryIndex.MountPoint;

        public bool TestAesKey(byte[] key)
        {
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

        public byte[] Read(FIoChunkId chunkId)
        {
            var offsetAndLength = Toc[chunkId];
            var tocResource = TocResource;
            var compressionBlockSize = tocResource.Header.CompressionBlockSize;
            var dst = new byte[offsetAndLength.Length];
            var firstBlockIndex = (int) (offsetAndLength.Offset / compressionBlockSize);
            var lastBlockIndex = (int) ((BinaryHelper.Align(offsetAndLength.Offset + dst.Length, compressionBlockSize) - 1) / compressionBlockSize);
            var offsetInBlock = offsetAndLength.Offset % compressionBlockSize;

            byte[] src;
            var remainingSize = dst.Length;
            var dstOffset = 0;
            for (int blockIndex = firstBlockIndex; blockIndex < lastBlockIndex; blockIndex++)
            {
                var compressionBlock = tocResource.CompressionBlocks[blockIndex];
                
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

                var sizeInBlock = (int) Math.Min(compressionBlockSize - offsetInBlock, remainingSize);
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
    }
}