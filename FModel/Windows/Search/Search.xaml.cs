using FModel.Utils;
using FModel.ViewModels.DataGrid;
using FModel.ViewModels.Treeview;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FModel.Windows.Search
{
    /// <summary>
    /// Logique d'interaction pour Search.xaml
    /// </summary>
    public partial class Search : Window
    {
        public Search()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AssetFilter_TxtBox.Focus();
            TotalAssets_Lbl.Text = string.Format(Properties.Resources.TotalAssetsLoaded, DataGridVm.dataGridViewModel.Count.ToString("# ### ###", new NumberFormatInfo { NumberGroupSeparator = " " }).Trim());
            Assets_DtGrd.ItemsSource = DataGridVm.dataGridViewModel;
        }

        private void OnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Name":
                    e.Column.Header = Properties.Resources.Name;
                    e.Column.Width = new DataGridLength(11, DataGridLengthUnitType.Star);
                    break;
                case "Extensions":
                    e.Column.Header = Properties.Resources.Include;
                    e.Column.Width = new DataGridLength(3, DataGridLengthUnitType.Star);
                    break;
                case "PakFile":
                    e.Column.Header = Properties.Resources.PAK;
                    e.Column.Width = new DataGridLength(4, DataGridLengthUnitType.Star);
                    break;
            }
        }

        private async void OnFilterTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string[] filters = textBox.Text.Trim().Split(' ');
                if (!string.IsNullOrEmpty(filters[0]))
                {
                    var filtered = new ObservableCollection<DataGridViewModel>();
                    await Task.Run(() =>
                    {
                        foreach (DataGridViewModel item in DataGridVm.dataGridViewModel)
                        {
                            bool bSearch = false;
                            if (filters.Length > 1)
                            {
                                foreach (string filter in filters)
                                {
                                    Assets.Filter(filter, item.Name, out bSearch);
                                    if (!bSearch)
                                        break;
                                }
                            }
                            else
                            {
                                Assets.Filter(filters[0], item.Name, out bSearch);
                            }

                            if (bSearch)
                                filtered.Add(item);
                        }
                    }).ContinueWith(t =>
                    {
                        if (t.Exception != null) Tasks.TaskCompleted(t.Exception);
                        else Assets_DtGrd.ItemsSource = filtered;
                    },
                    TaskScheduler.FromCurrentSynchronizationContext());
                }
                else
                {
                    Assets_DtGrd.ItemsSource = DataGridVm.dataGridViewModel;
                }
            }
        }

        private void CM_Copy_DPath_Click(object sender, RoutedEventArgs e)
        {
            if (Assets_DtGrd.HasItems && Assets_DtGrd.SelectedIndex >= 0 && Assets_DtGrd.SelectedItem is DataGridViewModel selectedItem)
                Assets.Copy(selectedItem.Name.Substring(0, selectedItem.Name.LastIndexOf("/") + 1));
        }
        private void CM_Copy_FPath_Click(object sender, RoutedEventArgs e)
        {
            if (Assets_DtGrd.HasItems && Assets_DtGrd.SelectedIndex >= 0 && Assets_DtGrd.SelectedItem is DataGridViewModel selectedItem)
                Assets.Copy(selectedItem.Name, ECopy.Path);
        }
        private void CM_Copy_FName_Click(object sender, RoutedEventArgs e)
        {
            if (Assets_DtGrd.HasItems && Assets_DtGrd.SelectedIndex >= 0 && Assets_DtGrd.SelectedItem is DataGridViewModel selectedItem)
                Assets.Copy(selectedItem.Name, ECopy.File);
        }
        private void CM_Copy_FPath_NoExt_Click(object sender, RoutedEventArgs e)
        {
            if (Assets_DtGrd.HasItems && Assets_DtGrd.SelectedIndex >= 0 && Assets_DtGrd.SelectedItem is DataGridViewModel selectedItem)
                Assets.Copy(selectedItem.Name, ECopy.PathNoExt);
        }
        private void CM_Copy_FName_NoExt_Click(object sender, RoutedEventArgs e)
        {
            if (Assets_DtGrd.HasItems && Assets_DtGrd.SelectedIndex >= 0 && Assets_DtGrd.SelectedItem is DataGridViewModel selectedItem)
                Assets.Copy(selectedItem.Name, ECopy.FileNoExt);
        }

        private void OnSelectedItemChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid dataGrid && dataGrid.SelectedItem is DataGridViewModel)
                GoTo_Btn.IsEnabled = true;
            else
                GoTo_Btn.IsEnabled = false;
        }

        private void OnGoToClick(object sender, RoutedEventArgs e)
        {
            if (Assets_DtGrd.HasItems && Assets_DtGrd.SelectedIndex >= 0 && Assets_DtGrd.SelectedItem is DataGridViewModel selectedItem)
            {
                string folders = selectedItem.Name.Substring(0, selectedItem.Name.LastIndexOf("/"));

                // i'd like to make the file selected but i have to wait for ListBoxVm.gameFiles to be populated by the new items
                // select the file in the items, bring it into view
                // and unselect it so it's not gonna count as a selected file to extract afterward
                // but i have no idea how to make it work so here's the workaround
                Globals.bSearch = true;
                Globals.sSearch = selectedItem.Name.Substring(selectedItem.Name.LastIndexOf("/") + 1);

                SortedTreeviewVm.JumpToFolder(folders); // this will trigger ListBoxVm.gameFiles population

                WindowState = WindowState.Minimized; // minimize search window
                FWindows.GetOpenedWindow<Window>("FModel").Focus(); // focus main window
            }
        }
    }
}
