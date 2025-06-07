using System.Collections.Generic;
using System.Threading.Tasks;
using FModel.Settings;

namespace FModel.ViewModels.CUE4Parse;

public partial class CUE4ParseViewModel
{
    public async Task RefreshAesForAllAsync()
    {
        var tasks = new List<Task> { RefreshAes(UserSettings.Default.CurrentDir) };

        if (UserSettings.Default.DiffDir != null)
            tasks.Add(RefreshAes(UserSettings.Default.DiffDir));

        await Task.WhenAll(tasks);
    }

    private async Task RefreshAes(DirectorySettings dir)
    {
        if (!UserSettings.IsEndpointValid(EEndpointType.Aes, out var endpoint))
            return;

        await _threadWorkerView.Begin(cancellationToken =>
        {
            var aes = _apiEndpointView.DynamicApi.GetAesKeys(cancellationToken, endpoint.Url, endpoint.Path);
            if (aes is not { IsValid: true })
                return;

            dir.AesKeys = aes;
        });
    }
}
