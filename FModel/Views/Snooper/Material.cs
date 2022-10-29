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

    private Vector3 _ambientLight;

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

    public Material()
    {
        Parameters = new CMaterialParams2();
        UvNumber = 1;
        DiffuseColor = Vector4.Zero;
        EmissionColor = Array.Empty<Vector4>();
        IsUsed = false;
    }

    public Material(int numUvs, UMaterialInterface unrealMaterial) : this()
    {
        UvNumber = numUvs;
        SwapMaterial(unrealMaterial);
    }

    public void SwapMaterial(UMaterialInterface unrealMaterial)
    {
        unrealMaterial.GetParams(Parameters);
    }

    public void Setup(Cache cache)
    {
        _handle = GL.CreateProgram();

        int index;
        var platform = UserSettings.Default.OverridedPlatform;
        void Add(Texture[] array, UTexture2D original)
        {
            var guid = original.LightingGuid;
            if (cache.TryGetTexture(guid, out var texture))
            {
                array[index] = texture;
            }
            else if (original.GetFirstMip() is { } mip)
            {
                TextureDecoder.DecodeTexture(mip, original.Format, original.isNormalMap, platform, out var data, out _);

                var t = new Texture(data, mip.SizeX, mip.SizeY, original);
                cache.AddTexture(guid, t);
                array[index] = t;
            }
        }

        index = 0;
        Diffuse = new Texture[UvNumber];
        foreach (var d in Parameters.GetDiffuseTextures())
        {
            if (index < UvNumber && d is UTexture2D original)
                Add(Diffuse, original);
            index++;
        }

        index = 0;
        Normals = new Texture[UvNumber];
        foreach (var n in Parameters.GetNormalsTextures())
        {
            if (index < UvNumber && n is UTexture2D original)
                Add(Normals, original);
            index++;
        }

        index = 0;
        SpecularMasks = new Texture[UvNumber];
        foreach (var s in Parameters.GetSpecularMasksTextures())
        {
            if (index < UvNumber && s is UTexture2D original)
                Add(SpecularMasks, original);
            index++;
        }

        index = 0;
        Emissive = new Texture[UvNumber];
        EmissionColor = new Vector4[UvNumber];
        foreach (var e in Parameters.GetEmissiveTextures())
        {
            if (index < UvNumber && e is UTexture2D original)
            {
                if (Parameters.TryGetLinearColor(out var color, $"Emissive{(index > 0 ? index + 1 : "")}") && color is { A: > 0})
                    EmissionColor[index] = new Vector4(color.R, color.G, color.B, color.A);
                else EmissionColor[index] = Vector4.One;

                Add(Emissive, original);
            }
            else if (index < UvNumber) EmissionColor[index] = Vector4.Zero;
            index++;
        }

        // diffuse light is based on normal map, so increase ambient if no normal map
        _ambientLight = new Vector3(Normals[0] == null ? 1.0f : 0.2f);
        HasSpecularMap = SpecularMasks[0] != null;
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
