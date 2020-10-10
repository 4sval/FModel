﻿using PakReader.Parsers.PropertyTagData;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace PakReader.Parsers.Class
{
    public sealed class UFontFace : IUExport
    {
        /** Font data v3. This points to a font face asset. */
        public UObject FontFaceAsset { get; }

        internal UFontFace(PackageReader reader, Stream ufont)
        {
            FontFaceAsset = new UObject(reader, true);
            foreach (KeyValuePair<string, object> prop in FontFaceAsset)
            {
                if (prop.Key.Equals("SourceFilename") && prop.Value is StrProperty str)
                {
                    string FontFilename = Path.GetFileName(str.Value);
                    string folder = FModel.Properties.Settings.Default.OutputPath + "\\Fonts\\";

                    if (ufont != null)
                    {
                        using var fileStream = new FileStream(folder + FontFilename, FileMode.Create, FileAccess.Write);
                        ufont.CopyTo(fileStream);
                    }
                }
            }
        }

        public object this[string key] => FontFaceAsset[key];
        public IEnumerable<string> Keys => FontFaceAsset.Keys;
        public IEnumerable<object> Values => FontFaceAsset.Values;
        public int Count => FontFaceAsset.Count;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(string key) => FontFaceAsset.ContainsKey(key);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => FontFaceAsset.GetEnumerator();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator() => FontFaceAsset.GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(string key, out object value) => FontFaceAsset.TryGetValue(key, out value);
    }
}
