using System;
using OpenTK.Graphics.OpenGL4;

namespace FModel.Views.Snooper.Buffers;

public class BufferObject<TDataType> : IDisposable where TDataType : unmanaged
{
    private readonly int _handle;
    private readonly int _sizeOf;
    private readonly BufferTarget _bufferTarget;

    public readonly int Size;

    private unsafe BufferObject(BufferTarget bufferTarget)
    {
        _bufferTarget = bufferTarget;
        _handle = GL.GenBuffer();
        _sizeOf = sizeof(TDataType);

        Bind();
    }

    public BufferObject(TDataType[] data, BufferTarget bufferTarget) : this(bufferTarget)
    {
        Size = data.Length;
        GL.BufferData(bufferTarget, Size * _sizeOf, data, BufferUsageHint.StaticDraw);
    }

    public BufferObject(int length, BufferTarget bufferTarget) : this(bufferTarget)
    {
        Size = length;
        GL.BufferData(bufferTarget, Size * _sizeOf, IntPtr.Zero, BufferUsageHint.DynamicDraw);
    }

    public void UpdateRange(TDataType data) => UpdateRange(Size, data);
    public void UpdateRange(int count, TDataType data)
    {
        Bind();
        for (int i = 0; i < count; i++) Update(i, data);
        Unbind();
    }

    public void Update(int offset, TDataType data)
    {
        GL.BufferSubData(_bufferTarget,  (IntPtr) (offset * _sizeOf), _sizeOf, ref data);
    }

    public void Update(TDataType[] data)
    {
        Bind();
        GL.BufferSubData(_bufferTarget, IntPtr.Zero, data.Length * _sizeOf, data);
        Unbind();
    }

    public TDataType Get(int offset)
    {
        TDataType data = default;
        GL.GetBufferSubData(_bufferTarget, (IntPtr) (offset * _sizeOf), _sizeOf, ref data);
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
