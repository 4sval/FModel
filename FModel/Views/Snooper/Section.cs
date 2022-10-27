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
    public bool Wireframe;

    public Section(int index, int facesCount, int firstFaceIndex)
    {
        MaterialIndex = index;
        FacesCount = facesCount;
        FirstFaceIndex = firstFaceIndex;
        Show = true;
    }

    public void Setup()
    {
        _handle = GL.CreateProgram();
    }

    public void Render(int instanceCount)
    {
        GL.PolygonMode(MaterialFace.FrontAndBack, Wireframe ? PolygonMode.Line : PolygonMode.Fill);
        if (Show) GL.DrawArraysInstanced(PrimitiveType.Triangles, FirstFaceIndex, FacesCount, instanceCount);
    }

    public void Dispose()
    {
        GL.DeleteProgram(_handle);
    }
}
