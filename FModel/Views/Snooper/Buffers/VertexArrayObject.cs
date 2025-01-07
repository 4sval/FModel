using System;
using System.Numerics;
using OpenTK.Graphics.OpenGL4;

namespace FModel.Views.Snooper.Buffers;

public class VertexArrayObject<TVertexType, TIndexType> : IDisposable where TVertexType : unmanaged where TIndexType : unmanaged
{
    private readonly int _handle;
    private readonly int _sizeOfVertex;
    private readonly int _sizeOfIndex;

    public unsafe VertexArrayObject(BufferObject<TVertexType> vbo, BufferObject<TIndexType> ebo)
    {
        _handle = GL.GenVertexArray();
        _sizeOfVertex = sizeof(TVertexType);
        _sizeOfIndex = sizeof(TIndexType);

        Bind();
        vbo.Bind();
        ebo.Bind();
    }

    public void VertexAttributePointer(uint index, int count, VertexAttribPointerType type, int vertexSize, int offset)
    {
        switch (type)
        {
            case VertexAttribPointerType.Int:
            case VertexAttribPointerType.UnsignedInt:
                GL.VertexAttribIPointer(index, count, (VertexAttribIntegerType) type, vertexSize * _sizeOfVertex, offset * _sizeOfVertex);
                break;
            default:
                GL.VertexAttribPointer(index, count, type, false, vertexSize * _sizeOfVertex, offset * _sizeOfVertex);
                break;
        }
        GL.EnableVertexAttribArray(index);
    }

    public void Bind()
    {
        GL.BindVertexArray(_handle);
    }

    public void Unbind()
    {
        GL.BindVertexArray(0);
    }

    public unsafe void BindInstancing(int startIndex)
    {
        Bind();

        var size = sizeof(Vector4);
        for (int i = 0; i < 4; i++)
        {
            var baseIndex = startIndex + i;

            GL.EnableVertexAttribArray(baseIndex);
            GL.VertexAttribPointer(baseIndex, 4, VertexAttribPointerType.Float, false, 4 * size, i * size);
            GL.VertexAttribDivisor(baseIndex, 1);
        }

        Unbind();
    }

    public void Dispose()
    {
        GL.DeleteVertexArray(_handle);
    }
}
