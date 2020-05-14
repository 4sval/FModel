namespace FModel.ViewModels.Buttons
{
    static class ExtractStopVm
    {
        public static ExtractStopViewModel extractViewModel = new ExtractStopViewModel
        {
            Content = Properties.Resources.Extract,
            IsEnabled = false,
        };
        public static ExtractStopViewModel stopViewModel = new ExtractStopViewModel
        {
            Content = Properties.Resources.Stop,
            IsEnabled = false,
        };
    }

    public class ExtractStopViewModel : PropertyChangedBase
    {
        private string _content;
        public string Content
        {
            get { return _content; }

            set { this.SetProperty(ref this._content, value); }
        }

        private bool _isEnabled;
        public bool IsEnabled
        {
            get { return _isEnabled; }

            set { this.SetProperty(ref this._isEnabled, value); }
        }
    }
}
