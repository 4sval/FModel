using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using CUE4Parse.UE4.Objects.Core.Misc;
using FModel.Framework;
using FModel.Services;
using FModel.Settings;
using FModel.ViewModels.ApiEndpoints.Models;

namespace FModel.ViewModels;

public class AesManagerViewModel(CUE4Parse.CUE4ParseViewModel cue4Parse) : ViewModel
{
    private ThreadWorkerViewModel _threadWorkerView => ApplicationService.ThreadWorkerView;

    public FullyObservableCollection<FileItem> AesKeys { get; private set; } // holds all aes keys even the main one
    public ICollectionView AesKeysView { get; private set; } // holds all aes key ordered by name for the ui
    public bool HasChange { get; set; } = false;

    public FullyObservableCollection<FileItem> DiffAesKeys { get; private set; }
    public ICollectionView DiffAesKeysView { get; private set; }
    public bool IsDiffDirectoryAvailable => !string.IsNullOrEmpty(UserSettings.Default.DiffGameDirectory);
    private AesResponse _diffKeysFromSettings;
    private HashSet<FGuid> _diffUniqueGuids;
    private readonly FileItem _diffMainKey = new("Compare Static Key", 0) { Guid = Constants.ZERO_GUID };

    private AesResponse _keysFromSettings;
    private HashSet<FGuid> _uniqueGuids;
    private readonly FileItem _mainKey = new("Main Static Key", 0) { Guid = Constants.ZERO_GUID }; // just so main key gets refreshed in the ui

    public async Task InitAes()
    {
        await _threadWorkerView.Begin(_ =>
        {
            _keysFromSettings = UserSettings.Default.CurrentDir.AesKeys;
            _mainKey.Key = Helper.FixKey(_keysFromSettings.MainKey);
            AesKeys = [];
            _uniqueGuids = [];
            EnumerateAesKeys(_keysFromSettings, cue4Parse.GameDirectory.DirectoryFiles, _mainKey, _uniqueGuids, AesKeys);
            AesKeys.ItemPropertyChanged += (s, e) => AesKeysOnItemPropertyChanged(e, _keysFromSettings, AesKeys);
            AesKeysView = new ListCollectionView(AesKeys) { SortDescriptions = { new SortDescription("Name", ListSortDirection.Ascending) } };

            if (IsDiffDirectoryAvailable)
            {
                _diffKeysFromSettings = UserSettings.Default.DiffDir.AesKeys;
                _diffMainKey.Key = Helper.FixKey(_diffKeysFromSettings.MainKey);
                DiffAesKeys = [];
                _diffUniqueGuids = [];
                EnumerateAesKeys(_diffKeysFromSettings, cue4Parse.DiffGameDirectory.DirectoryFiles, _diffMainKey, _diffUniqueGuids, DiffAesKeys);
                DiffAesKeys.ItemPropertyChanged += (s, e) => AesKeysOnItemPropertyChanged(e, _diffKeysFromSettings, DiffAesKeys);
                DiffAesKeysView = new ListCollectionView(DiffAesKeys) { SortDescriptions = { new SortDescription("Name", ListSortDirection.Ascending) } };
            }
        });
    }

    private void AesKeysOnItemPropertyChanged(ItemPropertyChangedEventArgs e, AesResponse settings, FullyObservableCollection<FileItem> collection)
    {
        if (e.PropertyName != "Key")
            return;

        var key = Helper.FixKey(collection[e.CollectionIndex].Key);
        if (e.CollectionIndex == 0)
        {
            if (!HasChange)
                HasChange = Helper.FixKey(settings.MainKey) != key;

            settings.MainKey = key;
        }
        else if (!settings.HasDynamicKeys)
        {
            HasChange = true;
            settings.DynamicKeys = new List<DynamicKey>
            {
                new()
                {
                    Key = key,
                    Name = collection[e.CollectionIndex].Name,
                    Guid = collection[e.CollectionIndex].Guid.ToString()
                }
            };
        }
        else if (settings.DynamicKeys.FirstOrDefault(x => x.Guid == collection[e.CollectionIndex].Guid.ToString()) is { } d)
        {
            if (!HasChange)
                HasChange = Helper.FixKey(d.Key) != key;

            d.Key = key;
        }
        else
        {
            HasChange = true;
            settings.DynamicKeys.Add(new DynamicKey
            {
                Key = key,
                Name = collection[e.CollectionIndex].Name,
                Guid = collection[e.CollectionIndex].Guid.ToString()
            });
        }
    }

    public void SetAesKeys()
    {
        UserSettings.Default.CurrentDir.AesKeys = _keysFromSettings;
        if (UserSettings.Default.DiffDir != null && _diffKeysFromSettings != null)
            UserSettings.Default.DiffDir.AesKeys = _diffKeysFromSettings;
        // Log.Information("{@Json}", UserSettings.Default);
    }

    private static void EnumerateAesKeys(AesResponse settings, IEnumerable<FileItem> files, FileItem mainKey, HashSet<FGuid> uniqueGuids, ICollection<FileItem> output)
    {
        uniqueGuids.Add(Constants.ZERO_GUID);
        output.Add(mainKey);

        var hasDynamicKeys = settings.HasDynamicKeys;
        foreach (var file in files)
        {
            if (file.Guid == Constants.ZERO_GUID || !uniqueGuids.Add(file.Guid))
                continue;

            var k = string.Empty;
            if (hasDynamicKeys && settings.DynamicKeys.FirstOrDefault(x => x.Guid == file.Guid.ToString()) is { } dynamicKey)
                k = dynamicKey.Key;

            file.Key = Helper.FixKey(k);
            output.Add(file);
        }
    }
}
