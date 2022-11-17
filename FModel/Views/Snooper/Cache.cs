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
    public readonly Dictionary<string, Texture> Icons;

    private ETexturePlatform _platform;
    private readonly FGame _game;

    public Cache()
    {
        Models = new Dictionary<FGuid, Model>();
        Textures = new Dictionary<FGuid, Texture>();
        Icons = new Dictionary<string, Texture>();
        _platform = UserSettings.Default.OverridedPlatform;
        _game = Services.ApplicationService.ApplicationView.CUE4Parse.Game;

        Icons["material"] = new Texture("materialicon");
        Icons["noimage"] = new Texture("T_Placeholder_Item_Image");
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

    public bool TryGetCachedTexture(UTexture2D o, bool fix, out Texture texture)
    {
        var guid = o.LightingGuid;
        if (!Textures.TryGetValue(guid, out texture) && o.GetFirstMip() is { } mip)
        {
            TextureDecoder.DecodeTexture(mip, o.Format, o.isNormalMap, _platform, out var data, out _);
            // if (fix) FixChannels(o, mip, ref data);

            texture = new Texture(data, mip.SizeX, mip.SizeY, o);
            Textures[guid] = texture;
        }
        return texture != null;
    }

    /// <summary>
    /// Red : Specular
    /// Blue : Roughness
    /// Green : Metallic
    /// </summary>
    private void FixChannels(UTexture2D o, FTexture2DMipMap mip, ref byte[] data)
    {
        // only if it makes a big difference pls
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

        foreach (var texture in Icons.Values)
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
        Icons.Clear();
    }
}
