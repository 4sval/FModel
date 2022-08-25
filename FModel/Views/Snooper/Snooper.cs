using System;
using System.Linq;
using System.Numerics;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
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

    private BufferObject<uint> _ebo;
    private BufferObject<float> _vbo;
    private VertexArrayObject<float, uint> _vao;
    private Shader _shader;

    private uint[] _indices;
    private float[] _vertices;

    public int Width { get; }
    public int Height { get; }

    private const double _ratio = .75;
    private const int _vertexSize = 8; // just so we don't have to do .Length
    private const uint _faceSize = 3; // just so we don't have to do .Length
    private readonly uint[] _facesIndex = { 1, 0, 2 };

    public Snooper(UObject export)
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
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.Closing += OnClose;

        switch (export)
        {
            case UStaticMesh st when st.TryConvert(out var mesh):
            {
                _indices = new uint[mesh.LODs[0].Sections.Value.Sum(section => section.NumFaces * _faceSize)];
                _vertices = new float[_indices.Length * _vertexSize];
                foreach (var section in mesh.LODs[0].Sections.Value)
                {
                    for (uint face = 0; face < section.NumFaces; face++)
                    {
                        foreach (var f in _facesIndex)
                        {
                            var index = section.FirstIndex + face * _faceSize + f;
                            var indice = mesh.LODs[0].Indices.Value[index];

                            var vert = mesh.LODs[0].Verts[indice];
                            _vertices[index * _vertexSize] = vert.Position.X * 0.01f;
                            _vertices[index * _vertexSize + 1] = vert.Position.Z * 0.01f;
                            _vertices[index * _vertexSize + 2] = vert.Position.Y * 0.01f;
                            _vertices[index * _vertexSize + 3] = vert.Normal.X;
                            _vertices[index * _vertexSize + 4] = vert.Normal.Z;
                            _vertices[index * _vertexSize + 5] = vert.Normal.Y;
                            _vertices[index * _vertexSize + 6] = vert.UV.U;
                            _vertices[index * _vertexSize + 7] = vert.UV.V;

                            _indices[index] = face * _faceSize + f;
                        }
                    }
                }
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

        _ebo = new BufferObject<uint>(_gl, _indices, BufferTargetARB.ElementArrayBuffer);
        _vbo = new BufferObject<float>(_gl, _vertices, BufferTargetARB.ArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo, _ebo);

        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, _vertexSize, 0); // position
        _vao.VertexAttributePointer(1, 3, VertexAttribPointerType.Float, _vertexSize, 3); // normals
        _vao.VertexAttributePointer(2, 2, VertexAttribPointerType.Float, _vertexSize, 6); // uv

        _shader = new Shader(_gl);

        _camera = new Camera(Vector3.UnitZ * 6, Vector3.UnitZ * -1, Vector3.UnitY, Width / Height);
    }

    private unsafe void OnRender(double deltaTime)
    {
        _gl.Clear((uint) ClearBufferMask.ColorBufferBit);

        _vao.Bind();
        _shader.Use();

        _shader.SetUniform("uModel", Matrix4x4.Identity);
        _shader.SetUniform("uView", _camera.GetViewMatrix());
        _shader.SetUniform("uProjection", _camera.GetProjectionMatrix());
        // _shader.SetUniform("viewPos", _camera.Position);

        _gl.DrawElements(PrimitiveType.Triangles, (uint) _indices.Length, DrawElementsType.UnsignedInt, null);
    }

    private void OnUpdate(double deltaTime)
    {
        var moveSpeed = 5f * (float) deltaTime;
        if (_keyboard.IsKeyPressed(Key.W))
        {
            _camera.Position += moveSpeed * _camera.Front;
        }
        if (_keyboard.IsKeyPressed(Key.S))
        {
            _camera.Position -= moveSpeed * _camera.Front;
        }
        if (_keyboard.IsKeyPressed(Key.A))
        {
            _camera.Position -= Vector3.Normalize(Vector3.Cross(_camera.Front, _camera.Up)) * moveSpeed;
        }
        if (_keyboard.IsKeyPressed(Key.D))
        {
            _camera.Position += Vector3.Normalize(Vector3.Cross(_camera.Front, _camera.Up)) * moveSpeed;
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
        _ebo.Dispose();
        _vbo.Dispose();
        _vao.Dispose();
        _shader.Dispose();
    }

    private void KeyDown(IKeyboard keyboard, Key key, int arg3)
    {
        if (key == Key.Escape)
        {
            _window.Close();
        }
    }
}
