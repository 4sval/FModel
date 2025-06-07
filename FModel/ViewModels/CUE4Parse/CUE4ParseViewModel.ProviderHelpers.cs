using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AdonisUI.Controls;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using FModel.Settings;
using FModel.Views.Resources.Controls;
using Serilog;

namespace FModel.ViewModels.CUE4Parse;

public partial class CUE4ParseViewModel
{
    #region ProviderHelpers
    public IEnumerable<AbstractVfsFileProvider> AllProviders()
    {
        yield return Provider;
        if (DiffProvider != null)
            yield return DiffProvider;
    }

    public void ForEachProvider(Action<AbstractVfsFileProvider> action)
    {
        foreach (var provider in AllProviders())
            action(provider);
    }

    public async Task ForEachProviderAsync(Func<AbstractVfsFileProvider, Task> action)
    {
        foreach (var provider in AllProviders())
            await action(provider);
    }

    //private IEnumerable<(AbstractVfsFileProvider Provider, EndpointSettings Endpoint)> ProvidersWithEndpoints()
    //{
    //    yield return (Provider, UserSettings.Default.CurrentDir.Endpoints[(int) EEndpointType.Mapping]);
    //    if (DiffProvider != null && UserSettings.Default.DiffDir != null)
    //        yield return (DiffProvider, UserSettings.Default.DiffDir.Endpoints[(int) EEndpointType.Mapping]);
    //}

    private IEnumerable<(AbstractVfsFileProvider Provider, EndpointSettings Endpoint)> ProvidersWithEndpoints(EEndpointType type)
    {
        if (UserSettings.IsEndpointValid(type, out var mainEndpoint))
            yield return (Provider, mainEndpoint);

        if (DiffProvider != null && UserSettings.Default.DiffDir != null &&
            UserSettings.IsEndpointValid(type, out var diffEndpoint))
            yield return (DiffProvider, diffEndpoint);
    }

    public IEnumerable<(AbstractVfsFileProvider Provider, DirectorySettings Dir)> ProvidersWithDirectories()
    {
        yield return (Provider, UserSettings.Default.CurrentDir);
        if (DiffProvider != null && UserSettings.Default.DiffDir != null)
            yield return (DiffProvider, UserSettings.Default.DiffDir);
    }
    #endregion

    #region Vfs
    public void LoadVfs(IEnumerable<KeyValuePair<FGuid, FAesKey>> aesKeys)
    {
        Provider.SubmitKeys(aesKeys);
        Provider.PostMount();

        if (DiffProvider != null)
        {
            DiffProvider.SubmitKeys(aesKeys);
            DiffProvider.PostMount();
        }

        var aesMax = Provider.RequiredKeys.Count + Provider.Keys.Count;
        var archiveMax = Provider.UnloadedVfs.Count + Provider.MountedVfs.Count;
        Log.Information($"Project: {Provider.ProjectName} | Mounted: {Provider.MountedVfs.Count}/{archiveMax} | AES: {Provider.Keys.Count}/{aesMax} | Files: x{Provider.Files.Count}");
    }

    #endregion

    #region Mappings
    //public async Task InitAllMappings(bool force = false)
    //{
    //    foreach (var (provider, endpoint) in ProvidersWithEndpoints())
    //    {
    //        await InitMappingsForProvider(provider, endpoint, force);
    //    }
    //}

    //private IEnumerable<(AbstractVfsFileProvider Provider, EndpointSettings Endpoint)> ProvidersWithEndpoints()
    //{
    //    yield return (Provider, UserSettings.Default.CurrentDir.Endpoints[(int) EEndpointType.Mapping]);
    //    if (DiffProvider != null && UserSettings.Default.DiffDir != null)
    //        yield return (DiffProvider, UserSettings.Default.DiffDir.Endpoints[(int) EEndpointType.Mapping]);
    //}

    //private Task InitMappingsForProvider(AbstractVfsFileProvider provider, EndpointSettings endpoint,
    //    bool force = false)
    //{
    //    if (provider == null || endpoint == null || !endpoint.IsValid)
    //    {
    //        if (provider != null)
    //            provider.MappingsContainer = null;
    //        return Task.CompletedTask;
    //    }

