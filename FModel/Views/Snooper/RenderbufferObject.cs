using System;
using Silk.NET.OpenGL;

namespace FModel.Views.Snooper;

public class RenderbufferObject : IDisposable
{
    private uint _handle;
    private GL _gl;

    private readonly uint _width;
    private readonly uint _height;

    public RenderbufferObject(uint width, uint height)
    {
        _width = width;
        _height = height;
    }

    public void Setup(GL gl)
    {
        _gl = gl;
        _handle = _gl.GenRenderbuffer();

        _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _handle);
        _gl.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, Constants.SAMPLES_COUNT, InternalFormat.Depth24Stencil8, _width, _height);
        _gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, _handle);
    }

    public void Dispose()
    {
        _gl.DeleteRenderbuffer(_handle);
    }
}
