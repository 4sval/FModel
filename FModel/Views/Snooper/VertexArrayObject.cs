using System;
using System.Numerics;
using Silk.NET.OpenGL;

namespace FModel.Views.Snooper;

public class VertexArrayObject<TVertexType, TIndexType> : IDisposable where TVertexType : unmanaged where TIndexType : unmanaged
{
    private uint _handle;
    private GL _gl;

    public VertexArrayObject(GL gl, BufferObject<TVertexType> vbo, BufferObject<TIndexType> ebo)
    {
        _gl = gl;

        _handle = _gl.GenVertexArray();
        Bind();
        vbo.Bind();
        ebo.Bind();
    }

    public unsafe void VertexAttributePointer(uint index, int count, VertexAttribPointerType type, uint vertexSize, int offSet)
    {
        switch (type)
        {
            case VertexAttribPointerType.Int:
                _gl.VertexAttribIPointer(index, count, VertexAttribIType.Int, vertexSize * (uint) sizeof(TVertexType), (void*) (offSet * sizeof(TVertexType)));
                break;
            default:
                _gl.VertexAttribPointer(index, count, type, false, vertexSize * (uint) sizeof(TVertexType), (void*) (offSet * sizeof(TVertexType)));
                break;
        }
        _gl.EnableVertexAttribArray(index);
    }

    public void Bind()
    {
        _gl.BindVertexArray(_handle);
    }

    public unsafe void BindInstancing()
    {
        Bind();

        var vec4Size = (uint) sizeof(Vector4);
        _gl.EnableVertexAttribArray(6);
        _gl.VertexAttribPointer(6, 4, VertexAttribPointerType.Float, false, 4 * vec4Size, (void*)0);
        _gl.EnableVertexAttribArray(7);
        _gl.VertexAttribPointer(7, 4, VertexAttribPointerType.Float, false, 4 * vec4Size, (void*)(1 * vec4Size));
        _gl.EnableVertexAttribArray(8);
        _gl.VertexAttribPointer(8, 4, VertexAttribPointerType.Float, false, 4 * vec4Size, (void*)(2 * vec4Size));
        _gl.EnableVertexAttribArray(9);
        _gl.VertexAttribPointer(9, 4, VertexAttribPointerType.Float, false, 4 * vec4Size, (void*)(3 * vec4Size));

        _gl.VertexAttribDivisor(6, 1);
        _gl.VertexAttribDivisor(7, 1);
        _gl.VertexAttribDivisor(8, 1);
        _gl.VertexAttribDivisor(9, 1);

        Unbind();
    }

    public void Unbind()
    {
        _gl.BindVertexArray(0);
    }

    public void Dispose()
    {
        _gl.DeleteVertexArray(_handle);
    }
}
