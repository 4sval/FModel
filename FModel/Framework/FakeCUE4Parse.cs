using System;
using CUE4Parse.Compression;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Readers;

namespace FModel.Framework;

public class FakeGameFile : GameFile
{
    public FakeGameFile(string path) : base(path, 0)
    {

    }

    public override bool IsEncrypted => false;
    public override CompressionMethod CompressionMethod => CompressionMethod.None;

    public override byte[] Read()
    {
        throw new NotImplementedException();
    }

    public override FArchive CreateReader()
    {
        throw new NotImplementedException();
    }
}
