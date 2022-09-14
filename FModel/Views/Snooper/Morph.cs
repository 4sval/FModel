using System;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.Core.Math;
using Silk.NET.OpenGL;

namespace FModel.Views.Snooper;

public class Morph : IDisposable
{
    private uint _handle;
    private GL _gl;

    private BufferObject<float> _vbo;

    public readonly string Name;
    public float[] Vertices;

    public float Value;

    public Morph(float[] vertices, uint vertexSize, UMorphTarget morphTarget)
    {
        Name = morphTarget.Name;
        Vertices = (float[]) vertices.Clone();

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

        for (uint i = 0; i < Vertices.Length; i += vertexSize)
        {
            if (!TryFindVertex((uint) Vertices[i + 0], out var positionDelta)) continue;

            Vertices[i + 1] += positionDelta.X * Constants.SCALE_DOWN_RATIO;
            Vertices[i + 2] += positionDelta.Z * Constants.SCALE_DOWN_RATIO;
            Vertices[i + 3] += positionDelta.Y * Constants.SCALE_DOWN_RATIO;
        }
    }

    public void Setup(GL gl)
    {
        _gl = gl;

        _handle = _gl.CreateProgram();

        _vbo = new BufferObject<float>(_gl, Vertices, BufferTargetARB.ArrayBuffer);
    }

    public void Bind(Shader shader)
    {
        shader.SetUniform("morph_time", Value);
    }

    public void Dispose()
    {
        _vbo.Dispose();
        _gl.DeleteProgram(_handle);
    }
}
