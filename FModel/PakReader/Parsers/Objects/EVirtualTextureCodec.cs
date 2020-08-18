namespace PakReader.Parsers.Objects
{
    public enum EVirtualTextureCodec : byte
    {
		Black,          //Special case codec, always outputs black pixels 0,0,0,0
		OpaqueBlack,    //Special case codec, always outputs opaque black pixels 0,0,0,255
		White,          //Special case codec, always outputs white pixels 255,255,255,255
		Flat,           //Special case codec, always outputs 128,125,255,255 (flat normal map)
		RawGPU,         //Uncompressed data in an GPU-ready format (e.g R8G8B8A8, BC7, ASTC, ...)
		ZippedGPU,      //Same as RawGPU but with the data zipped
		Crunch,         //Use the Crunch library to compress data
		Max,            // Add new codecs before this entry
	}
}
