using System;
using Silk.NET.OpenGL;

namespace FModel.Views.Snooper;

public class Grid : IDisposable
{
    private uint _handle;
    private GL _gl;

    private BufferObject<uint> _ebo;
    private BufferObject<float> _vbo;
    private VertexArrayObject<float, uint> _vao;

    private Shader _shader;

    public readonly uint[] Indices = { 0, 1, 2, 3, 4, 5 };
    public readonly float[] Vertices = {
         1f,  1f, 0f,
        -1f, -1f, 0f,
        -1f,  1f, 0f,
        -1f, -1f, 0f,
         1f,  1f, 0f,
         1f, -1f, 0
    };

    public Grid() {}

    public void Setup(GL gl)
    {
        _gl = gl;

        _handle = _gl.CreateProgram();

        _shader = new Shader(_gl, "grid.vert", "grid.frag");

        _ebo = new BufferObject<uint>(_gl, Indices, BufferTargetARB.ElementArrayBuffer);
        _vbo = new BufferObject<float>(_gl, Vertices, BufferTargetARB.ArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo, _ebo);

        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 3, 0); // position
    }

    public void Bind(Camera camera)
    {
        _gl.DepthMask(false);

        _vao.Bind();

        _shader.Use();

        _shader.SetUniform("view", camera.GetViewMatrix());
        _shader.SetUniform("proj", camera.GetProjectionMatrix());
        _shader.SetUniform("uNear", -0.01f);
        _shader.SetUniform("uFar", 100f);

        _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint) Indices.Length);

        _gl.DepthMask(true);
    }

    public void Dispose()
    {
        _ebo.Dispose();
        _vbo.Dispose();
        _vao.Dispose();
        _shader.Dispose();
        _gl.DeleteProgram(_handle);
    }
}
