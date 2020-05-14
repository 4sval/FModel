using PakReader.Parsers.Objects;
using System.Collections.ObjectModel;
using System.Windows;

namespace FModel.ViewModels.DataGrid
{
    static class DataGridVm
    {
        public static ObservableCollection<DataGridViewModel> dataGridViewModel = new ObservableCollection<DataGridViewModel>();

        public static void Add(this ObservableCollection<DataGridViewModel> vm, string name, string ext, string pakfile)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                vm.Add(new DataGridViewModel
                {
                    Name = name,
                    Extensions = ext,
                    PakFile = pakfile
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

        private string _pakFile;
        public string PakFile
        {
            get { return _pakFile; }

            set { this.SetProperty(ref this._pakFile, value); }
        }
    }
}
