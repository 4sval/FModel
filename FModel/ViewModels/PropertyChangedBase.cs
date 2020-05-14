using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FModel.ViewModels
{
    public class PropertyChangedBase : INotifyPropertyChanged
    {
        // PropertyChanged event fired once whenever the value of one of the public properties changes
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (object.Equals(storage, value)) return false;
            storage = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }
    }
}
