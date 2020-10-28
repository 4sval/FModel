namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FUniqueObjectGuid : IUStruct
    {
        /** Guid representing the object, should be unique */
        public readonly FGuid Guid;

        internal FUniqueObjectGuid(PackageReader reader)
        {
            Guid = new FGuid(reader);
        }
    }
}
