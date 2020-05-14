using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FCustomVersionContainer : IUStruct
    {
        public readonly FCustomVersion[] Versions; // actually FCustomVersionArray, but typedeffed to TArray<FCustomVersion>

        internal FCustomVersionContainer(BinaryReader reader)
        {
            Versions = reader.ReadTArray(() => new FCustomVersion(reader));
        }
    }
}
