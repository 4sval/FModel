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
    }
}