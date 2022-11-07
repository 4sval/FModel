using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using OpenTK.Graphics.OpenGL4;

namespace FModel.Views.Snooper;

public class Material : IDisposable
{
    private int _handle;

    public readonly CMaterialParams2 Parameters;
    public string Name;
    public int SelectedChannel;
    public bool IsUsed;

    public Texture[] Diffuse;
    public Texture[] Normals;
    public Texture[] SpecularMasks;
    public Texture[] Emissive;

    public Vector4[] DiffuseColor;
    public Vector4[] EmissiveColor;

    public Mask M;
    public bool HasM;

    public float Roughness;
    public float SpecularMult = 1f;
    public float EmissiveMult = 1f;

    public float UVScale = 1f;

    public Material()
    {
        Parameters = new CMaterialParams2();
        Name = "";
        IsUsed = false;

        Diffuse = Array.Empty<Texture>();
        Normals = Array.Empty<Texture>();
        SpecularMasks = Array.Empty<Texture>();
        Emissive = Array.Empty<Texture>();

        DiffuseColor = Array.Empty<Vector4>();
        EmissiveColor = Array.Empty<Vector4>();
    }

    public Material(UMaterialInterface unrealMaterial) : this()
    {
        SwapMaterial(unrealMaterial);
    }

    public void SwapMaterial(UMaterialInterface unrealMaterial)
    {
        Name = unrealMaterial.Name;
        unrealMaterial.GetParams(Parameters);
    }

    public void Setup(Cache cache, int numTexCoords)
    {
        _handle = GL.CreateProgram();

        if (numTexCoords < 1 || Parameters.IsNull)
        {
            Diffuse = new[] { new Texture(new FLinearColor(1f, 0f, 0f, 1f)) };
            Normals = new[] { new Texture(new FLinearColor(0.498f, 0.498f, 0.996f, 1f))};
            SpecularMasks = new Texture[1];
            Emissive = new Texture[1];
            DiffuseColor = new[] { new Vector4(0.5f) };
            EmissiveColor = new[] { Vector4.One };
        }
        else
        {
            {   // textures
                Diffuse = FillTextures(cache, numTexCoords, Parameters.HasTopDiffuse, CMaterialParams2.Diffuse, CMaterialParams2.FallbackDiffuse, true);
                Normals = FillTextures(cache, numTexCoords, Parameters.HasTopNormals, CMaterialParams2.Normals, CMaterialParams2.FallbackNormals);
                SpecularMasks = FillTextures(cache, numTexCoords, Parameters.HasTopSpecularMasks, CMaterialParams2.SpecularMasks, CMaterialParams2.FallbackSpecularMasks);
                Emissive = FillTextures(cache, numTexCoords, true, CMaterialParams2.Emissive, CMaterialParams2.FallbackEmissive);
            }

            {   // colors
                DiffuseColor = FillColors(numTexCoords, Diffuse, CMaterialParams2.DiffuseColors, new Vector4(0.5f));
                EmissiveColor = FillColors(numTexCoords, Emissive, CMaterialParams2.EmissiveColors, Vector4.One);
            }

            {   // scalars
                if (Parameters.TryGetTexture2d(out var original, "M") && cache.TryGetCachedTexture(original, out var transformed))
                {
                    M = new Mask { Texture = transformed, AmbientOcclusion = 0.7f };
                    HasM = true;
                    if (Parameters.TryGetLinearColor(out var l, "Skin Boost Color And Exponent"))
                        M.SkinBoost = new Boost { Color = new Vector3(l.R, l.G, l.B), Exponent = l.A };
                }

                if (Parameters.TryGetScalar(out var roughnessMin, "RoughnessMin", "SpecRoughnessMin") &&
                    Parameters.TryGetScalar(out var roughnessMax, "RoughnessMax", "SpecRoughnessMax"))
                    Roughness = (roughnessMin + roughnessMax) / 2f;
                if (Parameters.TryGetScalar(out var roughness, "Rough", "Roughness"))
                    Roughness = roughness;

                if (Parameters.TryGetScalar(out var specularMult, "SpecularMult"))
                    SpecularMult = specularMult;
                if (Parameters.TryGetScalar(out var emissiveMult, "emissive mult", "Emissive_Mult"))
                    EmissiveMult = emissiveMult;

                if (Parameters.TryGetScalar(out var uvScale, "UV Scale"))
                    UVScale = uvScale;
            }
        }
    }

