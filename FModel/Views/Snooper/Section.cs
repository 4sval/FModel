using System;
using System.Numerics;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse_Conversion.Textures;
using FModel.Services;
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
    private Texture _emissionMap;

    private bool _hasSpecularMap;
    private bool _hasDiffuseColor;
    private Vector4 _diffuseColor = Vector4.Zero;
    private Vector4 _emissionColor = Vector4.Zero;

    private Vector3 _ambientLight;
    private Vector3 _diffuseLight;
    private Vector3 _specularLight;

    private readonly FGame _game;

    public readonly string Name;
    public readonly int Index;
    public readonly uint FacesCount;
    public readonly int FirstFaceIndex;
    public readonly CMaterialParams Parameters;

    private bool _show = true;
    private bool _wireframe;
    private bool _selected;

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

        _game = ApplicationService.ApplicationView.CUE4Parse.Game;
    }

    public void Setup(GL gl)
    {
        _gl = gl;

        _handle = _gl.CreateProgram();

        if (Parameters.IsNull)
        {
            _diffuseColor = new Vector4(1, 0, 0, 1);
        }
        else
        {
            var platform = UserSettings.Default.OverridedPlatform;
            if (!Parameters.HasTopDiffuseTexture && Parameters.DiffuseColor is { A: > 0 } diffuseColor)
            {
                _diffuseColor = new Vector4(diffuseColor.R, diffuseColor.G, diffuseColor.B, diffuseColor.A);
            }
            else if (Parameters.Diffuse is UTexture2D { IsVirtual: false } diffuse)
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
                SwapSpecular(specular, mip, platform, out var data);
                _specularMap = new Texture(_gl, data, (uint) mip.SizeX, (uint) mip.SizeY);
            }

            if (Parameters.HasTopEmissiveTexture &&
                Parameters.EmissiveColor is { A: > 0 } emissiveColor &&
                Parameters.Emissive is UTexture2D { IsVirtual: false } emissive)
            {
                var mip = emissive.GetFirstMip();
                TextureDecoder.DecodeTexture(mip, emissive.Format, emissive.isNormalMap, platform, out var data, out _);
                _emissionMap = new Texture(_gl, data, (uint) mip.SizeX, (uint) mip.SizeY);
                _emissionColor = new Vector4(emissiveColor.R, emissiveColor.G, emissiveColor.B, emissiveColor.A);
            }
        }

        // diffuse light is based on normal map, so increase ambient if no normal map
        _ambientLight = new Vector3(_normalMap == null ? 1.0f : 0.2f);
        _diffuseLight = new Vector3(0.75f);
        _specularLight = new Vector3(0.5f);
        _hasSpecularMap = _specularMap != null;
        _hasDiffuseColor = _diffuseColor != Vector4.Zero;
        _show = !Parameters.IsNull && !Parameters.IsTransparent;
    }

    /// <summary>
    /// goal is to put
    /// Metallic on Blue
    /// Roughness on Green
    /// Ambient Occlusion on Red
    /// </summary>
    private void SwapSpecular(UTexture2D specular, FTexture2DMipMap mip, ETexturePlatform platform, out byte[] data)
    {
        TextureDecoder.DecodeTexture(mip, specular.Format, specular.isNormalMap, platform, out data, out _);

        switch (_game)
        {
            case FGame.FortniteGame:
            {
                // Fortnite's Specular Texture Channels
                // R Specular
                // G Metallic
                // B Roughness
                unsafe
                {
                    var offset = 0;
                    fixed (byte* d = data)
                    {
                        for (var i = 0; i < mip.SizeX * mip.SizeY; i++)
                        {
                            d[offset] = 0;
                            (d[offset + 1], d[offset + 2]) = (d[offset + 2], d[offset + 1]); // swap G and B
                            offset += 4;
                        }
                    }
                }

                Parameters.RoughnessValue = 1;
                Parameters.MetallicValue = 1;
                break;
            }
            case FGame.ShooterGame:
            {
                var packedPBRType = specular.Name[(specular.Name.LastIndexOf('_') + 1)..];
                switch (packedPBRType)
                {
                    case "MRAE": // R: Metallic, G: AO (0-127) & Emissive (128-255), B: Roughness   (Character PBR)
                        unsafe
                        {
                            var offset = 0;
                            fixed (byte* d = data)
                            {
                                for (var i = 0; i < mip.SizeX * mip.SizeY; i++)
                                {
                                    (d[offset], d[offset + 2]) = (d[offset + 2], d[offset]); // swap R and B
                                    (d[offset], d[offset + 1]) = (d[offset + 1], d[offset]); // swap R and G
                                    offset += 4;
                                }
                            }
                        }

                        break;
                    case "MRAS": // R: Metallic, B: Roughness, B: AO, A: Specular   (Legacy PBR)
                    case "MRA": // R: Metallic, B: Roughness, B: AO                (Environment PBR)
                    case "MRS": // R: Metallic, G: Roughness, B: Specular          (Weapon PBR)
                        unsafe
                        {
                            var offset = 0;
                            fixed (byte* d = data)
                            {
                                for (var i = 0; i < mip.SizeX * mip.SizeY; i++)
                                {
                                    (d[offset], d[offset + 2]) = (d[offset + 2], d[offset]); // swap R and B
                                    offset += 4;
                                }
                            }
                        }

                        break;
                }

                Parameters.RoughnessValue = 1;
                Parameters.MetallicValue = 1;
                break;
            }
            case FGame.Gameface:
            {
                // GTA's Specular Texture Channels
                // R Metallic
                // G Roughness
                // B Specular
                unsafe
                {
                    var offset = 0;
                    fixed (byte* d = data)
                    {
                        for (var i = 0; i < mip.SizeX * mip.SizeY; i++)
                        {
                            (d[offset], d[offset + 2]) = (d[offset + 2], d[offset]); // swap R and B
                            offset += 4;
                        }
                    }
                }

                break;
            }
        }
    }

    public void Bind(int index, Shader shader)
    {
        // ImGui.TableNextRow();
        //
        // ImGui.TableSetColumnIndex(0);
        // ImGui.Text(Index.ToString());
        // ImGui.TableSetColumnIndex(1);
        // ImGui.Text(Name);
        // if (ImGui.IsItemHovered())
        // {
        //     ImGui.BeginTooltip();
        //     ImGui.Text($"Faces: {FacesCount} ({Math.Round(FacesCount / indices * 100f, 2)}%%)");
        //     ImGui.Text($"First Face: {FirstFaceIndex}");
        //     ImGui.Separator();
        //     if (_hasDiffuseColor)
        //     {
        //         ImGui.ColorEdit4("Diffuse Color", ref _diffuseColor, ImGuiColorEditFlags.NoInputs);
        //     }
        //     else
        //     {
        //         ImGui.Text($"Diffuse: ({Parameters.Diffuse?.ExportType}) {Parameters.Diffuse?.Name}");
        //         ImGui.Text($"Normal: ({Parameters.Normal?.ExportType}) {Parameters.Normal?.Name}");
        //         ImGui.Text($"Specular: ({Parameters.Specular?.ExportType}) {Parameters.Specular?.Name}");
        //         if (Parameters.HasTopEmissiveTexture)
        //             ImGui.Text($"Emissive: ({Parameters.Emissive?.ExportType}) {Parameters.Emissive?.Name}");
        //         ImGui.Separator();
        //     }
        //     ImGui.EndTooltip();
        // }

        DrawImGui(index);

        _diffuseMap?.Bind(TextureUnit.Texture0);
        _normalMap?.Bind(TextureUnit.Texture1);
        _specularMap?.Bind(TextureUnit.Texture2);
        _emissionMap?.Bind(TextureUnit.Texture3);

        shader.SetUniform("material.useSpecularMap", _hasSpecularMap);

        shader.SetUniform("material.hasDiffuseColor", _hasDiffuseColor);
        shader.SetUniform("material.diffuseColor", _diffuseColor);

        shader.SetUniform("material.emissionColor", _emissionColor);

        shader.SetUniform("material.shininess", Parameters.MetallicValue);

        shader.SetUniform("light.ambient", _ambientLight);
        shader.SetUniform("light.diffuse", _diffuseLight);
        shader.SetUniform("light.specular", _specularLight);

        _gl.PolygonMode(MaterialFace.Front, _wireframe ? PolygonMode.Line : PolygonMode.Fill);
        if (_show) _gl.DrawArrays(PrimitiveType.Triangles, FirstFaceIndex, FacesCount);
    }

    public void Dispose()
    {
        _diffuseMap?.Dispose();
        _normalMap?.Dispose();
        _specularMap?.Dispose();
        _emissionMap?.Dispose();
        _gl.DeleteProgram(_handle);
    }

    private void DrawImGui(int index)
    {
        ImGui.PushID(index);
        ImGui.Selectable(Name, ref _selected);
        if (_selected)
        {

        }
        ImGui.PopID();
    }
}
