using System;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse_Conversion.Textures;
using FModel.Services;
using FModel.Settings;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SkiaSharp;

namespace FModel.Views.Snooper;

public class Section : IDisposable
{
    private int _handle;

    private Vector3 _ambientLight;

    private readonly FGame _game;

    public string Name;
    public readonly int Index;
    public readonly int FacesCount;
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

    private Section(string name, int index, int facesCount, int firstFaceIndex)
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

    public Section(string name, int index, int facesCount, int firstFaceIndex, CMeshSection section) : this(name, index, facesCount, firstFaceIndex)
    {
        if (section.Material != null && section.Material.TryLoad(out var material) && material is UMaterialInterface unrealMaterial)
        {
            SwapMaterial(unrealMaterial);
        }
    }

    public Section(int index, int facesCount, int firstFaceIndex, UMaterialInterface unrealMaterial) : this(string.Empty, index, facesCount, firstFaceIndex)
    {
        SwapMaterial(unrealMaterial);
    }

    public void SwapMaterial(UMaterialInterface unrealMaterial)
    {
        Name = unrealMaterial.Name;
        unrealMaterial.GetParams(Parameters);
    }

    public void Setup(Cache cache)
    {
        _handle = GL.CreateProgram();

        if (Parameters.IsNull)
        {
            DiffuseColor = new Vector4(1, 0, 0, 1);
        }
        else
        {
            var platform = UserSettings.Default.OverridedPlatform;
            void Add(int index, UTexture2D original)
            {
                var guid = original.LightingGuid;
                if (cache.TryGetTexture(guid, out var texture))
                {
                    // Anything in Parameters that is supposed to be modified will not be modified
                    // eg. Metallic Roughness
                    Textures[index] = texture;
                }
                else if (original.GetFirstMip() is { } mip)
                {
                    byte[] data;
                    if (index != 2) TextureDecoder.DecodeTexture(mip, original.Format, original.isNormalMap, platform, out data, out _);
                    else SwapSpecular(original, mip, platform, out data);

                    var t = new Texture(data, mip.SizeX, mip.SizeY, original);
                    cache.AddTexture(guid, t);
                    Textures[index] = t;
                }
            }

            if (!Parameters.HasTopDiffuseTexture && Parameters.DiffuseColor is { A: > 0 } diffuseColor)
                DiffuseColor = new Vector4(diffuseColor.R, diffuseColor.G, diffuseColor.B, diffuseColor.A);
            else if (Parameters.Diffuse is UTexture2D { IsVirtual: false } diffuse)
                Add(0, diffuse);

            if (Parameters.Normal is UTexture2D { IsVirtual: false } normal)
                Add(1, normal);

            if (Parameters.Specular is UTexture2D { IsVirtual: false } specular)
                Add(2, specular);

            if (Parameters.HasTopEmissiveTexture &&
                Parameters.Emissive is UTexture2D { IsVirtual: false } emissive)
            {
                Add(3, emissive);
                if (Parameters.EmissiveColor is { A: > 0 } emissiveColor)
                    EmissionColor = new Vector4(emissiveColor.R, emissiveColor.G, emissiveColor.B, emissiveColor.A);
                else
                    EmissionColor = Vector4.One;
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

    public void Render(Shader shader, int instanceCount)
    {
        for (var i = 0; i < Textures.Length; i++)
        {
            Textures[i]?.Bind(TextureUnit.Texture0 + i);
        }

        shader.SetUniform("material.useSpecularMap", HasSpecularMap);

        shader.SetUniform("material.hasDiffuseColor", HasDiffuseColor);
        shader.SetUniform("material.diffuseColor", DiffuseColor);

        shader.SetUniform("material.emissionColor", EmissionColor);

        shader.SetUniform("material.metallic_value", 1f);
        shader.SetUniform("material.roughness_value", 0f);

        shader.SetUniform("light.ambient", _ambientLight);

        GL.PolygonMode(MaterialFace.FrontAndBack, Wireframe ? PolygonMode.Line : PolygonMode.Fill);
        if (Show) GL.DrawArraysInstanced(PrimitiveType.Triangles, FirstFaceIndex, FacesCount, instanceCount);
    }

    public void Dispose()
    {
        for (var i = 0; i < Textures.Length; i++)
        {
            Textures[i]?.Dispose();
        }
        GL.DeleteProgram(_handle);
    }
}
