using System;
using System.IO;
using System.Linq;
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

        public PackageReader(string path) : this(path + ".uasset", path + ".uexp", path + ".ubulk") { }
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
                FName ObjectClassName;
                if (ExportMap[i].ClassIndex.IsNull)
                    ObjectClassName = DataExportTypes[i] = ReadFName(); // check if this is true, I don't know if Fortnite ever uses this
                else if (ExportMap[i].ClassIndex.IsExport)
                    ObjectClassName = DataExportTypes[i] = ExportMap[ExportMap[i].ClassIndex.AsExport].ObjectName;
                else if (ExportMap[i].ClassIndex.IsImport)
                    ObjectClassName = DataExportTypes[i] = ImportMap[ExportMap[i].ClassIndex.AsImport].ObjectName;
                else
                    throw new FileLoadException("Can't get class name"); // Shouldn't reach this unless the laws of math have bent to MagmaReef's will

                if (ObjectClassName.String.Equals("BlueprintGeneratedClass")) continue;

                var pos = Position = ExportMap[i].SerialOffset - PackageFileSummary.TotalHeaderSize;
                DataExports[i] = ObjectClassName.String switch
                {
                    "Texture2D" => new UTexture2D(this, ubulk, ExportMap.Sum(e => e.SerialSize) + PackageFileSummary.TotalHeaderSize),
                    "CurveTable" => new UCurveTable(this),
                    "DataTable" => new UDataTable(this),
                    "FontFace" => new UFontFace(this, ubulk),
                    "SoundWave" => new USoundWave(this, ubulk, ExportMap.Sum(e => e.SerialSize) + PackageFileSummary.TotalHeaderSize),
                    "StringTable" => new UStringTable(this),
                    _ => new UObject(this),
                };

#if DEBUG
                if (pos + ExportMap[i].SerialSize != Position)
                {
                    System.Diagnostics.Debug.WriteLine($"[ExportType={ObjectClassName.String}] Didn't read {ExportMap[i].ObjectName} correctly (at {Position}, should be {pos + ExportMap[i].SerialSize}, {pos + ExportMap[i].SerialSize - Position} behind)");
                }
#endif
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
            throw new FileLoadException($"Bad Name Index: {NameIndex}/{NameMap.Length} - Loader Position: {Loader.BaseStream.Position}");
        }


        public static implicit operator BinaryReader(PackageReader reader) => reader.Loader;

        public byte ReadByte() => Loader.ReadByte();
        public sbyte ReadSByte() => Loader.ReadSByte();
        public byte[] ReadBytes(int count) => Loader.ReadBytes(count);
        public string ReadFString() => Loader.ReadFString();
        public T[] ReadTArray<T>(Func<T> Getter) => Loader.ReadTArray(Getter);

        public short ReadInt16() => Loader.ReadInt16();
        public ushort ReadUInt16() => Loader.ReadUInt16();
        public int ReadInt32() => Loader.ReadInt32();
        public uint ReadUInt32() => Loader.ReadUInt32();
        public long ReadInt64() => Loader.ReadInt64();
        public ulong ReadUInt64() => Loader.ReadUInt64();
        public float ReadFloat() => Loader.ReadSingle();
        public double ReadDouble() => Loader.ReadDouble();

        public long Position { get => Loader.BaseStream.Position; set => Loader.BaseStream.Position = value; }
    }
}
