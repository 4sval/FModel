using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Versions;
using FModel.Framework;
using FModel.ViewModels;
using FModel.ViewModels.ApiEndpoints.Models;

namespace FModel.Settings;

public class DirectorySettings : ViewModel
{
    private string _gameName;
    public string GameName
    {
        get => _gameName;
        set => SetProperty(ref _gameName, value);
    }

    private string _gameDirectory;
    public string GameDirectory
    {
        get => _gameDirectory;
        set => SetProperty(ref _gameDirectory, value);
    }

    private bool _isManual;
    public bool IsManual
    {
        get => _isManual;
        set => SetProperty(ref _isManual, value);
    }

    private EGame _ueVersion = EGame.GAME_UE4_LATEST;
    public EGame UeVersion
    {
        get => _ueVersion;
        set => SetProperty(ref _ueVersion, value);
    }

    private ETexturePlatform _texturePlatform = ETexturePlatform.DesktopMobile;
    public ETexturePlatform TexturePlatform
    {
        get => _texturePlatform;
        set => SetProperty(ref _texturePlatform, value);
    }

    private AesResponse _aesKeys;
    public AesResponse AesKeys
    {
        get => _aesKeys;
        set => SetProperty(ref _aesKeys, value);
    }

    private VersioningSettings _versioning = new ();
    public VersioningSettings Versioning
    {
        get => _versioning;
        set => SetProperty(ref _versioning, value);
    }

    private FEndpoint[] _endpoints = { new (), new () };
    public FEndpoint[] Endpoints
    {
        get => _endpoints;
        set => SetProperty(ref _endpoints, value);
    }

    private IList<CustomDirectory> _directories = new List<CustomDirectory>();
    public IList<CustomDirectory> Directories
    {
        get => _directories;
        set => SetProperty(ref _directories, value);
    }

    private bool Equals(DirectorySettings other)
    {
        return GameDirectory == other.GameDirectory && UeVersion == other.UeVersion;
    }

    public override bool Equals(object obj)
    {
        return obj is DirectorySettings other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GameDirectory, (int) UeVersion);
    }
}
