using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using PakReader.Parsers;
using PakReader.Parsers.Class;
using PakReader.Parsers.Objects;
using PakReader.Parsers.PropertyTagData;

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
                                FModel.EJsonType.Default => GetJsonDict(Exports[i]),
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

        private Dictionary<string, object> GetJsonDict(IUExport export)
        {
            if (export != null)
            {
                var ret = new Dictionary<string, object>(export.Count);
                foreach (KeyValuePair<string, object> KvP in export)
                {
                    if (KvP.Value == null)
                        ret[KvP.Key] = null;
                    else
                        ret[KvP.Key] = KvP.Value.GetType().Name switch
                        {
                            "ByteProperty" => ((ByteProperty)KvP.Value).GetValue(),
                            "BoolProperty" => ((BoolProperty)KvP.Value).GetValue(),
                            "IntProperty" => ((IntProperty)KvP.Value).GetValue(),
                            "FloatProperty" => ((FloatProperty)KvP.Value).GetValue(),
                            "ObjectProperty" => ((ObjectProperty)KvP.Value).GetValue(),
                            "NameProperty" => ((NameProperty)KvP.Value).GetValue(),
                            "DoubleProperty" => ((DoubleProperty)KvP.Value).GetValue(),
                            "ArrayProperty" => ((ArrayProperty)KvP.Value).GetValue(),
                            "StructProperty" => ((StructProperty)KvP.Value).GetValue(),
                            "StrProperty" => ((StrProperty)KvP.Value).GetValue(),
                            "TextProperty" => ((TextProperty)KvP.Value).GetValue(),
                            "InterfaceProperty" => ((InterfaceProperty)KvP.Value).GetValue(),
                            "SoftObjectProperty" => ((SoftObjectProperty)KvP.Value).GetValue(),
                            "UInt64Property" => ((UInt64Property)KvP.Value).GetValue(),
                            "UInt32Property" => ((UInt32Property)KvP.Value).GetValue(),
                            "UInt16Property" => ((UInt16Property)KvP.Value).GetValue(),
                            "Int64Property" => ((Int64Property)KvP.Value).GetValue(),
                            "Int16Property" => ((Int16Property)KvP.Value).GetValue(),
                            "Int8Property" => ((Int8Property)KvP.Value).GetValue(),
                            "MapProperty" => ((MapProperty)KvP.Value).GetValue(),
                            "SetProperty" => ((SetProperty)KvP.Value).GetValue(),
                            "EnumProperty" => ((EnumProperty)KvP.Value).GetValue(),
                            "UObject" => ((UObject)KvP.Value).GetValue(),
                            _ => KvP.Value,
                        };
                }
                return ret;
            }
            return null;
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
