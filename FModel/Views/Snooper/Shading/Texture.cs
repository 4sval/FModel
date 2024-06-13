using System;
using System.Numerics;
using System.Windows;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;

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

    private const int DisabledChannel = (int)BlendingFactor.Zero;
    private readonly bool[] _values = [true, true, true, true];
    private readonly string[] _labels = ["R", "G", "B", "A"];
    public int[] SwizzleMask =
    [
        (int) PixelFormat.Red,
        (int) PixelFormat.Green,
        (int) PixelFormat.Blue,
        (int) PixelFormat.Alpha
    ];

    private Texture(TextureType type)
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
        GL.TexParameter(_target, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Linear);
        GL.TexParameter(_target, TextureParameterName.TextureWrapS, (int) TextureWrapMode.ClampToEdge);
        GL.TexParameter(_target, TextureParameterName.TextureWrapT, (int) TextureWrapMode.ClampToEdge);

        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, _target, _handle, 0);
    }

    public Texture(SKBitmap bitmap, UTexture2D texture2D) : this(TextureType.Normal)
    {
        Type = texture2D.ExportType;
        Guid = texture2D.LightingGuid;
        Name = texture2D.Name;
        Path = texture2D.GetPathName();
        Format = texture2D.Format;
        ImportedWidth = texture2D.ImportedSize.X;
        ImportedHeight = texture2D.ImportedSize.Y;
        Width = bitmap.Width;
        Height = bitmap.Height;
        Bind(TextureUnit.Texture0);

        GL.TexImage2D(_target, 0, texture2D.SRGB ? PixelInternalFormat.Srgb : PixelInternalFormat.Rgb, Width, Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, bitmap.Bytes);
        GL.TexParameter(_target, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.LinearMipmapLinear);
        GL.TexParameter(_target, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Linear);
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
        GL.TexParameter(_target, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Linear);
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
        GL.TexParameter(_target, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Linear);
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
        GL.TexParameter(_target, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Linear);
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

    public void Swizzle()
    {
        Bind();
        GL.TexParameter(_target, TextureParameterName.TextureSwizzleRgba, SwizzleMask);
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

    private Vector3 _scrolling = new (0.0f, 0.0f, 1.0f);
    public void ImGuiTextureInspector()
    {
        if (ImGui.BeginTable("texture_inspector", 2, ImGuiTableFlags.SizingStretchProp))
        {
            SnimGui.NoFramePaddingOnY(() =>
            {
                SnimGui.Layout("Type");ImGui.Text($" :  ({Format}) {Name}");
                SnimGui.TooltipCopy("(?) Click to Copy Path", Path);
                SnimGui.Layout("Guid");ImGui.Text($" :  {Guid.ToString(EGuidFormats.UniqueObjectGuid)}");
                SnimGui.Layout("Import");ImGui.Text($" :  {ImportedWidth}x{ImportedHeight}");
                SnimGui.Layout("Export");ImGui.Text($" :  {Width}x{Height}");

                SnimGui.Layout("Swizzle");
                for (int c = 0; c < SwizzleMask.Length; c++)
                {
                    if (ImGui.Checkbox(_labels[c], ref _values[c]))
                    {
                        Bind();
                        GL.TexParameter(_target, TextureParameterName.TextureSwizzleR + c, _values[c] ? SwizzleMask[c] : DisabledChannel);
                    }
                    ImGui.SameLine();
                }

                ImGui.EndTable();
            });
        }

        var io = ImGui.GetIO();
        var canvasP0 = ImGui.GetCursorScreenPos();
        var canvasSize = ImGui.GetContentRegionAvail();
        if (canvasSize.X < 50.0f) canvasSize.X = 50.0f;
        if (canvasSize.Y < 50.0f) canvasSize.Y = 50.0f;
        var canvasP1 = canvasP0 + canvasSize;
        var origin = new Vector2(canvasP0.X + _scrolling.X, canvasP0.Y + _scrolling.Y);
        var absoluteMiddle = canvasSize / 2.0f;

        ImGui.InvisibleButton("texture_inspector_canvas", canvasSize, ImGuiButtonFlags.MouseButtonLeft);
        if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            _scrolling.X += io.MouseDelta.X;
            _scrolling.Y += io.MouseDelta.Y;
        }
        else if (ImGui.IsItemHovered() && io.MouseWheel != 0.0f)
        {
            var zoomFactor = 1.0f + io.MouseWheel * 0.1f;
            var mousePosCanvas = io.MousePos - origin;

            _scrolling.X -= (mousePosCanvas.X - absoluteMiddle.X) * (zoomFactor - 1);
            _scrolling.Y -= (mousePosCanvas.Y - absoluteMiddle.Y) * (zoomFactor - 1);
            _scrolling.Z *= zoomFactor;
            origin = new Vector2(canvasP0.X + _scrolling.X, canvasP0.Y + _scrolling.Y);
        }

        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(canvasP0, canvasP1, 0xFF242424);
        drawList.PushClipRect(canvasP0, canvasP1, true);
        {
            var sensitivity = _scrolling.Z * 25.0f;
            for (float x = _scrolling.X % sensitivity; x < canvasSize.X; x += sensitivity)
                drawList.AddLine(canvasP0 with { X = canvasP0.X + x }, canvasP1 with { X = canvasP0.X + x }, 0x28C8C8C8);
            for (float y = _scrolling.Y % sensitivity; y < canvasSize.Y; y += sensitivity)
                drawList.AddLine(canvasP0 with { Y = canvasP0.Y + y }, canvasP1 with { Y = canvasP0.Y + y }, 0x28C8C8C8);
        }
        drawList.PopClipRect();

        drawList.PushClipRect(canvasP0, canvasP1, true);
        {
            var relativeMiddle = origin + absoluteMiddle;
            var ratio = Math.Min(canvasSize.X / Width, canvasSize.Y / Height) * 0.95f * _scrolling.Z;
            var size = new Vector2(Width, Height) * ratio / 2f;

            drawList.AddImage(GetPointer(), relativeMiddle - size, relativeMiddle + size);
            drawList.AddRect(relativeMiddle - size, relativeMiddle + size, 0xFFFFFFFF);
        }
        drawList.PopClipRect();
    }
}

public enum TextureType
{
    Normal,
    Cubemap,
    Framebuffer,
    MsaaFramebuffer
}
