using System;
using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FTexturePlatformData
    {
        public readonly int SizeX;
        public readonly int SizeY;
        public readonly int NumSlices;
        public readonly EPixelFormat PixelFormat;
        public readonly FTexture2DMipMap[] Mips;

        internal FTexturePlatformData(PackageReader reader, Stream ubulk, long bulkOffset)
        {
            SizeX = reader.ReadInt32();
            SizeY = reader.ReadInt32();
            NumSlices = reader.ReadInt32();
            PixelFormat = Enum.Parse<EPixelFormat>(reader.ReadFString());

            var FirstMipToSerialize = reader.ReadInt32();
            FirstMipToSerialize = 0; // what: https://github.com/EpicGames/UnrealEngine/blob/4.24/Engine/Source/Runtime/Engine/Private/TextureDerivedData.cpp#L1316

            Mips = reader.ReadTArray(() => new FTexture2DMipMap(reader, ubulk, bulkOffset));
            
            if (FModel.Globals.Game.ActualGame == FModel.EGame.Valorant || FModel.Globals.Game.Version > EPakVersion.FNAME_BASED_COMPRESSION_METHOD)
            {
                if (reader.ReadInt32() != 0) throw new FileLoadException("VirtualTextures are not supported");
            }
        }
    }
}
