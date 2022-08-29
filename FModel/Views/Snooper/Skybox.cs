using System;
using Silk.NET.OpenGL;

namespace FModel.Views.Snooper;

public class Skybox : IDisposable
{
    private uint _handle;
    private GL _gl;

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

    public void Setup(GL gl)
    {
        _gl = gl;

        _handle = _gl.CreateProgram();

        _cubeMap = new Texture(_gl, _textures);
        _shader = new Shader(_gl, "skybox.vert", "skybox.frag");

        _ebo = new BufferObject<uint>(_gl, Indices, BufferTargetARB.ElementArrayBuffer);
        _vbo = new BufferObject<float>(_gl, Vertices, BufferTargetARB.ArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo, _ebo);

        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 3, 0); // position
    }

    public void Bind(Camera camera)
    {
        _gl.DepthFunc(DepthFunction.Lequal);

        _vao.Bind();

        _cubeMap.Bind(TextureUnit.Texture0);
        _shader.Use();

        var view = camera.GetViewMatrix();
        view.M41 = 0;
        view.M42 = 0;
        view.M43 = 0;
        _shader.SetUniform("uView", view);
        _shader.SetUniform("uProjection", camera.GetProjectionMatrix());

        _shader.SetUniform("cubemap", 0);

        _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);

        _gl.DepthFunc(DepthFunction.Less);
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
