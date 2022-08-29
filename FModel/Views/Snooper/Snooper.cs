using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse_Conversion.Meshes;
using FModel.Extensions;
using ImGuiNET;
using Silk.NET.Core;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace FModel.Views.Snooper;

public class Snooper
{
    private IWindow _window;
    private ImGuiController _controller;
    private GL _gl;
    private Camera _camera;
    private IKeyboard _keyboard;
    private IMouse _mouse;
    private Vector2 _previousMousePosition;
    private RawImage _icon;

    private Skybox _skybox;
    private Grid _grid;
    private Model[] _models;

    public int Width { get; }
    public int Height { get; }

    public Snooper(UObject export)
    {
        const double ratio = .7;
        var x = SystemParameters.MaximizedPrimaryScreenWidth;
        var y = SystemParameters.MaximizedPrimaryScreenHeight;
        Width = Convert.ToInt32(x * ratio);
        Height = Convert.ToInt32(y * ratio);

        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>(Width, Height);
        options.WindowBorder = WindowBorder.Hidden;
        options.Title = "Snooper";
        options.Samples = 4;
        _window = Silk.NET.Windowing.Window.Create(options);

        unsafe
        {
            var info = Application.GetResourceStream(new Uri("/FModel;component/Resources/materialicon.png", UriKind.Relative));
            using var image = Image.Load<Rgba32>(info.Stream);
            var memoryGroup = image.GetPixelMemoryGroup();
            Memory<byte> array = new byte[memoryGroup.TotalLength * sizeof(Rgba32)];
            var block = MemoryMarshal.Cast<byte, Rgba32>(array.Span);
            foreach (var memory in memoryGroup)
            {
                memory.Span.CopyTo(block);
                block = block.Slice(memory.Length);
            }
            _icon = new RawImage(image.Width, image.Height, array);
        }

        _window.Load += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.Closing += OnClose;
        _window.FramebufferResize += OnFramebufferResize;

        _skybox = new Skybox();
        _grid = new Grid();
        _models = new Model[1];
        switch (export)
        {
            case UStaticMesh st when st.TryConvert(out var mesh):
            {
                _models[0] = new Model(st.Name, mesh.LODs[0], mesh.LODs[0].Verts);
                SetupCamera(mesh.BoundingBox *= Constants.SCALE_DOWN_RATIO);
                break;
            }
            case USkeletalMesh sk when sk.TryConvert(out var mesh):
            {
                _models[0] = new Model(sk.Name, mesh.LODs[0], mesh.LODs[0].Verts);
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
        var far = box.Max.Max();
        var center = box.GetCenter();
        var position = new Vector3(0f, center.Z, box.Max.Y * 3);
        _camera = new Camera(position, center, 0.01f, far * 50f, far / 2f);
    }

    private void OnLoad()
    {
        _window.SetWindowIcon(ref _icon);
        _window.Center();

        var input = _window.CreateInput();
        _keyboard = input.Keyboards[0];
        _keyboard.KeyDown += KeyDown;
        _mouse = input.Mice[0];
        _mouse.MouseDown += OnMouseDown;
        _mouse.MouseUp += OnMouseUp;
        _mouse.MouseMove += OnMouseMove;
        _mouse.Scroll += OnMouseWheel;

        _gl = GL.GetApi(_window);
        _gl.Enable(EnableCap.Blend);
        _gl.Enable(EnableCap.DepthTest);
        _gl.Enable(EnableCap.Multisample);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        _controller = new ImGuiController(_gl, _window, input);

        _skybox.Setup(_gl);
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

        _gl.ClearColor(0.102f, 0.102f, 0.129f, 1.0f);
        _gl.Clear((uint) ClearBufferMask.ColorBufferBit | (uint) ClearBufferMask.DepthBufferBit);

        _skybox.Bind(_camera);
        _grid.Bind(_camera);

        ImGuiExtensions.Theme();

        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("Edit"))
            {
                if (ImGui.MenuItem("Undo", "CTRL+Z")) {}
                if (ImGui.MenuItem("Redo", "CTRL+Y", false, false)) {}  // Disabled item
                ImGui.Separator();
                if (ImGui.MenuItem("Cut", "CTRL+X")) {}
                if (ImGui.MenuItem("Copy", "CTRL+C")) {}
                if (ImGui.MenuItem("Paste", "CTRL+V")) {}
                ImGui.EndMenu();
            }

            const string text = "Press ESC to Exit...";
            ImGui.SetCursorPosX(ImGui.GetWindowViewport().WorkSize.X - ImGui.CalcTextSize(text).X - 5);
            ImGui.TextColored(ImGuiExtensions.STYLE.Colors[(int) ImGuiCol.TextDisabled], text);

            ImGui.EndMainMenuBar();
        }

        ImGui.Begin("ImGui.NET", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoSavedSettings);
        foreach (var model in _models)
        {
            model.Bind(_camera);
        }
        ImGui.End();

        ImGuiExtensions.DrawFPS();

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

    private void OnMouseDown(IMouse mouse, MouseButton button)
    {
        if (button != MouseButton.Left) return;
        mouse.Cursor.CursorMode = CursorMode.Raw;
    }

    private void OnMouseUp(IMouse mouse, MouseButton button)
    {
        if (button != MouseButton.Left) return;
        mouse.Cursor.CursorMode = CursorMode.Normal;
    }

    private void OnMouseMove(IMouse mouse, Vector2 position)
    {
        if (_previousMousePosition == default) { _previousMousePosition = position; }
        else
        {
            if (mouse.Cursor.CursorMode == CursorMode.Raw)
            {
                const float lookSensitivity = 0.1f;
                var xOffset = (position.X - _previousMousePosition.X) * lookSensitivity;
                var yOffset = (position.Y - _previousMousePosition.Y) * lookSensitivity;

                _camera.ModifyDirection(xOffset, yOffset);
            }

            _previousMousePosition = position;
        }
    }

    private void OnMouseWheel(IMouse mouse, ScrollWheel scrollWheel)
    {
        _camera.ModifyZoom(scrollWheel.Y);
    }

    private void OnClose()
    {
        _grid.Dispose();
        _skybox.Dispose();
        foreach (var model in _models)
        {
            model.Dispose();
        }
        _controller.Dispose();
        _window.Dispose();
        _gl.Dispose();
    }

    private void KeyDown(IKeyboard keyboard, Key key, int arg3)
    {
        switch (key)
        {
            case Key.Escape:
                _window.Close();
                break;
        }
    }
}
