using System;
using System.IO;

namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FVirtualTextureBuiltData
    {
        public readonly uint NumLayers;
        public readonly uint NumMips;
        public readonly int Width; // Width of the texture in pixels. Note the physical width may be larger due to tiling
        public readonly int Height; // Height of the texture in pixels. Note the physical height may be larger due to tiling
        public readonly uint WidthInBlocks; // Number of UDIM blocks that make up the texture, used to compute UV scaling factor
        public readonly uint HeightInBlocks;
        public readonly uint TileSize; // Tile size excluding borders
        public readonly uint TileBorderSize; // A BorderSize pixel border will be added around all tiles

        /**
	     * The pixel format output of the data on the i'th layer. The actual data
	     * may still be compressed but will decompress to this pixel format (e.g. zipped DXT5 data).
	     */
        public readonly EPixelFormat[] LayerTypes;

        /**
         * Tile data is packed into separate chunks, typically there is 1 mip level in each chunk for high resolution mips.
         * After a certain threshold, all remaining low resolution mips will be packed into one final chunk.
         */
        public readonly FVirtualTextureDataChunk[] Chunks;

        /** Index of the first tile within each chunk */
        public readonly uint[] TileIndexPerChunk;

        /** Index of the first tile within each mip level */
        public readonly uint[] TileIndexPerMip;

        /**
	     * Info for the tiles organized per level. Within a level tile info is organized in Morton order.
	     * This is in morton order which can waste a lot of space in this array for non-square images
	     * e.g.:
	     * - An 8x1 tile image will allocate 8x4 indexes in this array.
	     * - An 1x8 tile image will allocate 8x8 indexes in this array.
	     */
        public readonly uint[] TileOffsetInChunk;

        internal FVirtualTextureBuiltData(BinaryReader reader, Stream ubulk, long bulkOffset)
        {
            reader.ReadInt32(); // bCooked
            NumLayers = reader.ReadUInt32();
            WidthInBlocks = reader.ReadUInt32();
            HeightInBlocks = reader.ReadUInt32();
            TileSize = reader.ReadUInt32();
            TileBorderSize = reader.ReadUInt32();

            NumMips = reader.ReadUInt32();
            Width = reader.ReadInt32();
            Height = reader.ReadInt32();
            TileIndexPerChunk = reader.ReadTArray(() => reader.ReadUInt32());
            TileIndexPerMip = reader.ReadTArray(() => reader.ReadUInt32());
            TileOffsetInChunk = reader.ReadTArray(() => reader.ReadUInt32());

            LayerTypes = new EPixelFormat[8];
            for (int Layer = 0; Layer < NumLayers; Layer++)
            {
                LayerTypes[Layer] = Enum.Parse<EPixelFormat>(reader.ReadFString());
            }

            Chunks = new FVirtualTextureDataChunk[reader.ReadInt32()];
            for (int ChunkId = 0; ChunkId < Chunks.Length; ChunkId++)
            {
                Chunks[ChunkId] = new FVirtualTextureDataChunk(reader, ubulk, bulkOffset, NumLayers);
            }
        }
    }
}
