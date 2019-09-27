using PakReader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FModel.Methods.Utilities
{
    static class ListBoxUtility
    {
        public static List<IEnumerable<string>> FilesListWithoutPath { get; set; }

        public static async Task PopulateListBox(TreeViewItem sItem)
        {
            FWindow.FMain.ListBox_Main.Items.Clear();
            FWindow.FMain.FilterTextBox_Main.Text = string.Empty;

            string path = TreeViewUtility.GetFullPath(sItem);

            FilesListWithoutPath = new List<IEnumerable<string>>();
            if (!string.IsNullOrEmpty(FWindow.FCurrentPAK))
            {
                IEnumerable<string> filesWithoutPath = PAKEntries.PAKToDisplay[FWindow.FCurrentPAK]
                    .Where(x => x.Name.Contains(path + "/" + Path.GetFileName(x.Name)))
                    .Select(x => Path.GetFileName(x.Name));

                if (filesWithoutPath != null) { FilesListWithoutPath.Add(filesWithoutPath); }
            }
            else
            {
                foreach (FPakEntry[] PAKsFileInfos in PAKEntries.PAKToDisplay.Values)
                {
                    IEnumerable<string> filesWithoutPath = PAKsFileInfos
                        .Where(x => x.Name.Contains(path + "/" + Path.GetFileName(x.Name)))
                        .Select(x => Path.GetFileName(x.Name));

                    if (filesWithoutPath != null) { FilesListWithoutPath.Add(filesWithoutPath); }
                }
            }

            if (FilesListWithoutPath != null && FilesListWithoutPath.Any())
            {
                await Task.Run(() =>
                {
                    FillMeThisPls();
                }).ContinueWith(TheTask =>
                {
                    TasksUtility.TaskCompleted(TheTask.Exception);
                });
            }

            FWindow.FMain.Button_Extract.IsEnabled = FWindow.FMain.ListBox_Main.SelectedIndex >= 0;
        }

        private static void FillMeThisPls()
        {
            foreach (IEnumerable<string> filesFromOnePak in FilesListWithoutPath)
            {
                foreach (string file in filesFromOnePak.OrderBy(s => s))
                {
                    string name = file;
                    if (name.EndsWith(".uasset") || name.EndsWith(".uexp") || name.EndsWith(".ubulk"))
                    {
                        name = name.Substring(0, name.LastIndexOf('.'));
                    }

                    FWindow.FMain.Dispatcher.InvokeAsync(() =>
                    {
                        if (!FWindow.FMain.ListBox_Main.Items.Contains(name))
                        {
                            FWindow.FMain.ListBox_Main.Items.Add(name);
                        }
                    });
                }
            }
        }

        public static async Task FilterListBox()
        {
            FWindow.FMain.ListBox_Main.Items.Clear();
            string FilterText = FWindow.FMain.FilterTextBox_Main.Text;

            if (FilesListWithoutPath != null && FilesListWithoutPath.Any())
            {
                await Task.Run(() =>
                {
                    foreach (IEnumerable<string> filesFromOnePak in FilesListWithoutPath)
                    {
                        foreach (string file in filesFromOnePak.OrderBy(s => s))
                        {
                            string name = file;
                            if (name.EndsWith(".uasset") || name.EndsWith(".uexp") || name.EndsWith(".ubulk"))
                            {
                                name = name.Substring(0, name.LastIndexOf('.'));
                            }

                            if (!string.IsNullOrEmpty(FilterText))
                            {
                                if (CaseInsensitiveContains(name, FilterText))
                                {
                                    FWindow.FMain.Dispatcher.InvokeAsync(() =>
                                    {
                                        if (!FWindow.FMain.ListBox_Main.Items.Contains(name))
                                        {
                                            FWindow.FMain.ListBox_Main.Items.Add(name);
                                        }
                                    });
                                }
                            }
                            else
                            {
                                FWindow.FMain.Dispatcher.InvokeAsync(() =>
                                {
                                    if (!FWindow.FMain.ListBox_Main.Items.Contains(name))
                                    {
                                        FWindow.FMain.ListBox_Main.Items.Add(name);
                                    }
                                });
                            }
                        }
                    }
                }).ContinueWith(TheTask =>
                {
                    TasksUtility.TaskCompleted(TheTask.Exception);
                });
            }
        }
        private static bool CaseInsensitiveContains(string text, string value, StringComparison stringComparison = StringComparison.CurrentCultureIgnoreCase)
        {
            return text.IndexOf(value, stringComparison) >= 0;
        }
    }
}
