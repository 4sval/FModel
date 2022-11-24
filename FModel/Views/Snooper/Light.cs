using System;
using System.Numerics;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.Math;
using OpenTK.Graphics.OpenGL4;

namespace FModel.Views.Snooper;

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
    public Texture Icon;
    public readonly Transform Transform;

    public readonly Vector4 Color;
    public readonly float Intensity;
    public readonly float Linear;
    public readonly float Quadratic;

    public Light(Texture icon, UObject light, FVector position)
    {
        var p = light.GetOrDefault("RelativeLocation", FVector.ZeroVector);
        var r = light.GetOrDefault("RelativeRotation", FRotator.ZeroRotator);

        Transform = Transform.Identity;
        Transform.Scale = new FVector(0.25f);
        Transform.Position = position + r.RotateVector(p.ToMapVector()) * Constants.SCALE_DOWN_RATIO;

        Icon = icon;

        Color = light.GetOrDefault("LightColor", new FColor(0xFF, 0xFF, 0xFF, 0xFF));
        Intensity = light.GetOrDefault("Intensity", 1.0f);

        var radius = light.GetOrDefault("AttenuationRadius", 0.0f) * Constants.SCALE_DOWN_RATIO;
        Linear = 4.5f / radius;
        Quadratic = 75.0f / MathF.Pow(radius, 2);
    }

    public void SetupInstances()
    {
        var instanceMatrix = new [] {Transform.Matrix};
        _matrixVbo = new BufferObject<Matrix4x4>(instanceMatrix, BufferTarget.ArrayBuffer);
        _vao.BindInstancing(); // VertexAttributePointer
    }

    public void Setup()
    {
        _handle = GL.CreateProgram();

        _ebo = new BufferObject<uint>(Indices, BufferTarget.ElementArrayBuffer);
        _vbo = new BufferObject<float>(Vertices, BufferTarget.ArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_vbo, _ebo);

        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 3, 0); // position
        SetupInstances();
    }

    public abstract void Render(int i, Shader shader);

    public void Render(Shader shader)
    {
        // GL.Disable(EnableCap.DepthTest);
        _vao.Bind();

        Icon?.Bind(TextureUnit.Texture0);
        shader.SetUniform("uIcon", 0);
        shader.SetUniform("uColor", Color);

        GL.DrawArrays(PrimitiveType.Triangles, 0, Indices.Length);
        // GL.Enable(EnableCap.DepthTest);
    }

    public void Dispose()
    {
        _ebo?.Dispose();
        _vbo?.Dispose();
        _vao?.Dispose();
        GL.DeleteProgram(_handle);
    }
}
