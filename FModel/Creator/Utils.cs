using FModel.Utils;
using PakReader.Pak;
using PakReader.Parsers.Class;
using PakReader.Parsers.PropertyTagData;
using SkiaSharp;
using System;

namespace FModel.Creator
{
    static class Utils
    {
        public static string GetFullPath(string partialPath)
        {
            foreach (var fileReader in Globals.CachedPakFiles.Values)
                if (fileReader.TryGetPartialKey(partialPath, out var fullPath))
                {
                    return fullPath;
                }
            return string.Empty;
        }

        public static PakPackage GetPropertyPakPackage(string value)
        {
            string path = Strings.FixPath(value);
            foreach (var fileReader in Globals.CachedPakFiles.Values)
                if (fileReader.TryGetValue(path, out var entry))
                {
                    // kinda sad to use Globals.CachedPakFileMountPoint when the mount point is already in the path ¯\_(ツ)_/¯
                    string mount = path.Substring(0, path.Length - entry.Name.Substring(0, entry.Name.LastIndexOf(".")).Length);
                    return Assets.GetPakPackage(entry, mount);
                }
            return default;
        }

        public static ArraySegment<byte>[] GetPropertyArraySegmentByte(string value)
        {
            string path = Strings.FixPath(value);
            foreach (var fileReader in Globals.CachedPakFiles.Values)
                if (fileReader.TryGetValue(path, out var entry))
                {
                    // kinda sad to use Globals.CachedPakFileMountPoint when the mount point is already in the path ¯\_(ツ)_/¯
                    string mount = path.Substring(0, path.Length - entry.Name.Substring(0, entry.Name.LastIndexOf(".")).Length);
                    return Assets.GetArraySegmentByte(entry, mount);
                }
            return default;
        }

        public static SKBitmap NewZeroedBitmap(int width, int height) => new SKBitmap(new SKImageInfo(width, height), SKBitmapAllocFlags.ZeroPixels);
        public static SKBitmap Resize(this SKBitmap me, int width, int height)
        {
            var bmp = NewZeroedBitmap(width, height);
            using var pixmap = bmp.PeekPixels();
            me.ScalePixels(pixmap, SKFilterQuality.Medium);
            return bmp;
        }

        public static SKBitmap GetObjectTexture(ObjectProperty o) => GetTexture(o.Value.Resource.OuterIndex.Resource.ObjectName.String);
        public static SKBitmap GetSoftObjectTexture(SoftObjectProperty s) => GetTexture(s.Value.AssetPathName.String);
        public static SKBitmap GetTexture(string s)
        {
            // FortniteGame/Content/Catalog/DisplayAssets/DA_BattlePassBundle_2020.uasset
            if (s != null && s.Equals("/Game/UI/Foundation/Textures/BattleRoyale/FeaturedItems/Outfit/T_UI_InspectScreen_annualPass"))
                s += "_1024";

            PakPackage p = GetPropertyPakPackage(s);
            if (p.HasExport() && !p.Equals(default))
            {
                var i = p.GetExport<UTexture2D>();
                if (i != null)
                    return SKBitmap.Decode(i.Image.Encode());

                var u = p.GetExport<UObject>();
                if (u != null)
                    if (u.TryGetValue("TextureParameterValues", out var v) && v is ArrayProperty a)
                        if (a.Value.Length > 0 && a.Value[0] is StructProperty str && str.Value is UObject o)
                            if (o.TryGetValue("ParameterValue", out var obj) && obj is ObjectProperty parameterValue)
                                return GetObjectTexture(parameterValue);
            }
            return null;
        }
    }
}
