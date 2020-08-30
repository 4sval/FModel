using System;
using System.IO;
using Newtonsoft.Json;
using PakReader.Parsers;
using PakReader.Parsers.Class;
using PakReader.Parsers.Objects;

namespace PakReader.Pak
{
    public readonly struct PakPackage
    {
        readonly ArraySegment<byte> UAsset;
        readonly ArraySegment<byte> UExp;
        readonly ArraySegment<byte> UBulk;

        public string JsonData
        {
            get
            {
                if (string.IsNullOrEmpty(exports.JsonData))
                {
                    var ret = new JsonExport[Exports.Length];
                    for (int i = 0; i < ret.Length; i++)
                    {
                        ret[i] = new JsonExport
                        {
                            ExportType = ExportTypes[i].String,
                            ExportValue = (FModel.EJsonType)FModel.Properties.Settings.Default.AssetsJsonType switch
                            {
                                FModel.EJsonType.Default => Exports[i].GetJsonDict(),
                                _ => Exports[i]
                            }
                        };
                    }
                    return exports.JsonData = JsonConvert.SerializeObject(ret, Formatting.Indented);
                }
                return exports.JsonData;
            }
        }
        public FName[] ExportTypes
        {
            get
            {
                if (exports.ExportTypes == null)
                {
                    using var asset = new MemoryStream(UAsset.Array, UAsset.Offset, UAsset.Count);
                    using var exp = new MemoryStream(UExp.Array, UExp.Offset, UExp.Count);
                    using var bulk = UBulk != null ? new MemoryStream(UBulk.Array, UBulk.Offset, UBulk.Count) : null;
                    asset.Position = 0;
                    exp.Position = 0;
                    if (bulk != null)
                        bulk.Position = 0;

                    var p = new PackageReader(asset, exp, bulk);
                    exports.Exports = p.DataExports;
                    return exports.ExportTypes = p.DataExportTypes;
                }
                return exports.ExportTypes;
            }
        }
        public IUExport[] Exports
        {
            get
            {
                if (exports.Exports == null)
                {
                    using var asset = new MemoryStream(UAsset.Array, UAsset.Offset, UAsset.Count);
                    using var exp = new MemoryStream(UExp.Array, UExp.Offset, UExp.Count);
                    using var bulk = UBulk != null ? new MemoryStream(UBulk.Array, UBulk.Offset, UBulk.Count) : null;
                    asset.Position = 0;
                    exp.Position = 0;
                    if (bulk != null)
                        bulk.Position = 0;

                    var p = new PackageReader(asset, exp, bulk);
                    exports.ExportTypes = p.DataExportTypes;
                    return exports.Exports = p.DataExports;
                }
                return exports.Exports;
            }
        }
        readonly ExportList exports;

        internal PakPackage(ArraySegment<byte> asset, ArraySegment<byte> exp, ArraySegment<byte> bulk)
        {
            UAsset = asset;
            UExp = exp;
            UBulk = bulk;
            exports = new ExportList();
        }

        public T GetExport<T>() where T : IUExport
        {
            var exports = Exports;
            for (int i = 0; i < exports.Length; i++)
            {
                if (exports[i] is T)
                    return (T)exports[i];
            }
            return default;
        }
        public T GetIndexedExport<T>(int index) where T : IUExport
        {
            var exports = Exports;
            int foundCount = 0;
            for (int i = 0; i < exports.Length; i++)
            {
                if (exports[i] is T)
                {
                    if (foundCount == index)
                        return (T)exports[i];
                    foundCount++;
                }
            }
            return default;
        }
        public T GetTypedExport<T>(string exportType) where T : IUExport
        {
            int index = 0;
            var exportTypes = ExportTypes;
            for (int i = 0; i < exportTypes.Length; i++)
            {
                if (exportTypes[i].String == exportType)
                    index = i;
            }
            return (T)Exports[index];
        }

        public bool HasExport() => exports != null;

        // hacky way to get the package to be a readonly struct, essentially a double pointer i guess
        sealed class ExportList
        {
            public string JsonData;
            public FName[] ExportTypes;
            public IUExport[] Exports;
        }

        sealed class JsonExport
        {
            public string ExportType;
            public object ExportValue;
        }
    }
}
