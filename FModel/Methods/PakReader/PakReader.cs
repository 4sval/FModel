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
        public string MountPoint;
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

                infoReader.BaseStream.Seek(0, SeekOrigin.Begin);
            }

            if (!ParseFiles) return;

            if (info.Version >= (int)PAK_VERSION.PAK_PATH_HASH_INDEX)
            {
                ReadIndexUpdated(infoReader, info, Aes);
            }
            else
            {
                MountPoint = infoReader.ReadFString();
                if (MountPoint.StartsWith("../../.."))
                {
                    MountPoint = MountPoint.Substring(8);
                }
                else
                {
                    MountPoint = "/";
                }

                FileInfos = new FPakEntry[infoReader.ReadInt32()];
                for (int i = 0; i < FileInfos.Length; i++)
                {
                    FileInfos[i] = new FPakEntry(infoReader, MountPoint, (PAK_VERSION)info.Version);
                }
            }
        }

        private void ReadIndexUpdated(BinaryReader reader, FPakInfo info, byte[] aesKey)
        {
            MountPoint = reader.ReadFString();
            if (MountPoint.StartsWith("../../.."))
            {
                MountPoint = MountPoint.Substring(8);
            }
            else
            {
                MountPoint = "/";
            }

            var filesNum = reader.ReadInt32();
            reader.ReadUInt64();

            if (reader.ReadInt32() == 0)
            {
                throw new FileLoadException("No path hash index");
            }

            //reader.ReadInt64();
            //reader.ReadInt64();
            reader.BaseStream.Position += 20L + 8L + 8L;

            if (reader.ReadInt32() == 0)
            {
                throw new FileLoadException("No directory index");
            }

            var position = reader.ReadInt64();
            var directoryIndexSize = reader.ReadInt64();
            reader.BaseStream.Position += 20L;
            var encodedPakEntries = reader.ReadTArray(reader.ReadByte);
            var files = reader.ReadInt32();

            if (files < 0)
            {
                throw new FileLoadException("Corrupt pak PrimaryIndex detected!");
            }

            Reader.BaseStream.Position = position;
            var directoryIndexData = Reader.ReadBytes((int)directoryIndexSize);

            if (info.bEncryptedIndex != 0)
            {
                directoryIndexData = AESDecryptor.DecryptAES(directoryIndexData, aesKey);
            }

            var directoryIndexReader = new BinaryReader(new MemoryStream(directoryIndexData));
            var directoryEntries = directoryIndexReader.ReadTArray(() => new FPakDirectoryEntry(directoryIndexReader));

            var entries = new List<FPakEntry>(filesNum);
            foreach (var directoryEntry in directoryEntries)
            {
                foreach (var hashIndexEntry in directoryEntry.Entries)
                {
                    string path = MountPoint + directoryEntry.Directory + hashIndexEntry.Filename;
                    entries.Add(GetEntry(path, hashIndexEntry.Location, encodedPakEntries));
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
                //pakLocation = -(pakLocation + 1);
                throw new FileLoadException("list indexes aren't supported");
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
