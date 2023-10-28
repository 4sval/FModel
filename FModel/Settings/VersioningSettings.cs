using System.Collections.Generic;
using CUE4Parse.UE4.Objects.Core.Serialization;
using FModel.Framework;

namespace FModel.Settings;

public class VersioningSettings : ViewModel
{
    private IList<FCustomVersion> _customVersions;
    public IList<FCustomVersion> CustomVersions
    {
        get => _customVersions;
        set => SetProperty(ref _customVersions, value);
    }

    private IDictionary<string, bool> _options;
    public IDictionary<string, bool> Options
    {
        get => _options;
        set => SetProperty(ref _options, value);
    }

    private IDictionary<string, KeyValuePair<string, string>> _mapStructTypes;
    public IDictionary<string, KeyValuePair<string, string>> MapStructTypes
    {
        get => _mapStructTypes;
        set => SetProperty(ref _mapStructTypes, value);
    }

    public VersioningSettings() {}
}
