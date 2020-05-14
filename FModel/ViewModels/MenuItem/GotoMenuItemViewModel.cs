using FModel.ViewModels.Treeview;
using FModel.Windows.UserInput;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace FModel.ViewModels.MenuItem
{
    public class GotoMenuItemViewModel : PropertyChangedBase
    {
        private string _header;
        private bool _isCheckable;
        private bool _isChecked;
        private bool _isEnabled = true;
        private bool _staysOpenOnClick = false;
        private string _inputGestureText;
        private Image _icon;
        private string _directoryPath;
        private GotoMenuItemViewModel _parent;
        private ObservableCollection<GotoMenuItemViewModel> _childrens;
        public string Header
        {
            get { return _header; }

            set { this.SetProperty(ref this._header, value); }
        }
        [JsonIgnore]
        public bool IsCheckable
        {
            get { return _isCheckable; }

            set { this.SetProperty(ref this._isCheckable, value); }
        }
        [JsonIgnore]
        public bool IsChecked
        {
            get { return _isChecked; }

            set { this.SetProperty(ref this._isChecked, value); }
        }
        [JsonIgnore]
        public bool IsEnabled
        {
            get { return _isEnabled; }

            set { this.SetProperty(ref this._isEnabled, value); }
        }
        [JsonIgnore]
        public bool StaysOpenOnClick
        {
            get { return _staysOpenOnClick; }

            set { this.SetProperty(ref this._staysOpenOnClick, value); }
        }
        [JsonIgnore]
        public string InputGestureText
        {
            get { return _inputGestureText; }

            set { this.SetProperty(ref this._inputGestureText, value); }
        }
        [JsonIgnore]
        public Image Icon
        {
            get { return _icon; }

            set { this.SetProperty(ref this._icon, value); }
        }
        [JsonIgnore]
        public GotoMenuItemViewModel Parent
        {
            get { return _parent; }

            set { this.SetProperty(ref this._parent, value); }
        }
        [JsonIgnore]
        public ObservableCollection<GotoMenuItemViewModel> Childrens
        {
            get { return _childrens; }

            set { this.SetProperty(ref this._childrens, value); }
        }
        public string DirectoryPath
        {
            get { return _directoryPath; }

            set { this.SetProperty(ref this._directoryPath, value); }
        }
        [JsonIgnore]
        public ICommand Command
        {
            get
            {
                if (DirectoryAddCanExecute()) // add
                    return new CommandHandler(AddDirectory, () => true);
                else if(GoToCanExecute()) // go
                    return new CommandHandler(GoToOmegalul, () => true);
                else if (DirectoryEditCanExecute()) // edit
                    return new CommandHandler(EditDirectory, () => true);
                else if (DirectoryRemoveCanExecute()) // delete
                    return new CommandHandler(RemoveDirectory, () => true);
                else
                    return null;
            }

            private set { }
        }

        private bool DirectoryAddCanExecute() => Header.Equals(Properties.Resources.AddDirectory);
        private void AddDirectory()
        {
            var input = new GoToUserInput();
            if ((bool)input.ShowDialog())
            {
                var newDir = new GotoMenuItemViewModel
                {
                    Header = input.Name,
                    DirectoryPath = input.Directory
                };
                newDir.Childrens = new ObservableCollection<GotoMenuItemViewModel>
                {
                    new GotoMenuItemViewModel
                    {
                        Header = Properties.Resources.GoTo,
                        Icon = new Image { Source = new BitmapImage(new Uri("/FModel;component/Resources/share.png", UriKind.Relative)) },
                        Parent = newDir
                    },
                    new GotoMenuItemViewModel
                    {
                        Header = Properties.Resources.EditDirectory,
                        Icon = new Image { Source = new BitmapImage(new Uri("/FModel;component/Resources/pencil.png", UriKind.Relative)) },
                        Parent = newDir
                    },
                    new GotoMenuItemViewModel
                    {
                        Header = Properties.Resources.RemoveDirectory,
                        Icon = new Image { Source = new BitmapImage(new Uri("/FModel;component/Resources/delete-forever.png", UriKind.Relative)) },
                        StaysOpenOnClick = true,
                        Parent = newDir
                    }
                };
                
                MenuItems.customGoTos.Add(newDir);
                Properties.Settings.Default.CustomGoTos = JsonConvert.SerializeObject(MenuItems.customGoTos.Skip(2), Formatting.None);
                Properties.Settings.Default.Save();
            }
        }

        private bool DirectoryEditCanExecute() => Header.Equals(Properties.Resources.EditDirectory);
        private void EditDirectory()
        {
            var input = new GoToUserInput(Parent.Header, Parent.DirectoryPath);
            if ((bool)input.ShowDialog())
            {
                Parent.Header = input.Name;
                Parent.DirectoryPath = input.Directory;
                Properties.Settings.Default.CustomGoTos = JsonConvert.SerializeObject(MenuItems.customGoTos.Skip(2), Formatting.None);
                Properties.Settings.Default.Save();
            }
        }

        private bool DirectoryRemoveCanExecute() => Header.Equals(Properties.Resources.RemoveDirectory);
        private void RemoveDirectory()
        {
            MenuItems.customGoTos.Remove(Parent);
            Properties.Settings.Default.CustomGoTos = JsonConvert.SerializeObject(MenuItems.customGoTos.Skip(2), Formatting.None);
            Properties.Settings.Default.Save();
        }

        private bool GoToCanExecute() => Header.Equals(Properties.Resources.GoTo) && !string.IsNullOrEmpty(Parent.DirectoryPath);
        private void GoToOmegalul() => SortedTreeviewVm.JumpToFolder(Parent.DirectoryPath.EndsWith("/") ? Parent.DirectoryPath[0..^1] : Parent.DirectoryPath);
    }
}
