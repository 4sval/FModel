using System.IO;
using System.Runtime.CompilerServices;

namespace FModel.PakReader
{
    public abstract class ReaderEntry
    {
        public abstract string Name { get; }
        public abstract long UncompressedSize { get; }
        public abstract long Offset { get; }
        public abstract long Size { get; }
        public abstract int StructSize { get; }
        public abstract uint CompressionMethodIndex { get; }
        public abstract bool Encrypted { get; }
        public abstract string ContainerName { get; }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsUE4Package() => Name[GetNameDefaultExt().LastIndexOf(".")..].Equals(".uasset");
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsLocres() => Name[GetNameDefaultExt().LastIndexOf(".")..].Equals(".locres");
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsUE4Map() => Name[GetNameDefaultExt().LastIndexOf(".")..].Equals(".umap");
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsUE4Font() => Name[GetNameDefaultExt().LastIndexOf(".")..].Equals(".ufont");
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetExtension() => Name[GetNameDefaultExt().LastIndexOf(".")..];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetPathWithoutFile()
        {
            int stop = Name.LastIndexOf("/");
            if (stop <= -1)
                stop = 0;
            return Name.Substring(0, stop);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetPathWithoutExtension() => Name.Substring(0, GetNameDefaultExt().LastIndexOf("."));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetNameWithExtension() => Name.Substring(Name.LastIndexOf("/") + 1);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetNameWithoutExtension()
        {
            int start = Name.LastIndexOf("/") + 1;
            int stop = GetNameDefaultExt().LastIndexOf(".") - start;
            return Name.Substring(start, stop);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetFirstFolder() => Name.Substring(Name.StartsWith('/') ? 1 : 0, Name.IndexOf('/'));
        
        public ReaderEntry Uexp = null;
        public ReaderEntry Ubulk = null;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasUexp() => Uexp != null;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasUbulk() => Ubulk != null;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetNameDefaultExt() => !Path.HasExtension(Name) ? Name + ".uasset" : Name;
        
        public override string ToString() => Name;
    }
}