using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;

namespace FModel.Views.Snooper;

public class Material : IDisposable
{
    private int _handle;

    public readonly CMaterialParams2 Parameters;
    public string Name;
    public int SelectedChannel;
    public int SelectedTexture;
    public bool IsUsed;

    public Texture[] Diffuse;
    public Texture[] Normals;
    public Texture[] SpecularMasks;
    public Texture[] Emissive;

    public Vector4[] DiffuseColor;
    public Vector4[] EmissiveColor;

    public Mask M;
    public bool HasM;

    public float Roughness = 0.5f;
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

    public void Setup(Options options, int numTexCoords)
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
                Diffuse = FillTextures(options, numTexCoords, Parameters.HasTopDiffuse, CMaterialParams2.Diffuse, CMaterialParams2.FallbackDiffuse, true);
                Normals = FillTextures(options, numTexCoords, Parameters.HasTopNormals, CMaterialParams2.Normals, CMaterialParams2.FallbackNormals);
                SpecularMasks = FillTextures(options, numTexCoords, Parameters.HasTopSpecularMasks, CMaterialParams2.SpecularMasks, CMaterialParams2.FallbackSpecularMasks);
                Emissive = FillTextures(options, numTexCoords, true, CMaterialParams2.Emissive, CMaterialParams2.FallbackEmissive);
            }

            {   // colors
                DiffuseColor = FillColors(numTexCoords, Diffuse, CMaterialParams2.DiffuseColors, Vector4.One);
                EmissiveColor = FillColors(numTexCoords, Emissive, CMaterialParams2.EmissiveColors, Vector4.One);
            }

            {   // scalars
                if (Parameters.TryGetTexture2d(out var original, "M", "AEM") && options.TryGetTexture(original, false, out var transformed))
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

                if (Parameters.TryGetScalar(out var emissiveMult, "emissive mult", "Emissive_Mult"))
                    EmissiveMult = emissiveMult;

                if (Parameters.TryGetScalar(out var uvScale, "UV Scale"))
                    UVScale = uvScale;
            }
        }
    }

    /// <param name="options">just the cache object</param>
    /// <param name="numTexCoords">number of item in the array</param>
    /// <param name="top">has at least 1 clearly defined texture, else will go straight to fallback</param>
    /// <param name="triggers">list of texture parameter names by uv channel</param>
    /// <param name="fallback">fallback texture name to use if no top texture found</param>
    /// <param name="first">if no top texture, no fallback texture, then use the first texture found</param>
    private Texture[] FillTextures(Options options, int numTexCoords, bool top, IReadOnlyList<string[]> triggers, string fallback, bool first = false)
    {
        UTexture2D original;
        Texture transformed;
        var fix = fallback == CMaterialParams2.FallbackSpecularMasks;
        var textures = new Texture[numTexCoords];

        if (top)
        {
            for (int i = 0; i < textures.Length; i++)
            {
                if (Parameters.TryGetTexture2d(out original, triggers[i]) && options.TryGetTexture(original, fix, out transformed))
                    textures[i] = transformed;
                else if (i > 0 && textures[i - 1] != null)
                    textures[i] = textures[i - 1];
            }
        }
        else if (Parameters.TryGetTexture2d(out original, fallback) && options.TryGetTexture(original, fix, out transformed))
        {
            for (int i = 0; i < textures.Length; i++)
                textures[i] = transformed;
        }
        else if (first && Parameters.TryGetFirstTexture2d(out original) && options.TryGetTexture(original, fix, out transformed))
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
        shader.SetUniform("uParameters.EmissiveMult", EmissiveMult);
        shader.SetUniform("uParameters.UVScale", UVScale);
    }

    private const string _mult = "x %.2f";
    private const float _step = 0.01f;
    private const float _zero = 0.000001f; // doesn't actually work if _infinite is used as max value /shrug
    private const float _infinite = 0.0f;
    private const ImGuiSliderFlags _clamp = ImGuiSliderFlags.AlwaysClamp;
    private void PushStyle()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8, 3));
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(0, 1));
    }

    public void ImGuiParameters()
    {
        PushStyle();
        if (ImGui.BeginTable("parameters", 2))
        {
            SnimGui.Layout("Roughness");ImGui.PushID(1);
            ImGui.DragFloat("", ref Roughness, _step, _zero, 1.0f, _mult, _clamp);
            SnimGui.Layout("Emissive Multiplier");ImGui.PushID(2);
            ImGui.DragFloat("", ref EmissiveMult, _step, _zero, _infinite, _mult, _clamp);
            ImGui.PopID();SnimGui.Layout("UV Scale");ImGui.PushID(3);
            ImGui.DragFloat("", ref UVScale, _step, _zero, _infinite, _mult, _clamp);
            ImGui.PopID();

            if (HasM)
            {
                SnimGui.Layout("Ambient Occlusion");ImGui.PushID(4);
                ImGui.DragFloat("", ref M.AmbientOcclusion, _step, _zero, 1.0f, _mult, _clamp);
                ImGui.PopID();SnimGui.Layout("Cavity");ImGui.PushID(5);
                ImGui.DragFloat("", ref M.Cavity, _step, _zero, 1.0f, _mult, _clamp);
                ImGui.PopID();SnimGui.Layout("Skin Boost Exponent");ImGui.PushID(6);
                ImGui.DragFloat("", ref M.SkinBoost.Exponent, _step, _zero, _infinite, _mult, _clamp);
                ImGui.PopID();SnimGui.Layout("Skin Boost Color");ImGui.PushID(7);
                ImGui.ColorEdit3("", ref M.SkinBoost.Color);
                ImGui.PopID();
            }
            ImGui.EndTable();
        }
        ImGui.PopStyleVar(2);
    }

    public void ImGuiDictionaries<T>(string id, Dictionary<string, T> dictionary, bool center = false, bool wrap = false)
    {
        if (ImGui.BeginTable(id, 2))
        {
            foreach ((string key, T value) in dictionary.Reverse())
            {
                SnimGui.Layout(key, true);
                var text = $"{value:N}";
                if (center) ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() - ImGui.CalcTextSize(text).X) / 2);
                if (wrap) ImGui.TextWrapped(text); else ImGui.Text(text);
                SnimGui.TooltipCopy(text);
            }
            ImGui.EndTable();
        }
    }

    public void ImGuiTextures(Dictionary<string, Texture> icons, Model model)
    {
        PushStyle();
        if (ImGui.BeginTable("material_textures", 2))
        {
            SnimGui.Layout("Channel");ImGui.PushID(1); ImGui.BeginDisabled(model.NumTexCoords < 2);
            ImGui.DragInt("", ref SelectedChannel, _step, 0, model.NumTexCoords - 1, "UV %i", ImGuiSliderFlags.AlwaysClamp);
            ImGui.EndDisabled();ImGui.PopID();SnimGui.Layout("Type");ImGui.PushID(2);
            ImGui.Combo("texture_type", ref SelectedTexture, "Diffuse\0Normals\0Specular\0Ambient Occlusion\0Emissive\0");
            ImGui.PopID();

            switch (SelectedTexture)
            {
                case 0:
                    SnimGui.Layout("Color");ImGui.PushID(3);
                    ImGui.ColorEdit4("", ref DiffuseColor[SelectedChannel], ImGuiColorEditFlags.NoAlpha);
                    ImGui.PopID();
                    break;
                case 4:
                    SnimGui.Layout("Color");ImGui.PushID(3);
                    ImGui.ColorEdit4("", ref EmissiveColor[SelectedChannel], ImGuiColorEditFlags.NoAlpha);
                    ImGui.PopID();
                    break;
            }

            ImGui.EndTable();
        }
        ImGui.PopStyleVar(2);

        ImGui.Image(GetSelectedTexture() ?? icons["noimage"].GetPointer(), new Vector2(ImGui.GetContentRegionAvail().X), Vector2.Zero, Vector2.One, Vector4.One, new Vector4(1.0f, 1.0f, 1.0f, 0.25f));
        ImGui.Spacing();
    }

    private IntPtr? GetSelectedTexture()
    {
        return SelectedTexture switch
        {
            0 => Diffuse[SelectedChannel]?.GetPointer(),
            1 => Normals[SelectedChannel]?.GetPointer(),
            2 => SpecularMasks[SelectedChannel]?.GetPointer(),
            3 => M.Texture?.GetPointer(),
            4 => Emissive[SelectedChannel]?.GetPointer(),
            _ => null
        };
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