    /// <param name="cache">just the cache object</param>
    /// <param name="numTexCoords">number of item in the array</param>
    /// <param name="top">has at least 1 clearly defined texture, else will go straight to fallback</param>
    /// <param name="triggers">list of texture parameter names by uv channel</param>
    /// <param name="fallback">fallback texture name to use if no top texture found</param>
    /// <param name="first">if no top texture, no fallback texture, then use the first texture found</param>
    private Texture[] FillTextures(Cache cache, int numTexCoords, bool top, IReadOnlyList<string[]> triggers, string fallback, bool first = false)
    {
        UTexture2D original;
        Texture transformed;
        var textures = new Texture[numTexCoords];

        if (top)
        {
            for (int i = 0; i < textures.Length; i++)
            {
                if (Parameters.TryGetTexture2d(out original, triggers[i]) && cache.TryGetCachedTexture(original, out transformed))
                    textures[i] = transformed;
                else if (i > 0 && textures[i - 1] != null)
                    textures[i] = textures[i - 1];
            }
        }
        else if (Parameters.TryGetTexture2d(out original, fallback) && cache.TryGetCachedTexture(original, out transformed))
        {
            for (int i = 0; i < textures.Length; i++)
                textures[i] = transformed;
        }
        else if (first && Parameters.TryGetFirstTexture2d(out original) && cache.TryGetCachedTexture(original, out transformed))
        {
            for (int i = 0; i < textures.Length; i++)
                textures[i] = transformed;
        }
        return textures;
    }

    /// <param name="numTexCoords">number of item in the array</param>
    /// <param name="textures">reference array</param>
    /// <param name="triggers">list of color parameter names by uv channel</param>
    /// <param name="fallback">fallback color to use if no trigger was found</param>
    private Vector4[] FillColors(int numTexCoords, IReadOnlyList<Texture> textures, IReadOnlyList<string[]> triggers, Vector4 fallback)
    {
        var colors = new Vector4[numTexCoords];
        for (int i = 0; i < colors.Length; i++)
        {
            if (textures[i] == null) continue;

            if (Parameters.TryGetLinearColor(out var color, triggers[i]) && color is { A: > 0 })
            {
                colors[i] = new Vector4(color.R, color.G, color.B, color.A);
            }
            else colors[i] = fallback;
        }
        return colors;
    }

    public void Render(Shader shader)
    {
        var unit = 0;
        for (var i = 0; i < Diffuse.Length; i++)
        {
            shader.SetUniform($"uParameters.Diffuse[{i}].Sampler", unit);
            shader.SetUniform($"uParameters.Diffuse[{i}].Color", DiffuseColor[i]);
            Diffuse[i]?.Bind(TextureUnit.Texture0 + unit++);
        }

        for (var i = 0; i < Normals.Length; i++)
        {
            shader.SetUniform($"uParameters.Normals[{i}].Sampler", unit);
            Normals[i]?.Bind(TextureUnit.Texture0 + unit++);
        }

        for (var i = 0; i < SpecularMasks.Length; i++)
        {
            shader.SetUniform($"uParameters.SpecularMasks[{i}].Sampler", unit);
            SpecularMasks[i]?.Bind(TextureUnit.Texture0 + unit++);
        }

        for (var i = 0; i < Emissive.Length; i++)
        {
            shader.SetUniform($"uParameters.Emissive[{i}].Sampler", unit);
            shader.SetUniform($"uParameters.Emissive[{i}].Color", EmissiveColor[i]);
            Emissive[i]?.Bind(TextureUnit.Texture0 + unit++);
        }

        M.Texture?.Bind(TextureUnit.Texture31);
        shader.SetUniform("uParameters.M.Sampler", 31);
        shader.SetUniform("uParameters.M.SkinBoost.Color", M.SkinBoost.Color);
        shader.SetUniform("uParameters.M.SkinBoost.Exponent", M.SkinBoost.Exponent);
        shader.SetUniform("uParameters.M.AmbientOcclusion", M.AmbientOcclusion);
        shader.SetUniform("uParameters.M.Cavity", M.Cavity);
        shader.SetUniform("uParameters.HasM", HasM);

        shader.SetUniform("uParameters.Roughness", Roughness);
        shader.SetUniform("uParameters.SpecularMult", SpecularMult);
        shader.SetUniform("uParameters.EmissiveMult", EmissiveMult);

        shader.SetUniform("uParameters.UVScale", UVScale);
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
        M.Texture?.Dispose();
        GL.DeleteProgram(_handle);
    }
}

public struct Mask
{
    public Texture Texture;
    public Boost SkinBoost;

    public float AmbientOcclusion;
    public float Cavity;
}

public struct Boost
{
    public Vector3 Color;
    public float Exponent;
}
