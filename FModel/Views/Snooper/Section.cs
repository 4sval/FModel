using System;
using System.Numerics;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse_Conversion.Textures;
using FModel.Services;
using FModel.Settings;
using Silk.NET.OpenGL;
using SkiaSharp;

namespace FModel.Views.Snooper;

public class Section : IDisposable
{
    private uint _handle;
    private GL _gl;

    private Vector3 _ambientLight;

    private readonly FGame _game;

    public string Name;
    public readonly int Index;
    public readonly uint FacesCount;
    public readonly int FirstFaceIndex;
    public readonly CMaterialParams Parameters;

    public bool Show;
    public bool Wireframe;
    public readonly Texture[] Textures;
    public readonly string[] TexturesLabels;
    public Vector4 DiffuseColor;
    public Vector4 EmissionColor;
    public bool HasSpecularMap;
    public bool HasDiffuseColor;

    private Section(string name, int index, uint facesCount, int firstFaceIndex)
    {
        Name = name;
        Index = index;
        FacesCount = facesCount;
        FirstFaceIndex = firstFaceIndex;
        Parameters = new CMaterialParams();

        Show = true;
        Textures = new Texture[4];
        TexturesLabels = new[] { "Diffuse", "Normal", "Specular", "Emissive" };
        DiffuseColor = Vector4.Zero;
        EmissionColor = Vector4.Zero;

        _game = ApplicationService.ApplicationView.CUE4Parse.Game;
    }

    public Section(string name, int index, uint facesCount, int firstFaceIndex, CMeshSection section) : this(name, index, facesCount, firstFaceIndex)
    {
        if (section.Material != null && section.Material.TryLoad(out var material) && material is UMaterialInterface unrealMaterial)
        {
            SwapMaterial(unrealMaterial);
        }
    }

    public Section(int index, uint facesCount, int firstFaceIndex, UMaterialInterface unrealMaterial) : this(string.Empty, index, facesCount, firstFaceIndex)
    {
        SwapMaterial(unrealMaterial);
    }

    public void SwapMaterial(UMaterialInterface unrealMaterial)
    {
        Name = unrealMaterial.Name;
        unrealMaterial.GetParams(Parameters);
    }

    public void Setup(GL gl)
    {
        _gl = gl;

        _handle = _gl.CreateProgram();

        if (Parameters.IsNull)
        {
            DiffuseColor = new Vector4(1, 0, 0, 1);
        }
        else
        {
            var platform = UserSettings.Default.OverridedPlatform;
            if (!Parameters.HasTopDiffuseTexture && Parameters.DiffuseColor is { A: > 0 } diffuseColor)
            {
                DiffuseColor = new Vector4(diffuseColor.R, diffuseColor.G, diffuseColor.B, diffuseColor.A);
            }
            else if (Parameters.Diffuse is UTexture2D { IsVirtual: false } diffuse)
            {
                var mip = diffuse.GetFirstMip();
                TextureDecoder.DecodeTexture(mip, diffuse.Format, diffuse.isNormalMap, platform, out var data, out var colorType);
                Textures[0] = new Texture(_gl, data, (uint) mip.SizeX, (uint) mip.SizeY, colorType, diffuse);
            }

            if (Parameters.Normal is UTexture2D { IsVirtual: false } normal)
            {
                var mip = normal.GetFirstMip();
                TextureDecoder.DecodeTexture(mip, normal.Format, normal.isNormalMap, platform, out var data, out var colorType);
                Textures[1] = new Texture(_gl, data, (uint) mip.SizeX, (uint) mip.SizeY, colorType, normal);
            }

            if (Parameters.Specular is UTexture2D { IsVirtual: false } specular)
            {
                var mip = specular.GetFirstMip();
                SwapSpecular(specular, mip, platform, out var data, out var colorType);
                Textures[2] = new Texture(_gl, data, (uint) mip.SizeX, (uint) mip.SizeY, colorType, specular);
            }

            if (Parameters.HasTopEmissiveTexture &&
                Parameters.Emissive is UTexture2D { IsVirtual: false } emissive)
            {
                var mip = emissive.GetFirstMip();
                TextureDecoder.DecodeTexture(mip, emissive.Format, emissive.isNormalMap, platform, out var data, out var colorType);
                Textures[3] = new Texture(_gl, data, (uint) mip.SizeX, (uint) mip.SizeY, colorType, emissive);
                if (Parameters.EmissiveColor is { A: > 0 } emissiveColor)
                {
                    EmissionColor = new Vector4(emissiveColor.R, emissiveColor.G, emissiveColor.B, emissiveColor.A);
                }
                else
                {
                    EmissionColor = Vector4.One;
                }
            }
        }

        // diffuse light is based on normal map, so increase ambient if no normal map
        _ambientLight = new Vector3(Textures[1] == null ? 1.0f : 0.2f);
        HasSpecularMap = Textures[2] != null;
        HasDiffuseColor = DiffuseColor != Vector4.Zero;
        Show = !Parameters.IsNull && !Parameters.IsTransparent;
    }

    /// <summary>
    /// goal is to put
    /// Metallic on Blue
    /// Roughness on Green
    /// Ambient Occlusion on Red
    /// </summary>
    private void SwapSpecular(UTexture2D specular, FTexture2DMipMap mip, ETexturePlatform platform, out byte[] data, out SKColorType colorType)
    {
        TextureDecoder.DecodeTexture(mip, specular.Format, specular.isNormalMap, platform, out data, out colorType);

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
                            (d[offset + 1], d[offset + 2]) = (d[offset + 2], d[offset + 1]); // swap G and B
                            offset += 4;
                        }
                    }
                }
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

        Parameters.MetallicValue = 1;
        Parameters.RoughnessValue = 0;
    }

    public void Bind(Shader shader, uint instanceCount)
    {
        for (var i = 0; i < Textures.Length; i++)
        {
            Textures[i]?.Bind(TextureUnit.Texture0 + i);
        }

        shader.SetUniform("material.useSpecularMap", HasSpecularMap);

        shader.SetUniform("material.hasDiffuseColor", HasDiffuseColor);
        shader.SetUniform("material.diffuseColor", DiffuseColor);

        shader.SetUniform("material.emissionColor", EmissionColor);

        shader.SetUniform("material.metallic_value", Parameters.MetallicValue);
        shader.SetUniform("material.roughness_value", Parameters.RoughnessValue);

        shader.SetUniform("light.ambient", _ambientLight);

        _gl.PolygonMode(MaterialFace.FrontAndBack, Wireframe ? PolygonMode.Line : PolygonMode.Fill);
        if (Show) _gl.DrawArraysInstanced(PrimitiveType.Triangles, FirstFaceIndex, FacesCount, instanceCount);
    }

    public void Dispose()
    {
        for (var i = 0; i < Textures.Length; i++)
        {
            Textures[i]?.Dispose();
        }
        _gl.DeleteProgram(_handle);
    }
}
