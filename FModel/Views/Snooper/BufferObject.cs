using System;
using Silk.NET.OpenGL;

namespace FModel.Views.Snooper;

public class BufferObject<TDataType> : IDisposable where TDataType : unmanaged
{
    private uint _handle;
    private BufferTargetARB _bufferType;
    private GL _gl;

    public BufferObject(GL gl, BufferTargetARB bufferType)
    {
        _gl = gl;
        _bufferType = bufferType;

        _handle = _gl.GenBuffer();
        Bind();
    }

    public unsafe BufferObject(GL gl, Span<TDataType> data, BufferTargetARB bufferType) : this(gl, bufferType)
    {
        fixed (void* d = data)
        {
            _gl.BufferData(bufferType, (nuint) (data.Length * sizeof(TDataType)), d, BufferUsageARB.StaticDraw);
        }
    }

    public unsafe void Update(int offset, TDataType data)
    {
        _gl.BufferSubData(_bufferType, offset * sizeof(TDataType), (nuint) sizeof(TDataType), data);
    }

    public void Bind()
    {
        _gl.BindBuffer(_bufferType, _handle);
    }

    public void Unbind()
    {
        _gl.BindBuffer(_bufferType, 0);
    }

    public void Dispose()
    {
        _gl.DeleteBuffer(_handle);
    }
}
