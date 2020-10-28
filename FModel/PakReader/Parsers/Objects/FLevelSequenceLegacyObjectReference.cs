namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FLevelSequenceLegacyObjectReference : IUStruct
    {
        /** Primary method of resolution - object ID, stored as an annotation on the object in the world, resolvable through TLazyObjectPtr */
        public readonly FUniqueObjectGuid ObjectId;
        /** Secondary method of resolution - path to the object within the context */
        public readonly string ObjectPath;

        internal FLevelSequenceLegacyObjectReference(PackageReader reader)
        {
            ObjectId = new FUniqueObjectGuid(reader);
            ObjectPath = reader.ReadFString();
        }
    }
}
