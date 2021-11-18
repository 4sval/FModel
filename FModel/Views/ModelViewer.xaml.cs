using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using CUE4Parse.UE4.Assets.Exports;
using FModel.Services;
using FModel.ViewModels;
using HelixToolkit.Wpf.SharpDX;

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

        private void OnMouse3DDown(object sender, MouseDown3DEventArgs e)
        {
            if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) || e.HitTestResult.ModelHit is not MeshGeometryModel3D m) return;
            _applicationView.ModelViewer.SelectedGeometry = m;
        }

        private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not TextBlock or Image)
                return;

            if (!_applicationView.IsReady || sender is not ListBox { SelectedItem: MeshGeometryModel3D }) return;
            _applicationView.ModelViewer.FocusOnSelectedGeometry();
        }
    }
}
