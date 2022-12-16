using System.Collections;
using System.Linq;
using FModel.Framework;
using FModel.Services;

namespace FModel.ViewModels.Commands;

public class RightClickMenuCommand : ViewModelCommand<ApplicationViewModel>
{
    private ThreadWorkerViewModel _threadWorkerView => ApplicationService.ThreadWorkerView;

    public RightClickMenuCommand(ApplicationViewModel contextViewModel) : base(contextViewModel)
    {
    }

    public override async void Execute(ApplicationViewModel contextViewModel, object parameter)
    {
        if (parameter is not object[] parameters || parameters[0] is not string trigger)
            return;

        var assetItems = ((IList) parameters[1]).Cast<AssetItem>().ToArray();
        if (!assetItems.Any()) return;

        await _threadWorkerView.Begin(cancellationToken =>
        {
            switch (trigger)
            {
                case "Assets_Extract_New_Tab":
                    foreach (var asset in assetItems)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        contextViewModel.CUE4Parse.Extract(cancellationToken, asset.FullPath, true);
                    }

                    break;
                case "Assets_Export_Data":
                    foreach (var asset in assetItems)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        contextViewModel.CUE4Parse.ExportData(asset.FullPath);
                    }

                    break;
                case "Assets_Save_Properties":
                    foreach (var asset in assetItems)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        contextViewModel.CUE4Parse.Extract(cancellationToken, asset.FullPath);
                        contextViewModel.CUE4Parse.TabControl.SelectedTab.SaveProperty(false);
                    }

                    break;
                case "Assets_Save_Textures":
                    foreach (var asset in assetItems)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        contextViewModel.CUE4Parse.Extract(cancellationToken, asset.FullPath);
                        contextViewModel.CUE4Parse.TabControl.SelectedTab.SaveImages(false);
                    }

                    break;
            }
        });
    }
}
