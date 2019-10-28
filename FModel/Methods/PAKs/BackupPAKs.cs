using FModel.Methods.Utilities;
using PakReader;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FProp = FModel.Properties.Settings;

namespace FModel.Methods.PAKs
{
    static class BackupPAKs
    {
        private static readonly string BACKUP_FILE_PATH = FProp.Default.FOutput_Path + "\\Backups\\FortniteGame_" + DateTime.Now.ToString("MMddyyyy") + ".fbkp";

        public static async Task CreateBackupFile()
        {
            await Task.Run(() =>
            {
                GetPAKsFileInfos();
            }).ContinueWith(TheTask =>
            {
                TasksUtility.TaskCompleted(TheTask.Exception);
            });
        }

        private static void GetPAKsFileInfos()
        {
            if (PAKEntries.PAKEntriesList != null && PAKEntries.PAKEntriesList.Any())
            {
                new UpdateMyProcessEvents($"Writing {Path.GetFileName(BACKUP_FILE_PATH)}", "Waiting").Update();
                Directory.CreateDirectory(Path.GetDirectoryName(BACKUP_FILE_PATH));
                var fileStream = new FileStream(BACKUP_FILE_PATH, FileMode.Create);
                var writer = new BinaryWriter(fileStream);
                foreach (PAKInfosEntry Pak in PAKEntries.PAKEntriesList)
                {
                    byte[] AESKey = null;
                    if (Pak.bTheDynamicPAK)
                    {
                        if (AESEntries.AESEntriesList != null && AESEntries.AESEntriesList.Any())
                        {
                            string AESFromManager = AESEntries.AESEntriesList.Where(x => string.Equals(x.ThePAKName, Path.GetFileNameWithoutExtension(Pak.ThePAKPath))).Select(x => x.ThePAKKey).FirstOrDefault();
                            if (!string.IsNullOrEmpty(AESFromManager))
                            {
                                AESKey = AESUtility.StringToByteArray(AESFromManager);
                            }
                        }
                    }
                    else
                    {
                        AESKey = AESUtility.StringToByteArray(FProp.Default.FPak_MainAES);
                    }

                    if (AESKey != null)
                    {
                        PakReader.PakReader reader = new PakReader.PakReader(Pak.ThePAKPath, AESKey);
                        if (reader != null)
                        {
                            new UpdateMyProcessEvents($"{Path.GetFileNameWithoutExtension(Pak.ThePAKPath)} mount point: {reader.MountPoint}", "Waiting").Update();

                            foreach (FPakEntry entry in reader.FileInfos)
                            {
                                writer.Write(entry.Pos);
                                writer.Write(entry.Size);
                                writer.Write(entry.UncompressedSize);
                                writer.Write(entry.Encrypted);
                                writer.Write(entry.StructSize);
                                writer.Write(entry.Name);
                                writer.Write(entry.CompressionMethod);
                            }
                        }
                    }
                }
                fileStream.Close();
                writer.Close();
                if (new FileInfo(BACKUP_FILE_PATH).Length > 0) //HENCE WE CHECK THE LENGTH
                {
                    new UpdateMyProcessEvents($"\\Backups\\{Path.GetFileName(BACKUP_FILE_PATH)} successfully created", "Success").Update();
                }
                else
                {
                    File.Delete(BACKUP_FILE_PATH); //WE DELETE THE EMPTY FILE CREATED
                    new UpdateMyProcessEvents($"Error while creating {Path.GetFileName(BACKUP_FILE_PATH)}", "Error").Update();
                }
            }
        }
    }
}
