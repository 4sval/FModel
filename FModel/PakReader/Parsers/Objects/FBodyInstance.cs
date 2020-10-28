using System;

namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FBodyInstance : IUStruct
    {
        internal FBodyInstance(PackageReader reader)
        {
            throw new NotImplementedException(string.Format(FModel.Properties.Resources.ParsingNotSupported, "FBodyInstance"));
        }
    }
}
