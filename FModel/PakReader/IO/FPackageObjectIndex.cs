using System;
using System.IO;

namespace FModel.PakReader.IO
{
    public readonly struct FPackageObjectIndex : IEquatable<FPackageObjectIndex>
    {
        public const int IndexBits = 62;
        public const ulong IndexMask = (1UL << IndexBits) - 1UL;
        public const ulong TypeMask = ~IndexMask;
        public const int TypeShift = IndexBits;
        public const ulong Invalid = ~0UL;

        
        private readonly ulong _typeAndId;
        public EType Type => (EType) (_typeAndId >> TypeShift);
        public ulong Value => _typeAndId & IndexMask;

        public bool IsNull => _typeAndId == Invalid;
        public bool IsExport => Type == EType.Export;
        public bool IsImport => IsScriptImport || IsPackageImport;
        public bool IsScriptImport => Type == EType.ScriptImport;
        public bool IsPackageImport => Type == EType.PackageImport;
        public uint AsExport => (uint) _typeAndId;

        public FPackageObjectIndex(BinaryReader reader)
        {
            //TypeAndId = Invalid;
            _typeAndId = reader.ReadUInt64();
        }

        public FPackageObjectIndex(ulong typeAndId)
        {
            _typeAndId = typeAndId;
        }

        public bool Equals(FPackageObjectIndex other)
        {
            return _typeAndId == other._typeAndId;
        }

        public override bool Equals(object obj)
        {
            return obj is FPackageObjectIndex other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _typeAndId.GetHashCode();
        }

        public static bool operator ==(FPackageObjectIndex left, FPackageObjectIndex right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FPackageObjectIndex left, FPackageObjectIndex right)
        {
            return !left.Equals(right);
        }
    }

    public enum EType
    {
        Export,
        ScriptImport,
        PackageImport,
        Null,
        TypeCount = Null
    };
}