    //    return Task.Run(() =>
    //    {
    //        var l = ELog.Information;
    //        if (endpoint.Overwrite && File.Exists(endpoint.FilePath))
    //        {
    //            provider.MappingsContainer = new FileUsmapTypeMappingsProvider(endpoint.FilePath);
    //        }
    //        else if (endpoint.IsValid)
    //        {
    //            var mappingsFolder = Path.Combine(UserSettings.Default.OutputDirectory, ".data");
    //            if (endpoint.Path == "$.[?(@.meta.compressionMethod=='Oodle')].['url','fileName']")
    //                endpoint.Path = "$.[0].['url','fileName']";
    //            var mappings = _apiEndpointView.DynamicApi.GetMappings(default, endpoint.Url, endpoint.Path);
    //            if (mappings is { Length: > 0 })
    //            {
    //                foreach (var mapping in mappings)
    //                {
    //                    if (!mapping.IsValid)
    //                        continue;

    //                    var mappingPath = Path.Combine(mappingsFolder, mapping.FileName);
    //                    if (force || !File.Exists(mappingPath))
    //                    {
    //                        _apiEndpointView.DownloadFile(mapping.Url, mappingPath);
    //                    }

    //                    provider.MappingsContainer = new FileUsmapTypeMappingsProvider(mappingPath);
    //                    break;
    //                }
    //            }

    //            if (provider.MappingsContainer == null)
    //            {
    //                var latestUsmaps = new DirectoryInfo(mappingsFolder).GetFiles("*_oo.usmap");
    //                if (latestUsmaps.Length <= 0)
    //                    return;

    //                var latestUsmapInfo = latestUsmaps.OrderBy(f => f.LastWriteTime).Last();
    //                provider.MappingsContainer = new FileUsmapTypeMappingsProvider(latestUsmapInfo.FullName);
    //                l = ELog.Warning;
    //            }
    //        }

    //        if (provider.MappingsContainer is FileUsmapTypeMappingsProvider m)
    //        {
    //            Log.Information($"Mappings pulled from '{m.FileName}'");
    //            FLogger.Append(l,
    //                () => FLogger.Text($"Mappings pulled from '{m.FileName}'", Constants.WHITE, true));
    //        }
    //    });
    //}

    #endregion

    //public async Task LoadAllVirtualPaths()
    //{
    //    foreach (var provider in AllProviders())
    //    {
    //        if (_virtualPathCount > 0)
    //            continue;
    //        await Task.Run(() =>
    //        {
    //            _virtualPathCount = provider.LoadVirtualPaths(UserSettings.Default.CurrentDir.UeVersion.GetVersion());
    //            if (_virtualPathCount > 0)
    //            {
    //                FLogger.Append(ELog.Information, () =>
    //                    FLogger.Text($"{_virtualPathCount} virtual paths loaded", Constants.WHITE, true));
    //            }
    //            else
    //            {
    //                FLogger.Append(ELog.Warning, () =>
    //                    FLogger.Text("Could not load virtual paths, plugin manifest may not exist", Constants.WHITE, true));
    //            }
    //        });
    //    }
    //}

    #region Localization
    //public void ChangeCultureForAllProviders(string languageCode)
    //{
    //    ForEachProvider(provider => provider.TryChangeCulture(languageCode));
    //}

    //public async Task LoadAllLocalizedResources()
    //{
    //    await Task.WhenAll(AllProviders().Select(provider =>
    //        Task.Run(() =>
    //        {
    //            provider.TryChangeCulture(provider.GetLanguageCode(UserSettings.Default.AssetLanguage));
    //        })
    //    ));
    //}
    #endregion

    #region Collector
    //public void ClearProvider()
    //{
    //    AssetsFolder.Folders.Clear();
    //    SearchVm.SearchResults.Clear();
    //    Helper.CloseWindow<AdonisWindow>("Search View");

    //    ForEachProvider(provider =>
    //    {
    //        provider.UnloadNonStreamedVfs();
    //    });

    //    GC.Collect();
    //}
    #endregion
}
