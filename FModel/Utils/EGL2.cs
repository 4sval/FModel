using FModel.Logger;
using System;
using System.IO;
using System.Text;

namespace FModel.Utils
{
    static class EGL2
    {
        const uint FILE_CONFIG_MAGIC = 0x279B21E6;
        const ushort FILE_CONFIG_VERSION = (ushort)ESettingsVersion.Version13;

        public static string GetEGL2PakFilesPath()
        {
            string configFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\EGL2\\config";
            if (File.Exists(configFile))
            {
                using Stream stream = new BufferedStream(new FileInfo(configFile).Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                using BinaryReader reader = new BinaryReader(stream, Encoding.Default);
                if (reader.ReadUInt32() != FILE_CONFIG_MAGIC)
                    throw new FileLoadException("Invalid file magic");

                if (reader.ReadUInt16BE() != FILE_CONFIG_VERSION)
                    throw new FileLoadException("Invalid egl2 version");

                int stringLength = reader.ReadUInt16BE();
                string cacheDirectory = Encoding.UTF8.GetString(reader.ReadBytes(stringLength));
                if (Directory.Exists(cacheDirectory + "\\game\\FortniteGame\\Content\\Paks"))
                {
                    return cacheDirectory + "\\game\\FortniteGame\\Content\\Paks";
                }
            }

            DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[EGL2]", "Fortnite not found");
            return string.Empty;
        }

        private static ushort ReadUInt16BE(this BinaryReader binRdr)
        {
            return BitConverter.ToUInt16(binRdr.ReadBytesRequired(sizeof(ushort)).Reverse(), 0);
        }

        public static byte[] Reverse(this byte[] b)
        {
            Array.Reverse(b);
            return b;
        }

        private static byte[] ReadBytesRequired(this BinaryReader binRdr, int byteCount)
        {
            var result = binRdr.ReadBytes(byteCount);

            if (result.Length != byteCount)
                throw new EndOfStreamException(string.Format("{0} bytes required from stream, but only {1} returned.", byteCount, result.Length));

            return result;
        }
    }

    public enum ESettingsVersion : ushort
    {
        // Initial Version
        Initial,

        // Removes GameDir and MountDrive
        // Adds CommandArgs
        SimplifyPathsAndCmdLine,

        // Adds ThreadCount, BufferCount, and UpdateInterval
        // Removes VerifyCache and EnableGaming
        Version13,

        LatestPlusOne,
        Latest = LatestPlusOne - 1
    }

    public enum ESettingsCompressionMethod : byte
    {
        Zstandard,
        LZ4,
        Decompressed
    }

    public enum ESettingsCompressionLevel : byte
    {
        Fastest,
        Fast,
        Normal,
        Slow,
        Slowest
    }

    public enum ESettingsUpdateInterval : byte
    {
        Second1,
        Second5,
        Second10,
        Second30,
        Minute1,
        Minute5,
        Minute10,
        Minute30,
        Hour1
    }
}
