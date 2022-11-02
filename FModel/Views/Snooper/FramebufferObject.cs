using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace FModel.Views.Snooper;

public class FramebufferObject : IDisposable
{
    private int _framebufferHandle;
    private int _postProcessingHandle;

    private int _width;
    private int _height;
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

    public FramebufferObject(Vector2i size)
    {
        _width = size.X;
        _height = size.Y;
        _renderbuffer = new RenderbufferObject(_width, _height);
    }

    public void Setup()
    {
        _framebufferHandle = GL.GenFramebuffer();
        Bind();

        _framebufferTexture = new Texture((uint) _width, (uint) _height);

        _renderbuffer.Setup();

        _shader = new Shader("framebuffer");
        _shader.Use();
        _shader.SetUniform("screenTexture", 0);

        _ebo = new BufferObject<uint>(Indices, BufferTarget.ElementArrayBuffer);
        _vbo = new BufferObject<float>(Vertices, BufferTarget.ArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_vbo, _ebo);

        _vao.VertexAttributePointer(0, 2, VertexAttribPointerType.Float, 4, 0); // position
        _vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 4, 2); // uv

        var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != FramebufferErrorCode.FramebufferComplete)
        {
            throw new Exception($"Framebuffer failed to bind with error: {GL.GetProgramInfoLog(_framebufferHandle)}");
        }

        _postProcessingHandle = GL.GenFramebuffer();
        Bind(_postProcessingHandle);

        _postProcessingTexture = new Texture(_width, _height);

        status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != FramebufferErrorCode.FramebufferComplete)
        {
            throw new Exception($"Post-Processing framebuffer failed to bind with error: {GL.GetProgramInfoLog(_postProcessingHandle)}");
        }
    }

    public void Bind() => Bind(_framebufferHandle);
    public void Bind(int handle)
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, handle);
    }

    public void BindMsaa()
    {
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _framebufferHandle);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _postProcessingHandle);
        GL.BlitFramebuffer(0, 0, _width, _height, 0, 0, _width, _height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
        GL.Disable(EnableCap.DepthTest);

        _shader.Use();
        _vao.Bind();

        _postProcessingTexture.Bind(TextureUnit.Texture0);

        GL.DrawArrays(PrimitiveType.Triangles, 0, Indices.Length);
        GL.Enable(EnableCap.DepthTest);
    }

    public IntPtr GetPointer() => _postProcessingTexture.GetPointer();

    public void WindowResized(int width, int height)
    {
        _width = width;
        _height = height;

        _renderbuffer.WindowResized(width, height);

        _framebufferTexture.WindowResized(width, height);
        _postProcessingTexture.WindowResized(width, height);
    }

    public void Dispose()
    {
        _vao.Dispose();
        _shader.Dispose();
        _framebufferTexture.Dispose();
        _postProcessingTexture.Dispose();
        _renderbuffer.Dispose();
        GL.DeleteFramebuffer(_framebufferHandle);
        GL.DeleteFramebuffer(_postProcessingHandle);
    }
}
