using FModel.Utils;
using SkiaSharp;
using System;
using System.Runtime.CompilerServices;
using FModel.PakReader;
using FModel.PakReader.IO;
using FModel.PakReader.Parsers.Class;
using FModel.PakReader.Parsers.PropertyTagData;

namespace FModel.Creator
{
    static class Utils
    {
        public static string GetFullPath(FPackageId id)
        {
            foreach (var ioStore in Globals.CachedIoStores.Values)
            {
                if (ioStore.Chunks.TryGetValue(id.Id, out string path))
                {
                    return ioStore.MountPoint + path;
                }
            }

            return null;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetFullPath(string partialPath)
        {
            foreach (var fileReader in Globals.CachedPakFiles.Values)
                if (fileReader.TryGetPartialKey(partialPath, out var fullPath))
                {
                    return fullPath;
                }

            foreach (var ioStoreReader in Globals.CachedIoStores.Values)
            {
                if (ioStoreReader.TryGetPartialKey(partialPath, out var fullPath))
                {
                    return fullPath;
                }
            }
            return string.Empty;
        }

        public static Package GetPropertyPakPackage(string value)
        {
            string path = Strings.FixPath(value);
            foreach (var fileReader in Globals.CachedPakFiles.Values)
                if (fileReader.TryGetCaseInsensiteveValue(path, out var entry))
                {
                    // kinda sad to use Globals.CachedPakFileMountPoint when the mount point is already in the path ¯\_(ツ)_/¯
                    string mount = path.Substring(0, path.Length - entry.Name.Substring(0, entry.Name.LastIndexOf('.')).Length);
                    return Assets.GetPackage(entry, mount);
                }
            foreach (var ioStoreReader in Globals.CachedIoStores.Values)
                if (ioStoreReader.TryGetCaseInsensiteveValue(path, out var entry))
                {
                    // kinda sad to use Globals.CachedPakFileMountPoint when the mount point is already in the path ¯\_(ツ)_/¯
                    string mount = path.Substring(0, path.Length - entry.Name.Substring(0, entry.Name.LastIndexOf('.')).Length);
                    return Assets.GetPackage(entry, mount);
                }
            return default;
        }

        public static ArraySegment<byte>[] GetPropertyArraySegmentByte(string value)
        {
            string path = Strings.FixPath(value);
            foreach (var fileReader in Globals.CachedPakFiles.Values)
                if (fileReader.TryGetCaseInsensiteveValue(path, out var entry))
                {
                    // kinda sad to use Globals.CachedPakFileMountPoint when the mount point is already in the path ¯\_(ツ)_/¯
                    string mount = path.Substring(0, path.Length - entry.Name.Substring(0, entry.Name.LastIndexOf('.')).Length);
                    return Assets.GetArraySegmentByte(entry, mount);
                }
            foreach (var ioStoreReader in Globals.CachedIoStores.Values)
                if (ioStoreReader.TryGetCaseInsensiteveValue(path, out var entry))
                {
                    // kinda sad to use Globals.CachedPakFileMountPoint when the mount point is already in the path ¯\_(ツ)_/¯
                    string mount = path.Substring(0, path.Length - entry.Name.Substring(0, entry.Name.LastIndexOf('.')).Length);
                    return Assets.GetArraySegmentByte(entry, mount);
                }
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SKBitmap NewZeroedBitmap(int width, int height) => new SKBitmap(new SKImageInfo(width, height), SKBitmapAllocFlags.ZeroPixels);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            if (s != null)
            {
                if (s.Equals("/Game/UI/Foundation/Textures/BattleRoyale/FeaturedItems/Outfit/T_UI_InspectScreen_annualPass"))
                    s += "_1024";
                else if (s.Equals("/Game/UI/Foundation/Textures/BattleRoyale/BattlePass/T-BattlePass-Season14-Tile") || s.Equals("/Game/UI/Foundation/Textures/BattleRoyale/BattlePass/T-BattlePassWithLevels-Season14-Tile"))
                    s += "_1";
                else if (s.Equals("/Game/UI/Textures/assets/cosmetics/skins/headshot/Skin_Headshot_WolfsBlood_UIT"))
                    s = "/Game/UI/Textures/assets/cosmetics/skins/headshot/Skin_Headshot_Wolfsblood_UIT";
                else if (s.Equals("/Game/UI/Textures/assets/cosmetics/skins/headshot/Skin_Headshot_Timeweaver_UIT"))
                    s = "/Game/UI/Textures/assets/cosmetics/skins/headshot/Skin_Headshot_TimeWeaver_UIT";
            }

            var p = GetPropertyPakPackage(s);
            if (p != null && p.HasExport() && !p.Equals(default))
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
