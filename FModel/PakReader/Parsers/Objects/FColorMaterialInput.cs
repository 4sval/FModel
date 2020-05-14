namespace PakReader.Parsers.Objects
{
    public readonly struct FColorMaterialInput : IUStruct
    {
        public readonly FMaterialInput Parent;
        public readonly bool UseConstant;
        public readonly FColor Constant;

        internal FColorMaterialInput(PackageReader reader)
        {
            Parent = new FMaterialInput(reader);
            UseConstant = reader.ReadByte() != 0;
            Constant = new FColor(reader);
            reader.ReadByte(); // bTemp
            reader.ReadInt16(); // TempType
        }
    }
}
