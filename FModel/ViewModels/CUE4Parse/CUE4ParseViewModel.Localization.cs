using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse.FileProvider.Vfs;
using FModel.Creator;
using FModel.Extensions;
using FModel.Services;
using FModel.Settings;
using FModel.Views.Resources.Controls;

namespace FModel.ViewModels.CUE4Parse;

public partial class CUE4ParseViewModel
{
    private readonly Dictionary<AbstractVfsFileProvider, bool> _localResourcesDone = new();
    private readonly Dictionary<AbstractVfsFileProvider, bool> _hotfixedResourcesDone = new();
    public int LocalizedResourcesCount { get; set; }

    public async Task LoadLocalizedResources()
    {
        int snapshot = LocalizedResourcesCount;
        await Task.WhenAll(
            Task.WhenAll(AllProviders().Select(LoadGameLocalizedResources)),
            Task.WhenAll(AllProviders().Select(LoadHotfixedLocalizedResources))
        ).ConfigureAwait(false);

        LocalizedResourcesCount = AllProviders().Sum(p => p.Internationalization.Count);
        if (snapshot != LocalizedResourcesCount)
        {
            FLogger.Append(ELog.Information, () =>
                FLogger.Text($"{LocalizedResourcesCount} localized resources loaded for '{UserSettings.Default.AssetLanguage.GetDescription()}'", Constants.WHITE, true));
            Utils.Typefaces = new Typefaces(this);
        }
    }

    private Task LoadGameLocalizedResources(AbstractVfsFileProvider provider)
    {
        if (_localResourcesDone.TryGetValue(provider, out var done) && done)
            return Task.CompletedTask;

        return Task.Run(() =>
        {
            _localResourcesDone[provider] = provider.TryChangeCulture(provider.GetLanguageCode(UserSettings.Default.AssetLanguage));
        });
    }

    private Task LoadHotfixedLocalizedResources(AbstractVfsFileProvider provider)
    {
        if (!provider.ProjectName.Equals("fortnitegame", StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        if (_hotfixedResourcesDone.TryGetValue(provider, out var done) && done)
            return Task.CompletedTask;

        return Task.Run(() =>
        {
            var hotfixes = ApplicationService.ApiEndpointView.CentralApi.GetHotfixes(default, provider.GetLanguageCode(UserSettings.Default.AssetLanguage));
            if (hotfixes == null)
                return;

            provider.Internationalization.Override(hotfixes);
            _hotfixedResourcesDone[provider] = true;
        });
    }

    public void ResetLocalizationState()
    {
        _localResourcesDone.Clear();
        _hotfixedResourcesDone.Clear();
    }
}
