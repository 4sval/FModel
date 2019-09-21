using FModel.Methods.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FProp = FModel.Properties.Settings;

namespace FModel.Methods.PAKs
{
    static class RegisterFromPath
    {
        private static readonly string PAK_PATH = FProp.Default.FPak_Path;

        public static void FilterPAKs()
        {
            if (Directory.Exists(PAK_PATH))
            {
                PAKEntries.PAKEntriesList = new List<PAKInfosEntry>();
                foreach (string Pak in GetPAKsFromPath())
                {
                    if (!PAKsUtility.IsPAKLocked(new FileInfo(Pak)))
                    {
                        if (PAKsUtility.GetPAKVersion(Pak) == 8)
                        {
                            string PAKGuid = PAKsUtility.GetPAKGuid(Pak);
                            if (string.Equals(PAKGuid, "0-0-0-0")) //MAIN PAK FILES
                            {
                                PAKEntries.PAKEntriesList.Add(new PAKInfosEntry(Pak, PAKGuid, false));
                                FWindow.FMain.Dispatcher.InvokeAsync(() =>
                                {
                                    MenuItem MI_Pak = new MenuItem();
                                    MI_Pak.Header = Path.GetFileName(Pak);
                                    MI_Pak.Click += new RoutedEventHandler(FWindow.FMain.MI_Pak_Click);

                                    FWindow.FMain.MI_LoadOnePAK.Items.Add(MI_Pak);
                                });
                            }
                            else if (!string.Equals(PAKGuid, "0-0-0-0")) //DYNAMIC PAK FILES
                            {
                                PAKEntries.PAKEntriesList.Add(new PAKInfosEntry(Pak, PAKGuid, true));
                                FWindow.FMain.Dispatcher.InvokeAsync(() =>
                                {
                                    MenuItem MI_Pak = new MenuItem();
                                    MI_Pak.Header = Path.GetFileName(Pak);
                                    MI_Pak.Click += new RoutedEventHandler(FWindow.FMain.MI_Pak_Click);

                                    FWindow.FMain.MI_LoadOnePAK.Items.Add(MI_Pak);
                                });
                            }
                        }
                        else { new UpdateMyProcessEvents($"Unsupported .PAK Version for {Path.GetFileName(Pak)}", "Error").Update(); }
                    }
                    else
                    {
                        new UpdateMyConsole(Path.GetFileName(Pak), CColors.Blue).Append();
                        new UpdateMyConsole(" is locked by another process.", CColors.White, true).Append();
                    }
                }

                FWindow.FMain.Dispatcher.InvokeAsync(() =>
                {
                    FWindow.FMain.MI_LoadOnePAK.IsEnabled = true;
                    FWindow.FMain.MI_LoadAllPAKs.IsEnabled = true;
                    FWindow.FMain.MI_BackupPAKs.IsEnabled = true;
                });
            }
            else { new UpdateMyProcessEvents(".PAK Files Input Path is missing", "Error").Update(); }
        }

        private static IEnumerable<string> GetPAKsFromPath()
        {
            return Directory.GetFiles(PAK_PATH).Where(x => x.EndsWith(".pak"));
        }
    }
}