using FModel.Methods.Utilities;
using PakReader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using FProp = FModel.Properties.Settings;

namespace FModel.Methods.PAKs
{
    static class BackupPAKs
    {
        public static readonly XmlSerializer serializer = new XmlSerializer(typeof(List<FPakEntry>));
        private static readonly string BACKUP_FILE_PATH = FProp.Default.FOutput_Path + "\\Backups\\FortniteGame_" + DateTime.Now.ToString("MMddyyyy") + ".xml";

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
                List<FPakEntry> BackupList = new List<FPakEntry>();
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
                                BackupList.Add(entry);
                            }
                        }
                    }
                }

                new UpdateMyProcessEvents($"Writing {Path.GetFileName(BACKUP_FILE_PATH)}", "Waiting").Update();
                Directory.CreateDirectory(Path.GetDirectoryName(BACKUP_FILE_PATH));
                using (var fileStream = new FileStream(BACKUP_FILE_PATH, FileMode.Create))
                {
                    serializer.Serialize(fileStream, BackupList);
                }
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
