using System;
using FModel.Views.Snooper.Shading;
using OpenTK.Graphics.OpenGL4;

namespace FModel.Views.Snooper.Models;

public class Section : IDisposable
{
    private int _handle;

    public readonly int MaterialIndex;
    public readonly int FacesCount;
    public readonly int FirstFaceIndex;
    public readonly IntPtr FirstFaceIndexPtr;

    public bool Show;

    public Section(int index, int facesCount, int firstFaceIndex)
    {
        MaterialIndex = index;
        FacesCount = facesCount;
        FirstFaceIndex = firstFaceIndex;
        FirstFaceIndexPtr = new IntPtr(FirstFaceIndex * sizeof(uint));
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

    public void Dispose()
    {
        GL.DeleteProgram(_handle);
    }
}
