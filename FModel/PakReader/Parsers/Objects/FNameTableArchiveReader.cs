using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FNameTableArchiveReader : IUStruct
    {
        private readonly FNameEntrySerialized[] _nameMap;
        public readonly BinaryReader Loader;

        internal FNameTableArchiveReader(BinaryReader reader)
        {
            Loader = reader;
            long NameOffset = reader.ReadInt64();

            if (NameOffset > reader.BaseStream.Length)
                throw new FileLoadException("NameOffset is larger than original file size");
            if (NameOffset <= 0)
                throw new FileLoadException("NameOffset is not positive");

            long OriginalOffset = reader.BaseStream.Position;
            reader.BaseStream.Seek(NameOffset, SeekOrigin.Begin);

            _nameMap = reader.ReadTArray(() => new FNameEntrySerialized(reader));

            reader.BaseStream.Seek(OriginalOffset, SeekOrigin.Begin);
        }

        public FName ReadFName()
        {
            var NameIndex = Loader.ReadInt32();
            var Number = Loader.ReadInt32();

            if (NameIndex >= 0 && NameIndex < _nameMap.Length)
            {
                return new FName(_nameMap[NameIndex], NameIndex, Number);
            }
            throw new FileLoadException($"Bad Name Index: {NameIndex}/{_nameMap.Length} - Loader Position: {Loader.BaseStream.Position}");
        }
    }
}
