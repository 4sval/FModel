using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

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

    public Vector4 DiffuseColor;
    public Vector4[] EmissionColor;
    public bool HasSpecularMap;
    public bool HasDiffuseColor;

    private Vector3 _ambientLight;

    public Material()
    {
        Parameters = new CMaterialParams2();
        Name = "";
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
            DiffuseColor = new Vector4(1, 0, 0, 1);
        }
        else
        {
            Fill(cache, numTexCoords, ref Diffuse, Parameters.HasTopDiffuse, CMaterialParams2.Diffuse, CMaterialParams2.FallbackDiffuse);
            Fill(cache, numTexCoords, ref Normals, true, CMaterialParams2.Normals, CMaterialParams2.FallbackNormals);
            Fill(cache, numTexCoords, ref SpecularMasks, true, CMaterialParams2.SpecularMasks, CMaterialParams2.FallbackSpecularMasks);
            Fill(cache, numTexCoords, ref Emissive, true, CMaterialParams2.Emissive, CMaterialParams2.FallbackEmissive);

            // if (Parameters.Colors.TryGetValue("ColorMult", out var color) && color is { A: > 0})
            // {
            //     DiffuseColor = new Vector4(color.R, color.G, color.B, color.A);
            // }

            EmissionColor = new Vector4[numTexCoords];
            for (int i = 0; i < EmissionColor.Length; i++)
            {
                if (Emissive[i] == null) continue;

                if (Parameters.TryGetLinearColor(out var color, $"Emissive{(i > 0 ? i + 1 : "")}") && color is { A: > 0 })
                {
                    EmissionColor[i] = new Vector4(color.R, color.G, color.B, color.A);
                }
                else EmissionColor[i] = Vector4.One;
            }

            // diffuse light is based on normal map, so increase ambient if no normal map
            _ambientLight = new Vector3(Normals[0] == null ? 1.0f : 0.2f);
            HasSpecularMap = SpecularMasks[0] != null;
        }

        HasDiffuseColor = DiffuseColor != Vector4.Zero;
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
        else if (Parameters.Colors.TryGetValue("Color", out var linearColor)) // POC
        {
            for (int i = 0; i < array.Length; i++)
                array[i] = new Texture(linearColor);
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
