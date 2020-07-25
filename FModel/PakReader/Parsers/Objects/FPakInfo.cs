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

        internal const long _SIZE = 4 * 2 + 8 * 2 + 20 + 1 + 16;
        internal const long _SIZE8 = _SIZE + 4 * 32;
        internal const long _SIZE8A = _SIZE8 + 32;
        internal const long _SIZE9 = _SIZE8A + 1;

        internal FPakInfo ReadPakInfo(BinaryReader reader)
        {
            long fileSize = reader.BaseStream.Length;
            long[] OffsetsToTry = new long[4] { _SIZE, _SIZE8, _SIZE8A, _SIZE9 };
            for (int i = 0; i < OffsetsToTry.Length; i++)
            {
                if (fileSize - OffsetsToTry[i] > 0)
                {
                    reader.BaseStream.Seek(fileSize - OffsetsToTry[i], SeekOrigin.Begin);
                    FPakInfo info = new FPakInfo(reader, OffsetsToTry[i]);
                    if (info.Version != EPakVersion.INVALID)
                        return info;
                }
            }
            return default;
        }

        internal FPakInfo(BinaryReader reader, long offset)
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
            SubVersion = (offset == _SIZE8A && Version == EPakVersion.FNAME_BASED_COMPRESSION_METHOD) ? 1 : 0;
            IndexOffset = reader.ReadInt64();
            IndexSize = reader.ReadInt64();
            IndexHash = new FSHAHash(reader);
            if (Version == EPakVersion.FROZEN_INDEX)
                reader.ReadByte(); // bIndexIsFrozen

            if (Version < EPakVersion.FNAME_BASED_COMPRESSION_METHOD)
            {
                CompressionMethods = new string[] { "Zlib", "Gzip", "Oodle" };
            }
            else
            {
                int BufferSize = COMPRESSION_METHOD_NAME_LEN * 4;
                byte[] Methods = reader.ReadBytes(BufferSize);
                var MethodList = new List<string>(4);
                for (int i = 0; i < 4; i++)
                {
                    if (Methods[i*COMPRESSION_METHOD_NAME_LEN] != 0)
                    {
                        MethodList.Add(Encoding.ASCII.GetString(Methods, i * COMPRESSION_METHOD_NAME_LEN, COMPRESSION_METHOD_NAME_LEN).TrimEnd('\0'));
                    }
                }
                CompressionMethods = MethodList.ToArray();
            }

            if (Version < EPakVersion.INDEX_ENCRYPTION)
                bEncryptedIndex = false;
            if (Version < EPakVersion.ENCRYPTION_KEY_GUID)
                EncryptionKeyGuid = new FGuid(0u, 0u, 0u, 0u);
        }
    }
}
