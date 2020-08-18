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
                    int sizeX = 0;
                    int sizeY = 0;
                    int sizeZ = 1;
                    List<byte> data = new List<byte>();
                    if (PlatformDatas[0].Mips.Length > 0)
                    {
                        sizeX = PlatformDatas[0].Mips[0].SizeX;
                        sizeY = PlatformDatas[0].Mips[0].SizeY;
                        sizeZ = PlatformDatas[0].Mips[0].SizeZ;
                        data.AddRange(PlatformDatas[0].Mips[0].BulkData.Data);
                    }

                    //if (PlatformDatas[0].bIsVirtual)
                    //{
                    //    sizeX = PlatformDatas[0].VTData.Width;
                    //    sizeY = PlatformDatas[0].VTData.Height;
                    //}

                    image = TextureDecoder.DecodeImage(data.ToArray(), sizeX, sizeY, sizeZ, PlatformDatas[0].PixelFormat);
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
