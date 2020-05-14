using System.Windows;
using System.Windows.Media;

namespace FModel.ViewModels.StatusBar
{
    public static class StatusBarVm
    {
        public static readonly StatusBarViewModel statusBarViewModel = new StatusBarViewModel();

        public static void Set(this StatusBarViewModel vm, string process, string state)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                bool red = state.Equals(Properties.Resources.Error) || state.Equals("Yikes");
                bool orange =
                    state.Equals(Properties.Resources.Waiting) ||
                    state.Equals(Properties.Resources.Loading) ||
                    state.Equals(Properties.Resources.Processing);
                bool blue = state.Equals(Properties.Resources.Success);

                SolidColorBrush stateBg =
                    red ? new SolidColorBrush(Color.FromRgb(237, 28, 36))
                    : orange ? new SolidColorBrush(Color.FromRgb(237, 77, 28))
                    : blue ? new SolidColorBrush(Color.FromRgb(40, 72, 163))
                    : new SolidColorBrush(Color.FromRgb(28, 32, 38));

                vm.Pevent = process;
                vm.State = state;
                vm.StateBg = stateBg;
            });
        }

        public static void Reset(this StatusBarViewModel vm)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                vm.Pevent = string.Empty;
                vm.State = string.Empty;
                vm.StateBg = new SolidColorBrush(Color.FromRgb(28, 32, 38));
            });
        }
    }

    public class StatusBarViewModel : PropertyChangedBase
    {
        private string _pevent;
        private string _state;
        private Brush _statebg;

        public string Pevent
        {
            get { return _pevent; }

            set { this.SetProperty(ref this._pevent, value); }
        }
        public string State
        {
            get { return _state; }

            set { this.SetProperty(ref this._state, value); }
        }
        public Brush StateBg
        {
            get { return _statebg; }

            set { this.SetProperty(ref this._statebg, value); }
        }
    }
}
