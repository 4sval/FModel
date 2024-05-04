using System;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.PhysicsEngine;
using FModel.Views.Snooper.Buffers;
using FModel.Views.Snooper.Shading;
using OpenTK.Graphics.OpenGL4;

namespace FModel.Views.Snooper.Models;

public class Collision : IDisposable
{
    private readonly int[] _indexData;
    private readonly FVector[] _vertexData;
    private readonly Transform _transform;

    private int _handle;
    private BufferObject<int> _ebo { get; set; }
    private BufferObject<FVector> _vbo { get; set; }
    private VertexArrayObject<FVector, int> _vao { get; set; }

    public Collision(FKConvexElem convexElems)
    {
        _indexData = convexElems.IndexData;
        _vertexData = convexElems.VertexData;
        _transform = new Transform
        {
            Position = convexElems.Transform.Translation * Constants.SCALE_DOWN_RATIO,
            Rotation = convexElems.Transform.Rotation,
            Scale = convexElems.Transform.Scale3D
        };
    }

    public void Setup()
    {
        _handle = GL.CreateProgram();
        _ebo = new BufferObject<int>(_indexData, BufferTarget.ElementArrayBuffer);
        _vbo = new BufferObject<FVector>(_vertexData, BufferTarget.ArrayBuffer);
        _vao = new VertexArrayObject<FVector, int>(_vbo, _ebo);

        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 1, 0);
        _vao.Unbind();
    }

    public void Render(Shader shader)
    {
        shader.SetUniform("uCollisionMatrix", _transform.Matrix);

        _vao.Bind();
        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
        GL.DrawElements(PrimitiveType.Triangles, _ebo.Size, DrawElementsType.UnsignedInt, 0);
        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        _vao.Unbind();
    }

    public void Dispose()
    {
        _ebo?.Dispose();
        _vbo?.Dispose();
        _vao?.Dispose();
        GL.DeleteProgram(_handle);
    }
}
