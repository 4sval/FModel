using System;
using CUE4Parse.Compression;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Readers;

namespace FModel.Framework;

public class FakeGameFile(string path) : GameFile(path, 0)
{
    public override bool IsEncrypted => false;
    public override CompressionMethod CompressionMethod => CompressionMethod.None;

    public override byte[] Read(FByteBulkDataHeader? header = null)
    {
        throw new NotImplementedException();
    }

    public override FArchive CreateReader(FByteBulkDataHeader? header = null)
    {
        throw new NotImplementedException();
    }
}
