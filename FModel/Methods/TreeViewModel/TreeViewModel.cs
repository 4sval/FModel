using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace FModel.Methods.TreeViewModel
{
    /// <summary>
    /// https://stackoverflow.com/questions/17949777/numerically-sort-a-list-of-treeviewitems-in-c-sharp
    /// </summary>
    public class TreeViewModel : PropertyChangedBase
    {
        public string Value { get; set; }

        public ObservableCollection<TreeViewModel> Items { get; set; }

        public CollectionView ItemsView { get; set; }

        public TreeViewModel(string value)
        {
            Items = new ObservableCollection<TreeViewModel>();
            ItemsView = new ListCollectionView(Items)
            {
                SortDescriptions =
                    {
                        new SortDescription("Value",ListSortDirection.Ascending)
                    }
            };
            Value = value;
        }
    }
}

