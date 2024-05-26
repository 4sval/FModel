using System;
using System.Collections.Generic;
using System.Numerics;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.PhysicsEngine;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Views.Snooper.Buffers;
using FModel.Views.Snooper.Shading;
using OpenTK.Graphics.OpenGL4;

namespace FModel.Views.Snooper.Models;

public class Collision : IDisposable
{
    private const int Slices = 16;
    private const int Stacks = 8;
    private const float SectorStep = 2 * MathF.PI / Slices;
    private const float StackStep = MathF.PI / Stacks;

    private readonly int[] _indexData;
    private readonly FVector[] _vertexData;
    private readonly Transform _transform;
    public readonly string LoweredBoneName;

    private int _handle;
    private BufferObject<int> _ebo { get; set; }
    private BufferObject<FVector> _vbo { get; set; }
    private VertexArrayObject<FVector, int> _vao { get; set; }

    private Collision()
    {
        _indexData = [];
        _vertexData = [];
        _transform = Transform.Identity;
    }

    private Collision(FName boneName) : this()
    {
        LoweredBoneName = boneName.Text.ToLower();
    }

    public Collision(FKConvexElem convexElems, FName boneName = default) : this(boneName)
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

    public Collision(FKSphereElem sphereElem, FName boneName = default) : this(boneName)
    {
        _vertexData = new FVector[(Slices + 1) * (Stacks + 1)];
        for (var i = 0; i <= Stacks; i++)
        {
            var stackAngle = MathF.PI / 2 - i * StackStep;
            var xy = MathF.Cos(stackAngle);
            var z = MathF.Sin(stackAngle);

            for (var j = 0; j <= Slices; j++)
            {
                var sectorAngle = j * SectorStep;
                var x = xy * MathF.Cos(sectorAngle);
                var y = xy * MathF.Sin(sectorAngle);
                _vertexData[i * (Slices + 1) + j] = new FVector(x, y, z);
            }
        }

        _indexData = new int[Stacks * Slices * 6];
        for (var i = 0; i < Stacks; i++)
        {
            for (var j = 0; j < Slices; j++)
            {
                var a = i * (Slices + 1) + j;
                var b = a + Slices + 1;
                _indexData[(i * Slices + j) * 6 + 0] = a;
                _indexData[(i * Slices + j) * 6 + 1] = b;
                _indexData[(i * Slices + j) * 6 + 2] = a + 1;
                _indexData[(i * Slices + j) * 6 + 3] = b;
                _indexData[(i * Slices + j) * 6 + 4] = b + 1;
                _indexData[(i * Slices + j) * 6 + 5] = a + 1;
            }
        }

        _transform = new Transform
        {
            Position = sphereElem.Center * Constants.SCALE_DOWN_RATIO,
            Scale = new FVector(sphereElem.Radius)
        };
    }

    public Collision(FKBoxElem boxElem, FName boneName = default) : this(boneName)
    {
        _vertexData =
        [
            new FVector(-boxElem.X, -boxElem.Y, -boxElem.Z),
            new FVector(boxElem.X, -boxElem.Y, -boxElem.Z),
            new FVector(boxElem.X, boxElem.Y, -boxElem.Z),
            new FVector(-boxElem.X, boxElem.Y, -boxElem.Z),
            new FVector(-boxElem.X, -boxElem.Y, boxElem.Z),
            new FVector(boxElem.X, -boxElem.Y, boxElem.Z),
            new FVector(boxElem.X, boxElem.Y, boxElem.Z),
            new FVector(-boxElem.X, boxElem.Y, boxElem.Z)
        ];

        _indexData =
        [
            0, 1, 2, 2, 3, 0,
            1, 5, 6, 6, 2, 1,
            5, 4, 7, 7, 6, 5,
            4, 0, 3, 3, 7, 4,
            3, 2, 6, 6, 7, 3,
            4, 5, 1, 1, 0, 4
        ];

        _transform = new Transform
        {
            Position = boxElem.Center * Constants.SCALE_DOWN_RATIO,
            Rotation = boxElem.Rotation.Quaternion(),
            Scale = new FVector(.5f)
        };
    }

    public Collision(FKSphylElem sphylElem, FName boneName = default)
        : this(sphylElem.Length, [sphylElem.Radius, sphylElem.Radius], sphylElem.Center, sphylElem.Rotation, boneName) {}
    public Collision(FKTaperedCapsuleElem taperedCapsuleElem, FName boneName = default)
        : this(taperedCapsuleElem.Length, [taperedCapsuleElem.Radius1, taperedCapsuleElem.Radius0], taperedCapsuleElem.Center, taperedCapsuleElem.Rotation, boneName) {}

    private Collision(float length, float[] radius, FVector center = default, FRotator rotator = default, FName boneName = default) : this(boneName)
    {
        int vLength = 0;
        int half = Slices / 2;
        int k2 = (Slices + 1) * (Stacks + 1);

        _vertexData = new FVector[k2 + Slices + 1];
        for(int i = 0; i < 2; ++i)
        {
            float h = -length / 2.0f + i * length;
            int start = i == 0 ? Stacks / 2 : 0;
            int end = i == 0 ? Stacks : Stacks / 2;

            for(int j = start; j <= end; ++j)
            {
                var stackAngle = MathF.PI / 2 - j * StackStep;
                var xy = radius[i] * MathF.Cos(stackAngle);
                var z = radius[i] * MathF.Sin(stackAngle) + h;

                for(int k = 0; k <= Slices; ++k)
                {
                    var sectorAngle = k * SectorStep;
                    var x = xy * MathF.Cos(sectorAngle);
                    var y = xy * MathF.Sin(sectorAngle);
                    _vertexData[vLength++] = new FVector(x, y, z);
                }
            }
        }

        var indices = new List<int>();
        AddIndicesForSlices(indices, ref k2);
        indices.AddRange(new[] {0, k2, k2, k2 - half, half, half});
        AddIndicesForStacks(indices);
        half /= 2;
        indices.AddRange(new[] {half, k2 - half * 3, k2 - half * 3, half * 3, k2 - half, k2 - half});
        AddIndicesForStacks(indices, Stacks / 2);
        _indexData = indices.ToArray();

        _transform = new Transform
        {
            Position = center * Constants.SCALE_DOWN_RATIO,
            Rotation = rotator.Quaternion()
        };
    }

    private void AddIndicesForSlices(List<int> indices, ref int k2)
    {
        for(int k1 = 0; k1 < Slices; ++k1, ++k2)
        {
            indices.AddRange(new[] {k1, k1 + 1, k1 + 1, k2, k2 + 1, k2 + 1});
        }
    }

    private void AddIndicesForStacks(List<int> indices, int start = 0)
    {
        for (int k1 = start; k1 < Stacks * Slices + Slices; k1 += Slices + 1)
        {
            if (k1 == Stacks / 2 * (Slices + 1) + start) continue;
            indices.AddRange(new[] {k1, k1 + Slices + 1, k1 + Slices + 1, k1 + Slices / 2, k1 + Slices / 2 + Slices + 1, k1 + Slices / 2 + Slices + 1});
        }
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

    public void Render(Shader shader, Matrix4x4 boneMatrix)
    {
        shader.SetUniform("uCollisionMatrix", _transform.Matrix * boneMatrix);

        _vao.Bind();
        if (_indexData.Length > 0)
        {
            GL.DrawElements(PrimitiveType.Triangles, _ebo.Size, DrawElementsType.UnsignedInt, 0);
        }
        else
        {
            GL.DrawArrays(PrimitiveType.Points, 0, _vbo.Size);
        }
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
