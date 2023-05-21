﻿using System;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.Core.Math;
using OpenTK.Graphics.OpenGL4;

namespace FModel.Views.Snooper.Models;

public class Morph : IDisposable
{
    private int _handle;

    public static readonly int VertexSize = 6; // Position + Tangent

    public readonly string Name;
    public readonly float[] Vertices;

    public Morph(float[] vertices, int vertexSize, UMorphTarget morphTarget)
    {
        Name = morphTarget.Name;
        Vertices = new float[vertices.Length / vertexSize * VertexSize];

        bool TryFindVertex(uint index, out FVector positionDelta, out FVector tangentDelta)
        {
            foreach (var vertex in morphTarget.MorphLODModels[0].Vertices)
            {
                if (vertex.SourceIdx == index)
                {
                    positionDelta = vertex.PositionDelta;
                    tangentDelta = vertex.TangentZDelta;
                    return true;
                }
            }
            positionDelta = FVector.ZeroVector;
            tangentDelta = FVector.ZeroVector;
            return false;
        }

        for (int i = 0; i < vertices.Length; i += vertexSize)
        {
            var count = 0;
            var baseIndex = i / vertexSize * VertexSize;
            if (TryFindVertex((uint) vertices[i + 0], out var positionDelta, out var tangentDelta))
            {
                Vertices[baseIndex + count++] = vertices[i + 1] + positionDelta.X * Constants.SCALE_DOWN_RATIO;
                Vertices[baseIndex + count++] = vertices[i + 2] + positionDelta.Z * Constants.SCALE_DOWN_RATIO;
                Vertices[baseIndex + count++] = vertices[i + 3] + positionDelta.Y * Constants.SCALE_DOWN_RATIO;
                Vertices[baseIndex + count++] = vertices[i + 7] + tangentDelta.X * Constants.SCALE_DOWN_RATIO;
                Vertices[baseIndex + count++] = vertices[i + 8] + tangentDelta.Z * Constants.SCALE_DOWN_RATIO;
                Vertices[baseIndex + count++] = vertices[i + 9] + tangentDelta.Y * Constants.SCALE_DOWN_RATIO;
            }
            else
            {
                Vertices[baseIndex + count++] = vertices[i + 1];
                Vertices[baseIndex + count++] = vertices[i + 2];
                Vertices[baseIndex + count++] = vertices[i + 3];
                Vertices[baseIndex + count++] = vertices[i + 7];
                Vertices[baseIndex + count++] = vertices[i + 8];
                Vertices[baseIndex + count++] = vertices[i + 9];
            }
        }
    }

    public void Setup()
    {
        _handle = GL.CreateProgram();
    }

    public void Dispose()
    {
        GL.DeleteProgram(_handle);
    }
}
