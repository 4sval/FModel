using System.Collections.Generic;
using System.IO;
using FModel.PakReader.Parsers.Objects;
using FModel.PakReader.Textures;
using SkiaSharp;

namespace FModel.PakReader.Parsers.Class
{
    public sealed class UTexture2D : UObject
    {
        public FTexturePlatformData[] PlatformDatas { get; private set; }

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
                        int i = 0;
                        byte[] d = PlatformDatas[0].Mips[i].BulkData.Data;
                        while (d == null)
                        {
                            i++;
                            d = PlatformDatas[0].Mips[i].BulkData.Data;
                        }

                        sizeX = PlatformDatas[0].Mips[i].SizeX;
                        sizeY = PlatformDatas[0].Mips[i].SizeY;
                        sizeZ = PlatformDatas[0].Mips[i].SizeZ;
                        data.AddRange(d);
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

        internal UTexture2D(IoPackageReader reader, string type, Stream ubulk, long bulkOffset) : base(reader, type)
        {
            Serialize(reader, ubulk, bulkOffset);
        }
        internal UTexture2D(PackageReader reader, Stream ubulk, long bulkOffset) : base(reader)
        {
            Serialize(reader, ubulk, bulkOffset);
        }

        private void Serialize(PackageReader reader, Stream ubulk, long bulkOffset)
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
