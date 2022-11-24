using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Objects.Core.Misc;
using OpenTK.Graphics.OpenGL4;
using System.Numerics;

namespace FModel.Views.Snooper;

public class PickingTexture : IDisposable
{
    private int _width;
    private int _height;

    private int _framebufferHandle;

    private Shader _shader;
    private int _pickingTexture;
    private int _depthTexture;

    public PickingTexture(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public void Setup()
    {
        _framebufferHandle = GL.GenFramebuffer();
        Bind();

        _shader = new Shader("picking");

        _pickingTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _pickingTexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32ui, _width, _height, 0, PixelFormat.RgbaInteger, PixelType.UnsignedInt, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMinFilter.Nearest);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _pickingTexture, 0);

        _depthTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _depthTexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent, _width, _height, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, _depthTexture, 0);

        var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != FramebufferErrorCode.FramebufferComplete)
        {
            throw new Exception($"Framebuffer failed to bind with error: {GL.GetProgramInfoLog(_framebufferHandle)}");
        }

        GL.BindTexture(TextureTarget.Texture2D, 0);
        Bind(0);
    }

    public void Render(Matrix4x4 viewMatrix, Matrix4x4 projMatrix, IDictionary<FGuid,Model> models)
    {
        Bind();
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _shader.Render(viewMatrix, projMatrix);
        foreach ((FGuid guid, Model model) in models)
        {
            _shader.SetUniform("uA", guid.A);
            _shader.SetUniform("uB", guid.B);
            _shader.SetUniform("uC", guid.C);
            _shader.SetUniform("uD", guid.D);

            if (!model.Show) continue;
            model.SimpleRender(_shader);
        }

        Bind(0);
    }

    public void Bind() => Bind(_framebufferHandle);
    public void Bind(int handle)
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, handle);
    }

    public FGuid ReadPixel(Vector2 mousePos, Vector2 windowPos, Vector2 windowSize)
    {
        Bind();
        FGuid pixel = default;

        var scaleX = windowSize.X / _width;
        var scaleY = windowSize.Y / _height;
        var x = Convert.ToInt32((mousePos.X - windowPos.X) / scaleX);
        var y = -Convert.ToInt32((mousePos.Y - windowPos.Y) / scaleY);

        GL.ReadBuffer(ReadBufferMode.ColorAttachment0);
        GL.ReadPixels(x, y, 1, 1, PixelFormat.RgbaInteger, PixelType.UnsignedInt, ref pixel);
        GL.ReadBuffer(ReadBufferMode.None);

        Bind(0);
        return pixel;
    }

    public void WindowResized(int width, int height)
    {
        _width = width;
        _height = height;

        GL.BindTexture(TextureTarget.Texture2D, _pickingTexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32ui, _width, _height, 0, PixelFormat.RgbaInteger, PixelType.UnsignedInt, IntPtr.Zero);

        GL.BindTexture(TextureTarget.Texture2D, _depthTexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent, _width, _height, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
    }

    public void Dispose()
    {
        _shader?.Dispose();
        GL.DeleteTexture(_pickingTexture);
        GL.DeleteTexture(_depthTexture);
        GL.DeleteFramebuffer(_framebufferHandle);
    }
}
