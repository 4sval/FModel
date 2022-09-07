using System;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace FModel.Views.Snooper;

public class FramebufferObject : IDisposable
{
    private uint _framebufferHandle;
    private uint _postProcessingHandle;
    private GL _gl;

    private readonly int _width;
    private readonly int _height;
    private readonly RenderbufferObject _renderbuffer;

    private BufferObject<uint> _ebo;
    private BufferObject<float> _vbo;
    private VertexArrayObject<float, uint> _vao;

    private Shader _shader;
    private Texture _framebufferTexture;
    private Texture _postProcessingTexture;

    public readonly uint[] Indices = { 0, 1, 2, 3, 4, 5 };
    public readonly float[] Vertices = {
        // Coords    // texCoords
        1.0f, -1.0f,  1.0f, 0.0f,
        -1.0f, -1.0f,  0.0f, 0.0f,
        -1.0f,  1.0f,  0.0f, 1.0f,

        1.0f,  1.0f,  1.0f, 1.0f,
        1.0f, -1.0f,  1.0f, 0.0f,
        -1.0f,  1.0f,  0.0f, 1.0f
    };

    public FramebufferObject(Vector2D<int> size)
    {
        _width = size.X;
        _height = size.Y;
        _renderbuffer = new RenderbufferObject((uint) _width, (uint) _height);
    }

    public void Setup(GL gl)
    {
        _gl = gl;

        _framebufferHandle = _gl.GenFramebuffer();
        Bind(_framebufferHandle);

        _framebufferTexture = new Texture(_gl, (uint) _width, (uint) _height);

        _renderbuffer.Setup(gl);

        _shader = new Shader(_gl, "framebuffer");
        _shader.Use();
        _shader.SetUniform("screenTexture", 0);

        _ebo = new BufferObject<uint>(_gl, Indices, BufferTargetARB.ElementArrayBuffer);
        _vbo = new BufferObject<float>(_gl, Vertices, BufferTargetARB.ArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo, _ebo);

        _vao.VertexAttributePointer(0, 2, VertexAttribPointerType.Float, 4, 0); // position
        _vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 4, 2); // uv

        var status = _gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != GLEnum.FramebufferComplete)
        {
            throw new Exception($"Framebuffer failed to bind with error: {_gl.GetProgramInfoLog(_framebufferHandle)}");
        }

        _postProcessingHandle = _gl.GenFramebuffer();
        Bind(_postProcessingHandle);

        _postProcessingTexture = new Texture(_gl, _width, _height);

        status = _gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != GLEnum.FramebufferComplete)
        {
            throw new Exception($"Post-Processing framebuffer failed to bind with error: {_gl.GetProgramInfoLog(_postProcessingHandle)}");
        }
    }

    public void Bind() => Bind(_framebufferHandle);
    public void Bind(uint handle)
    {
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, handle);
    }

    public void BindMsaa()
    {
        _gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _framebufferHandle);
        _gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _postProcessingHandle);
        _gl.BlitFramebuffer(0, 0, _width, _height, 0, 0, _width, _height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
    }

    public void BindStuff()
    {
        _gl.Disable(EnableCap.DepthTest);

        _shader.Use();
        _vao.Bind();

        _postProcessingTexture.Bind(TextureUnit.Texture0);

        _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint) Indices.Length);
        _gl.Enable(EnableCap.DepthTest);
    }

    public IntPtr GetPointer() => _postProcessingTexture.GetPointer();

    public void Dispose()
    {
        _vao.Dispose();
        _shader.Dispose();
        _framebufferTexture.Dispose();
        _postProcessingTexture.Dispose();
        _renderbuffer.Dispose();
        _gl.DeleteFramebuffer(_framebufferHandle);
        _gl.DeleteFramebuffer(_postProcessingHandle);
    }
}
