using FModel.Methods.Utilities;
using RestSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FProp = FModel.Properties.Settings;

namespace FModel.Methods.BackupsManager
{
    class RegisterDownloadedBackups
    {
        private static List<BackupInfosEntry> BackupsFromDropbox { get; set; }
        private static readonly string OUTPUT_PATH = FProp.Default.FOutput_Path;

        public static void LoadBackupFiles()
        {
            BackupsFromDropbox = EndpointsUtility.GetBackupsFromDropbox();
            if (BackupsFromDropbox != null && BackupsFromDropbox.Any())
            {
                foreach (BackupInfosEntry Backup in BackupsFromDropbox)
                {
                    FWindow.FMain.Dispatcher.InvokeAsync(() =>
                    {
                        MenuItem MI_Backup = new MenuItem();
                        MI_Backup.Header = Backup.TheFileName;
                        MI_Backup.Click += new RoutedEventHandler(MI_Backup_Click);

                        FWindow.FMain.MI_DownloadBackups.Items.Add(MI_Backup);
                    });
                }
            }
        }

        private static void MI_Backup_Click(object sender, RoutedEventArgs e)
        {
            MenuItem ClickedBackup = sender as MenuItem;
            if (BackupsFromDropbox != null && BackupsFromDropbox.Any())
            {
                foreach (BackupInfosEntry Backup in BackupsFromDropbox)
                {
                    string BackupFileName = Backup.TheFileName;
                    if (string.Equals(BackupFileName, ClickedBackup.Header))
                    {
                        new UpdateMyProcessEvents($"Downloading {Backup.TheFileName}", "Waiting").Update();

                        RestClient EndpointClient = new RestClient(Backup.TheFileDownload);
                        RestRequestAsyncHandle asyncHandle = EndpointClient.ExecuteAsync(new RestRequest(Method.GET), response => {
                            File.WriteAllText($"{OUTPUT_PATH}\\Backup\\{BackupFileName}", response.Content); //FILE WILL ALWAYS EXIST
                            if (new FileInfo($"{OUTPUT_PATH}\\Backup\\{BackupFileName}").Length > 0) //HENCE WE CHECK THE LENGTH
                            {
                                new UpdateMyProcessEvents($"\\Backup\\{BackupFileName} successfully downloaded", "Success").Update();
                            }
                            else
                            {
                                File.Delete($"{OUTPUT_PATH}\\Backup\\{BackupFileName}"); //WE DELETE THE EMPTY FILE CREATED
                                new UpdateMyProcessEvents($"Error while downloading {BackupFileName}", "Error").Update();
                            }
                        });
                    }
                }
            }
        }
    }
}
