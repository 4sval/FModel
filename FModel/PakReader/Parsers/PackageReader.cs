using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using PakReader.Parsers.Class;
using PakReader.Parsers.Objects;

namespace PakReader.Parsers
{
    public sealed class PackageReader
    {
        BinaryReader Loader { get; }

        public FPackageFileSummary PackageFileSummary { get; }
        FNameEntrySerialized[] NameMap { get; }
        public FObjectImport[] ImportMap { get; }
        public FObjectExport[] ExportMap { get; }

        public IUExport[] DataExports { get; }
        public FName[] DataExportTypes { get; }

        public PackageReader(string uasset, string uexp, string ubulk) : this(File.OpenRead(uasset), File.OpenRead(uexp), File.Exists(ubulk) ? File.OpenRead(ubulk) : null) { }
        public PackageReader(Stream uasset, Stream uexp, Stream ubulk) : this(new BinaryReader(uasset), new BinaryReader(uexp), ubulk) { }

        PackageReader(BinaryReader uasset, BinaryReader uexp, Stream ubulk)
        {
            Loader = uasset;
            PackageFileSummary = new FPackageFileSummary(Loader);

            NameMap = SerializeNameMap();
            ImportMap = SerializeImportMap();
            ExportMap = SerializeExportMap();
            DataExports = new IUExport[ExportMap.Length];
            DataExportTypes = new FName[ExportMap.Length];
            Loader = uexp;
            for(int i = 0; i < ExportMap.Length; i++)
            {
                FObjectExport Export = ExportMap[i];
                {
                    FName ExportType;
                    if (Export.ClassIndex.IsNull)
                        ExportType = DataExportTypes[i] = ReadFName(); // check if this is true, I don't know if Fortnite ever uses this
                    else if (Export.ClassIndex.IsExport)
                        ExportType = DataExportTypes[i] = ExportMap[Export.ClassIndex.AsExport].SuperIndex.Resource.ObjectName;
                    else if (Export.ClassIndex.IsImport)
                        ExportType = DataExportTypes[i] = ImportMap[Export.ClassIndex.AsImport].ObjectName;
                    else
                        throw new FileLoadException("Can't get class name"); // Shouldn't reach this unless the laws of math have bent to MagmaReef's will

                    var pos = Position = Export.SerialOffset - PackageFileSummary.TotalHeaderSize;
                    DataExports[i] = ExportType.String switch
                    {
                        "Texture2D" => new UTexture2D(this, ubulk, ExportMap.Sum(e => e.SerialSize) + PackageFileSummary.TotalHeaderSize),
                        "VirtualTexture2D" => new UTexture2D(this, ubulk, ExportMap.Sum(e => e.SerialSize) + PackageFileSummary.TotalHeaderSize),
                        "CurveTable" => new UCurveTable(this),
                        "DataTable" => new UDataTable(this),
                        "FontFace" => new UFontFace(this, ubulk),
                        "SoundWave" => new USoundWave(this, ubulk, ExportMap.Sum(e => e.SerialSize) + PackageFileSummary.TotalHeaderSize),
                        "StringTable" => new UStringTable(this),
                        //"AkAudioEvent" => new UAkAudioEvent(this),
                        _ => new UObject(this),
                    };

#if DEBUG
                    if (pos + Export.SerialSize != Position)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ExportType={ExportType.String}] Didn't read {Export.ObjectName} correctly (at {Position}, should be {pos + Export.SerialSize}, {pos + Export.SerialSize - Position} behind)");
                    }
#endif
                }
            }
            return;
        }

        FNameEntrySerialized[] SerializeNameMap()
        {
            if (PackageFileSummary.NameCount > 0)
            {
                Loader.BaseStream.Position = PackageFileSummary.NameOffset;

                var OutNameMap = new FNameEntrySerialized[PackageFileSummary.NameCount];
                for (int NameMapIdx = 0; NameMapIdx < PackageFileSummary.NameCount; ++NameMapIdx)
                {
                    // Read the name entry from the file.
                    OutNameMap[NameMapIdx] = new FNameEntrySerialized(Loader);
                }
                return OutNameMap;
            }
            return Array.Empty<FNameEntrySerialized>();
        }

        FObjectImport[] SerializeImportMap()
        {
            if (PackageFileSummary.ImportCount > 0)
            {
                Loader.BaseStream.Position = PackageFileSummary.ImportOffset;

                var OutImportMap = new FObjectImport[PackageFileSummary.ImportCount];
                for (int ImportMapIdx = 0; ImportMapIdx < PackageFileSummary.ImportCount; ++ImportMapIdx)
                {
                    OutImportMap[ImportMapIdx] = new FObjectImport(this);
                }
                return OutImportMap;
            }
            return Array.Empty<FObjectImport>();
        }

        FObjectExport[] SerializeExportMap()
        {
            if (PackageFileSummary.ExportCount > 0)
            {
                Loader.BaseStream.Position = PackageFileSummary.ExportOffset;

                var OutExportMap = new FObjectExport[PackageFileSummary.ExportCount];
                for (int ExportMapIdx = 0; ExportMapIdx < PackageFileSummary.ExportCount; ++ExportMapIdx)
                {
                    OutExportMap[ExportMapIdx] = new FObjectExport(this);
                }
                return OutExportMap;
            }
            return Array.Empty<FObjectExport>();
        }

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

        public long Position { get => Loader.BaseStream.Position; set => Loader.BaseStream.Position = value; }
    }
}
