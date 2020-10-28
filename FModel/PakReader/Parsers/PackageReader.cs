using System;
using System.IO;
using System.Runtime.CompilerServices;
using FModel.PakReader.Parsers.Class;
using FModel.PakReader.Parsers.Objects;

namespace FModel.PakReader.Parsers
{
    public abstract class PackageReader
    {
        protected BinaryReader Loader { get; set; }
        public abstract FNameEntrySerialized[] NameMap { get; }
        public abstract IUExport[] DataExports { get; }
        public abstract FName[] DataExportTypes { get; }
        
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FName ReadFName()
        {
            var NameIndex = Loader.ReadInt32();
            var Number = Loader.ReadInt32();

            // https://github.com/EpicGames/UnrealEngine/blob/bf95c2cbc703123e08ab54e3ceccdd47e48d224a/Engine/Source/Runtime/CoreUObject/Public/UObject/LinkerLoad.h#L821
            // Has some more complicated stuff related to name map pools etc. that seems unnecessary atm
            if (NameIndex >= 0 && NameIndex < NameMap.Length)
            {
                return new FName(NameMap[NameIndex], NameIndex, Number);
            }
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Bad Name Index: {NameIndex}/{NameMap.Length} - Loader Position: {Loader.BaseStream.Position}");
#endif
            return default;
        }


        public static implicit operator BinaryReader(PackageReader reader) => reader.Loader;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte() => Loader.ReadByte();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte ReadSByte() => Loader.ReadSByte();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] ReadBytes(int count) => Loader.ReadBytes(count);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadFString() => Loader.ReadFString();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ReadTArray<T>(Func<T> Getter) => Loader.ReadTArray(Getter);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadInt16() => Loader.ReadInt16();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16() => Loader.ReadUInt16();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32() => Loader.ReadInt32();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32() => Loader.ReadUInt32();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64() => Loader.ReadInt64();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64() => Loader.ReadUInt64();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ReadFloat() => Loader.ReadSingle();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ReadDouble() => Loader.ReadDouble();
        public void SkipBytes(int count) => Loader.BaseStream.Position += count;

        public long Position { get => Loader.BaseStream.Position; set => Loader.BaseStream.Position = value; }
    }
}