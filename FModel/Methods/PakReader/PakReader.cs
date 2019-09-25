using System;
using System.IO;

namespace PakReader
{
    public class PakReader
    {
        readonly Stream Stream;
        readonly BinaryReader Reader;
        readonly byte[] Aes;
        public readonly string MountPoint;
        public readonly FPakEntry[] FileInfos;
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
                throw new FileLoadException("The file magic is invalid");
            }

            if (info.Version > (int)PAK_VERSION.PAK_LATEST)
            {
                Console.Error.WriteLine($"WARNING: Pak file \"{Name}\" has unsupported version {info.Version}");
            }

            if (info.bEncryptedIndex != 0)
            {
                if (Aes == null)
                {
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
                InfoBlock = AESDecryptor.DecryptAES(InfoBlock, (int)info.IndexSize, Aes, Aes.Length);

                infoReader = new BinaryReader(new MemoryStream(InfoBlock));
                int stringLen = infoReader.ReadInt32();
                if (stringLen > 512 || stringLen < -512)
                {
                    throw new FileLoadException("The AES key is invalid");
                }
                if (stringLen < 0)
                {
                    infoReader.BaseStream.Seek((stringLen - 1) * 2, SeekOrigin.Current);
                    ushort c = infoReader.ReadUInt16();
                    if (c != 0)
                    {
                        throw new FileLoadException("The AES key is invalid");
                    }
                }
                else
                {
                    infoReader.BaseStream.Seek(stringLen - 1, SeekOrigin.Current);
                    byte c = infoReader.ReadByte();
                    if (c != 0)
                    {
                        throw new FileLoadException("The AES key is invalid");
                    }
                }
            }

            if (!ParseFiles) return;

            // Pak index reading time :)
            infoReader.BaseStream.Seek(0, SeekOrigin.Begin);
            MountPoint = infoReader.ReadString(FPakInfo.MAX_PACKAGE_PATH);

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
                Console.Error.WriteLine($"WARNING: Pak \"{Name}\" has strange mount point \"{MountPoint}\", mounting to root");
                MountPoint = "/";
            }
            
            FileInfos = new FPakEntry[infoReader.ReadInt32()];
            for (int i = 0; i < FileInfos.Length; i++)
            {
                FileInfos[i] = new FPakEntry(infoReader, MountPoint, info.Version);
            }
        }

        public string GetFile(int i) => FileInfos[i].Name;

        public Stream GetPackageStream(BasePakEntry entry)
        {
            lock (Reader)
            {
                return new FPakFile(Reader, entry, Aes).GetStream();
            }
        }

        public void Export(BasePakEntry uasset, BasePakEntry uexp, BasePakEntry ubulk)
        {
            if (uasset == null || uexp == null) return;
            var assetStream = new FPakFile(Reader, uasset, Aes).GetStream();
            var expStream = new FPakFile(Reader, uexp, Aes).GetStream();
            var bulkStream = ubulk == null ? null : new FPakFile(Reader, ubulk, Aes).GetStream();

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
                Console.WriteLine(e);
            }
        }
    }
}
