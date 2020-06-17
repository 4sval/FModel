using FModel.Windows.SoundPlayer.Visualization;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;

namespace FModel.ViewModels.SoundPlayer
{
    public static class InputFileVm
    {
        public static readonly InputFileViewModel inputFileViewModel = new InputFileViewModel();

        public static void Set(this InputFileViewModel vm, string filename, OutputSource source)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                vm.Content = filename;
                vm.Bytes = source.BytesPerSecond.ToString();
                vm.Duration = source.Length.ToString();
                vm.Volume = source.Volume;
            });
        }

        public static void Reset(this InputFileViewModel vm)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                vm.Content = string.Empty;
                vm.Bytes = string.Empty;
                vm.Duration = string.Empty;
                vm.Volume = 0.5f;
            });
        }
    }

    public class InputFileViewModel : PropertyChangedBase
    {
        private ObservableCollection<Device> _devices = new ObservableCollection<Device>(Device.GetOutputDevices());
        private bool _isEnabled = true;
        private string _content;
        private string _bytes;
        private string _duration;
        private float _volume = float.TryParse(Properties.Settings.Default.AudioPlayerVolume, out var v) ? v : 0.5f;

        public ObservableCollection<Device> Devices
        {
            get { return _devices; }

            set { this.SetProperty(ref this._devices, value); }
        }
        public bool IsEnabled
        {
            get { return _isEnabled; }

            set { this.SetProperty(ref this._isEnabled, value); }
        }
        public string Content
        {
            get { return _content; }

            set { this.SetProperty(ref this._content, value); }
        }
        public string Bytes
        {
            get { return _bytes; }

            set { this.SetProperty(ref this._bytes, value); }
        }
        public string Duration
        {
            get { return _duration; }

            set { this.SetProperty(ref this._duration, value); }
        }
        public float Volume
        {
            get { return _volume; }

            set { this.SetProperty(ref this._volume, value); }
        }
    }
}
