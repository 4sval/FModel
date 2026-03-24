using System;
using System.Collections;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.Utils;
using FModel.Framework;
using FModel.Services;
using FModel.Settings;
using FModel.Views.Resources.Controls;

namespace FModel.ViewModels.Commands;

public class RightClickMenuCommand : ViewModelCommand<ApplicationViewModel>
{
    private ThreadWorkerViewModel _threadWorkerView => ApplicationService.ThreadWorkerView;

    public RightClickMenuCommand(ApplicationViewModel contextViewModel) : base(contextViewModel) { }

    private enum EAction
    {
        Show,
        Export,
    }

    private enum EShowAssetType
    {
        None,
        JSON,
        Metadata,
        References,
        Decompile,
    }

    public override async void Execute(ApplicationViewModel contextViewModel, object parameter)
    {
        if (parameter is not object[] parameters || parameters[0] is not string trigger)
            return;

        var param = (parameters[1] as IEnumerable)?.OfType<object>().ToArray() ?? [];
        if (param.Length == 0) return;

        var folders = param.OfType<TreeItem>().ToArray();
        var assets = param
            .Select(static item => item switch
            {
                GameFile gf => gf, // Search view passes GameFile directly
                GameFileViewModel gvm => gvm.Asset,
                _ => null
            })
            .Where(static gf => gf is not null).ToArray();

        if (folders.Length == 0 && assets.Length == 0)
            return;

        var assetsGroups = assets.GroupBy(static gf => gf.Directory);
        var (action, showtype, bulktype) = trigger switch
        {
            "Assets_Extract_New_Tab" => (EAction.Show, EShowAssetType.JSON, EBulkType.None),
            "Assets_Show_Metadata" => (EAction.Show, EShowAssetType.Metadata, EBulkType.None),
            "Assets_Show_References" => (EAction.Show, EShowAssetType.References, EBulkType.None),
            "Assets_Decompile" => (EAction.Show, EShowAssetType.Decompile, EBulkType.Code),

            "Save_Data" => (EAction.Export, EShowAssetType.None, EBulkType.Raw),
            "Save_Properties" => (EAction.Export, EShowAssetType.None, EBulkType.Properties),
            "Save_Textures" => (EAction.Export, EShowAssetType.None, EBulkType.Textures),
            "Save_Models" => (EAction.Export, EShowAssetType.None, EBulkType.Meshes),
            "Save_Animations" => (EAction.Export, EShowAssetType.None, EBulkType.Animations),
            "Save_Audio" => (EAction.Export, EShowAssetType.None, EBulkType.Audio),
            "Save_Code" => (EAction.Export, EShowAssetType.None, EBulkType.Code),

            _ => throw new ArgumentOutOfRangeException("Unsupported asset action."),
        };

        Interlocked.Exchange(ref contextViewModel.CUE4Parse.ExportedCount, 0);
        Interlocked.Exchange(ref contextViewModel.CUE4Parse.FailedExportCount, 0);
        await _threadWorkerView.Begin(cancellationToken =>
        {
            if (action is EAction.Show)
            {
                if (showtype is EShowAssetType.References)
                    assets = [assets.FirstOrDefault()];

                Action<GameFile> entryAction = showtype switch
                {
                    EShowAssetType.JSON => entry => contextViewModel.CUE4Parse.Extract(cancellationToken, entry, true),
                    EShowAssetType.Metadata => entry => contextViewModel.CUE4Parse.ShowMetadata(entry),
                    EShowAssetType.Decompile => entry => contextViewModel.CUE4Parse.Decompile(entry),
                    EShowAssetType.References => entry => contextViewModel.CUE4Parse.FindReferences(entry),
                    _ => throw new ArgumentOutOfRangeException("Unsupported asset action type."),
                };

                foreach (var entry in assets)
                {
                    Thread.Yield();
                    cancellationToken.ThrowIfCancellationRequested();
                    entryAction(entry);
                }

                return;
            }

            var (dirType, filetype) = bulktype switch
            {
                EBulkType.Raw => (UserSettings.Default.RawDataDirectory, "files"),
                EBulkType.Properties => (UserSettings.Default.PropertiesDirectory, "json files"),
                EBulkType.Textures => (UserSettings.Default.TextureDirectory, "textures"),
                EBulkType.Meshes => (UserSettings.Default.ModelDirectory, "models"),
                EBulkType.Animations => (UserSettings.Default.ModelDirectory, "animations"),
                EBulkType.Audio => (UserSettings.Default.AudioDirectory, "audio files"),
                EBulkType.Code => (UserSettings.Default.CodeDirectory, "code files"),
                _ => (null, null),
            };

            if (string.IsNullOrEmpty(dirType))
                return;

            Action<TreeItem> folderAction = bulktype switch
            {
                EBulkType.Raw => folder => contextViewModel.CUE4Parse.ExportFolder(cancellationToken, folder),
                _ => folder => contextViewModel.CUE4Parse.ExtractFolder(cancellationToken, folder, bulktype | EBulkType.Auto),
            };

            foreach (var folder in folders)
            {
                cancellationToken.ThrowIfCancellationRequested();
                folderAction(folder);

                var path = Path.Combine(dirType, UserSettings.Default.KeepDirectoryStructure ? folder.PathAtThisPoint : folder.PathAtThisPoint.SubstringAfterLast('/')).Replace('\\', '/');
                LogExport(contextViewModel, folder.PathAtThisPoint, path, dirType, filetype);
            }

            Action<GameFile, EBulkType, bool> fileAction = bulktype switch
            {
                EBulkType.Raw => (entry, _, update) => contextViewModel.CUE4Parse.ExportData(entry, !update),
                _ => (entry, bulk, update) => contextViewModel.CUE4Parse.Extract(cancellationToken, entry, false, bulk),
            };

            foreach (var group in assetsGroups)
            {
                var directory = group.Key;
                var list = group.ToArray();
                var update = list.Length > 1;
                var bulk = bulktype | (update ? EBulkType.Auto : EBulkType.None);
                foreach (var entry in list)
                {
                    Thread.Yield();
                    cancellationToken.ThrowIfCancellationRequested();
                    fileAction(entry, bulk, update);
                }

                if (update)
                {
                    var path = Path.Combine(dirType, UserSettings.Default.KeepDirectoryStructure ? directory : directory.SubstringAfterLast('/')).Replace('\\', '/');
                    LogExport(contextViewModel, directory, path, dirType, filetype);
                }
            }
        });
    }

