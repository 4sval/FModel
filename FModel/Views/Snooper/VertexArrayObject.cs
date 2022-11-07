using System;
using System.Numerics;
using OpenTK.Graphics.OpenGL4;

namespace FModel.Views.Snooper;

public class VertexArrayObject<TVertexType, TIndexType> : IDisposable where TVertexType : unmanaged where TIndexType : unmanaged
{
    private readonly int _handle;

    public VertexArrayObject(BufferObject<TVertexType> vbo, BufferObject<TIndexType> ebo)
    {
        _handle = GL.GenVertexArray();

        Bind();
        vbo.Bind();
        ebo.Bind();
    }

    public unsafe void VertexAttributePointer(uint index, int count, VertexAttribPointerType type, int vertexSize, int offset)
    {
        switch (type)
        {
            case VertexAttribPointerType.Int:
                GL.VertexAttribIPointer(index, count, VertexAttribIntegerType.Int, vertexSize * sizeof(TVertexType), (IntPtr) (offset * sizeof(TVertexType)));
                break;
            default:
                GL.VertexAttribPointer(index, count, type, false, vertexSize * sizeof(TVertexType), offset * sizeof(TVertexType));
                break;
        }
        GL.EnableVertexAttribArray(index);
    }

    public void Bind()
    {
        GL.BindVertexArray(_handle);
    }

    public unsafe void BindInstancing()
    {
        Bind();

        var size = sizeof(Vector4);
        GL.EnableVertexAttribArray(9);
        GL.VertexAttribPointer(9, 4, VertexAttribPointerType.Float, false, 4 * size, 0);
        GL.EnableVertexAttribArray(10);
        GL.VertexAttribPointer(10, 4, VertexAttribPointerType.Float, false, 4 * size, 1 * size);
        GL.EnableVertexAttribArray(11);
        GL.VertexAttribPointer(11, 4, VertexAttribPointerType.Float, false, 4 * size, 2 * size);
        GL.EnableVertexAttribArray(12);
        GL.VertexAttribPointer(12, 4, VertexAttribPointerType.Float, false, 4 * size, 3 * size);

        GL.VertexAttribDivisor(9, 1);
        GL.VertexAttribDivisor(10, 1);
        GL.VertexAttribDivisor(11, 1);
        GL.VertexAttribDivisor(12, 1);

        Unbind();
    }

    public void Unbind()
    {
        GL.BindVertexArray(0);
    }

    public void Dispose()
    {
        GL.DeleteVertexArray(_handle);
    }
}
