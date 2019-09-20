using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using FProp = FModel.Properties.Settings;

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

        public static void DisableNonKeyedPAKs()
        {
            if (PAKEntries.PAKEntriesList != null && PAKEntries.PAKEntriesList.Any())
            {
                foreach (MenuItem MI_Pak in FWindow.FMain.MI_LoadOnePAK.Items)
                {
                    MI_Pak.IsEnabled = false;

                    if (!string.IsNullOrEmpty(FProp.Default.FPak_MainAES))
                    {
                        foreach (PAKInfosEntry Pak in PAKEntries.PAKEntriesList.Where(x => !x.bTheDynamicPAK))
                        {
                            if (string.Equals(Path.GetFileName(Pak.ThePAKPath), MI_Pak.Header))
                            {
                                MI_Pak.IsEnabled = true;
                            }
                        }
                    }

                    foreach (PAKInfosEntry Pak in PAKEntries.PAKEntriesList.Where(x => x.bTheDynamicPAK))
                    {
                        if (AESEntries.AESEntriesList != null && AESEntries.AESEntriesList.Any())
                        {
                            AESInfosEntry AESFromManager = AESEntries.AESEntriesList.Where(x => string.Equals(x.ThePAKName, Path.GetFileNameWithoutExtension(Pak.ThePAKPath))).FirstOrDefault();
                            if (!string.IsNullOrEmpty(AESFromManager.ThePAKKey))
                            {
                                if (string.Equals(AESFromManager.ThePAKName, Path.GetFileNameWithoutExtension(MI_Pak.Header.ToString())))
                                {
                                    MI_Pak.IsEnabled = true;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
