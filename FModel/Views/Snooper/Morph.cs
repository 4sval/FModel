using System;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.Core.Math;
using Silk.NET.OpenGL;

namespace FModel.Views.Snooper;

public class Morph : IDisposable
{
    private uint _handle;
    private GL _gl;

    private uint _vertexSize = 3; // Position

    public readonly string Name;
    public readonly float[] Vertices;

    public Morph(float[] vertices, uint vertexSize, UMorphTarget morphTarget)
    {
        Name = morphTarget.Name;
        Vertices = new float[vertices.Length / vertexSize * _vertexSize];

        bool TryFindVertex(uint index, out FVector positionDelta)
        {
            foreach (var vertex in morphTarget.MorphLODModels[0].Vertices)
            {
                if (vertex.SourceIdx == index)
                {
                    positionDelta = vertex.PositionDelta;
                    return true;
                }
            }
            positionDelta = FVector.ZeroVector;
            return false;
        }

        for (uint i = 0; i < vertices.Length; i += vertexSize)
        {
            var count = 0;
            var baseIndex = i / vertexSize * _vertexSize;
            if (TryFindVertex((uint) vertices[i + 0], out var positionDelta))
            {
                Vertices[baseIndex + count++] = vertices[i + 1] + positionDelta.X * Constants.SCALE_DOWN_RATIO;
                Vertices[baseIndex + count++] = vertices[i + 2] + positionDelta.Z * Constants.SCALE_DOWN_RATIO;
                Vertices[baseIndex + count++] = vertices[i + 3] + positionDelta.Y * Constants.SCALE_DOWN_RATIO;
            }
            else
            {
                Vertices[baseIndex + count++] = vertices[i + 1];
                Vertices[baseIndex + count++] = vertices[i + 2];
                Vertices[baseIndex + count++] = vertices[i + 3];
            }
        }
    }

    public void Setup(GL gl)
    {
        _gl = gl;
        _handle = _gl.CreateProgram();
    }

    public void Dispose()
    {
        _gl.DeleteProgram(_handle);
    }
}
