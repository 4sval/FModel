using System;
using OpenTK.Graphics.OpenGL4;

namespace FModel.Views.Snooper;

public class BufferObject<TDataType> : IDisposable where TDataType : unmanaged
{
    private readonly int _handle;
    private readonly BufferTarget _bufferTarget;

    private BufferObject(BufferTarget bufferTarget)
    {
        _bufferTarget = bufferTarget;
        _handle = GL.GenBuffer();

        Bind();
    }

    public unsafe BufferObject(TDataType[] data, BufferTarget bufferTarget) : this(bufferTarget)
    {
        GL.BufferData(bufferTarget, data.Length * sizeof(TDataType), data, BufferUsageHint.StaticDraw);
    }

    public unsafe void Update(int offset, TDataType data)
    {
        GL.BufferSubData(_bufferTarget,  (IntPtr) (offset * sizeof(TDataType)), sizeof(TDataType), ref data);
    }

    public unsafe void Update(TDataType[] data)
    {
        GL.BufferSubData(_bufferTarget, IntPtr.Zero, data.Length * sizeof(TDataType), data);
    }

    public void Bind()
    {
        GL.BindBuffer(_bufferTarget, _handle);
    }

    public void Unbind()
    {
        GL.BindBuffer(_bufferTarget, 0);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_handle);
    }
}
