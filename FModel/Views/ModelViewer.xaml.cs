using System.Windows.Input;
using CUE4Parse.UE4.Assets.Exports;
using FModel.Services;
using FModel.Settings;
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
        
        private void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (UserSettings.Default.DirLeftTab.IsTriggered(e.Key))
                _applicationView.ModelViewer.PreviousLod();
            else if (UserSettings.Default.DirRightTab.IsTriggered(e.Key))
                _applicationView.ModelViewer.NextLod();
            else if (e.Key == Key.W)
                _applicationView.ModelViewer.ShowWireframe = !_applicationView.ModelViewer.ShowWireframe;
        }
    }
}