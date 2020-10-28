using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using FModel.PakReader.Parsers.Objects;

namespace FModel.PakReader.Pak
{
    public class PakIndex : IEnumerable<string>
    {
        public bool CacheFiles { get; }
        public bool CaseSensitive { get; }
        public PakFilter Filter { get; }
        readonly List<PakFileReader> PakFiles = new List<PakFileReader>();

        public PakIndex(bool cacheFiles, bool caseSensitive = true) : this(cacheFiles, caseSensitive, null) { }
        public PakIndex(bool cacheFiles, bool caseSensitive = true, IEnumerable<string> filter = null) : this(cacheFiles, caseSensitive, new PakFilter(filter, caseSensitive)) { }
        public PakIndex(bool cacheFiles, bool caseSensitive = true, PakFilter filter = null)
        {
            CacheFiles = cacheFiles;
            CaseSensitive = caseSensitive;
            Filter = filter;
            if (CacheFiles)
                CachedFiles = new Dictionary<string, ArraySegment<byte>>();
        }

        public PakIndex(string path, bool cacheFiles, bool caseSensitive = true) : this(path, cacheFiles, caseSensitive, null) { }
        public PakIndex(string path, bool cacheFiles, bool caseSensitive = true, IEnumerable<string> filter = null) : this(path, cacheFiles, caseSensitive, new PakFilter(filter, caseSensitive)) { }
        public PakIndex(string path, bool cacheFiles, bool caseSensitive = true, PakFilter filter = null) : this(Directory.EnumerateFiles(path, "*.pak"), cacheFiles, caseSensitive, filter) { }

        public PakIndex(IEnumerable<string> files, bool cacheFiles, bool caseSensitive = true) : this(files, cacheFiles, caseSensitive, null) { }
        public PakIndex(IEnumerable<string> files, bool cacheFiles, bool caseSensitive = true, IEnumerable<string> filter = null) : this(files, cacheFiles, caseSensitive, new PakFilter(filter, caseSensitive)) { }
        public PakIndex(IEnumerable<string> files, bool cacheFiles, bool caseSensitive = true, PakFilter filter = null)
        {
            CacheFiles = cacheFiles;
            CaseSensitive = caseSensitive;
            Filter = filter;
            if (CacheFiles)
                CachedFiles = new Dictionary<string, ArraySegment<byte>>();
            foreach (var file in files)
            {
                PakFiles.Add(new PakFileReader(file, caseSensitive));
            }
        }

        public PakIndex(IEnumerable<Stream> streams, bool cacheFiles, bool caseSensitive = true) : this(streams, cacheFiles, caseSensitive, null) { }
        public PakIndex(IEnumerable<Stream> streams, bool cacheFiles, bool caseSensitive = true, IEnumerable<string> filter = null) : this(streams, cacheFiles, caseSensitive, new PakFilter(filter, caseSensitive)) { }
        public PakIndex(IEnumerable<Stream> streams, bool cacheFiles, bool caseSensitive = true, PakFilter filter = null)
        {
            CacheFiles = cacheFiles;
            CaseSensitive = caseSensitive;
            Filter = filter;
            if (CacheFiles)
                CachedFiles = new Dictionary<string, ArraySegment<byte>>();
            foreach (var stream in streams)
            {
                PakFiles.Add(new PakFileReader(string.Empty, stream, caseSensitive));
            }
        }

        public void AddPak(string path, byte[] key = null)
        {
            var reader = new PakFileReader(path, CaseSensitive);
            if (key != null)
                reader.ReadIndex(key, Filter);
            PakFiles.Add(reader);
        }
        public void AddPak(Stream stream, byte[] key = null)
        {
            var reader = new PakFileReader(string.Empty, stream, CaseSensitive);
            if (key != null)
                reader.ReadIndex(key, Filter);
            PakFiles.Add(reader);
        }

        public int UseKey(byte[] key)
        {
            int n = 0;
            foreach (var pak in PakFiles)
            {
                if (!pak.Initialized)
                {
                    if (pak.TryReadIndex(key, Filter))
                        n++;
                }
            }
            return n;
        }

        public int UseKeys(IEnumerable<byte[]> keys)
        {
            int n = 0;
            foreach(var key in keys)
                n += UseKey(key);
            return n;
        }

        public int UseKey(FGuid EncryptionGuid, byte[] key)
        {
            int n = 0;
            foreach (var pak in PakFiles)
            {
                if (!pak.Initialized && EncryptionGuid == pak.Info.EncryptionKeyGuid)
                {
                    if (pak.TryReadIndex(key, Filter))
                        n++;
                }
            }
            return n;
        }

        public int UseKey(FGuid EncryptionGuid, string key)
        {
            int n = 0;
            foreach (var pak in PakFiles)
            {
                if (!pak.Initialized && EncryptionGuid == pak.Info.EncryptionKeyGuid)
                {
                    if (pak.TryReadIndex(key.ToBytesKey(), Filter))
                        n++;
                }
            }
            return n;
        }

        readonly Dictionary<string, ArraySegment<byte>> CachedFiles;
        public ArraySegment<byte> GetFile(string path)
        {
            TryGetFile(path, out var ret);
            return ret;
        }
        public bool TryGetFile(string path, out ArraySegment<byte> ret)
        {
            if (!CaseSensitive)
                path = path.ToLowerInvariant();
            if (CacheFiles && CachedFiles.TryGetValue(path, out ret))
                return true;
            /*foreach (var pak in PakFiles)
            {
                if (!pak.Initialized) continue;
                if (path.IndexOf(pak.MountPoint, 0) == 0) // same as StartsWith but more performant
                {
                    if (pak.TryGetFile(path.Substring(pak.MountPoint.Length), out ret))
                    {
                        if (CacheFiles)
                            CachedFiles[path] = ret;
                        return true;
                    }
                }
            }*/
            ret = null;
            return false;
        }

        Dictionary<string, PakPackage> Packages;
        public PakPackage GetPackage(string path)
        {
            TryGetPackage(path, out var ret);
            return ret;
        }
        public bool TryGetPackage(string path, out PakPackage package)
        {
            if (Packages == null)
                Packages = new Dictionary<string, PakPackage>();
            if (!Packages.TryGetValue(path, out package))
            {
                var uasset = GetFile(path + ".uasset");
                var uexp = GetFile(path + ".uexp");
                var ubulk = GetFile(path + ".ubulk");
                if (uasset == null || uexp == null) return false; // Can't have a package without uassets or uexps
                Packages[path] = package = new PakPackage(uasset, uexp, ubulk);
            }
            return true;
        }

        public IEnumerator<string> GetEnumerator()
        {
            foreach(var pak in PakFiles)
            {
                if (!pak.Initialized)
                    continue;
                foreach(var file in pak)
                {
                    yield return pak.MountPoint + file.Key;
                }
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
