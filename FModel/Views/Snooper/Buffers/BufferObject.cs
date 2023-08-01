using System;
using OpenTK.Graphics.OpenGL4;

namespace FModel.Views.Snooper.Buffers;

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

    public unsafe BufferObject(int length, BufferTarget bufferTarget) : this(bufferTarget)
    {
        GL.BufferData(bufferTarget, length * sizeof(TDataType), IntPtr.Zero, BufferUsageHint.DynamicDraw);
    }

    public void UpdateRange(int count, TDataType data)
    {
        Bind();
        for (int i = 0; i < count; i++) Update(i, data);
        Unbind();
    }

    public unsafe void Update(int offset, TDataType data)
    {
        GL.BufferSubData(_bufferTarget,  (IntPtr) (offset * sizeof(TDataType)), sizeof(TDataType), ref data);
    }

    public unsafe void Update(TDataType[] data)
    {
        GL.BufferSubData(_bufferTarget, IntPtr.Zero, data.Length * sizeof(TDataType), data);
    }

    public unsafe TDataType Get(int offset)
    {
        TDataType data = default;

        Bind();
        GL.GetBufferSubData(_bufferTarget, (IntPtr) (offset * sizeof(TDataType)), sizeof(TDataType), ref data);
        Unbind();

        return data;
    }

    public void Bind()
    {
        GL.BindBuffer(_bufferTarget, _handle);
    }

    public void BindBufferBase(int index)
    {
        if (_bufferTarget != BufferTarget.ShaderStorageBuffer)
            throw new ArgumentException("BindBufferBase is not allowed for anything but Shader Storage Buffers");
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, index, _handle);
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
