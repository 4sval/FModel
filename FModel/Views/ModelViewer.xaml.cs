using System.ComponentModel;
using System.Windows.Input;
using CUE4Parse.UE4.Assets.Exports;
using FModel.Services;
using FModel.ViewModels;

namespace FModel.Views
{
    public partial class ModelViewer
    {
        private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;

        public ModelViewer()
        {
            DataContext = _applicationView;
            InitializeComponent();
        }

        public void Load(UObject export) => _applicationView.ModelViewer.LoadExport(export);
        private void OnClosing(object sender, CancelEventArgs e)
        {
            _applicationView.ModelViewer.AppendModeEnabled = false;
            MyAntiCrashGroup.ItemsSource = null; // <3
        }

        private void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.W:
                    _applicationView.ModelViewer.WirefreameToggle();
                    break;
                case Key.H:
                    _applicationView.ModelViewer.RenderingToggle();
                    break;
                case Key.D:
                    _applicationView.ModelViewer.DiffuseOnlyToggle();
                    break;
            }
        }
    }
}
