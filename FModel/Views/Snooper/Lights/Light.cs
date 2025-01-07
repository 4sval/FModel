using System;
using System.Numerics;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using FModel.Views.Snooper.Buffers;
using FModel.Views.Snooper.Shading;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;

namespace FModel.Views.Snooper.Lights;

public abstract class Light : IDisposable
{
    private int _handle;

    private BufferObject<uint> _ebo;
    private BufferObject<float> _vbo;
    private BufferObject<Matrix4x4> _matrixVbo;
    private VertexArrayObject<float, uint> _vao;

    public readonly uint[] Indices = { 0, 1, 2, 3, 4, 5 };
    public readonly float[] Vertices = {
        1f,  1f, 0f,
        -1f, -1f, 0f,
        -1f,  1f, 0f,
        -1f, -1f, 0f,
        1f,  1f, 0f,
        1f, -1f, 0
    };
    public readonly FGuid Model;
    public readonly Texture Icon;
    public Transform Transform;

    public Vector4 Color;
    public float Intensity;
    public bool IsSetup;

    public Light(Texture icon, UObject light)
    {
        Transform = new Transform
        {
            Position = light.GetOrDefault("RelativeLocation", FVector.ZeroVector) * Constants.SCALE_DOWN_RATIO,
            Rotation = light.GetOrDefault("RelativeRotation", FRotator.ZeroRotator).Quaternion(),
            Scale = light.GetOrDefault("RelativeScale3D", FVector.OneVector)
        };

        Model = new FGuid((uint) light.GetFullName().GetHashCode());
        Icon = icon;

        Color = light.GetOrDefault("LightColor", new FColor(0xFF, 0xFF, 0xFF, 0xFF));
        Intensity = light.GetOrDefault("Intensity", 1.0f);
    }

    public Light(FGuid model, Texture icon, UObject parent, UObject light, Transform transform)
    {
        Transform = new Transform
        {
            Relation = transform.Matrix,
            Position = light.GetOrDefault("RelativeLocation", parent.GetOrDefault("RelativeLocation", FVector.ZeroVector)) * Constants.SCALE_DOWN_RATIO,
            Rotation = light.GetOrDefault("RelativeRotation", parent.GetOrDefault("RelativeRotation", FRotator.ZeroRotator)).Quaternion(),
            Scale = light.GetOrDefault("RelativeScale3D", parent.GetOrDefault("RelativeScale3D", FVector.OneVector))
        };

        Model = model;
        Icon = icon;

        Color = light.GetOrDefault("LightColor", parent.GetOrDefault("LightColor", new FColor(0xFF, 0xFF, 0xFF, 0xFF)));
        Intensity = light.GetOrDefault("Intensity", parent.GetOrDefault("Intensity", 1.0f));
    }

    public void SetupInstances()
    {
        var instanceMatrix = new [] {Transform.Matrix};
        _matrixVbo = new BufferObject<Matrix4x4>(instanceMatrix, BufferTarget.ArrayBuffer);
        _vao.BindInstancing(9); // VertexAttributePointer
    }

    public void UpdateMatrices()
    {
        _matrixVbo.Bind();
        _matrixVbo.Update(0, Transform.Matrix);
        _matrixVbo.Unbind();
    }

    public void Setup()
    {
        _handle = GL.CreateProgram();

        _ebo = new BufferObject<uint>(Indices, BufferTarget.ElementArrayBuffer);
        _vbo = new BufferObject<float>(Vertices, BufferTarget.ArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_vbo, _ebo);

        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 3, 0); // position
        SetupInstances();

        IsSetup = true;
    }

    public void Render(Shader shader)
    {
        GL.Disable(EnableCap.CullFace);

        _vao.Bind();

        Icon?.Bind(TextureUnit.Texture0);
        shader.SetUniform("uIcon", 0);
        shader.SetUniform("uColor", Color);

        GL.DrawArrays(PrimitiveType.Triangles, 0, Indices.Length);

        GL.Enable(EnableCap.CullFace);
    }

    public virtual void Render(int i, Shader shader)
    {
        shader.SetUniform($"uLights[{i}].Base.Color", Color);
        shader.SetUniform($"uLights[{i}].Base.Position", Transform.Matrix.Translation);
        shader.SetUniform($"uLights[{i}].Base.Intensity", Intensity);
    }

    public virtual void ImGuiLight()
    {
        SnimGui.Layout("Color");ImGui.PushID(1);
        ImGui.ColorEdit4("", ref Color, ImGuiColorEditFlags.NoAlpha);
        ImGui.PopID();SnimGui.Layout("Intensity");ImGui.PushID(2);
        ImGui.DragFloat("", ref Intensity, 0.1f);ImGui.PopID();
    }

    public void Dispose()
    {
        _ebo?.Dispose();
        _vbo?.Dispose();
        _vao?.Dispose();
        GL.DeleteProgram(_handle);
    }
}
