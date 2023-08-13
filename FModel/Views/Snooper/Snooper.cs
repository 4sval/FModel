using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using CUE4Parse.UE4.Assets.Exports;
using FModel.Views.Snooper.Buffers;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using Application = System.Windows.Application;

namespace FModel.Views.Snooper;

public class Snooper : GameWindow
{
    public readonly FramebufferObject Framebuffer;
    public readonly Renderer Renderer;

    private readonly SnimGui _gui;

    private bool _init;

    public Snooper(GameWindowSettings gwSettings, NativeWindowSettings nwSettings) : base(gwSettings, nwSettings)
    {
        Framebuffer = new FramebufferObject(ClientSize);
        Renderer = new Renderer(ClientSize.X, ClientSize.Y);

        _gui = new SnimGui(ClientSize.X, ClientSize.Y);
        _init = false;
    }

    public bool TryLoadExport(CancellationToken cancellationToken, UObject export)
    {
        Renderer.Load(cancellationToken, export);
        return Renderer.Options.Models.Count > 0;
    }

    public unsafe void WindowShouldClose(bool value, bool clear)
    {
        if (clear)
        {
            Renderer.CameraOp.Speed = 0;
            Renderer.Save();
        }

        GLFW.SetWindowShouldClose(WindowPtr, value); // start / stop game loop
        IsVisible = !value;
    }

    public unsafe void WindowShouldFreeze(bool value)
    {
        GLFW.SetWindowShouldClose(WindowPtr, value); // start / stop game loop
        IsVisible = true;
    }

    public override void Run()
    {
        Renderer.Options.SwapMaterial(false);
        Renderer.Options.AnimateMesh(false);
        Application.Current.Dispatcher.Invoke(delegate
        {
            WindowShouldClose(false, false);
            base.Run();
        });
    }

    private unsafe void LoadWindowIcon()
    {
        var info = Application.GetResourceStream(new Uri("/FModel;component/Resources/engine.png", UriKind.Relative));
        using var img = SixLabors.ImageSharp.Image.Load<Rgba32>(info.Stream);
        var memoryGroup = img.GetPixelMemoryGroup();
        Memory<byte> array = new byte[memoryGroup.TotalLength * sizeof(Rgba32)];
        var block = MemoryMarshal.Cast<byte, Rgba32>(array.Span);
        foreach (var memory in memoryGroup)
        {
            memory.Span.CopyTo(block);
            block = block[memory.Length..];
        }

        Icon = new WindowIcon(new OpenTK.Windowing.Common.Input.Image(img.Width, img.Height, array.ToArray()));
    }

    protected override void OnLoad()
    {
        if (_init)
        {
            Renderer.Options.SetupModelsAndLights();
            return;
        }

        base.OnLoad();
        CenterWindow();
        LoadWindowIcon();

        GL.ClearColor(OpenTK.Mathematics.Color4.Black);
        GL.Enable(EnableCap.Blend);
        GL.Enable(EnableCap.CullFace);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Multisample);
        GL.StencilOp(StencilOp.Keep, StencilOp.Replace, StencilOp.Replace);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        Framebuffer.Setup();
        Renderer.Setup();
        _init = true;
    }

    private void ClearWhatHasBeenDrawn()
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        if (!IsVisible)
            return;

        ClearWhatHasBeenDrawn(); // clear window background
        Framebuffer.Bind(); // switch to viewport background
        ClearWhatHasBeenDrawn(); // clear viewport background

        Renderer.Render(); // render everything

        Framebuffer.BindMsaa();
        Framebuffer.Bind(0); // switch to window background

        _gui.Render(this); // render UI
        SwapBuffers();
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);
        if (!IsVisible)
            return;

        var delta = (float) e.Time;

        _gui.Controller.Update(this, delta);
        Renderer.Update(this, delta);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        if (!IsVisible)
            return;

        _gui.Controller.PressChar((char) e.Unicode);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        GL.Viewport(0, 0, e.Width, e.Height);

        Framebuffer.WindowResized(e.Width, e.Height);
        Renderer.WindowResized(e.Width, e.Height);

        _gui.Controller.WindowResized(e.Width, e.Height);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        WindowShouldClose(true, true);
    }

    [DllImport("user32.dll")]
    private static extern bool EnumDisplaySettings(
        string deviceName, int modeNum, ref DEVMODE devMode);

    [StructLayout(LayoutKind.Sequential)]
    private struct DEVMODE
    {
        private const int CCHDEVICENAME = 0x20;
        private const int CCHFORMNAME = 0x20;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public ScreenOrientation dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;

    }

    public static int GetMaxRefreshFrequency()
    {
        var rf = 60;
        var vDevMode = new DEVMODE();
        var i = 0;
        while (EnumDisplaySettings(null, i, ref vDevMode))
        {
            i++;
            rf = Math.Max(rf, vDevMode.dmDisplayFrequency);
        }

        return rf;
    }
}
