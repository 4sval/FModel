using System;
using System.IO;
using PakReader.Parsers;
using PakReader.Parsers.Class;

namespace PakReader.Pak
{
    public readonly struct PakPackage
    {
        readonly ArraySegment<byte> UAsset;
        readonly ArraySegment<byte> UExp;
        readonly ArraySegment<byte> UBulk;

        public string[] ExportTypes
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
                    exports.Exports = p.Exports;
                    return exports.ExportTypes = p.ExportTypes;
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
                    exports.ExportTypes = p.ExportTypes;
                    return exports.Exports = p.Exports;
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

        public string GetFirstExportType() => ExportTypes[0];

        public IUExport[] GetAllExports() => Exports;
        public IUExport GetFirstExport() => Exports[0];

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

        public bool HasExport() => exports != null;

        // hacky way to get the package to be a readonly struct, essentially a double pointer i guess
        sealed class ExportList
        {
            public IUExport[] Exports;
            public string[] ExportTypes;
        }
    }
}
