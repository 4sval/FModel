using FModel.Utils;
using PakReader.Parsers.Objects;
using System;

namespace FModel.ViewModels.ListBox
{
    static class ListBoxVm
    {
        public static ObservableSortedList<ListBoxViewModel> gameFiles = new ObservableSortedList<ListBoxViewModel>();
        public static ObservableSortedList<ListBoxViewModel2> soundFiles = new ObservableSortedList<ListBoxViewModel2>();
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

    public class ListBoxViewModel2 : PropertyChangedBase, IComparable
    {
        public int CompareTo(object o)
        {
            ListBoxViewModel2 a = this;
            ListBoxViewModel2 b = o as ListBoxViewModel2;
            return string.CompareOrdinal(a.Content, b.Content);
        }

        private string _content;
        public string Content
        {
            get { return _content; }

            set { this.SetProperty(ref this._content, value); }
        }

        private string _fullPath;
        public string FullPath
        {
            get { return _fullPath; }

            set { this.SetProperty(ref this._fullPath, value); }
        }

        private string _folder;
        public string Folder
        {
            get { return _folder; }

            set { this.SetProperty(ref this._folder, value); }
        }

        private byte[] _data;
        public byte[] Data
        {
            get { return _data; }

            set { this.SetProperty(ref this._data, value); }
        }
    }
}
