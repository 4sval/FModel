using System;
using OpenTK.Graphics.OpenGL4;

namespace FModel.Views.Snooper.Buffers;

public class RenderbufferObject : IDisposable
{
    private int _handle;

    private int _width;
    private int _height;

    public RenderbufferObject(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public void Setup()
    {
        _handle = GL.GenRenderbuffer();

        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _handle);
        GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, Constants.SAMPLES_COUNT, RenderbufferStorage.Depth24Stencil8, _width, _height);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, _handle);
    }

    public void WindowResized(int width, int height)
    {
        _width = width;
        _height = height;

        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _handle);
        GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, Constants.SAMPLES_COUNT, RenderbufferStorage.Depth24Stencil8, _width, _height);
    }

    public void Dispose()
    {
        GL.DeleteRenderbuffer(_handle);
    }
}
