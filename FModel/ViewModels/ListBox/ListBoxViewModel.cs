using FModel.Utils;
using PakReader.Parsers.Objects;
using System;

namespace FModel.ViewModels.ListBox
{
    static class ListBoxVm
    {
        public static ObservableSortedList<ListBoxViewModel> gameFiles = new ObservableSortedList<ListBoxViewModel>();
    }

    public class ListBoxViewModel : PropertyChangedBase, IComparable
    {
        public int CompareTo(object o)
        {
            ListBoxViewModel a = this;
            ListBoxViewModel b = o as ListBoxViewModel;
            return string.CompareOrdinal(a.Content, b.Content);
        }

        private string _content;
        public string Content
        {
            get { return _content; }

            set { this.SetProperty(ref this._content, value); }
        }

        private FPakEntry _pakEntry;
        public FPakEntry PakEntry
        {
            get { return _pakEntry; }

            set { this.SetProperty(ref this._pakEntry, value); }
        }
    }
}
