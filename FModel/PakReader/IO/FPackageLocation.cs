using FModel.PakReader.Parsers.Objects;

namespace FModel.PakReader.IO
{
    public readonly struct FPackageLocation
    {
        public readonly FName ContainerName;
        public readonly ulong Offset;
    }
}