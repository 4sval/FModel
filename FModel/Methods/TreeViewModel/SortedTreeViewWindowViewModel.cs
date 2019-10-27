using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace FModel.Methods.TreeViewModel
{
    public class SortedTreeViewWindowViewModel : PropertyChangedBase
    {
        private string _newValueString;
        public string NewValue { get; set; }

        public string NewValueString
        {
            get { return _newValueString; }
            set
            {
                _newValueString = value;
                NewValue = value;

                OnPropertyChanged("NewValueString");
            }
        }

        public TreeViewModel SelectedItem { get; set; }

        public ObservableCollection<TreeViewModel> Items { get; set; }

        public ICollectionView ItemsView { get; set; }

        public SortedTreeViewWindowViewModel()
        {
            Items = new ObservableCollection<TreeViewModel>();
            ItemsView = new ListCollectionView(Items) { SortDescriptions = { new SortDescription("Value", ListSortDirection.Ascending) } };
        }

        public void AddNewItem()
        {
            ObservableCollection<TreeViewModel> targetcollection = Items;

            if (!string.IsNullOrEmpty(NewValue) && !targetcollection.Any(x => x.Value == NewValue))
            {
                targetcollection.Add(new TreeViewModel(NewValue));
                NewValueString = string.Empty;
            }

        }
    }
}
