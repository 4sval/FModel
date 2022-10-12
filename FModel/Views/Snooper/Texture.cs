using System;
using System.Windows;
using CUE4Parse.UE4.Assets.Exports.Texture;
using OpenTK.Graphics.OpenGL4;
using SkiaSharp;

namespace FModel.Views.Snooper;

public class Texture : IDisposable
{
    private readonly int _handle;
    private readonly TextureType _type;

    public readonly string Type;
    public readonly string Name;
    public readonly string Path;
    public readonly EPixelFormat Format;
    public readonly uint ImportedWidth;
    public readonly uint ImportedHeight;
    public readonly int Width;
    public readonly int Height;
    public string Label;

    public Texture(TextureType type)
    {
        _handle = GL.GenTexture();
        _type = type;
        Label = "(?) Click to Copy Path";
    }

    public Texture(uint width, uint height) : this(TextureType.MsaaFramebuffer)
    {
        Width = width;
        Height = height;
        Bind(TextureUnit.Texture0);

        GL.TexImage2DMultisample(TextureTargetMultisample.Texture2DMultisample, Constants.SAMPLES_COUNT, PixelInternalFormat.Rgb, Width, Height, true);

        GL.TexParameter(TextureTarget.Texture2DMultisample, TextureParameterName.TextureMinFilter, (int) GLEnum.Nearest);
        GL.TexParameter(TextureTarget.Texture2DMultisample, TextureParameterName.TextureMagFilter, (int) GLEnum.Nearest);
        GL.TexParameter(TextureTarget.Texture2DMultisample, TextureParameterName.TextureWrapS, (int) GLEnum.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2DMultisample, TextureParameterName.TextureWrapT, (int) GLEnum.ClampToEdge);

        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2DMultisample, _handle, 0);
    }

    public unsafe Texture(int width, int height) : this(TextureType.Framebuffer)
    {
        Width = width;
        Height = height;
        Bind(TextureUnit.Texture0);

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, Width, Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, null);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) GLEnum.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) GLEnum.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) GLEnum.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) GLEnum.ClampToEdge);

        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _handle, 0);
    }

    public unsafe Texture(byte[] data, uint width, uint height, SKColorType colorType, UTexture2D texture2D) : this(TextureType.Normal)
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
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, Width, Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, d);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) GLEnum.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) GLEnum.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 8);

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }
    }

    public unsafe Texture(string[] textures) : this(TextureType.Cubemap)
    {
        Bind(TextureUnit.Texture0);

        for (int t = 0; t < textures.Length; t++)
        {
            var info = Application.GetResourceStream(new Uri($"/FModel;component/Resources/{textures[t]}.png", UriKind.Relative));
            using var img = Image.Load<Rgba32>(info.Stream);
            Width = (uint) img.Width; // we don't care anyway
            Height = (uint) img.Height; // we don't care anyway
            GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + t, 0, PixelInternalFormat.Rgba8, Width, Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);

            img.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    fixed (void* data = accessor.GetRowSpan(y))
                    {
                        GL.TexSubImage2D(TextureTarget.TextureCubeMapPositiveX + t, 0, 0, y, (uint) accessor.Width, 1, PixelFormat.Rgba, PixelType.UnsignedByte, data);
                    }
                }
            });
        }

        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int) GLEnum.Linear);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int) GLEnum.Linear);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int) GLEnum.ClampToEdge);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int) GLEnum.ClampToEdge);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int) GLEnum.ClampToEdge);
    }

    public unsafe Texture(uint width, uint height, IntPtr data) : this(TextureType.Normal)
    {
        Width = width;
        Height = height;
        Bind(TextureTarget.Texture2D);

        GL.TexStorage2D(TextureTarget2d.Texture2D, 1, SizedInternalFormat.Rgba8, Width, Height);
        GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, Width, Height, PixelFormat.Bgra, PixelType.UnsignedByte, (void*) data);

        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 1 - 1);
    }

    public void Bind(TextureUnit textureSlot)
    {
        GL.ActiveTexture(textureSlot);
        Bind(_type switch
        {
            TextureType.Cubemap => TextureTarget.TextureCubeMap,
            TextureType.MsaaFramebuffer => TextureTarget.Texture2DMultisample,
            _ => TextureTarget.Texture2D
        });
    }

    public void Bind(TextureTarget target)
    {
        GL.BindTexture(target, _handle);
    }

    public void SetMinFilter(int filter)
    {
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, ref filter);
    }

    public void SetMagFilter(int filter)
    {
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, ref filter);
    }

    public IntPtr GetPointer() => (IntPtr) _handle;

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
