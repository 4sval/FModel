using System;
using OpenTK.Graphics.OpenGL4;

namespace FModel.Views.Snooper;

public class Section : IDisposable
{
    private int _handle;

    public readonly int Index;
    public readonly int FacesCount;
    public readonly int FirstFaceIndex;
    public readonly Material Material;

    public bool Show;
    public bool Wireframe;

    private Section(int index, int facesCount, int firstFaceIndex)
    {
        Index = index;
        FacesCount = facesCount;
        FirstFaceIndex = firstFaceIndex;
        Show = true;
    }

    public Section(int index, int facesCount, int firstFaceIndex, Material material) : this(index, facesCount, firstFaceIndex)
    {
        Material = material;
    }

    public void Setup(Cache cache)
    {
        _handle = GL.CreateProgram();

        Material.Setup(cache);
    }

    public void Render(Shader shader, int instanceCount)
    {
        Material.Render(shader);

        GL.PolygonMode(MaterialFace.FrontAndBack, Wireframe ? PolygonMode.Line : PolygonMode.Fill);
        if (Show) GL.DrawArraysInstanced(PrimitiveType.Triangles, FirstFaceIndex, FacesCount, instanceCount);
    }

    public void Dispose()
    {
        GL.DeleteProgram(_handle);
    }
}
