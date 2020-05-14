using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FOodleDictionaryArchive : IUStruct
    {
        /** The dictionary file header */
        public readonly FDictionaryHeader Header;

        internal FOodleDictionaryArchive(Stream stream) : this(new BinaryReader(stream)) { }
        internal FOodleDictionaryArchive(BinaryReader reader)
        {
            Header = new FDictionaryHeader(reader);
        }
    }
}
