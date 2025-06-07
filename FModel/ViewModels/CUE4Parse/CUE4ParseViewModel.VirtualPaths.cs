using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Versions;
using FModel.Views.Resources.Controls;

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

    public void ResetVirtualPaths()
    {
        _virtualPathCounts.Clear();
    }
}
