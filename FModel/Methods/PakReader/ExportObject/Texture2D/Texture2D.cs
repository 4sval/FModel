using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using static PakReader.AssetReader;

namespace PakReader
{
    public sealed class Texture2D : ExportObject, IDisposable
    {
        public UObject base_object;
        public bool cooked;
        internal FTexturePlatformData[] textures;

        internal Texture2D(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map, int asset_file_size, long export_size, BinaryReader ubulk)
        {
            var uobj = new UObject(reader, name_map, import_map, "Texture2D", true);

            new FStripDataFlags(reader); // no idea
            new FStripDataFlags(reader); // why are there two

            List<FTexturePlatformData> texs = new List<FTexturePlatformData>();
            cooked = reader.ReadUInt32() == 1;
            if (cooked)
            {
                string pixel_format = read_fname(reader, name_map);
                while (pixel_format != "None")
                {
                    long skipOffset = reader.ReadInt64();
                    var texture = new FTexturePlatformData(reader, ubulk, export_size + asset_file_size);
                    if (reader.BaseStream.Position + asset_file_size != skipOffset)
                    {
                        throw new IOException("Texture read incorrectly");
                    }
                    texs.Add(texture);
                    pixel_format = read_fname(reader, name_map);
                }
            }

            textures = texs.ToArray();
        }

        public SKImage GetImage() => ImageExporter.GetImage(textures[0].mips[0], textures[0].pixel_format);

        public void Dispose()
        {
            textures = null;
        }
    }
}
