namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct TEvaluationTreeEntryContainer<T> : IUStruct
    {
        public readonly FEntry[] Entries;
        public readonly T[] Items;

        internal TEvaluationTreeEntryContainer(PackageReader reader)
        {
            Entries = reader.ReadTArray(() => new FEntry(reader));
            Items = reader.ReadTArray(() => default(T));
        }
    }
}
