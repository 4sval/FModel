using System.Runtime.CompilerServices;

namespace PakReader.Pak.IO
{
    public readonly struct FIoDirectoryIndexHandle
    {
        public static FIoDirectoryIndexHandle InvalidHandle = new FIoDirectoryIndexHandle(uint.MaxValue); 
        private readonly uint _handle;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ToIndex() => _handle;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FIoDirectoryIndexHandle(uint handle)
        {
            _handle = handle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValid() => this != InvalidHandle;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => (int) _handle;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(FIoDirectoryIndexHandle a, FIoDirectoryIndexHandle b) => a._handle == b._handle;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(FIoDirectoryIndexHandle a, FIoDirectoryIndexHandle b) => a._handle != b._handle;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FIoDirectoryIndexHandle FromIndex(uint index) => new FIoDirectoryIndexHandle(index);
    }
}