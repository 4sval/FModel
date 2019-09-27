using FModel.Methods.Utilities;
using PakReader;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FProp = FModel.Properties.Settings;

namespace FModel.Methods.PAKs
{
    static class BackupPAKs
    {
        private static readonly string OUTPUT_PATH = FProp.Default.FOutput_Path;

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
                StringBuilder sb = new StringBuilder();
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
                                sb.Append(entry.Name.Substring(1) + "\n"); //SUBSTRING(1) TO REMOVE THE FIRST '/'
                            }
                        }
                    }
                }

                string BackupFileName = $"FortniteGame_{DateTime.Now.ToString("MMddyyyy")}.txt";
                File.WriteAllText($"{OUTPUT_PATH}\\Backup\\{BackupFileName}", sb.ToString()); //FILE WILL ALWAYS EXIST
                if (new FileInfo($"{OUTPUT_PATH}\\Backup\\{BackupFileName}").Length > 0) //HENCE WE CHECK THE LENGTH
                {
                    new UpdateMyProcessEvents($"\\Backup\\{BackupFileName} successfully created", "Success").Update();
                }
                else
                {
                    File.Delete($"{OUTPUT_PATH}\\Backup\\{BackupFileName}"); //WE DELETE THE EMPTY FILE CREATED
                    new UpdateMyProcessEvents($"Error while creating {BackupFileName}", "Error").Update();
                }
            }
        }
    }
}
