using System;
using OpenTK.Graphics.OpenGL4;

namespace FModel.Views.Snooper;

public class Grid : IDisposable
{
    private int _handle;

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

    public void Setup()
    {
        _handle = GL.CreateProgram();

        _ebo = new BufferObject<uint>(Indices, BufferTarget.ElementArrayBuffer);
        _vbo = new BufferObject<float>(Vertices, BufferTarget.ArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_vbo, _ebo);

        _shader = new Shader("grid");

        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 3, 0); // position
    }

    public void Render(Camera camera)
    {
        GL.Disable(EnableCap.DepthTest);
        _vao.Bind();

        _shader.Use();

        _shader.SetUniform("view", camera.GetViewMatrix());
        _shader.SetUniform("proj", camera.GetProjectionMatrix());
        _shader.SetUniform("uNear", camera.Near);
        _shader.SetUniform("uFar", camera.Far);

        GL.DrawArrays(PrimitiveType.Triangles, 0, Indices.Length);
        GL.Enable(EnableCap.DepthTest);
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
