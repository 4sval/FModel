using System;
using System.IO;
using FModel.PakReader.Parsers;
using FModel.PakReader.Parsers.Class;
using FModel.PakReader.Parsers.Objects;
using Newtonsoft.Json;

namespace FModel.PakReader.Pak
{
    public sealed class PakPackage : Package
    {
        readonly ArraySegment<byte> UAsset;
        readonly ArraySegment<byte> UExp;
        readonly ArraySegment<byte> UBulk;
        
        internal PakPackage(ArraySegment<byte> asset, ArraySegment<byte> exp, ArraySegment<byte> bulk)
        {
            UAsset = asset;
            UExp = exp;
            UBulk = bulk;
            exports = new ExportList();
        }

        public override string JsonData
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
        public override FName[] ExportTypes
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

                    var p = new LegacyPackageReader(asset, exp, bulk);
                    exports.Exports = p.DataExports;
                    return exports.ExportTypes = p.DataExportTypes;
                }
                return exports.ExportTypes;
            }
        }
        public override IUExport[] Exports
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

                    var p = new LegacyPackageReader(asset, exp, bulk);
                    exports.ExportTypes = p.DataExportTypes;
                    return exports.Exports = p.DataExports;
                }
                return exports.Exports;
            }
        }
        private readonly ExportList exports;


        // hacky way to get the package to be a readonly struct, essentially a double pointer i guess
        
    }
}
