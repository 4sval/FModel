using System;
using System.IO;
using System.Linq;
using FModel.PakReader.Parsers.Class;
using FModel.PakReader.Parsers.Objects;

namespace FModel.PakReader.Parsers
{
    public sealed class LegacyPackageReader : PackageReader
    {
        public FPackageFileSummary PackageFileSummary { get; }
        public FObjectImport[] ImportMap { get; }
        public FObjectExport[] ExportMap { get; }

        public override FNameEntrySerialized[] NameMap { get; }

        public override IUExport[] DataExports { get; }
        public override FName[] DataExportTypes { get; }

        public LegacyPackageReader(string uasset, string uexp, string ubulk) : this(File.OpenRead(uasset), File.OpenRead(uexp), File.Exists(ubulk) ? File.OpenRead(ubulk) : null) { }
        public LegacyPackageReader(Stream uasset, Stream uexp, Stream ubulk) : this(new BinaryReader(uasset), new BinaryReader(uexp), ubulk) { }

        LegacyPackageReader(BinaryReader uasset, BinaryReader uexp, Stream ubulk)
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
                        "AkMediaAssetData" => new UAkMediaAssetData(this, ubulk, ExportMap.Sum(e => e.SerialSize) + PackageFileSummary.TotalHeaderSize),
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
        
    }
}
