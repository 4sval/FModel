using System;
using System.IO;
using System.Text;

namespace FModel.Methods.Utilities
{
    static class PAKsUtility
    {
        public static string GetPAKGuid(string PAKPath)
        {
            using (BinaryReader reader = new BinaryReader(File.Open(PAKPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                reader.BaseStream.Seek(reader.BaseStream.Length - 61 - 160, SeekOrigin.Begin);
                uint g1 = reader.ReadUInt32();
                reader.BaseStream.Seek(reader.BaseStream.Length - 57 - 160, SeekOrigin.Begin);
                uint g2 = reader.ReadUInt32();
                reader.BaseStream.Seek(reader.BaseStream.Length - 53 - 160, SeekOrigin.Begin);
                uint g3 = reader.ReadUInt32();
                reader.BaseStream.Seek(reader.BaseStream.Length - 49 - 160, SeekOrigin.Begin);
                uint g4 = reader.ReadUInt32();

                string guid = g1 + "-" + g2 + "-" + g3 + "-" + g4;
                return guid;
            }
        }

        public static string GetEpicGuid(string PAKGuid)
        {
            StringBuilder sB = new StringBuilder();
            foreach (string part in PAKGuid.Split('-'))
            {
                sB.Append(Int64.Parse(part).ToString("X8"));
            }
            return sB.ToString();
        }

        public static uint GetPAKVersion(string PAKPath)
        {
            using (BinaryReader reader = new BinaryReader(File.Open(PAKPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                reader.BaseStream.Seek(reader.BaseStream.Length - 40 - 160, SeekOrigin.Begin);
                uint version = reader.ReadUInt32();

                return version;
            }
        }

        public static bool IsPAKLocked(FileInfo PakFileInfo)
        {
            FileStream stream = null;
            try
            {
                stream = PakFileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            return false;
        }
    }
}
