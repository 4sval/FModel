using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace FModel.Views.Snooper;

public class Skybox : IDisposable
{
    private int _handle;

    private BufferObject<uint> _ebo;
    private BufferObject<float> _vbo;
    private VertexArrayObject<float, uint> _vao;

    private string[] _textures = { "px", "nx", "py", "ny", "pz", "nz" };

    private Texture _cubeMap;
    private Shader _shader;

    public readonly uint[] Indices = { 0, 1, 3, 1, 2, 3 };
    public readonly float[] Vertices = {
        //X    Y      Z
        -0.5f, -0.5f, -0.5f,
        0.5f, -0.5f, -0.5f,
        0.5f,  0.5f, -0.5f,
        0.5f,  0.5f, -0.5f,
        -0.5f,  0.5f, -0.5f,
        -0.5f, -0.5f, -0.5f,

        -0.5f, -0.5f,  0.5f,
        0.5f, -0.5f,  0.5f,
        0.5f,  0.5f,  0.5f,
        0.5f,  0.5f,  0.5f,
        -0.5f,  0.5f,  0.5f,
        -0.5f, -0.5f,  0.5f,

        -0.5f,  0.5f,  0.5f,
        -0.5f,  0.5f, -0.5f,
        -0.5f, -0.5f, -0.5f,
        -0.5f, -0.5f, -0.5f,
        -0.5f, -0.5f,  0.5f,
        -0.5f,  0.5f,  0.5f,

        0.5f,  0.5f,  0.5f,
        0.5f,  0.5f, -0.5f,
        0.5f, -0.5f, -0.5f,
        0.5f, -0.5f, -0.5f,
        0.5f, -0.5f,  0.5f,
        0.5f,  0.5f,  0.5f,

        -0.5f, -0.5f, -0.5f,
        0.5f, -0.5f, -0.5f,
        0.5f, -0.5f,  0.5f,
        0.5f, -0.5f,  0.5f,
        -0.5f, -0.5f,  0.5f,
        -0.5f, -0.5f, -0.5f,

        -0.5f,  0.5f, -0.5f,
        0.5f,  0.5f, -0.5f,
        0.5f,  0.5f,  0.5f,
        0.5f,  0.5f,  0.5f,
        -0.5f,  0.5f,  0.5f,
        -0.5f,  0.5f, -0.5f
    };

    public Skybox() {}

    public void Setup()
    {
        _handle = GL.CreateProgram();

        _ebo = new BufferObject<uint>(Indices, BufferTarget.ElementArrayBuffer);
        _vbo = new BufferObject<float>(Vertices, BufferTarget.ArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_vbo, _ebo);

        _cubeMap = new Texture(_textures);
        _shader = new Shader("skybox");

        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 3, 0); // position
    }

    public void Render(Matrix4 viewMatrix, Matrix4 projMatrix)
    {
        GL.DepthFunc(DepthFunction.Lequal);

        _vao.Bind();

        _cubeMap.Bind(TextureUnit.Texture0);
        _shader.Use();

        viewMatrix.M41 = 0;
        viewMatrix.M42 = 0;
        viewMatrix.M43 = 0;
        _shader.SetUniform("uView", viewMatrix);
        _shader.SetUniform("uProjection", projMatrix);

        _shader.SetUniform("cubemap", 0);

        GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

        GL.DepthFunc(DepthFunction.Less);
    }

    public void Dispose()
    {
        _ebo.Dispose();
        _vbo.Dispose();
        _vao.Dispose();
        _shader.Dispose();
        GL.DeleteProgram(_handle);
    }
}
