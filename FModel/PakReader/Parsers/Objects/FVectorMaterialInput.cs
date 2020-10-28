namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FVectorMaterialInput : IUStruct
    {
        public readonly FMaterialInput Parent;
        public readonly bool UseConstant;
        public readonly FVector Constant;

        internal FVectorMaterialInput(PackageReader reader)
        {
            Parent = new FMaterialInput(reader);
            UseConstant = reader.ReadByte() != 0;
            Constant = new FVector(reader);
            reader.ReadByte(); // bTemp
            reader.ReadInt16(); // TempType
        }
    }
}
