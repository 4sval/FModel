using System.Collections;
using System.Linq;
using System.Threading;
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

        var updateUi = assetItems.Length > 1 ? EBulkType.Auto : EBulkType.None;
        await _threadWorkerView.Begin(cancellationToken =>
        {
            switch (trigger)
            {
                case "Assets_Extract_New_Tab":
                    foreach (var asset in assetItems)
                    {
                        Thread.Yield();
                        cancellationToken.ThrowIfCancellationRequested();
                        contextViewModel.CUE4Parse.Extract(cancellationToken, asset.FullPath, true);
                    }
                    break;
                case "Assets_Show_Metadata":
                    foreach (var asset in assetItems)
                    {
                        Thread.Yield();
                        cancellationToken.ThrowIfCancellationRequested();
                        contextViewModel.CUE4Parse.ShowMetadata(asset.FullPath);
                    }
                    break;
                case "Assets_Export_Data":
                    foreach (var asset in assetItems)
                    {
                        Thread.Yield();
                        cancellationToken.ThrowIfCancellationRequested();
                        contextViewModel.CUE4Parse.ExportData(asset.FullPath);
                    }
                    break;
                case "Assets_Save_Properties":
                    foreach (var asset in assetItems)
                    {
                        Thread.Yield();
                        cancellationToken.ThrowIfCancellationRequested();
                        contextViewModel.CUE4Parse.Extract(cancellationToken, asset.FullPath, false, EBulkType.Properties | updateUi);
                    }
                    break;
                case "Assets_Save_Textures":
                    foreach (var asset in assetItems)
                    {
                        Thread.Yield();
                        cancellationToken.ThrowIfCancellationRequested();
                        contextViewModel.CUE4Parse.Extract(cancellationToken, asset.FullPath, false, EBulkType.Textures | updateUi);
                    }
                    break;
                case "Assets_Save_Models":
                    foreach (var asset in assetItems)
                    {
                        Thread.Yield();
                        cancellationToken.ThrowIfCancellationRequested();
                        contextViewModel.CUE4Parse.Extract(cancellationToken, asset.FullPath, false, EBulkType.Meshes | updateUi);
                    }
                    break;
                case "Assets_Save_Animations":
                    foreach (var asset in assetItems)
                    {
                        Thread.Yield();
                        cancellationToken.ThrowIfCancellationRequested();
                        contextViewModel.CUE4Parse.Extract(cancellationToken, asset.FullPath, false, EBulkType.Animations | updateUi);
                    }
                    break;
            }
        });
    }
}
