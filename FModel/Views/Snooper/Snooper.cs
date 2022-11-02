using System.Threading;
using System.Windows;
using CUE4Parse.UE4.Assets.Exports;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace FModel.Views.Snooper;

public class Snooper : GameWindow
{
    public Camera Camera;
    public readonly FramebufferObject Framebuffer;
    public readonly Renderer Renderer;

    private readonly Skybox _skybox;
    private readonly Grid _grid;
    private readonly SnimGui _gui;

    private float _previousSpeed;

    private bool _init;

    public Snooper(GameWindowSettings gwSettings, NativeWindowSettings nwSettings) : base(gwSettings, nwSettings)
    {
        Framebuffer = new FramebufferObject(ClientSize);
        Renderer = new Renderer(ClientSize.X, ClientSize.Y);

        _skybox = new Skybox();
        _grid = new Grid();
        _gui = new SnimGui(ClientSize.X, ClientSize.Y);

        _init = false;
    }

    public void LoadExport(CancellationToken cancellationToken, UObject export)
    {
        var newCamera = Renderer.Load(cancellationToken, export);
        if (newCamera == null || !(newCamera.Speed > _previousSpeed)) return;

        Camera = newCamera;
        _previousSpeed = Camera.Speed;
    }

    public unsafe void WindowShouldClose(bool value, bool clear)
    {
        if (clear)
        {
            Renderer.Cache.DisposeModels();
            Renderer.Cache.Models.Clear();
            Renderer.Settings.Reset();
            _previousSpeed = 0f;
        }

        GLFW.SetWindowShouldClose(WindowPtr, value); // start / stop game loop
        IsVisible = !value;
    }

    public override void Run()
    {
        Application.Current.Dispatcher.Invoke(delegate
        {
            WindowShouldClose(false, false);
            base.Run();
        });
    }

    protected override void OnLoad()
    {
        if (_init)
        {
            Renderer.Cache.Setup();
            return;
        }

        base.OnLoad();
        CenterWindow();

        GL.ClearColor(Color4.Red);
        GL.Enable(EnableCap.Blend);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Multisample);
        GL.StencilOp(StencilOp.Keep, StencilOp.Replace, StencilOp.Replace);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        Framebuffer.Setup();
        Renderer.Setup();

        _skybox.Setup();
        _grid.Setup();
        _init = true;
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        if (!IsVisible)
            return;

        ClearWhatHasBeenDrawn(); // clear window background
        _gui.Controller.Update(this, (float)args.Time);
        _gui.Render(this);

        Framebuffer.Bind(); // switch to viewport background
        ClearWhatHasBeenDrawn(); // clear viewport background

        _skybox.Render(Camera);
        _grid.Render(Camera);
        Renderer.Render(Camera);

        Framebuffer.BindMsaa();
        Framebuffer.Bind(0); // switch to window background

        SwapBuffers();
    }

    private void ClearWhatHasBeenDrawn()
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        if (!IsVisible)
            return;

        _gui.Controller.MouseScroll(e.Offset);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        if (!IsVisible)
            return;

        _gui.Controller.PressChar((char) e.Unicode);
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);
        if (!IsVisible || ImGui.GetIO().WantTextInput)
            return;

        var multiplier = KeyboardState.IsKeyDown(Keys.LeftShift) ? 2f : 1f;
        var moveSpeed = Camera.Speed * multiplier * (float) e.Time;
        if (KeyboardState.IsKeyDown(Keys.W))
            Camera.Position += moveSpeed * Camera.Direction;
        if (KeyboardState.IsKeyDown(Keys.S))
            Camera.Position -= moveSpeed * Camera.Direction;
        if (KeyboardState.IsKeyDown(Keys.A))
            Camera.Position -= Vector3.Normalize(Vector3.Cross(Camera.Direction, Camera.Up)) * moveSpeed;
        if (KeyboardState.IsKeyDown(Keys.D))
            Camera.Position += Vector3.Normalize(Vector3.Cross(Camera.Direction, Camera.Up)) * moveSpeed;
        if (KeyboardState.IsKeyDown(Keys.E))
            Camera.Position += moveSpeed * Camera.Up;
        if (KeyboardState.IsKeyDown(Keys.Q))
            Camera.Position -= moveSpeed * Camera.Up;
        if (KeyboardState.IsKeyDown(Keys.X))
            Camera.ModifyZoom(-.5f);
        if (KeyboardState.IsKeyDown(Keys.C))
            Camera.ModifyZoom(+.5f);

        if (KeyboardState.IsKeyPressed(Keys.R))
            WindowShouldClose(true, false);
        if (KeyboardState.IsKeyPressed(Keys.Escape))
            WindowShouldClose(true, true);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        GL.Viewport(0, 0, e.Width, e.Height);

        Camera.AspectRatio = e.Width / (float) e.Height;
        Framebuffer.WindowResized(e.Width, e.Height);
        Renderer.Picking.WindowResized(e.Width, e.Height);

        _gui.Controller.WindowResized(e.Width, e.Height);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        Framebuffer?.Dispose();
        Renderer?.Dispose();

        _skybox?.Dispose();
        _grid?.Dispose();
        _gui?.Controller.Dispose();
    }
}
