using System;
using OpenTK.Graphics.OpenGL4;

namespace FModel.Views.Snooper;

public class Section : IDisposable
{
    private int _handle;

    public readonly int MaterialIndex;
    public readonly int FacesCount;
    public readonly int FirstFaceIndex;

    public bool Show;

    public Section(int index, int facesCount, int firstFaceIndex)
    {
        MaterialIndex = index;
        FacesCount = facesCount;
        FirstFaceIndex = firstFaceIndex;
        Show = true;
    }

    public Section(int index, int facesCount, int firstFaceIndex, Material material) : this(index, facesCount, firstFaceIndex)
    {
        material.IsUsed = true;
        Show = !material.Parameters.IsNull && !material.Parameters.IsTransparent;
    }

    public void Setup()
    {
        _handle = GL.CreateProgram();
    }

    public void Render(int instanceCount)
    {
        if (Show) GL.DrawArraysInstanced(PrimitiveType.Triangles, FirstFaceIndex, FacesCount, instanceCount);
    }

    public void Dispose()
    {
        GL.DeleteProgram(_handle);
    }
}
