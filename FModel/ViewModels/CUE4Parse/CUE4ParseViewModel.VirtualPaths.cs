using System.Collections.Generic;
using System.Threading.Tasks;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using FModel.Views.Resources.Controls;
using Serilog;

namespace FModel.ViewModels.CUE4Parse;

public partial class CUE4ParseViewModel
{
    private readonly Dictionary<AbstractVfsFileProvider, int> _virtualPathCounts = new();

    public async Task LoadAllVirtualPaths()
    {
        foreach (var (provider, settings) in ProvidersWithDirectories())
        {
            if (_virtualPathCounts.TryGetValue(provider, out var count) && count > 0)
                continue;

            await Task.Run(() =>
            {
                var count = provider.LoadVirtualPaths(settings.UeVersion.GetVersion());
                _virtualPathCounts[provider] = count;
                if (count > 0)
                {
                    FLogger.Append(ELog.Information, () =>
                        FLogger.Text($"{count} virtual paths loaded for {provider.ProjectName}", Constants.WHITE, true));
                }
                else
                {
                    FLogger.Append(ELog.Warning, () =>
                        FLogger.Text($"Could not load virtual paths for {provider.ProjectName}, plugin manifest may not exist", Constants.WHITE, true));
                }
            });
        }
    }

    public void LoadVfs(IEnumerable<KeyValuePair<FGuid, FAesKey>> aesKeys, IEnumerable<KeyValuePair<FGuid, FAesKey>> diffAesKeys)
    {
        Provider.SubmitKeys(aesKeys);
        Provider.PostMount();

        if (DiffProvider != null)
        {
            DiffProvider.SubmitKeys(diffAesKeys);
            DiffProvider.PostMount();

            if (DiffProvider.MountedVfs.Count == 0 && DiffProvider.UnloadedVfs.Count > 0)
            {
                FLogger.Append(ELog.Error, () =>
                    FLogger.Text("Compared game could not mount any VFS archives. Possibly due to missing AES keys.", Constants.WHITE, true));
            }
        }

        var aesMax = Provider.RequiredKeys.Count + Provider.Keys.Count;
        var archiveMax = Provider.UnloadedVfs.Count + Provider.MountedVfs.Count;
        Log.Information($"Project: {Provider.ProjectName} | Mounted: {Provider.MountedVfs.Count}/{archiveMax} | AES: {Provider.Keys.Count}/{aesMax} | Files: x{Provider.Files.Count}");
    }

    public void ResetVirtualPaths()
    {
        _virtualPathCounts.Clear();
    }
}
