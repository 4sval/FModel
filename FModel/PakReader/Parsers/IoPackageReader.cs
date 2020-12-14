using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using FModel.Logger;
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
        public readonly FExportBundle ExportBundle;

        internal List<FObjectResource> FakeImportMap;
        
        public override FNameEntrySerialized[] NameMap { get; }

        private IUExport[] _dataExports;
        private readonly Stream _ubulk;
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

        public IoPackageReader(Stream uasset, Stream ubulk, FIoGlobalData globalData, bool onlyInfo = false) : this(new BinaryReader(uasset),
            ubulk, globalData, onlyInfo) { }
        public IoPackageReader(BinaryReader uasset, Stream ubulk, FIoGlobalData globalData, bool onlyInfo = false)
        {
            Loader = uasset;
            _ubulk = ubulk;
            GlobalData = globalData;
            Summary = new FPackageSummary(this);
            
            var nameMap = new List<FNameEntrySerialized>();
            var nameHashes = new List<ulong>();
            if (Summary.NameMapNamesSize > 0)
            {
                Loader.BaseStream.Seek(Summary.NameMapNamesOffset, SeekOrigin.Begin);
                var nameMapNames = Loader.ReadBytes(Summary.NameMapNamesSize);
                Loader.BaseStream.Seek(Summary.NameMapHashesOffset, SeekOrigin.Begin);
                var nameMapHashes = Loader.ReadBytes(Summary.NameMapHashesSize);

                FNameEntrySerialized.LoadNameBatch(nameMap, nameHashes, nameMapNames, nameMapHashes);
            }

            NameMap = nameMap.ToArray();

            Loader.BaseStream.Seek(Summary.ImportMapOffset, SeekOrigin.Begin);
            var importMapCount = (Summary.ExportMapOffset - Summary.ImportMapOffset) / /*sizeof(FPackageObjectIndex)*/ sizeof(ulong);
            ImportMap = new FPackageObjectIndex[importMapCount];
            for (int i = 0; i < importMapCount; i++)
            {
                ImportMap[i] = new FPackageObjectIndex(Loader);
            }

            Loader.BaseStream.Seek(Summary.ExportMapOffset, SeekOrigin.Begin);
            var exportMapCount = (Summary.ExportBundlesOffset - Summary.ExportMapOffset) / FExportMapEntry.SIZE;
            ExportMap = new FExportMapEntry[exportMapCount];
            for (int i = 0; i < exportMapCount; i++)
            {
                ExportMap[i] = new FExportMapEntry(this);
            }
            
            ExportBundle = new FExportBundle(this);

            if (!onlyInfo)
                ReadContent();
        }

        private void ReadContent()
        {
            Loader.BaseStream.Seek(Summary.GraphDataOffset, SeekOrigin.Begin);
            var referencedPackagesCount = Loader.ReadInt32();
            var graphData = new (FPackageId importedPackageId, FArc[] arcs)[referencedPackagesCount];
            FakeImportMap = new List<FObjectResource>();
            for (int i = 0; i < ImportMap.Length; i++)
                FakeImportMap.Add(new FObjectResource(new FName(), new FPackageIndex()));
            for (int i = 0; i < referencedPackagesCount; i++)
            {
                var importedPackageId = new FPackageId(Loader);
                var arcs = Loader.ReadTArray(() => new FArc(Loader));
                graphData[i] = (importedPackageId, arcs);
                string importedPackageName = Creator.Utils.GetFullPath(importedPackageId);
                {
                    string gname = Folders.GetGameName();
                    if (importedPackageName.StartsWith($"/{gname}/Plugins/GameFeatures"))
                    {
                        importedPackageName = importedPackageName.Replace($"{gname}/Plugins/GameFeatures/", "").Replace("/Content", "");
                    }
                    else if (importedPackageName.StartsWith($"/{gname}/Content"))
                    {
                        importedPackageName = importedPackageName.Replace($"{Folders.GetGameName()}/Content", "Game");
                    }
                }
                if (!(Creator.Utils.GetPropertyPakPackage(importedPackageName) is IoPackage package)) continue;
                foreach (var export in package.Reader.ExportMap)
                {
                    var realImportIndex = Array.FindIndex(ImportMap, it => it == export.GlobalImportIndex);
                    if (realImportIndex > -1)
                    {
                        FakeImportMap[realImportIndex] = new FObjectResource(new FName(export.ObjectName.String), new FPackageIndex(this, -(FakeImportMap.Count + 1)));
                        FakeImportMap.Add(new FObjectResource(new FName(package.Reader.Summary.Name.String), new FPackageIndex()));
                    }
                }
            }

            var beginExportOffset = Summary.GraphDataOffset + Summary.GraphDataSize;
            var currentExportDataOffset = beginExportOffset;
            _dataExports = new IUExport[ExportMap.Length];
            _dataExportTypes = new FName[ExportMap.Length];

            var exportOrder = ExportBundle.GetExportOrder();
            
            foreach (var i in exportOrder)
            {
                var exportMapEntry = ExportMap[i];
                FPackageObjectIndex trigger;
                if (exportMapEntry.ClassIndex.IsExport)
                    trigger = exportMapEntry.SuperIndex;
                else if (exportMapEntry.ClassIndex.IsImport)
                    trigger = exportMapEntry.ClassIndex;
                else
                    throw new FileLoadException("Can't get class name");

                FName exportType;
                if (GlobalData != null && GlobalData.ScriptObjectByGlobalId.TryGetValue(trigger, out var scriptObject))
                {
                    exportType = scriptObject.Name;
                }
                else
                {
                    exportType = new FName("Unknown");
                }

                Loader.BaseStream.Seek(currentExportDataOffset, SeekOrigin.Begin);
                _dataExportTypes[i] = exportType;
                try
                {
                    if (Globals.TypeMappings.TryGetValue(exportType.String, out var properties))
                    {
                        _dataExports[i] = exportType.String switch
                        {
                            "Texture2D" => new UTexture2D(this, properties, _ubulk, ExportMap.Sum(e => (long)e.CookedSerialSize) + beginExportOffset),
                            "TextureCube" => new UTexture2D(this, properties, _ubulk, ExportMap.Sum(e => (long)e.CookedSerialSize) + beginExportOffset),
                            "VirtualTexture2D" => new UTexture2D(this, properties, _ubulk, ExportMap.Sum(e => (long)e.CookedSerialSize) + beginExportOffset),
                            "CurveTable" => new UCurveTable(this, properties),
                            "DataTable" => new UDataTable(this, properties, exportType.String),
                            "SoundWave" => new USoundWave(this, properties, _ubulk, ExportMap.Sum(e => (long)e.CookedSerialSize) + beginExportOffset),
                            _ => new UObject(this, properties, type: exportType.String),
                        };
                    }
                    else
                    {
                        _dataExports[i] = new UObject();
#if DEBUG
                        try
                        {
                            var header = new FUnversionedHeader(this);
                            if (!header.HasValues)
                                continue;
                            using var it = new FIterator(header);
                            FConsole.AppendText(string.Concat("\n", exportType.String, ": ", Summary.Name.String), "#CA6C6C", true);

                            do
                            {
                                FConsole.AppendText($"Val: {it.Current.Val} (IsNonZero: {it.Current.IsNonZero})", FColors.Yellow, true);
                            }
                            while (it.MoveNext());
                        }
                        catch (FileLoadException)
                        {
                            continue;
                        }
#endif
                    }
                }
                catch (Exception e)
                {
                    DebugHelper.WriteLine("Failed to read export {0} ({1}) of type {2}", exportMapEntry.ObjectName.String, i, exportType.String);
                    DebugHelper.WriteException(e);
                }
                

                currentExportDataOffset += (int) exportMapEntry.CookedSerialSize;
            }
        }

        public override string ToString() => Summary.Name.String;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FExportBundleHeader
    {
        public readonly uint FirstEntryIndex;
        public readonly uint EntryCount;

        public FExportBundleHeader(BinaryReader reader)
        {
            FirstEntryIndex = reader.ReadUInt32();
            EntryCount = reader.ReadUInt32();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FExportBundleEntry
    {
        public readonly uint LocalExportIndex;
        public readonly EExportCommandType CommandType;

        public FExportBundleEntry(BinaryReader reader)
        {
            LocalExportIndex = reader.ReadUInt32();
            CommandType = (EExportCommandType) reader.ReadUInt32();
        }
    }

    public enum EExportCommandType : uint
    {
        ExportCommandType_Create,
        ExportCommandType_Serialize,
        ExportCommandType_Count
    }

    public class FExportBundle
    {
        public readonly FExportBundleHeader Header;
        public readonly FExportBundleEntry[] Entries;

        public FExportBundle(PackageReader reader)
        {
            Header = new FExportBundleHeader(reader);
            Entries = new FExportBundleEntry[Header.EntryCount];
            for (var i = 0; i < Header.EntryCount; i++)
            {
                Entries[i] = new FExportBundleEntry(reader);
            }
        }

        public List<uint> GetExportOrder()
        {
           return Entries.Where(it => it.CommandType == EExportCommandType.ExportCommandType_Serialize)
                .Select(it => Math.Min(Header.EntryCount - 1, it.LocalExportIndex)).ToList();
        }
    }
}
