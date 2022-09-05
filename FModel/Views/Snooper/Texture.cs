using Silk.NET.OpenGL;
using System;
using System.Windows;
using CUE4Parse.UE4.Assets.Exports.Texture;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;

namespace FModel.Views.Snooper;

public class Texture : IDisposable
{
    private readonly uint _handle;
    private readonly GL _gl;
    private readonly TextureType _type;

    public readonly string Type;
    public readonly string Name;
    public readonly string Path;
    public readonly EPixelFormat Format;
    public readonly uint ImportedWidth;
    public readonly uint ImportedHeight;
    public readonly uint Width;
    public readonly uint Height;
    public string Label;

    public Texture(GL gl, TextureType type)
    {
        _gl = gl;
        _handle = _gl.GenTexture();
        _type = type;

        Label = "(?) Click to Copy";
    }

    public Texture(GL gl, uint width, uint height) : this(gl, TextureType.MsaaFramebuffer)
    {
        Width = width;
        Height = height;
        Bind(TextureUnit.Texture0);

        _gl.TexImage2DMultisample(TextureTarget.Texture2DMultisample, Constants.SAMPLES_COUNT, InternalFormat.Rgb, Width, Height, Silk.NET.OpenGL.Boolean.True);

        _gl.TexParameter(TextureTarget.Texture2DMultisample, TextureParameterName.TextureMinFilter, (int) GLEnum.Nearest);
        _gl.TexParameter(TextureTarget.Texture2DMultisample, TextureParameterName.TextureMagFilter, (int) GLEnum.Nearest);
        _gl.TexParameter(TextureTarget.Texture2DMultisample, TextureParameterName.TextureWrapS, (int) GLEnum.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2DMultisample, TextureParameterName.TextureWrapT, (int) GLEnum.ClampToEdge);

        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2DMultisample, _handle, 0);
    }

    public unsafe Texture(GL gl, int width, int height) : this(gl, TextureType.Framebuffer)
    {
        Width = (uint) width;
        Height = (uint) height;
        Bind(TextureUnit.Texture0);

        _gl.TexImage2D(TextureTarget.Texture2D, 0, (int) InternalFormat.Rgb, Width, Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, null);

        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) GLEnum.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) GLEnum.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) GLEnum.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) GLEnum.ClampToEdge);

        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _handle, 0);
    }

    public unsafe Texture(GL gl, byte[] data, uint width, uint height, SKColorType colorType, UTexture2D texture2D) : this(gl, TextureType.Normal)
    {
        Type = texture2D.ExportType;
        Name = texture2D.Name;
        Path = texture2D.Owner?.Name;
        Format = texture2D.Format;
        ImportedWidth = texture2D.ImportedSize.X;
        ImportedHeight = texture2D.ImportedSize.Y;
        Width = width;
        Height = height;
        Bind(TextureUnit.Texture0);

        fixed (void* d = &data[0])
        {
            _gl.TexImage2D(TextureTarget.Texture2D, 0, (int) InternalFormat.Rgb, Width, Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, d);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) GLEnum.LinearMipmapLinear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) GLEnum.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 8);

            _gl.GenerateMipmap(TextureTarget.Texture2D);
        }
    }

    public unsafe Texture(GL gl, string[] textures) : this(gl, TextureType.Cubemap)
    {
        Bind(TextureUnit.Texture0);

        for (int t = 0; t < textures.Length; t++)
        {
            var info = Application.GetResourceStream(new Uri($"/FModel;component/Resources/{textures[t]}.png", UriKind.Relative));
            using var img = Image.Load<Rgba32>(info.Stream);
            Width = (uint) img.Width; // we don't care anyway
            Height = (uint) img.Height; // we don't care anyway
            _gl.TexImage2D(TextureTarget.TextureCubeMapPositiveX + t, 0, InternalFormat.Rgba8, Width, Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);

            img.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    fixed (void* data = accessor.GetRowSpan(y))
                    {
                        gl.TexSubImage2D(TextureTarget.TextureCubeMapPositiveX + t, 0, 0, y, (uint) accessor.Width, 1, PixelFormat.Rgba, PixelType.UnsignedByte, data);
                    }
                }
            });
        }

        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int) GLEnum.Linear);
        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int) GLEnum.Linear);
        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int) GLEnum.ClampToEdge);
        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int) GLEnum.ClampToEdge);
        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int) GLEnum.ClampToEdge);
    }

    public unsafe Texture(GL gl, uint width, uint height, IntPtr data) : this(gl, TextureType.Normal)
    {
        Width = width;
        Height = height;
        Bind(TextureTarget.Texture2D);

        _gl.TexStorage2D(GLEnum.Texture2D, 1, SizedInternalFormat.Rgba8, Width, Height);
        _gl.TexSubImage2D(GLEnum.Texture2D, 0, 0, 0, Width, Height, PixelFormat.Bgra, PixelType.UnsignedByte, (void*) data);

        _gl.TexParameterI(GLEnum.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        _gl.TexParameterI(GLEnum.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        _gl.TexParameterI(GLEnum.Texture2D, TextureParameterName.TextureMaxLevel, 1 - 1);
    }

    public void Bind(TextureUnit textureSlot)
    {
        _gl.ActiveTexture(textureSlot);
        Bind(_type switch
        {
            TextureType.Cubemap => TextureTarget.TextureCubeMap,
            TextureType.MsaaFramebuffer => TextureTarget.Texture2DMultisample,
            _ => TextureTarget.Texture2D
        });
    }

    public void Bind(TextureTarget target)
    {
        _gl.BindTexture(target, _handle);
    }

    public void SetMinFilter(TextureMinFilter filter)
    {
        _gl.TexParameterI(GLEnum.Texture2D, TextureParameterName.TextureMinFilter, (int)filter);
    }

    public void SetMagFilter(TextureMagFilter filter)
    {
        _gl.TexParameterI(GLEnum.Texture2D, TextureParameterName.TextureMagFilter, (int)filter);
    }

    public IntPtr GetPointer() => (IntPtr) _handle;

    public void Dispose()
    {
        _gl.DeleteTexture(_handle);
    }
}

public enum TextureType
{
    Normal,
    Cubemap,
    Framebuffer,
    MsaaFramebuffer
}
