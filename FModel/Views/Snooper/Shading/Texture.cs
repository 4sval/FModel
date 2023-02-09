using System;
using System.Windows;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using OpenTK.Graphics.OpenGL4;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace FModel.Views.Snooper.Shading;

public class Texture : IDisposable
{
    private readonly int _handle;
    private readonly TextureType _type;
    private readonly TextureTarget _target;

    public readonly string Type;
    public readonly FGuid Guid;
    public readonly string Name;
    public readonly string Path;
    public readonly EPixelFormat Format;
    public readonly uint ImportedWidth;
    public readonly uint ImportedHeight;
    public int Width;
    public int Height;

    public Texture(TextureType type)
    {
        _handle = GL.GenTexture();
        _type = type;
        _target = _type switch
        {
            TextureType.Cubemap => TextureTarget.TextureCubeMap,
            TextureType.MsaaFramebuffer => TextureTarget.Texture2DMultisample,
            _ => TextureTarget.Texture2D
        };

        Guid = new FGuid();
    }

    public Texture(uint width, uint height) : this(TextureType.MsaaFramebuffer)
    {
        Width = (int) width;
        Height = (int) height;
        Bind(TextureUnit.Texture0);

        GL.TexImage2DMultisample(TextureTargetMultisample.Texture2DMultisample, Constants.SAMPLES_COUNT, PixelInternalFormat.Rgb, Width, Height, true);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, _target, _handle, 0);
    }

    public Texture(int width, int height) : this(TextureType.Framebuffer)
    {
        Width = width;
        Height = height;
        Bind(TextureUnit.Texture0);

        GL.TexImage2D(_target, 0, PixelInternalFormat.Rgb, Width, Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);

        GL.TexParameter(_target, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);
        GL.TexParameter(_target, TextureParameterName.TextureMagFilter, (int) TextureMinFilter.Linear);
        GL.TexParameter(_target, TextureParameterName.TextureWrapS, (int) TextureWrapMode.ClampToEdge);
        GL.TexParameter(_target, TextureParameterName.TextureWrapT, (int) TextureWrapMode.ClampToEdge);

        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, _target, _handle, 0);
    }

    public Texture(byte[] data, int width, int height, UTexture2D texture2D) : this(TextureType.Normal)
    {
        Type = texture2D.ExportType;
        Guid = texture2D.LightingGuid;
        Name = texture2D.Name;
        Path = texture2D.GetPathName();
        Format = texture2D.Format;
        ImportedWidth = texture2D.ImportedSize.X;
        ImportedHeight = texture2D.ImportedSize.Y;
        Width = width;
        Height = height;
        Bind(TextureUnit.Texture0);

        GL.TexImage2D(_target, 0, texture2D.SRGB ? PixelInternalFormat.Srgb : PixelInternalFormat.Rgb, Width, Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
        GL.TexParameter(_target, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.LinearMipmapLinear);
        GL.TexParameter(_target, TextureParameterName.TextureMagFilter, (int) TextureMinFilter.Linear);
        GL.TexParameter(_target, TextureParameterName.TextureBaseLevel, 0);
        GL.TexParameter(_target, TextureParameterName.TextureMaxLevel, 8);

        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
    }

    public Texture(FLinearColor color) : this(TextureType.Normal)
    {
        Type = "LinearColor";
        Name = color.Hex;
        Width = 1;
        Height = 1;
        Bind(TextureUnit.Texture0);

        GL.TexImage2D(_target, 0, PixelInternalFormat.Rgba, Width, Height, 0, PixelFormat.Rgba, PixelType.Float, ref color);
        GL.TexParameter(_target, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.LinearMipmapLinear);
        GL.TexParameter(_target, TextureParameterName.TextureMagFilter, (int) TextureMinFilter.Linear);
        GL.TexParameter(_target, TextureParameterName.TextureBaseLevel, 0);
        GL.TexParameter(_target, TextureParameterName.TextureMaxLevel, 8);

        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
    }

    public Texture(string[] textures) : this(TextureType.Cubemap)
    {
        Bind(TextureUnit.Texture0);

        for (int t = 0; t < textures.Length; t++)
        {
            ProcessPixels(textures[t], TextureTarget.TextureCubeMapPositiveX + t);
        }

        GL.TexParameter(_target, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.LinearMipmapLinear);
        GL.TexParameter(_target, TextureParameterName.TextureMagFilter, (int) TextureMinFilter.Linear);
        GL.TexParameter(_target, TextureParameterName.TextureWrapR, (int) TextureWrapMode.ClampToEdge);
        GL.TexParameter(_target, TextureParameterName.TextureWrapS, (int) TextureWrapMode.ClampToEdge);
        GL.TexParameter(_target, TextureParameterName.TextureWrapT, (int) TextureWrapMode.ClampToEdge);

        GL.GenerateMipmap(GenerateMipmapTarget.TextureCubeMap);
    }

    public Texture(string texture) : this(TextureType.Normal)
    {
        Bind(TextureUnit.Texture0);

        ProcessPixels(texture, _target);

        GL.TexParameter(_target, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);
        GL.TexParameter(_target, TextureParameterName.TextureMagFilter, (int) TextureMinFilter.Linear);
        GL.TexParameter(_target, TextureParameterName.TextureWrapR, (int) TextureWrapMode.ClampToEdge);
        GL.TexParameter(_target, TextureParameterName.TextureWrapS, (int) TextureWrapMode.ClampToEdge);
        GL.TexParameter(_target, TextureParameterName.TextureWrapT, (int) TextureWrapMode.ClampToEdge);
    }

    private void ProcessPixels(string texture, TextureTarget target)
    {
        var info = Application.GetResourceStream(new Uri($"/FModel;component/Resources/{texture}.png", UriKind.Relative));
        using var img = Image.Load<Rgba32>(info.Stream);
        Width = img.Width;
        Height = img.Height;
        GL.TexImage2D(target, 0, PixelInternalFormat.Rgba8, Width, Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
        img.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                GL.TexSubImage2D(target, 0, 0, y, accessor.Width, 1, PixelFormat.Rgba, PixelType.UnsignedByte, accessor.GetRowSpan(y).ToArray());
            }
        });
    }

    public void Bind(TextureUnit textureSlot)
    {
        GL.ActiveTexture(textureSlot);
        Bind(_target);
    }

    public void Bind(TextureTarget target)
    {
        GL.BindTexture(target, _handle);
    }

    public void Bind()
    {
        GL.BindTexture(_target, _handle);
    }

    public IntPtr GetPointer() => (IntPtr) _handle;

    public void WindowResized(int width, int height)
    {
        Width = width;
        Height = height;

        Bind();
        switch (_type)
        {
            case TextureType.MsaaFramebuffer:
                GL.TexImage2DMultisample(TextureTargetMultisample.Texture2DMultisample, Constants.SAMPLES_COUNT, PixelInternalFormat.Rgb, Width, Height, true);
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, _target, _handle, 0);
                break;
            case TextureType.Framebuffer:
                GL.TexImage2D(_target, 0, PixelInternalFormat.Rgb, Width, Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, _target, _handle, 0);
                break;
            default:
                throw new NotSupportedException();
        }
    }

    public void Dispose()
    {
        GL.DeleteTexture(_handle);
    }
}

public enum TextureType
{
    Normal,
    Cubemap,
    Framebuffer,
    MsaaFramebuffer
}