    private void LogExport(ApplicationViewModel contextViewModel, string directory, string path, string basePath, string fileType)
    {
        if (contextViewModel.CUE4Parse.ExportedCount > 0)
        {
            FLogger.Append(ELog.Information, () =>
            {
                FLogger.Text($"Successfully exported {contextViewModel.CUE4Parse.ExportedCount} {fileType} from ", Constants.WHITE);
                FLogger.Link(directory, Path.Exists(path) ? path : basePath, true);
            });
        }
        else if (contextViewModel.CUE4Parse.FailedExportCount == 0)
        {
            // Not an error because folder simply might not contain type of asset user is trying to save
            FLogger.Append(ELog.Warning, () =>
            {
                FLogger.Text($"Failed to find any {fileType} in {directory}", Constants.WHITE, true);
            });
        }

        if (contextViewModel.CUE4Parse.FailedExportCount > 0)
        {
            FLogger.Append(ELog.Error, () =>
            {
                FLogger.Text($"Failed to export {contextViewModel.CUE4Parse.FailedExportCount} {fileType} from {directory}", Constants.WHITE, true);
            });
        }

        Interlocked.Exchange(ref contextViewModel.CUE4Parse.ExportedCount, 0);
        Interlocked.Exchange(ref contextViewModel.CUE4Parse.FailedExportCount, 0);
    }
}
