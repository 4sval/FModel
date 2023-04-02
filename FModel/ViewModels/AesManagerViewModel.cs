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
using Serilog;

namespace FModel.ViewModels;

public class AesManagerViewModel : ViewModel
{
    private ThreadWorkerViewModel _threadWorkerView => ApplicationService.ThreadWorkerView;

    public FullyObservableCollection<FileItem> AesKeys { get; private set; } // holds all aes keys even the main one
    public ICollectionView AesKeysView { get; private set; } // holds all aes key ordered by name for the ui
    public bool HasChange { get; set; }

    private AesResponse _keysFromSettings;
    private HashSet<FGuid> _uniqueGuids;
    private readonly CUE4ParseViewModel _cue4Parse;
    private readonly FileItem _mainKey = new("Main Static Key", 0) { Guid = Constants.ZERO_GUID }; // just so main key gets refreshed in the ui

    public AesManagerViewModel(CUE4ParseViewModel cue4Parse)
    {
        _cue4Parse = cue4Parse;
        HasChange = false;
    }

    public async Task InitAes()
    {
        await _threadWorkerView.Begin(_ =>
        {
            if (_cue4Parse.Game == FGame.Unknown &&
                UserSettings.Default.ManualGames.TryGetValue(UserSettings.Default.GameDirectory, out var settings))
            {
                _keysFromSettings = settings.AesKeys;
            }
            else
            {
                UserSettings.Default.AesKeys.TryGetValue(_cue4Parse.Game, out _keysFromSettings);
            }

            _keysFromSettings ??= new AesResponse
            {
                MainKey = string.Empty,
                DynamicKeys = null
            };

            _mainKey.Key = Helper.FixKey(_keysFromSettings.MainKey);
            AesKeys = new FullyObservableCollection<FileItem>(EnumerateAesKeys());
            AesKeys.ItemPropertyChanged += AesKeysOnItemPropertyChanged;
            AesKeysView = new ListCollectionView(AesKeys) { SortDescriptions = { new SortDescription("Name", ListSortDirection.Ascending) } };
        });
    }

    private void AesKeysOnItemPropertyChanged(object sender, ItemPropertyChangedEventArgs e)
    {
        if (e.PropertyName != "Key" || sender is not FullyObservableCollection<FileItem> collection)
            return;

        var key = Helper.FixKey(collection[e.CollectionIndex].Key);
        if (e.CollectionIndex == 0)
        {
            if (!HasChange)
                HasChange = Helper.FixKey(_keysFromSettings.MainKey) != key;

            _keysFromSettings.MainKey = key;
        }
        else if (!_keysFromSettings.HasDynamicKeys)
        {
            HasChange = true;
            _keysFromSettings.DynamicKeys = new List<DynamicKey>
            {
                new()
                {
                    Key = key,
                    Name = collection[e.CollectionIndex].Name,
                    Guid = collection[e.CollectionIndex].Guid.ToString()
                }
            };
        }
        else if (_keysFromSettings.DynamicKeys.FirstOrDefault(x => x.Guid == collection[e.CollectionIndex].Guid.ToString()) is { } d)
        {
            if (!HasChange)
                HasChange = Helper.FixKey(d.Key) != key;

            d.Key = key;
        }
        else
        {
            HasChange = true;
            _keysFromSettings.DynamicKeys.Add(new DynamicKey
            {
                Key = key,
                Name = collection[e.CollectionIndex].Name,
                Guid = collection[e.CollectionIndex].Guid.ToString()
            });
        }
    }

    public void SetAesKeys()
    {
        if (_cue4Parse.Game == FGame.Unknown && UserSettings.Default.ManualGames.ContainsKey(UserSettings.Default.GameDirectory))
            UserSettings.Default.ManualGames[UserSettings.Default.GameDirectory].AesKeys = _keysFromSettings;
        else UserSettings.Default.AesKeys[_cue4Parse.Game] = _keysFromSettings;
        Log.Information("{@Json}", UserSettings.Default);
    }

    private IEnumerable<FileItem> EnumerateAesKeys()
    {
        yield return _mainKey;
        _uniqueGuids = new HashSet<FGuid> { Constants.ZERO_GUID };

        var hasDynamicKeys = _keysFromSettings.HasDynamicKeys;
        foreach (var file in _cue4Parse.GameDirectory.DirectoryFiles)
        {
            if (file.Guid == Constants.ZERO_GUID || !_uniqueGuids.Add(file.Guid))
                continue;

            var k = string.Empty;
            if (hasDynamicKeys && _keysFromSettings.DynamicKeys.FirstOrDefault(x => x.Guid == file.Guid.ToString()) is { } dynamicKey)
            {
                k = dynamicKey.Key;
            }

            file.Key = Helper.FixKey(k);
            yield return file;
        }
    }
}
