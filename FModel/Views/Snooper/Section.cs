using System;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse_Conversion.Textures;
using FModel.Settings;
using ImGuiNET;
using Silk.NET.OpenGL;

namespace FModel.Views.Snooper;

public class Section : IDisposable
{
    private uint _handle;
    private GL _gl;

    private Texture _diffuseMap;
    private Texture _normalMap;
    private Texture _specularMap;
    // private Texture _metallicMap;
    private Texture _emissionMap;

    public string Name;
    public int Index;
    public uint FacesCount;
    public int FirstFaceIndex;
    public CMaterialParams Parameters;

    public Section(string name, int index, uint facesCount, int firstFaceIndex, CMeshSection section)
    {
        Name = name;
        Index = index;
        FacesCount = facesCount;
        FirstFaceIndex = firstFaceIndex;
        Parameters = new CMaterialParams();
        if (section.Material != null && section.Material.TryLoad(out var material) && material is UMaterialInterface unrealMaterial)
        {
            Name = unrealMaterial.Name;
            unrealMaterial.GetParams(Parameters);
        }
    }

    public void Setup(GL gl)
    {
        _gl = gl;

        _handle = _gl.CreateProgram();

        var platform = UserSettings.Default.OverridedPlatform;
        if (Parameters.Diffuse is UTexture2D { IsVirtual: false } diffuse)
        {
            var mip = diffuse.GetFirstMip();
            TextureDecoder.DecodeTexture(mip, diffuse.Format, diffuse.isNormalMap, platform, out var data, out _);
            _diffuseMap = new Texture(_gl, data, (uint) mip.SizeX, (uint) mip.SizeY);
        }

        if (Parameters.Normal is UTexture2D { IsVirtual: false } normal)
        {
            var mip = normal.GetFirstMip();
            TextureDecoder.DecodeTexture(mip, normal.Format, normal.isNormalMap, platform, out var data, out _);
            _normalMap = new Texture(_gl, data, (uint) mip.SizeX, (uint) mip.SizeY);
        }

        if (Parameters.Specular is UTexture2D { IsVirtual: false } specular)
        {
            var mip = specular.GetFirstMip();
            TextureDecoder.DecodeTexture(mip, specular.Format, specular.isNormalMap, platform, out var data, out _);
            _specularMap = new Texture(_gl, data, (uint) mip.SizeX, (uint) mip.SizeY);
        }

        if (Parameters.HasTopEmissiveTexture &&
            Parameters.EmissiveColor is { A: > 0 } emissiveColor &&
            Parameters.Emissive is UTexture2D { IsVirtual: false } emissive)
        {
            var mip = emissive.GetFirstMip();
            TextureDecoder.DecodeTexture(mip, emissive.Format, emissive.isNormalMap, platform, out var data, out _);
            _emissionMap = new Texture(_gl, data, (uint) mip.SizeX, (uint) mip.SizeY);
        }
    }

    public void Bind(float indices)
    {
        ImGui.TableNextRow();

        ImGui.TableSetColumnIndex(0);
        ImGui.Text(Index.ToString());
        ImGui.TableSetColumnIndex(1);
        ImGui.Text(Name);
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text($"Faces: {FacesCount} ({Math.Round(FacesCount / indices * 100f, 2)}%%)");
            ImGui.Text($"First Face: {FirstFaceIndex}");
            ImGui.Separator();
            ImGui.Text($"Diffuse: ({Parameters.Diffuse?.ExportType}) {Parameters.Diffuse?.Name}");
            ImGui.Text($"Normal: ({Parameters.Normal?.ExportType}) {Parameters.Normal?.Name}");
            ImGui.EndTooltip();
        }

        if (Parameters.IsNull) return;
        _diffuseMap?.Bind(TextureUnit.Texture0);
        _normalMap?.Bind(TextureUnit.Texture1);
        _specularMap?.Bind(TextureUnit.Texture2);
        _emissionMap?.Bind(TextureUnit.Texture4);
    }

    public void Dispose()
    {
        _diffuseMap?.Dispose();
        _normalMap?.Dispose();
        _specularMap?.Dispose();
        _emissionMap?.Dispose();
        _gl.DeleteProgram(_handle);
    }
}
