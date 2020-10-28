using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FModel.PakReader.IO;
using FModel.PakReader.Parsers.Class;
using FModel.PakReader.Parsers.Objects;
using FModel.Utils;

namespace FModel.PakReader.Parsers
{
    public sealed class IoPackageReader : PackageReader
    {
        public readonly FIoGlobalData GlobalData;
        public readonly FPackageSummary Summary;
        public readonly FPackageObjectIndex[] ImportMap;
        public readonly FExportMapEntry[] ExportMap;

        internal List<FObjectResource> FakeImportMap;
        
        public override FNameEntrySerialized[] NameMap { get; }

        private IUExport[] _dataExports;
        private Stream _ubulk;
        public override IUExport[] DataExports {
            get
            {
                if (_dataExports == null)
                    ReadContent();
                return _dataExports;
            } 
        }

        private FName[] _dataExportTypes;
        public override FName[] DataExportTypes
        {
            get
            {
                if (_dataExportTypes == null)
                    ReadContent();
                return _dataExportTypes;
            }
        }

        private Dictionary<FPackageObjectIndex, string> _importMappings;

        public IoPackageReader(Stream uasset, Stream ubulk, FIoGlobalData globalData, FFileIoStoreReader reader, bool onlyInfo = false) : this(new BinaryReader(uasset),
            ubulk, globalData, reader, onlyInfo) { }
        public IoPackageReader(BinaryReader uasset, Stream ubulk, FIoGlobalData globalData, FFileIoStoreReader reader, bool onlyInfo = false)
        {
            Loader = uasset;
            _ubulk = ubulk;
            GlobalData = globalData;
            Summary = new FPackageSummary(this);
            
            var nameMap = new List<FNameEntrySerialized>();
            var nameHashes = new List<ulong>();
            if (Summary.NameMapNamesSize > 0)
            {
                Loader.BaseStream.Position = Summary.NameMapNamesOffset;
                var nameMapNames = Loader.ReadBytes(Summary.NameMapNamesSize);
                Loader.BaseStream.Position = Summary.NameMapHashesOffset;
                var nameMapHashes = Loader.ReadBytes(Summary.NameMapHashesSize);

                FNameEntrySerialized.LoadNameBatch(nameMap, nameHashes, nameMapNames, nameMapHashes);
            }

            NameMap = nameMap.ToArray();

            Loader.BaseStream.Position = Summary.ImportMapOffset;
            var importMapCount = (Summary.ExportMapOffset - Summary.ImportMapOffset) / /*sizeof(FPackageObjectIndex)*/ sizeof(ulong);
            ImportMap = new FPackageObjectIndex[importMapCount];
            for (int i = 0; i < importMapCount; i++)
            {
                ImportMap[i] = new FPackageObjectIndex(Loader);
            }

            Loader.BaseStream.Position = Summary.ExportMapOffset;
            var exportMapCount = (Summary.ExportBundlesOffset - Summary.ExportMapOffset) / FExportMapEntry.SIZE;
            ExportMap = new FExportMapEntry[exportMapCount];
            for (int i = 0; i < exportMapCount; i++)
            {
                ExportMap[i] = new FExportMapEntry(this);
            }

            if (!onlyInfo)
                ReadContent();
        }

        private void ReadContent()
        {
            Loader.BaseStream.Position = Summary.GraphDataOffset;
            var referencedPackagesCount = Loader.ReadInt32();
            var graphData = new (FPackageId importedPackageId, FArc[] arcs)[referencedPackagesCount];
            _importMappings = new Dictionary<FPackageObjectIndex, string>(referencedPackagesCount);
            FakeImportMap = new List<FObjectResource>();
            for (int i = 0; i < ImportMap.Length; i++)
                FakeImportMap.Add(new FObjectResource(new FName(), new FPackageIndex()));
            for (int i = 0; i < referencedPackagesCount; i++)
            {
                var importedPackageId = new FPackageId(Loader);
                var arcs = Loader.ReadTArray(() => new FArc(Loader));
                graphData[i] = (importedPackageId, arcs);
                var importedPackageName = Creator.Utils.GetFullPath(importedPackageId)
                    ?.Replace($"{Folders.GetGameName()}/Content", "Game");
                var package = Creator.Utils.GetPropertyPakPackage(importedPackageName) as IoPackage;
                if (package == null) continue;
                foreach (var export in package.Reader.ExportMap)
                {
                    var realImportIndex = Array.FindIndex(ImportMap, it => it == export.GlobalImportIndex);
                    var nextIndex = FakeImportMap.Count;
                    FakeImportMap[realImportIndex] = new FObjectResource(new FName(export.ObjectName.String), new FPackageIndex(this, -(nextIndex + 1)));
                    var outerResource = new FObjectResource(new FName(string.Concat(package.Reader.Summary.Name.String, ".", export.ObjectName.String)), new FPackageIndex());
                    FakeImportMap.Add(outerResource);
                }
            }

            var beginExportOffset = Summary.GraphDataOffset + Summary.GraphDataSize;
            var currentExportDataOffset = beginExportOffset;
            _dataExports = new IUExport[ExportMap.Length];
            _dataExportTypes = new FName[ExportMap.Length];
            for (var i = 0; i < ExportMap.Length; i++)
            {
                var exportMapEntry = ExportMap[i];
                FName exportType;

                if (GlobalData != null && GlobalData.ScriptObjectByGlobalId.TryGetValue(exportMapEntry.ClassIndex, out var scriptObject))
                {
                    exportType = scriptObject.Name;
                }
                else
                {
                    exportType = new FName("Unknown");
                }

                Loader.BaseStream.Position = currentExportDataOffset;

                if (Globals.TypeMappings.TryGetValue(exportType.String, out var properties))
                {
                    _dataExports[i] = exportType.String switch
                    {
                        "Texture2D" => new UTexture2D(this, properties, _ubulk,
                            ExportMap.Sum(e => (long) e.CookedSerialSize) + beginExportOffset),
                        "VirtualTexture2D" => new UTexture2D(this, properties, _ubulk, ExportMap.Sum(e => (long) e.CookedSerialSize) + beginExportOffset),
                        //"CurveTable" => new UCurveTable(this),
                        "DataTable" => new UDataTable(this, properties, exportType.String),
                        //"FontFace" => new UFontFace(this, ubulk),
                        "SoundWave" => new USoundWave(this, properties, _ubulk, ExportMap.Sum(e => (long) e.CookedSerialSize) + beginExportOffset),
                        //"StringTable" => new UStringTable(this),
                        //"AkMediaAssetData" => new UAkMediaAssetData(this, ubulk, ExportMap.Sum(e => e.SerialSize) + PackageFileSummary.TotalHeaderSize),
                        _ => new UObject(this, properties, type: exportType.String),
                    };
                    _dataExportTypes[i] = exportType;    
                }
                else
                {
#if DEBUG
                    var header = new FUnversionedHeader(this);
                    using var it = new FIterator(header);

                    FConsole.AppendText(string.Concat("\n", exportType.String, ": ", Summary.Name.String), "#CA6C6C", true);

                    do
                    {
                        FConsole.AppendText($"Val: {it.Current.Val} (IsNonZero: {it.Current.IsNonZero})", FColors.Yellow, true);
                    }
                    while (it.MoveNext());
#endif
                }
                currentExportDataOffset += (int) exportMapEntry.CookedSerialSize;
            }
        }

        public override string ToString() => Summary.Name.String;
    }
}