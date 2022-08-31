using System;
using System.Collections.Generic;
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
    private RawImage _icon;

    private readonly FramebufferObject _framebuffer;
    private readonly Skybox _skybox;
    private readonly Grid _grid;
    private readonly List<Model> _models;

    private Shader _shader;

    private Vector2D<int> _size;

    public Snooper()
    {
        const double ratio = .7;
        var x = SystemParameters.MaximizedPrimaryScreenWidth;
        var y = SystemParameters.MaximizedPrimaryScreenHeight;

        var options = WindowOptions.Default;
        options.Size = _size = new Vector2D<int>(Convert.ToInt32(x * ratio), Convert.ToInt32(y * ratio));
        options.WindowBorder = WindowBorder.Hidden;
        options.Title = "Snooper";
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
        _window.FramebufferResize += delegate(Vector2D<int> vector2D)
        {
            _gl.Viewport(vector2D);
            _size = vector2D;
        };

        _framebuffer = new FramebufferObject(_size);
        _skybox = new Skybox();
        _grid = new Grid();
        _models = new List<Model>();
    }

    public void Run(UObject export)
    {
        switch (export)
        {
            case UStaticMesh st when st.TryConvert(out var mesh):
            {
                _models.Add(new Model(st.Name, mesh.LODs[0], mesh.LODs[0].Verts));
                SetupCamera(mesh.BoundingBox *= Constants.SCALE_DOWN_RATIO);
                break;
            }
            case USkeletalMesh sk when sk.TryConvert(out var mesh):
            {
                _models.Add(new Model(sk.Name, mesh.LODs[0], mesh.LODs[0].Verts));
                SetupCamera(mesh.BoundingBox *= Constants.SCALE_DOWN_RATIO);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(export));
        }

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

        _gl = GL.GetApi(_window);
        _gl.Enable(EnableCap.Multisample);

        _controller = new ImGuiController(_gl, _window, input);

        ImGuiExtensions.Theme();

        _framebuffer.Setup(_gl);
        _skybox.Setup(_gl);
        _grid.Setup(_gl);

        _shader = new Shader(_gl);
        foreach (var model in _models)
        {
            model.Setup(_gl);
        }
    }

    /// <summary>
    /// friendly reminder this is called each frame
    /// don't do crazy things inside
    /// </summary>
    private void OnRender(double deltaTime)
    {
        _controller.Update((float) deltaTime);

        ClearWhatHasBeenDrawn(); // in main window

        _framebuffer.Bind(); // switch to dedicated window
        ClearWhatHasBeenDrawn(); // in dedicated window

        ImGuiExtensions.DrawDockSpace(_size);
        ImGuiExtensions.DrawNavbar();
        ImGui.ShowDemoWindow();

        _skybox.Bind(_camera);
        _grid.Bind(_camera);

        _shader.Use();

        _shader.SetUniform("uModel", Matrix4x4.Identity);
        _shader.SetUniform("uView", _camera.GetViewMatrix());
        _shader.SetUniform("uProjection", _camera.GetProjectionMatrix());
        _shader.SetUniform("viewPos", _camera.Position);

        _shader.SetUniform("material.diffuseMap", 0);
        _shader.SetUniform("material.normalMap", 1);
        _shader.SetUniform("material.specularMap", 2);
        _shader.SetUniform("material.emissionMap", 3);

        _shader.SetUniform("light.position", _camera.Position);

        ImGui.Begin("ImGui.NET");
        foreach (var model in _models)
        {
            model.Bind(_shader);
        }
        ImGui.End();

        ImGuiExtensions.DrawViewport(_framebuffer, _camera, _mouse);
        ImGuiExtensions.DrawFPS();

        _framebuffer.BindMsaa();
        _framebuffer.Bind(0); // switch back to main window
        _framebuffer.BindStuff();

        _controller.Render(); // render ImGui in main window
    }

    private void ClearWhatHasBeenDrawn()
    {
        _gl.Enable(EnableCap.Blend);
        _gl.Enable(EnableCap.DepthTest);
        _gl.ClearColor(1.0f, 0.102f, 0.129f, 1.0f);
        _gl.Clear((uint) ClearBufferMask.ColorBufferBit | (uint) ClearBufferMask.DepthBufferBit);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _gl.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
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

    private void OnClose()
    {
        _framebuffer.Dispose();
        _grid.Dispose();
        _skybox.Dispose();
        _shader.Dispose();
        foreach (var model in _models)
        {
            model.Dispose();
        }
        _models.Clear();
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
