using System.Threading;
using System.Windows;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace FModel.Views.Snooper;

public class Snooper : GameWindow
{
    // private readonly FramebufferObject _framebuffer;
    private readonly Skybox _skybox;
    private readonly Grid _grid;
    private readonly Renderer _renderer;

    private Camera _camera;
    private float _previousSpeed;

    private bool _init;

    public Snooper(GameWindowSettings gwSettings, NativeWindowSettings nwSettings) : base(gwSettings, nwSettings)
    {
        // _framebuffer = new FramebufferObject(Size);
        _skybox = new Skybox();
        _grid = new Grid();
        _renderer = new Renderer();
        _init = false;
    }

    public void SwapMaterial(UMaterialInstance mi) => _renderer.Swap(mi);
    public void LoadExport(CancellationToken cancellationToken, UObject export)
    {
        var newCamera = _renderer.Load(cancellationToken, export);
        if (newCamera == null || !(newCamera.Speed > _previousSpeed)) return;

        _camera = newCamera;
        _previousSpeed = _camera.Speed;
    }

    private unsafe void WindowShouldClose(bool value, bool clear)
    {
        if (clear)
        {
            _renderer.Cache.DisposeModels();
            _renderer.Cache.ClearModels();
            _renderer.Settings.Reset();
            _previousSpeed = 0f;
        }

        GLFW.SetWindowShouldClose(WindowPtr, value); // start / stop game loop
        CursorState = value ? CursorState.Normal : CursorState.Grabbed;
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
            _renderer.Cache.Setup();
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

        // _framebuffer.Setup();
        _skybox.Setup();
        _grid.Setup();
        _renderer.Setup();
        _init = true;
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        if (!IsVisible)
            return;

        ClearWhatHasBeenDrawn(); // in main window

        // _framebuffer.Bind(); // switch to dedicated window
        // ClearWhatHasBeenDrawn(); // in dedicated window

        _skybox.Render(_camera);
        _grid.Render(_camera);
        _renderer.Render(_camera);

        // _framebuffer.BindMsaa();
        // _framebuffer.Bind(0); // switch back to main window
        // _framebuffer.BindStuff();

        SwapBuffers();
    }

    private void ClearWhatHasBeenDrawn()
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        if (!IsVisible)
            return;

        const float lookSensitivity = 0.1f;
        var delta = e.Delta * lookSensitivity;
        _camera.ModifyDirection(delta.X, delta.Y);
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);
        if (!IsVisible)
            return;

        var multiplier = KeyboardState.IsKeyDown(Keys.LeftShift) ? 2f : 1f;
        var moveSpeed = _camera.Speed * multiplier * (float) e.Time;
        if (KeyboardState.IsKeyDown(Keys.W))
            _camera.Position += moveSpeed * _camera.Direction;
        if (KeyboardState.IsKeyDown(Keys.S))
            _camera.Position -= moveSpeed * _camera.Direction;
        if (KeyboardState.IsKeyDown(Keys.A))
            _camera.Position -= Vector3.Normalize(Vector3.Cross(_camera.Direction, _camera.Up)) * moveSpeed;
        if (KeyboardState.IsKeyDown(Keys.D))
            _camera.Position += Vector3.Normalize(Vector3.Cross(_camera.Direction, _camera.Up)) * moveSpeed;
        if (KeyboardState.IsKeyDown(Keys.E))
            _camera.Position += moveSpeed * _camera.Up;
        if (KeyboardState.IsKeyDown(Keys.Q))
            _camera.Position -= moveSpeed * _camera.Up;
        if (KeyboardState.IsKeyDown(Keys.X))
            _camera.ModifyZoom(-.5f);
        if (KeyboardState.IsKeyDown(Keys.C))
            _camera.ModifyZoom(+.5f);

        if (KeyboardState.IsKeyPressed(Keys.R))
            WindowShouldClose(true, false);
        if (KeyboardState.IsKeyPressed(Keys.Escape))
            WindowShouldClose(true, true);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        GL.Viewport(0, 0, Size.X, Size.Y);
        // _camera.AspectRatio = Size.X / (float)Size.Y;
    }
}
