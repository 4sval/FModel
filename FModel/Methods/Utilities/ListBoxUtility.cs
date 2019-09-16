using PakReader;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FModel.Methods.Utilities
{
    class ListBoxUtility
    {
        public static async void PopulateListBox(TreeViewItem sItem)
        {
            FWindow.FMain.ListBox_Main.Items.Clear();
            FWindow.FMain.FilterTextBox_Main.Text = string.Empty;

            FWindow.FCurrentAssetParentPath = TreeViewUtility.GetFullPath(sItem);

            List<IEnumerable<string>> FilesListWithoutPath = new List<IEnumerable<string>>();
            if (!string.IsNullOrEmpty(FWindow.FCurrentPAK))
            {
                IEnumerable<string> filesWithoutPath = PAKEntries.PAKToDisplay[FWindow.FCurrentPAK]
                    .Where(x => x.Name.Contains(FWindow.FCurrentAssetParentPath + "/" + Path.GetFileName(x.Name)))
                    .Select(x => Path.GetFileName(x.Name));

                if (filesWithoutPath != null) { FilesListWithoutPath.Add(filesWithoutPath); }
            }
            else
            {
                foreach (FPakEntry[] PAKsFileInfos in PAKEntries.PAKToDisplay.Values)
                {
                    IEnumerable<string> filesWithoutPath = PAKsFileInfos
                        .Where(x => x.Name.Contains(FWindow.FCurrentAssetParentPath + "/" + Path.GetFileName(x.Name)))
                        .Select(x => Path.GetFileName(x.Name));

                    if (filesWithoutPath != null) { FilesListWithoutPath.Add(filesWithoutPath); }
                }
            }

            if (FilesListWithoutPath.Any())
            {
                await Task.Run(() =>
                {
                    FillMeThisPls(FilesListWithoutPath);
                }).ContinueWith(TheTask =>
                {
                    TasksUtility.TaskCompleted(TheTask.Exception);
                });
            }

            FWindow.FMain.Button_Export.IsEnabled = FWindow.FMain.ListBox_Main.SelectedIndex >= 0;
        }

        private static void FillMeThisPls(List<IEnumerable<string>> FilesListWithoutPath)
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
    }
}
