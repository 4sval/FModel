using System;
using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace FModel.Views.Snooper;

public class Viewer
{
    private IWindow _window;
    private GL _gl;
    private Camera _camera;
    private IKeyboard _keyboard;

    public int Width { get; }
    public int Height { get; }

    private const double _ratio = .75;

    public Viewer()
    {
        var x = System.Windows.SystemParameters.MaximizedPrimaryScreenWidth;
        var y = System.Windows.SystemParameters.MaximizedPrimaryScreenHeight;
        Width = Convert.ToInt32(x * _ratio);
        Height = Convert.ToInt32(y * _ratio);

        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>(Width, Height);
        options.Title = "Snooper";
        _window = Window.Create(options);

        _window.Load += OnLoad;
        _window.Closing += OnClose;
    }

    public void Run()
    {
        _window.Run();
    }

    private void OnLoad()
    {
        var input = _window.CreateInput();
        _keyboard = input.Keyboards[0];
        _keyboard.KeyDown += KeyDown;

        _gl = GL.GetApi(_window);

        //Start a camera at position 3 on the Z axis, looking at position -1 on the Z axis
        _camera = new Camera(Vector3.UnitZ * 6, Vector3.UnitZ * -1, Vector3.UnitY, Width / Height);
    }

    private void OnClose()
    {

    }

    private void KeyDown(IKeyboard keyboard, Key key, int arg3)
    {
        if (key == Key.Escape)
        {
            _window.Close();
        }
    }
}
