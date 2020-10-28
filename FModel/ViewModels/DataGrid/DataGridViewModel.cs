using System.Collections.ObjectModel;
using System.Windows;

namespace FModel.ViewModels.DataGrid
{
    static class DataGridVm
    {
        public static ObservableCollection<DataGridViewModel> dataGridViewModel = new ObservableCollection<DataGridViewModel>();

        public static void Add(this ObservableCollection<DataGridViewModel> vm, string name, string ext, string containerFile)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                vm.Add(new DataGridViewModel
                {
                    Name = name,
                    Extensions = ext,
                    ContainerFile = containerFile
                });
            });
        }
    }

    public class DataGridViewModel : PropertyChangedBase
    {
        private string _name;
        public string Name
        {
            get { return _name; }

            set { this.SetProperty(ref this._name, value); }
        }

        private string _extensions;
        public string Extensions
        {
            get { return _extensions; }

            set { this.SetProperty(ref this._extensions, value); }
        }

        private string _containerFile;
        public string ContainerFile
        {
            get { return _containerFile; }

            set { this.SetProperty(ref this._containerFile, value); }
        }
    }
}
