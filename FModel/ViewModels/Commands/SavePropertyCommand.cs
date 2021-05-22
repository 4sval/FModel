using System.Collections;
using System.Linq;
using FModel.Framework;
using FModel.Services;

namespace FModel.ViewModels.Commands
{
    public class SavePropertyCommand : ViewModelCommand<ApplicationViewModel>
    {
        private ThreadWorkerViewModel _threadWorkerView => ApplicationService.ThreadWorkerView;

        public SavePropertyCommand(ApplicationViewModel contextViewModel) : base(contextViewModel)
        {
        }

        public override async void Execute(ApplicationViewModel contextViewModel, object parameter)
        {
            if (parameter == null) return;

            var assetItems = ((IList) parameter).Cast<AssetItem>().ToArray();
            if (!assetItems.Any()) return;

            await _threadWorkerView.Begin(_ =>
            {
                foreach (var asset in assetItems)
                {
                    contextViewModel.CUE4Parse.Extract(asset.FullPath);
                    contextViewModel.CUE4Parse.TabControl.SelectedTab.SaveProperty(false);
                }
            });
        }
    }
}