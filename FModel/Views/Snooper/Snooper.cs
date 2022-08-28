using System;
using System.Numerics;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse_Conversion.Meshes;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace FModel.Views.Snooper;

public class Snooper
{
    private IWindow _window;
    private IInputContext _input;
    private ImGuiController _controller;
    private GL _gl;
    private Camera _camera;
    private IKeyboard _keyboard;
    private Vector2 _previousMousePosition;

    private Model[] _models;
    private Grid _grid;

    public int Width { get; }
    public int Height { get; }

    public Snooper(UObject export)
    {
        const double ratio = .6;
        var x = System.Windows.SystemParameters.MaximizedPrimaryScreenWidth;
        var y = System.Windows.SystemParameters.MaximizedPrimaryScreenHeight;
        Width = Convert.ToInt32(x * ratio);
        Height = Convert.ToInt32(y * ratio);

        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>(Width, Height);
        options.Title = "Snooper";
        _window = Window.Create(options);

        _window.Load += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.Closing += OnClose;
        _window.FramebufferResize += OnFramebufferResize;

        _grid = new Grid();
        _models = new Model[1];
        switch (export)
        {
            case UStaticMesh st when st.TryConvert(out var mesh):
            {
                _models[0] = new Model(mesh.LODs[0], mesh.LODs[0].Verts);
                SetupCamera(mesh.BoundingBox *= Constants.SCALE_DOWN_RATIO);
                break;
            }
            case USkeletalMesh sk when sk.TryConvert(out var mesh):
            {
                _models[0] = new Model(mesh.LODs[0], mesh.LODs[0].Verts);
                SetupCamera(mesh.BoundingBox *= Constants.SCALE_DOWN_RATIO);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(export));
        }
    }

    public void Run()
    {
        _window.Run();
    }

    private void SetupCamera(FBox box)
    {
        var center = box.GetCenter();
        var position = new Vector3(0f, center.Z, box.Max.Y * 3);
        _camera = new Camera(position, center, box.Max.Max() / 2f);
    }

    private void OnLoad()
    {
        _input = _window.CreateInput();
        _keyboard = _input.Keyboards[0];
        _keyboard.KeyDown += KeyDown;
        foreach (var mouse in _input.Mice)
        {
            mouse.Cursor.CursorMode = CursorMode.Raw;
            mouse.MouseMove += OnMouseMove;
            mouse.Scroll += OnMouseWheel;
        }

        _gl = GL.GetApi(_window);
        _gl.Enable(EnableCap.Blend);
        _gl.Enable(EnableCap.DepthTest);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        _controller = new ImGuiController(_gl, _window, _input);

        _grid.Setup(_gl);

        foreach (var model in _models)
        {
            model.Setup(_gl);
        }
    }

    private void OnFramebufferResize(Vector2D<int> size)
    {
        _gl.Viewport(size);
    }

    private void OnRender(double deltaTime)
    {
        _controller.Update((float) deltaTime);

        _gl.ClearColor(0.149f, 0.149f, 0.188f, 1.0f);
        _gl.Clear((uint) ClearBufferMask.ColorBufferBit | (uint) ClearBufferMask.DepthBufferBit);

        _grid.Bind(_camera);

        foreach (var model in _models)
        {
            model.Bind(_camera);
        }

        ImGuiNET.ImGui.ShowAboutWindow();

        _controller.Render();
    }

    private void OnUpdate(double deltaTime)
    {
        var multiplier = _keyboard.IsKeyPressed(Key.ShiftLeft) ? 2f : 1f;
        var moveSpeed = _camera.Speed * multiplier * (float) deltaTime;
        if (_keyboard.IsKeyPressed(Key.W))
        {
            _camera.Position += moveSpeed * _camera.Direction;
        }
        if (_keyboard.IsKeyPressed(Key.S))
        {
            _camera.Position -= moveSpeed * _camera.Direction;
        }
        if (_keyboard.IsKeyPressed(Key.A))
        {
            _camera.Position -= Vector3.Normalize(Vector3.Cross(_camera.Direction, _camera.Up)) * moveSpeed;
        }
        if (_keyboard.IsKeyPressed(Key.D))
        {
            _camera.Position += Vector3.Normalize(Vector3.Cross(_camera.Direction, _camera.Up)) * moveSpeed;
        }
        if (_keyboard.IsKeyPressed(Key.E))
        {
            _camera.Position += moveSpeed * _camera.Up;
        }
        if (_keyboard.IsKeyPressed(Key.Q))
        {
            _camera.Position -= moveSpeed * _camera.Up;
        }
    }

    private void OnMouseMove(IMouse mouse, Vector2 position)
    {
        const float lookSensitivity = 0.1f;
        if (_previousMousePosition == default) { _previousMousePosition = position; }
        else
        {
            var xOffset = (position.X - _previousMousePosition.X) * lookSensitivity;
            var yOffset = (position.Y - _previousMousePosition.Y) * lookSensitivity;
            _previousMousePosition = position;

            _camera.ModifyDirection(xOffset, yOffset);
        }
    }

    private void OnMouseWheel(IMouse mouse, ScrollWheel scrollWheel)
    {
        _camera.ModifyZoom(scrollWheel.Y);
    }

    private void OnClose()
    {
        _grid.Dispose();
        foreach (var model in _models)
        {
            model.Dispose();
        }
        _input.Dispose();
        _controller.Dispose();
        _gl.Dispose();
    }

    private void KeyDown(IKeyboard keyboard, Key key, int arg3)
    {
        if (key == Key.Escape)
        {
            _window.Close();
        }
    }
}
