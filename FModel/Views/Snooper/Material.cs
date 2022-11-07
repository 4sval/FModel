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

        if (Parameters.IsNull)
        {
            Diffuse = new[] { new Texture(new FLinearColor(1f, 0f, 0f, 1f)) };
        }
        else
        {
            Fill(cache, numTexCoords, ref Diffuse, Parameters.HasTopDiffuse, CMaterialParams2.Diffuse, CMaterialParams2.FallbackDiffuse);
            Fill(cache, numTexCoords, ref Normals, true, CMaterialParams2.Normals, CMaterialParams2.FallbackNormals);
            Fill(cache, numTexCoords, ref SpecularMasks, true, CMaterialParams2.SpecularMasks, CMaterialParams2.FallbackSpecularMasks);
            Fill(cache, numTexCoords, ref Emissive, true, CMaterialParams2.Emissive, CMaterialParams2.FallbackEmissive);

            if (Parameters.TryGetTexture2d(out var o, "M") && cache.TryGetCachedTexture(o, out var t))
            {
                M = new Mask { Texture = t, AmbientOcclusion = 0.7f };
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

            DiffuseColor = new Vector4[numTexCoords];
            for (int i = 0; i < DiffuseColor.Length; i++)
            {
                if (Diffuse[i] == null) continue;

                if (Parameters.TryGetLinearColor(out var color, "ColorMult", "Color_mul", "Color") && color is { A: > 0 })
                {
                    DiffuseColor[i] = new Vector4(color.R, color.G, color.B, color.A);
                }
                else DiffuseColor[i] = new Vector4(0.5f);
            }

            EmissiveColor = new Vector4[numTexCoords];
            for (int i = 0; i < EmissiveColor.Length; i++)
            {
                if (Emissive[i] == null) continue;

                string[] names = i == 0 ? new[] { "Emissive", "EmissiveColor", "Emissive Color" } : new[] { $"Emissive{i + 1}" };
                if (Parameters.TryGetLinearColor(out var color, names) && color is { A: > 0 })
                {
                    EmissiveColor[i] = new Vector4(color.R, color.G, color.B, color.A);
                }
                else EmissiveColor[i] = Vector4.One;
            }
        }
    }

    /// <param name="cache"></param>
    /// <param name="numTexCoords"></param>
    /// <param name="array"></param>
    /// <param name="top">has at least 1 clearly defined texture</param>
    /// <param name="triggers">list of texture parameter names</param>
    /// <param name="fallback">fallback texture parameter name</param>
    private void Fill(Cache cache, int numTexCoords, ref Texture[] array, bool top, IReadOnlyList<string[]> triggers, string fallback)
    {
        array = new Texture[numTexCoords];
        if (top)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (Parameters.TryGetTexture2d(out var o, triggers[i]) && cache.TryGetCachedTexture(o, out var t))
                    array[i] = t;
                else if (i > 0 && array[i - 1] != null)
                    array[i] = array[i - 1];
            }
        }
        else if (Parameters.Textures.TryGetValue(fallback, out var u) && u is UTexture2D o && cache.TryGetCachedTexture(o, out var t))
        {
            for (int i = 0; i < array.Length; i++)
                array[i] = t;
        }
        else if (Parameters.Textures.First() is { Value: UTexture2D d } && cache.TryGetCachedTexture(d, out var rip))
        {
            for (int i = 0; i < array.Length; i++)
                array[i] = rip;
        }
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
