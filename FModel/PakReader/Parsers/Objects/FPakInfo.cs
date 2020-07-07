using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PakReader.Parsers.Objects
{
    public readonly struct FPakInfo
    {
        const uint PAK_FILE_MAGIC = 0x5A6F12E1;
        const int COMPRESSION_METHOD_NAME_LEN = 32;

        // Magic                                        //   4 bytes
        public readonly EPakVersion Version;            //   4 bytes
        public readonly int SubVersion;
        public readonly long IndexOffset;               //   8 bytes
        public readonly long IndexSize;                 //   8 bytes
        public readonly FSHAHash IndexHash;             //  20 bytes
        public readonly bool bEncryptedIndex;           //   1 byte
        public readonly FGuid EncryptionKeyGuid;        //  16 bytes
        public readonly string[] CompressionMethods;    // 160 bytes
                                                        // 221 bytes total

        internal const long SERIALIZED_SIZE = 4 * 2 + 8 * 2 + 20 + /* new fields */ 1 + 16;

        internal FPakInfo ReadPakInfo(BinaryReader reader)
        {
            long offset = reader.BaseStream.Length - SERIALIZED_SIZE;
            long terminator = offset - 300;
            int maxNumCompressionMethods = 0;
            while (offset > terminator)
            {
                reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                FPakInfo info = new FPakInfo(reader, maxNumCompressionMethods);
                if (info.Version != EPakVersion.INVALID)
                    return info;
                else
                {
                    offset -= COMPRESSION_METHOD_NAME_LEN;
                    maxNumCompressionMethods++;
                }
            }
            return default;
        }

        internal FPakInfo(BinaryReader reader, int maxNumCompressionMethods)
        {
            // Serialize if version is at least EPakVersion.ENCRYPTION_KEY_GUID
            EncryptionKeyGuid = new FGuid(reader);
            bEncryptedIndex = reader.ReadByte() != 0;

            if (reader.ReadUInt32() != PAK_FILE_MAGIC)
            {
                Version = EPakVersion.INVALID;
                SubVersion = 0;
                IndexOffset = 0;
                IndexSize = 0;
                IndexHash = default;
                CompressionMethods = null;
                return;
                // UE4 tries to handle old versions but I'd rather not deal with that right now
                //throw new FileLoadException("Invalid pak magic");
            }

            Version = (EPakVersion)reader.ReadInt32();
            int b;
            if (maxNumCompressionMethods == 5 && Version == EPakVersion.FNAME_BASED_COMPRESSION_METHOD) // UE4.23
                b = (((int)Version) << 4) | (1);
            else
                b = (((int)Version) << 4) | (0);
            SubVersion = b & 15;
            IndexOffset = reader.ReadInt64();
            IndexSize = reader.ReadInt64();
            IndexHash = new FSHAHash(reader);

            if (Version < EPakVersion.FNAME_BASED_COMPRESSION_METHOD)
            {
                CompressionMethods = new string[] { "Zlib", "Gzip", "Oodle" };
            }
            else
            {
                int BufferSize = COMPRESSION_METHOD_NAME_LEN * maxNumCompressionMethods;
                byte[] Methods = reader.ReadBytes(BufferSize);
                var MethodList = new List<string>(maxNumCompressionMethods);
                for (int i = 0; i < maxNumCompressionMethods; i++)
                {
                    if (Methods[i*COMPRESSION_METHOD_NAME_LEN] != 0)
                    {
                        MethodList.Add(Encoding.ASCII.GetString(Methods, i * COMPRESSION_METHOD_NAME_LEN, COMPRESSION_METHOD_NAME_LEN).TrimEnd('\0'));
                    }
                }
                CompressionMethods = MethodList.ToArray();
            }
        }
    }
}
