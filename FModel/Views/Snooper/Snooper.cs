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
using Silk.NET.Windowing;

namespace FModel.Views.Snooper;

public class Snooper
{
    private IWindow _window;
    private GL _gl;
    private Camera _camera;
    private IKeyboard _keyboard;
    private Vector2 _previousMousePosition;

    private Mesh[] _meshes;

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
        options.Position = new Vector2D<int>(Width, Height);
        options.Title = "Snooper";
        _window = Window.Create(options);

        _window.Load += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.Closing += OnClose;

        _meshes = new Mesh[1];
        switch (export)
        {
            case UStaticMesh st when st.TryConvert(out var mesh):
            {
                _meshes[0] = new Mesh(mesh.LODs[0], mesh.LODs[0].Verts);
                SetupCamera(mesh.BoundingBox *= Constants.SCALE_DOWN_RATIO);
                break;
            }
            case USkeletalMesh sk when sk.TryConvert(out var mesh):
            {
                _meshes[0] = new Mesh(mesh.LODs[0], mesh.LODs[0].Verts);
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
        // X Yaw Gauche Droite
        // Y Pitch Haut Bas
        // Z Avant Arrière

        var center = box.GetCenter();
        var position = new Vector3(0f, center.Z, box.Max.Y * 3);
        _camera = new Camera(position, center);
    }

    private void OnLoad()
    {
        var input = _window.CreateInput();
        _keyboard = input.Keyboards[0];
        _keyboard.KeyDown += KeyDown;
        foreach (var mouse in input.Mice)
        {
            mouse.Cursor.CursorMode = CursorMode.Raw;
            mouse.MouseMove += OnMouseMove;
            mouse.Scroll += OnMouseWheel;
        }

        _gl = GL.GetApi(_window);
        _gl.Enable(EnableCap.DepthTest);

        foreach (var mesh in _meshes)
        {
            mesh.Setup(_gl);
        }
    }

    private void OnRender(double deltaTime)
    {
        _gl.ClearColor(0.149f, 0.149f, 0.188f, 1.0f);
        _gl.Clear((uint) ClearBufferMask.ColorBufferBit | (uint) ClearBufferMask.DepthBufferBit);

        foreach (var mesh in _meshes)
        {
            mesh.Bind(_camera);
        }
    }

    private void OnUpdate(double deltaTime)
    {
        var speed = _keyboard.IsKeyPressed(Key.ShiftLeft) ? 2.5f : 1f;
        var moveSpeed = speed * (float) deltaTime;
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
        foreach (var mesh in _meshes)
        {
            mesh.Dispose();
        }
    }

    private void KeyDown(IKeyboard keyboard, Key key, int arg3)
    {
        if (key == Key.Escape)
        {
            _window.Close();
        }
    }
}
