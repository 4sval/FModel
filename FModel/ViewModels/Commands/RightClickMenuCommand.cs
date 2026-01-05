using System.Collections;
using System.Linq;
using System.Threading;
using CUE4Parse.FileProvider.Objects;
using FModel.Framework;
using FModel.Services;
using FModel.Settings;
using FModel.Views.Resources.Controls;

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

        var param = (parameters[1] as IEnumerable)?.OfType<object>().ToArray() ?? [];
        if (param.Length == 0) return;

        var folders = param.OfType<TreeItem>().ToArray();
        var assets = param.SelectMany(item => item switch
        {
            GameFile gf => new[] { gf }, // search view passes GameFile directly
            GameFileViewModel gvm => new[] { gvm.Asset },
            _ => []
        }).ToArray();

        if (folders.Length == 0 && assets.Length == 0)
            return;

        var updateUi = assets.Length > 1 ? EBulkType.Auto : EBulkType.None;
        await _threadWorkerView.Begin(cancellationToken =>
        {
            switch (trigger)
            {
                #region Asset Commands
                case "Assets_Extract_New_Tab":
                    foreach (var entry in assets)
                    {
                        Thread.Yield();
                        cancellationToken.ThrowIfCancellationRequested();
                        contextViewModel.CUE4Parse.Extract(cancellationToken, entry, true);
                    }
                    break;
                case "Assets_Show_Metadata":
                    foreach (var entry in assets)
                    {
                        Thread.Yield();
                        cancellationToken.ThrowIfCancellationRequested();
                        contextViewModel.CUE4Parse.ShowMetadata(entry);
                    }
                    break;
                case "Assets_Show_References":
                    {
                        Thread.Yield();
                        cancellationToken.ThrowIfCancellationRequested();
                        contextViewModel.CUE4Parse.FindReferences(assets.FirstOrDefault());
                    }
                    break;
                case "Assets_Decompile":
                    foreach (var entry in assets)
                    {
                        Thread.Yield();
                        cancellationToken.ThrowIfCancellationRequested();
                        contextViewModel.CUE4Parse.Decompile(entry);
                    }
                    break;
                case "Assets_Export_Data":
                    foreach (var entry in assets)
                    {
                        Thread.Yield();
                        cancellationToken.ThrowIfCancellationRequested();
                        contextViewModel.CUE4Parse.ExportData(entry);
                    }
                    break;
                case "Assets_Save_Properties":
                    foreach (var entry in assets)
                    {
                        Thread.Yield();
                        cancellationToken.ThrowIfCancellationRequested();
                        contextViewModel.CUE4Parse.Extract(cancellationToken, entry, false, EBulkType.Properties | updateUi);
                    }
                    break;
                case "Assets_Save_Textures":
                    foreach (var entry in assets)
                    {
                        Thread.Yield();
                        cancellationToken.ThrowIfCancellationRequested();
                        contextViewModel.CUE4Parse.Extract(cancellationToken, entry, false, EBulkType.Textures | updateUi);
                    }
                    break;
                case "Assets_Save_Models":
                    foreach (var entry in assets)
                    {
                        Thread.Yield();
                        cancellationToken.ThrowIfCancellationRequested();
                        contextViewModel.CUE4Parse.Extract(cancellationToken, entry, false, EBulkType.Meshes | updateUi);
                    }
                    break;
                case "Assets_Save_Animations":
                    foreach (var entry in assets)
                    {
                        Thread.Yield();
                        cancellationToken.ThrowIfCancellationRequested();
                        contextViewModel.CUE4Parse.Extract(cancellationToken, entry, false, EBulkType.Animations | updateUi);
                    }
                    break;
                case "Assets_Save_Audio":
                    foreach (var entry in assets)
                    {
                        Thread.Yield();
                        cancellationToken.ThrowIfCancellationRequested();
                        contextViewModel.CUE4Parse.Extract(cancellationToken, entry, false, EBulkType.Audio | updateUi);
                    }
                    break;
                #endregion

                #region Folder Commands
                case "Folders_Export_Data":
                    foreach (var folder in folders)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        contextViewModel.CUE4Parse.ExportFolder(cancellationToken, folder);

                        FLogger.Append(ELog.Information, () =>
                        {
                            FLogger.Text("Successfully exported ", Constants.WHITE);
                            FLogger.Link(folder.PathAtThisPoint, UserSettings.Default.RawDataDirectory, true);
                        });
                    }
                    break;
                case "Folders_Save_Properties":
                    foreach (var folder in folders)
                    {
                        Thread.Yield();
                        cancellationToken.ThrowIfCancellationRequested();
                        contextViewModel.CUE4Parse.SaveFolder(cancellationToken, folder);

                        FLogger.Append(ELog.Information, () =>
                        {
                            FLogger.Text("Successfully saved ", Constants.WHITE);
                            FLogger.Link(folder.PathAtThisPoint, UserSettings.Default.PropertiesDirectory, true);
                        });
                    }
                    break;
                case "Folders_Save_Textures":
                    foreach (var folder in folders)
                    {
                        Thread.Yield();
                        cancellationToken.ThrowIfCancellationRequested();
                        contextViewModel.CUE4Parse.TextureFolder(cancellationToken, folder);

                        FLogger.Append(ELog.Information, () =>
                        {
                            FLogger.Text("Successfully saved textures from ", Constants.WHITE);
                            FLogger.Link(folder.PathAtThisPoint, UserSettings.Default.TextureDirectory, true);
                        });
                    }
                    break;
                case "Folders_Save_Models":
                    foreach (var folder in folders)
                    {
                        Thread.Yield();
                        cancellationToken.ThrowIfCancellationRequested();
                        contextViewModel.CUE4Parse.ModelFolder(cancellationToken, folder);

                        FLogger.Append(ELog.Information, () =>
                        {
                            FLogger.Text("Successfully saved models from ", Constants.WHITE);
                            FLogger.Link(folder.PathAtThisPoint, UserSettings.Default.ModelDirectory, true);
                        });
                    }
                    break;
                case "Folders_Save_Animations":
                    foreach (var folder in folders)
                    {
                        Thread.Yield();
                        cancellationToken.ThrowIfCancellationRequested();
                        contextViewModel.CUE4Parse.AnimationFolder(cancellationToken, folder);

                        FLogger.Append(ELog.Information, () =>
                        {
                            FLogger.Text("Successfully saved animations from ", Constants.WHITE);
                            FLogger.Link(folder.PathAtThisPoint, UserSettings.Default.ModelDirectory, true);
                        });
                    }
                    break;
                case "Folders_Save_Audio":
                    foreach (var folder in folders)
                    {
                        Thread.Yield();
                        cancellationToken.ThrowIfCancellationRequested();
                        contextViewModel.CUE4Parse.AudioFolder(cancellationToken, folder);

                        FLogger.Append(ELog.Information, () =>
                        {
                            FLogger.Text("Successfully saved audio from ", Constants.WHITE);
                            FLogger.Link(folder.PathAtThisPoint, UserSettings.Default.AudioDirectory, true);
                        });
                    }
                    break;
                #endregion
            }
        });
    }
}
