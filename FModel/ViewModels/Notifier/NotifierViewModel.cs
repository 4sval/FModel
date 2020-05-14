using FModel.Windows.CustomNotifier;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ToastNotifications.Core;

namespace FModel.ViewModels.Notifier
{
    public class NotifierViewModel : NotificationBase, INotifyPropertyChanged
    {
        private CustomNotifier _displayPart;

        public override NotificationDisplayPart DisplayPart => _displayPart ?? (_displayPart = new CustomNotifier(this));

        public NotifierViewModel(string title, string message, string icon, MessageOptions messageOptions) : base(message, messageOptions)
        {
            Title = title;
            Message = message;
            Icon = icon;
        }

        private string _title;
        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        private string _message;
        public new string Message
        {
            get { return _message; }
            set
            {
                _message = value;
                OnPropertyChanged();
            }
        }

        private string _icon;
        public string Icon
        {
            get { return _icon; }
            set
            {
                _icon = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
