using FModel.Utils;
using FModel.ViewModels.ListBox;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace FModel.ViewModels.Treeview
{
    public class TreeviewViewModel : PropertyChangedBase
    {
        public CollectionView ChildrensView { get; set; }
        public ObservableCollection<TreeviewViewModel> Childrens { get; set; }

        public TreeviewViewModel(string header, dynamic parent)
        {
            Childrens = new ObservableCollection<TreeviewViewModel>();
            ChildrensView = new ListCollectionView(Childrens)
            {
                SortDescriptions =
                    {
                        new SortDescription("Header", ListSortDirection.Ascending)
                    }
            };
            Header = header;
            Parent = parent as TreeviewViewModel;
        }

        private string _header;
        public string Header
        {
            get { return _header; }

            set { this.SetProperty(ref this._header, value); }
        }
        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }

            set { this.SetProperty(ref this._isSelected, value); }
        }
        private bool _isExpanded;
        public bool IsExpanded
        {
            get { return _isExpanded; }

            set { this.SetProperty(ref this._isExpanded, value); }
        }
        private TreeviewViewModel _parent;
        public TreeviewViewModel Parent
        {
            get { return _parent; }

            set { this.SetProperty(ref this._parent, value); }
        }
        private Dictionary<string, ObservableSortedList<ListBoxViewModel>> _gameFiles = new Dictionary<string, ObservableSortedList<ListBoxViewModel>>();
        public Dictionary<string, ObservableSortedList<ListBoxViewModel>> GameFiles
        {
            get { return _gameFiles; }

            set { this.SetProperty(ref this._gameFiles, value); }
        }
    }
}
