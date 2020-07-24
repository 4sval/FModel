using System.Collections.Generic;
using System.IO;
using PakReader.Parsers.Objects;
using SkiaSharp;

namespace PakReader.Parsers.Class
{
    public sealed class UTexture2D : UObject
    {
        public FTexturePlatformData[] PlatformDatas { get; }

        SKImage image;
        public SKImage Image
        {
            get
            {
                if (image == null)
                {
                    var mip = PlatformDatas[0].Mips[0];
                    image = TextureDecoder.DecodeImage(mip.BulkData.Data, mip.SizeX, mip.SizeY, mip.SizeZ, PlatformDatas[0].PixelFormat);
                }
                return image;
            }
        }

        internal UTexture2D(PackageReader reader, Stream ubulk, long bulkOffset) : base(reader)
        {
            new FStripDataFlags(reader); // and I quote, "still no idea"
            new FStripDataFlags(reader); // "why there are two" :)

            if (reader.ReadInt32() != 0) // bIsCooked
            {
                var data = new List<FTexturePlatformData>(1); // Probably gonna be only one texture anyway
                var PixelFormatName = reader.ReadFName();
                while (!PixelFormatName.IsNone)
                {
                    _ = reader.ReadInt32(); // SkipOffset
                    if (FModel.Globals.Game.Version >= EPakVersion.RELATIVE_CHUNK_OFFSETS)
                        _ = reader.ReadInt32(); // SkipOffsetH

                    data.Add(new FTexturePlatformData(reader, ubulk, bulkOffset));
                    PixelFormatName = reader.ReadFName();

                    if (FModel.Globals.Game.Version < EPakVersion.RELATIVE_CHUNK_OFFSETS)
                        break;
                }
                PlatformDatas = data.ToArray();
            }
        }
    }
}
