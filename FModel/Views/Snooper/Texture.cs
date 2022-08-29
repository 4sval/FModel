using Silk.NET.OpenGL;
using System;
using System.IO;
using System.Windows;

namespace FModel.Views.Snooper;

public class Texture : IDisposable
{
    private uint _handle;
    private GL _gl;

    private TextureType _type;

    public Texture(GL gl, TextureType type)
    {
        _gl = gl;
        _handle = _gl.GenTexture();
        _type = type;

        Bind(TextureUnit.Texture0);
    }

    public unsafe Texture(GL gl, uint width, uint height) : this(gl, TextureType.Framebuffer)
    {
        _gl = gl;
        _handle = _gl.GenTexture();

        _gl.TexImage2D(TextureTarget.Texture2D, 0, (int) InternalFormat.Rgb, width, height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, null);

        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) GLEnum.Nearest);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) GLEnum.Nearest);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) GLEnum.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) GLEnum.ClampToEdge);

        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _handle, 0);
    }

    public unsafe Texture(GL gl, byte[] data, uint width, uint height) : this(gl, TextureType.Normal)
    {
        fixed (void* d = &data[0])
        {
            _gl.TexImage2D(TextureTarget.Texture2D, 0, (int) InternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, d);

            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) GLEnum.LinearMipmapLinear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) GLEnum.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 8);

            _gl.GenerateMipmap(TextureTarget.Texture2D);
        }
    }

    public unsafe Texture(GL gl, string[] textures) : this(gl, TextureType.Cubemap)
    {
        for (int t = 0; t < textures.Length; t++)
        {
            var info = Application.GetResourceStream(new Uri($"/FModel;component/Resources/{textures[t]}.png", UriKind.Relative));
            var stream = new MemoryStream();
            info.Stream.CopyTo(stream);

            fixed (void* d = &stream.ToArray()[0])
            {
                _gl.TexImage2D(TextureTarget.TextureCubeMapPositiveX + t, 0, (int) InternalFormat.Rgb, 256, 256, 0, PixelFormat.Rgb, PixelType.UnsignedByte, d);
            }
        }

        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int) GLEnum.Linear);
        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int) GLEnum.Linear);
        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int) GLEnum.ClampToEdge);
        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int) GLEnum.ClampToEdge);
        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int) GLEnum.ClampToEdge);
    }

    public void Bind(TextureUnit textureSlot)
    {
        _gl.ActiveTexture(textureSlot);
        var target = _type switch
        {
            TextureType.Cubemap => TextureTarget.TextureCubeMap,
            _ => TextureTarget.Texture2D
        };
        _gl.BindTexture(target, _handle);
    }

    public void Dispose()
    {
        _gl.DeleteTexture(_handle);
    }
}

public enum TextureType
{
    Normal,
    Cubemap,
    Framebuffer
}
