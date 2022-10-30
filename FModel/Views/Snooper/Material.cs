using System;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using FModel.Settings;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace FModel.Views.Snooper;

public class Material : IDisposable
{
    private int _handle;

    public readonly CMaterialParams2 Parameters;
    public readonly int UvNumber;
    public bool IsUsed;

    public Texture[] Diffuse;
    public Texture[] Normals;
    public Texture[] SpecularMasks;
    public Texture[] Emissive;

    public Vector4 DiffuseColor;
    public Vector4[] EmissionColor;
    public bool HasSpecularMap;
    public bool HasDiffuseColor;

    private Vector3 _ambientLight;

    public Material(int numUvs)
    {
        Parameters = new CMaterialParams2();
        UvNumber = numUvs;
        IsUsed = false;

        Diffuse = Array.Empty<Texture>();
        Normals = Array.Empty<Texture>();
        SpecularMasks = Array.Empty<Texture>();
        Emissive = Array.Empty<Texture>();

        DiffuseColor = Vector4.Zero;
        EmissionColor = Array.Empty<Vector4>();
        HasSpecularMap = false;
        HasDiffuseColor = false;

        _ambientLight = Vector3.One;
    }

    public Material(int numUvs, UMaterialInterface unrealMaterial) : this(numUvs)
    {
        SwapMaterial(unrealMaterial);
    }

    public void SwapMaterial(UMaterialInterface unrealMaterial)
    {
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
            bool TryGetCached(UTexture2D o, out Texture t)
            {
                var guid = o.LightingGuid;
                if (!cache.TryGetTexture(guid, out t))
                {
                    if (o.GetFirstMip() is { } mip)
                    {
                        TextureDecoder.DecodeTexture(mip, o.Format, o.isNormalMap, platform, out var data, out _);

                        t = new Texture(data, mip.SizeX, mip.SizeY, o);
                        cache.AddTexture(guid, t);
                    }
                    else t = null;
                }
                return t != null;
            }

            Diffuse = new Texture[UvNumber];
            for (int i = 0; i < Diffuse.Length; i++)
                if (Parameters.TryGetTexture2d(out var o, CMaterialParams2.Diffuse[i]) && TryGetCached(o, out var t))
                    Diffuse[i] = t;

            Normals = new Texture[UvNumber];
            for (int i = 0; i < Normals.Length; i++)
                if (Parameters.TryGetTexture2d(out var o, CMaterialParams2.Normals[i]) && TryGetCached(o, out var t))
                    Normals[i] = t;

            SpecularMasks = new Texture[UvNumber];
            for (int i = 0; i < SpecularMasks.Length; i++)
                if (Parameters.TryGetTexture2d(out var o, CMaterialParams2.SpecularMasks[i]) && TryGetCached(o, out var t))
                    SpecularMasks[i] = t;

            Emissive = new Texture[UvNumber];
            EmissionColor = new Vector4[UvNumber];
            for (int i = 0; i < Emissive.Length; i++)
                if (Parameters.TryGetTexture2d(out var o, CMaterialParams2.Emissive[i]) && TryGetCached(o, out var t))
                {
                    Emissive[i] = t;

                    if (Parameters.TryGetLinearColor(out var color, $"Emissive{(i > 0 ? i + 1 : "")}") && color is { A: > 0})
                        EmissionColor[i] = new Vector4(color.R, color.G, color.B, color.A);
                    else EmissionColor[i] = Vector4.One;
                }

            // diffuse light is based on normal map, so increase ambient if no normal map
            _ambientLight = new Vector3(Normals[0] == null ? 1.0f : 0.2f);
            HasSpecularMap = SpecularMasks[0] != null;
        }

        HasDiffuseColor = DiffuseColor != Vector4.Zero;
    }

    public void Render(Shader shader)
    {
        var unit = 0;

        for (var i = 0; i < Diffuse.Length; i++)
        {
            shader.SetUniform($"material.diffuseMap[{i}]", unit);
            Diffuse[i]?.Bind(TextureUnit.Texture0 + unit++);
        }

        for (var i = 0; i < Normals.Length; i++)
        {
            shader.SetUniform($"material.normalMap[{i}]", unit);
            Normals[i]?.Bind(TextureUnit.Texture0 + unit++);
        }

        for (var i = 0; i < SpecularMasks.Length; i++)
        {
            shader.SetUniform($"material.specularMap[{i}]", unit);
            SpecularMasks[i]?.Bind(TextureUnit.Texture0 + unit++);
        }

        for (var i = 0; i < Emissive.Length; i++)
        {
            shader.SetUniform($"material.emissionMap[{i}]", unit);
            shader.SetUniform($"material.emissionColor[{i}]", EmissionColor[i]);
            Emissive[i]?.Bind(TextureUnit.Texture0 + unit++);
        }

        shader.SetUniform("material.useSpecularMap", HasSpecularMap);

        shader.SetUniform("material.hasDiffuseColor", HasDiffuseColor);
        shader.SetUniform("material.diffuseColor", DiffuseColor);

        shader.SetUniform("material.metallic_value", 1f);
        shader.SetUniform("material.roughness_value", 0f);

        shader.SetUniform("light.ambient", _ambientLight);
    }

    public void Dispose()
    {
        for (int i = 0; i < Diffuse.Length; i++)
        {
            Diffuse[i]?.Dispose();
        }
        for (int i = 0; i < Normals.Length; i++)
        {
            Normals[i]?.Dispose();
        }
        for (int i = 0; i < SpecularMasks.Length; i++)
        {
            SpecularMasks[i]?.Dispose();
        }
        for (int i = 0; i < Emissive.Length; i++)
        {
            Emissive[i]?.Dispose();
        }
        GL.DeleteProgram(_handle);
    }
}
