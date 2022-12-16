using System;
using System.Collections.Generic;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Misc;
using FModel.Settings;

namespace FModel.Views.Snooper;

public class Options
{
    public FGuid SelectedModel { get; private set; }
    public int SelectedSection { get; private set; }
    public int SelectedMorph { get; private set; }

    public readonly Dictionary<FGuid, Model> Models;
    public readonly Dictionary<FGuid, Texture> Textures;
    public readonly List<Light> Lights;

    public readonly Dictionary<string, Texture> Icons;

    private ETexturePlatform _platform;
    private readonly string _game;

    public Options()
    {
        Models = new Dictionary<FGuid, Model>();
        Textures = new Dictionary<FGuid, Texture>();
        Lights = new List<Light>();

        Icons = new Dictionary<string, Texture>
        {
            ["material"] = new ("materialicon"),
            ["noimage"] = new ("T_Placeholder_Item_Image"),
            ["pointlight"] = new ("pointlight"),
            ["spotlight"] = new ("spotlight"),
        };

        _platform = UserSettings.Default.OverridedPlatform;
        _game = Services.ApplicationService.ApplicationView.CUE4Parse.Provider.GameName;

        SelectModel(Guid.Empty);
    }

    public void SetupModelsAndLights()
    {
        foreach (var model in Models.Values)
        {
            if (model.IsSetup) continue;
            model.Setup(this);
        }

        foreach (var light in Lights)
        {
            if (light.IsSetup) continue;
            light.Setup();
        }
    }

    public void SelectModel(FGuid guid)
    {
        // unselect old
        if (TryGetModel(out var model))
            model.IsSelected = false;

        // select new
        if (!TryGetModel(guid, out model))
            SelectedModel = Guid.Empty;
        else
        {
            model.IsSelected = true;
            SelectedModel = guid;
        }

        SelectedSection = 0;
        SelectedMorph = 0;
    }

    public void SelectSection(int index)
    {
        SelectedSection = index;
    }

    public void SelectMorph(int index, Model model)
    {
        SelectedMorph = index;
        model.UpdateMorph(SelectedMorph);
    }

    public bool TryGetTexture(UTexture2D o, bool fix, out Texture texture)
    {
        var guid = o.LightingGuid;
        if (!Textures.TryGetValue(guid, out texture) && o.GetFirstMip() is { } mip)
        {
            TextureDecoder.DecodeTexture(mip, o.Format, o.isNormalMap, _platform, out var data, out _);
            if (fix) FixChannels(o, mip, ref data);

            texture = new Texture(data, mip.SizeX, mip.SizeY, o);
            Textures[guid] = texture;
        }
        return texture != null;
    }

    /// <summary>
    /// Red : Specular (if possible)
    /// Blue : Roughness
    /// Green : Metallic
    /// </summary>
    private void FixChannels(UTexture2D o, FTexture2DMipMap mip, ref byte[] data)
    {
        // only if it makes a big difference pls
        switch (_game)
        {
            case "hk_project":
            case "gameface":
            case "divineknockout":
            {
                unsafe
                {
                    var offset = 0;
                    fixed (byte* d = data)
                    {
                        for (var i = 0; i < mip.SizeX * mip.SizeY; i++)
                        {
                            (d[offset + 1], d[offset + 2]) = (d[offset + 2], d[offset + 1]); // RBG
                            offset += 4;
                        }
                    }
                }
                break;
            }
            // R: Roughness
            // G: Metallic
            // B: Whatever (AO / S / E / ...)
            case "ccff7r":
            {
                unsafe
                {
                    var offset = 0;
                    fixed (byte* d = data)
                    {
                        for (var i = 0; i < mip.SizeX * mip.SizeY; i++)
                        {
                            (d[offset + 1], d[offset + 2]) = (d[offset + 2], d[offset + 1]); // RBG
                            (d[offset], d[offset + 1]) = (d[offset + 1], d[offset]); // BRG
                            offset += 4;
                        }
                    }
                }
                break;
            }
            case "shootergame":
            {
                var packedPBRType = o.Name[(o.Name.LastIndexOf('_') + 1)..];
                switch (packedPBRType)
                {
                    case "MRAE": // R: Metallic, G: Roughness, B: AO (0-127) & Emissive (128-255)   (Character PBR)
                        unsafe
                        {
                            var offset = 0;
                            fixed (byte* d = data)
                            {
                                for (var i = 0; i < mip.SizeX * mip.SizeY; i++)
                                {
                                    (d[offset], d[offset + 1]) = (d[offset + 1], d[offset]); // RMAE
                                    // (d[offset], d[offset + 2]) = (d[offset + 2], d[offset]); // AEMR
                                    offset += 4;
                                }
                            }
                        }
                        break;
                    case "MRAS": // R: Metallic, G: Roughness, B: AO, A: Specular   (Legacy PBR)
                    case "MRA": // R: Metallic, G: Roughness, B: AO                (Environment PBR)
                    case "MRS": // R: Metallic, G: Roughness, B: Specular          (Weapon PBR)
                        unsafe
                        {
                            var offset = 0;
                            fixed (byte* d = data)
                            {
                                for (var i = 0; i < mip.SizeX * mip.SizeY; i++)
                                {
                                    (d[offset], d[offset + 2]) = (d[offset + 2], d[offset]); // SRM
                                    (d[offset + 1], d[offset + 2]) = (d[offset + 2], d[offset + 1]); // SMR
                                    offset += 4;
                                }
                            }
                        }
                        break;
                }
                break;
            }
        }
    }

    public bool TryGetModel(out Model model) => Models.TryGetValue(SelectedModel, out model);
    public bool TryGetModel(FGuid guid, out Model model) => Models.TryGetValue(guid, out model);

    public bool TryGetSection(out Section section) => TryGetSection(SelectedModel, out section);
    public bool TryGetSection(FGuid guid, out Section section)
    {
        if (TryGetModel(guid, out var model))
        {
            return TryGetSection(model, out section);
        }

        section = null;
        return false;
    }
    public bool TryGetSection(Model model, out Section section)
    {
        if (SelectedSection >= 0 && SelectedSection < model.Sections.Length)
            section = model.Sections[SelectedSection]; else section = null;
        return section != null;
    }

    public void SwapMaterial(bool value)
    {
        Services.ApplicationService.ApplicationView.CUE4Parse.ModelIsOverwritingMaterial = value;
    }

    public void AnimateMesh(bool value)
    {
        Services.ApplicationService.ApplicationView.CUE4Parse.ModelIsWaitingAnimation = value;
    }

    public void ResetModelsAndLights()
    {
        foreach (var model in Models.Values)
        {
            model.Dispose();
        }
        Models.Clear();
        Lights.Clear();
    }

    public void Dispose()
    {
        ResetModelsAndLights();
        foreach (var texture in Textures.Values)
        {
            texture.Dispose();
        }
        Textures.Clear();
        foreach (var texture in Icons.Values)
        {
            texture.Dispose();
        }
        Icons.Clear();
    }
}
