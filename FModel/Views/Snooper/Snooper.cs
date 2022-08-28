using System;
using System.Numerics;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse_Conversion.Meshes;
using ImGuiNET;
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
        const double ratio = .7;
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
            // mouse.Cursor.CursorMode = CursorMode.Raw;
            // mouse.MouseMove += OnMouseMove;
            // mouse.Scroll += OnMouseWheel;
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

        var padding = Theme();
        ImGui.Begin("ImGui.NET", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBackground);
        ImGui.SetWindowSize(new Vector2(Width / 4f, Height));
        ImGui.SetWindowPos(new Vector2(0));

        foreach (var model in _models)
        {
            ImGui.Text($"Entity: {model.Name}");
            model.Bind(_camera);
            ImGui.Separator();
        }

        float framerate = ImGui.GetIO().Framerate;
        string f = $"FPS: {framerate:0.#} ({1000.0f / framerate:0.##} ms)";
        ImGui.SetCursorPosY(ImGui.GetWindowHeight() - padding.Y - ImGui.CalcTextSize(f).Y);
        ImGui.Text(f);
        ImGui.End();

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
        switch (key)
        {
            case Key.Escape:
                _window.Close();
                break;
        }
    }

    private Vector2 Theme()
    {
        var style = ImGui.GetStyle();
        style.FrameRounding = 4.0f;
        style.GrabRounding = 4.0f;

        style.Colors[(int) ImGuiCol.Text] = new Vector4(0.95f, 0.96f, 0.98f, 1.00f);
        style.Colors[(int) ImGuiCol.TextDisabled] = new Vector4(0.36f, 0.42f, 0.47f, 1.00f);
        style.Colors[(int) ImGuiCol.WindowBg] = new Vector4(0.11f, 0.15f, 0.17f, 1.00f);
        style.Colors[(int) ImGuiCol.ChildBg] = new Vector4(0.15f, 0.18f, 0.22f, 1.00f);
        style.Colors[(int) ImGuiCol.PopupBg] = new Vector4(0.08f, 0.08f, 0.08f, 0.94f);
        style.Colors[(int) ImGuiCol.Border] = new Vector4(0.08f, 0.10f, 0.12f, 1.00f);
        style.Colors[(int) ImGuiCol.BorderShadow] = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
        style.Colors[(int) ImGuiCol.FrameBg] = new Vector4(0.20f, 0.25f, 0.29f, 1.00f);
        style.Colors[(int) ImGuiCol.FrameBgHovered] = new Vector4(0.12f, 0.20f, 0.28f, 1.00f);
        style.Colors[(int) ImGuiCol.FrameBgActive] = new Vector4(0.09f, 0.12f, 0.14f, 1.00f);
        style.Colors[(int) ImGuiCol.TitleBg] = new Vector4(0.09f, 0.12f, 0.14f, 0.65f);
        style.Colors[(int) ImGuiCol.TitleBgActive] = new Vector4(0.08f, 0.10f, 0.12f, 1.00f);
        style.Colors[(int) ImGuiCol.TitleBgCollapsed] = new Vector4(0.00f, 0.00f, 0.00f, 0.51f);
        style.Colors[(int) ImGuiCol.MenuBarBg] = new Vector4(0.15f, 0.18f, 0.22f, 1.00f);
        style.Colors[(int) ImGuiCol.ScrollbarBg] = new Vector4(0.02f, 0.02f, 0.02f, 0.39f);
        style.Colors[(int) ImGuiCol.ScrollbarGrab] = new Vector4(0.20f, 0.25f, 0.29f, 1.00f);
        style.Colors[(int) ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.18f, 0.22f, 0.25f, 1.00f);
        style.Colors[(int) ImGuiCol.ScrollbarGrabActive] = new Vector4(0.09f, 0.21f, 0.31f, 1.00f);
        style.Colors[(int) ImGuiCol.CheckMark] = new Vector4(0.28f, 0.56f, 1.00f, 1.00f);
        style.Colors[(int) ImGuiCol.SliderGrab] = new Vector4(0.28f, 0.56f, 1.00f, 1.00f);
        style.Colors[(int) ImGuiCol.SliderGrabActive] = new Vector4(0.37f, 0.61f, 1.00f, 1.00f);
        style.Colors[(int) ImGuiCol.Button] = new Vector4(0.20f, 0.25f, 0.29f, 1.00f);
        style.Colors[(int) ImGuiCol.ButtonHovered] = new Vector4(0.28f, 0.56f, 1.00f, 1.00f);
        style.Colors[(int) ImGuiCol.ButtonActive] = new Vector4(0.06f, 0.53f, 0.98f, 1.00f);
        style.Colors[(int) ImGuiCol.Header] = new Vector4(0.20f, 0.25f, 0.29f, 0.55f);
        style.Colors[(int) ImGuiCol.HeaderHovered] = new Vector4(0.26f, 0.59f, 0.98f, 0.80f);
        style.Colors[(int) ImGuiCol.HeaderActive] = new Vector4(0.26f, 0.59f, 0.98f, 1.00f);
        style.Colors[(int) ImGuiCol.Separator] = new Vector4(0.20f, 0.25f, 0.29f, 1.00f);
        style.Colors[(int) ImGuiCol.SeparatorHovered] = new Vector4(0.10f, 0.40f, 0.75f, 0.78f);
        style.Colors[(int) ImGuiCol.SeparatorActive] = new Vector4(0.10f, 0.40f, 0.75f, 1.00f);
        style.Colors[(int) ImGuiCol.ResizeGrip] = new Vector4(0.26f, 0.59f, 0.98f, 0.25f);
        style.Colors[(int) ImGuiCol.ResizeGripHovered] = new Vector4(0.26f, 0.59f, 0.98f, 0.67f);
        style.Colors[(int) ImGuiCol.ResizeGripActive] = new Vector4(0.26f, 0.59f, 0.98f, 0.95f);
        style.Colors[(int) ImGuiCol.Tab] = new Vector4(0.11f, 0.15f, 0.17f, 1.00f);
        style.Colors[(int) ImGuiCol.TabHovered] = new Vector4(0.26f, 0.59f, 0.98f, 0.80f);
        style.Colors[(int) ImGuiCol.TabActive] = new Vector4(0.20f, 0.25f, 0.29f, 1.00f);
        style.Colors[(int) ImGuiCol.TabUnfocused] = new Vector4(0.11f, 0.15f, 0.17f, 1.00f);
        style.Colors[(int) ImGuiCol.TabUnfocusedActive] = new Vector4(0.11f, 0.15f, 0.17f, 1.00f);
        style.Colors[(int) ImGuiCol.PlotLines] = new Vector4(0.61f, 0.61f, 0.61f, 1.00f);
        style.Colors[(int) ImGuiCol.PlotLinesHovered] = new Vector4(1.00f, 0.43f, 0.35f, 1.00f);
        style.Colors[(int) ImGuiCol.PlotHistogram] = new Vector4(0.90f, 0.70f, 0.00f, 1.00f);
        style.Colors[(int) ImGuiCol.PlotHistogramHovered] = new Vector4(1.00f, 0.60f, 0.00f, 1.00f);
        style.Colors[(int) ImGuiCol.TextSelectedBg] = new Vector4(0.26f, 0.59f, 0.98f, 0.35f);
        style.Colors[(int) ImGuiCol.DragDropTarget] = new Vector4(1.00f, 1.00f, 0.00f, 0.90f);
        style.Colors[(int) ImGuiCol.NavHighlight] = new Vector4(0.26f, 0.59f, 0.98f, 1.00f);
        style.Colors[(int) ImGuiCol.NavWindowingHighlight] = new Vector4(1.00f, 1.00f, 1.00f, 0.70f);
        style.Colors[(int) ImGuiCol.NavWindowingDimBg] = new Vector4(0.80f, 0.80f, 0.80f, 0.20f);
        style.Colors[(int) ImGuiCol.ModalWindowDimBg] = new Vector4(0.80f, 0.80f, 0.80f, 0.35f);

        return style.WindowPadding;
    }
}
