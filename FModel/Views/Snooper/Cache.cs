using System;
using System.Collections.Generic;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Misc;
using FModel.Settings;

namespace FModel.Views.Snooper;

public class Cache : IDisposable
{
    public readonly Dictionary<FGuid, Model> Models;
    public readonly Dictionary<FGuid, Texture> Textures;

    private ETexturePlatform _platform;

    public Cache()
    {
        Models = new Dictionary<FGuid, Model>();
        Textures = new Dictionary<FGuid, Texture>();
        _platform = UserSettings.Default.OverridedPlatform;
    }

    public bool TryGetCachedModel(UStaticMesh o, out Model model)
    {
        var guid = o.LightingGuid;
        if (!Models.TryGetValue(guid, out model) && o.TryConvert(out var mesh))
        {
            model = new Model(o.Name, o.ExportType, o.Materials, mesh);
            Models[guid] = model;
        }
        return model != null;
    }

    public bool TryGetCachedTexture(UTexture2D o, out Texture texture)
    {
        var guid = o.LightingGuid;
        if (!Textures.TryGetValue(guid, out texture) && o.GetFirstMip() is { } mip)
        {
            TextureDecoder.DecodeTexture(mip, o.Format, o.isNormalMap, _platform, out var data, out _);

            texture = new Texture(data, mip.SizeX, mip.SizeY, o);
            Textures[guid] = texture;
        }
        return texture != null;
    }

    public void Setup()
    {
        foreach (var model in Models.Values)
        {
            if (model.IsSetup) continue;
            model.Setup(this);
        }
    }

    public void DisposeModels()
    {
        foreach (var model in Models.Values)
        {
            model.Dispose();
        }
    }
    public void DisposeTextures()
    {
        foreach (var texture in Textures.Values)
        {
            texture.Dispose();
        }
    }

    public void Dispose()
    {
        DisposeModels();
        Models.Clear();

        DisposeTextures();
        Textures.Clear();
    }
}
