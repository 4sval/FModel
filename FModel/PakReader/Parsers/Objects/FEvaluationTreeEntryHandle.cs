namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FEvaluationTreeEntryHandle : IUStruct
    {
        public readonly int EntryIndex;

        internal FEvaluationTreeEntryHandle(PackageReader reader)
        {
            EntryIndex = reader.ReadInt32();
        }
    }
}
