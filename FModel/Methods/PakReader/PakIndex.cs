using SkiaSharp;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace PakReader
{
    public class PakIndex : IEnumerable<(string Path, PakPackage Package)>
    {
        ConcurrentDictionary<string, PakPackage> index = new ConcurrentDictionary<string, PakPackage>();

        static (string Path, string Extension) GetPath(string inp)
        {
            int extInd = inp.LastIndexOf('.');
            return (inp.Substring(0, extInd).ToLowerInvariant(), inp.Substring(extInd + 1).ToLowerInvariant());
        }

        static PakPackage InsertEntry(BasePakEntry entry, PakPackage package, string extension, PakReader reader)
        {
            package.Extensions[extension] = (entry, reader);
            return package;
        }

        public void AddPak(string file, byte[] aes = null) => AddPak(new PakReader(file, aes));
        public void AddPak(Stream stream, string name, byte[] aes = null) => AddPak(new PakReader(stream, name, aes));
        public void AddPak(PakReader reader)
        {
            foreach (var info in reader.FileInfos)
            {
                var path = GetPath(info.Name);
                if (!index.ContainsKey(path.Path))
                {
                    var pak = index[path.Path] = new PakPackage();
                    InsertEntry(info, pak, path.Extension, reader);
                }
                else
                {
                    InsertEntry(info, index[path.Path], path.Extension, reader);
                }
            }
        }

        public PakPackage GetPackage(string name) => index.TryGetValue(name.ToLowerInvariant(), out PakPackage ret) ? ret : null;

        public IEnumerator<(string Path, PakPackage Package)> GetEnumerator()
        {
            foreach (var kv in index)
            {
                yield return (kv.Key, kv.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public sealed class PakPackage
    {
        public SortedList<string, (BasePakEntry Entry, PakReader Reader)> Extensions = new SortedList<string, (BasePakEntry Entry, PakReader Reader)>();

        public ExportObject[] Exports => GetAssetReader(true)?.Exports;

        public AssetReader GetAssetReader(bool ignoreErrors = false) => Exportable ? new AssetReader(GetPackageStream("uasset"), GetPackageStream("uexp"), GetPackageStream("ubulk"), ignoreErrors) : null;

        public bool Exportable => HasExtension("uasset") && HasExtension("uexp");

        public bool HasExtension(string extension) => Extensions.ContainsKey(extension);

        public Stream GetPackageStream(string extension) =>
            Extensions.TryGetValue(extension, out var ext) ?
                ext.Reader.GetPackageStream(ext.Entry) :
                null;

        public UObject GetUObject() => Exports[0] as UObject;

        public SKImage GetTexture()
        {
            return Exports[0] is Texture2D tex ? ImageExporter.GetImage(tex.textures[0].mips[0], tex.textures[0].pixel_format) : null;
        }
    }
}
