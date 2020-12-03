using FModel.Utils;
using FModel.ViewModels.Buttons;
using FModel.ViewModels.DataGrid;
using FModel.ViewModels.ListBox;
using FModel.ViewModels.StatusBar;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using FModel.PakReader.Pak;
using FModel.PakReader.IO;

namespace FModel.ViewModels.Treeview
{
    static class SortedTreeviewVm
    {
        public static SortedTreeviewViewModel gameFilesPath = new SortedTreeviewViewModel();

        public static string GetFullPath(this TreeviewViewModel selected)
        {
            StringBuilder sb = new StringBuilder();
            sb.Insert(0, "/" + selected.Header);
            while (selected.Parent != null)
            {
                sb.Insert(0, "/" + selected.Parent.Header);
                selected = selected.Parent;
            }
            return sb.ToString();
        }

        public static void JumpToFolder(string node)
        {
            bool done = false;
            var childsView = gameFilesPath.ChildrensView;

            while (!done)
            {
                bool found = false;
                foreach (TreeviewViewModel tv in childsView)
                {
                    int sep = node.IndexOf("/");
                    if (node.StartsWith(tv.Header) && node.Substring(0, sep > 0 ? sep : node.Length).Length.Equals(tv.Header.Length))
                    {
                        found = true;
                        tv.IsExpanded = true;
                        childsView = tv.ChildrensView;

                        node = node.Substring(node.IndexOf("/") + 1);
                        if (node.Equals(tv.Header) && node.Length.Equals(tv.Header.Length))
                        {
                            if (tv.IsSelected) // i hope this will trigger a "OnSelectedItemChanged" in case user is already in the selected folder
                                tv.IsSelected = false; // so it'll trigger the auto listbox selection

                            tv.IsSelected = true;
                            done = true;
                        }
                        break;
                    }
                }
                done = !found && !done;
            }
        }

        public static async Task ExtractFolder(TreeviewViewModel treeItem)
        {
            var entriesToExtract = new List<ListBoxViewModel>();
            string fullPath = treeItem.GetFullPath().Substring(1);
            foreach (var entry in DataGridVm.dataGridViewModel) // current loaded pak files
            {
                var m = Regex.Match(entry.Name, $"{fullPath}/*", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    if (Globals.CachedPakFiles.TryGetValue(entry.ContainerFile, out PakFileReader pak))
                    {
                        if (pak.TryGetValue("/" + entry.Name.Substring(0, entry.Name.LastIndexOf(".")), out var pakEntry)) // remove the extension to get the entry
                        {
                            entriesToExtract.Add(new ListBoxViewModel
                            {
                                Content = pakEntry.GetNameWithExtension(),
                                ReaderEntry = pakEntry
                            });
                        }
                    }
                    else if (Globals.CachedIoStores.TryGetValue(entry.ContainerFile, out FFileIoStoreReader IoStore))
                    {
                        if (IoStore.TryGetValue("/" + entry.Name.Substring(0, entry.Name.LastIndexOf(".")), out var IoEntry)) // remove the extension to get the entry
                        {
                            entriesToExtract.Add(new ListBoxViewModel
                            {
                                Content = IoEntry.GetNameWithExtension(),
                                ReaderEntry = IoEntry
                            });
                        }
                    }
                }
            }

            if (entriesToExtract.Any())
                await Assets.GetUserSelection(entriesToExtract);
        }

        public static async Task ExportFolder(TreeviewViewModel treeItem)
        {
            string fullPath = treeItem.GetFullPath().Substring(1);
            Stopwatch timer = Stopwatch.StartNew();
            ExtractStopVm.stopViewModel.IsEnabled = true;
            ExtractStopVm.extractViewModel.IsEnabled = false;
            StatusBarVm.statusBarViewModel.Set(string.Empty, Properties.Resources.Loading);
            Tasks.TokenSource = new CancellationTokenSource();

            await Task.Run(() =>
            {
                foreach (var entry in DataGridVm.dataGridViewModel) // current loaded pak files
                {
                    if (Tasks.TokenSource.IsCancellationRequested)
                        throw new TaskCanceledException(Properties.Resources.Canceled);

                    var m = Regex.Match(entry.Name, $"{fullPath}/*", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        if (Globals.CachedPakFiles.TryGetValue(entry.ContainerFile, out PakFileReader pak))
                        {
                            if (pak.TryGetValue("/" + entry.Name.Substring(0, entry.Name.LastIndexOf(".")), out var pakEntry)) // remove the extension to get the entry
                            {
                                Assets.Export(pakEntry, true);
                            }
                        }
                        else if (Globals.CachedIoStores.TryGetValue(entry.ContainerFile, out FFileIoStoreReader IoStore))
                        {
                            if (IoStore.TryGetValue("/" + entry.Name.Substring(0, entry.Name.LastIndexOf(".")), out var IoEntry)) // remove the extension to get the entry
                            {
                                Assets.Export(IoEntry, true);
                            }
                        }
                    }
                }
            }).ContinueWith(t =>
            {
                timer.Stop();
                ExtractStopVm.stopViewModel.IsEnabled = false;
                ExtractStopVm.extractViewModel.IsEnabled = true;

                if (t.Exception != null) Tasks.TaskCompleted(t.Exception);
                else StatusBarVm.statusBarViewModel.Set(string.Format(Properties.Resources.TimeElapsed, timer.ElapsedMilliseconds), Properties.Resources.Success);
            },
            TaskScheduler.FromCurrentSynchronizationContext());
        }
    }

    public class SortedTreeviewViewModel : PropertyChangedBase
    {
        public ICollectionView ChildrensView { get; set; }
        public ObservableCollection<TreeviewViewModel> Childrens { get; set; }

        public SortedTreeviewViewModel()
        {
            Childrens = new ObservableCollection<TreeviewViewModel>();
            ChildrensView = new ListCollectionView(Childrens) { SortDescriptions = { new SortDescription("Header", ListSortDirection.Ascending) } };
        }
    }
}
