using Newtonsoft.Json;

namespace PakReader.Parsers.Objects
{
    public readonly struct FMaterialInput : IUStruct
    {
        /** Index into Expression's outputs array that this input is connected to. */
        [JsonIgnore]
        public readonly int OutputIndex;
        [JsonIgnore]
        public readonly FName InputName;
        [JsonIgnore]
        public readonly int Mask;
        [JsonIgnore]
        public readonly int MaskR;
        [JsonIgnore]
        public readonly int MaskG;
        [JsonIgnore]
        public readonly int MaskB;
        [JsonIgnore]
        public readonly int MaskA;
        /** Material expression name that this input is connected to, or None if not connected. Used only in cooked builds */
        public readonly FName ExpressionName;

        internal FMaterialInput(PackageReader reader)
        {
            OutputIndex = reader.ReadInt32();
            InputName = reader.ReadFName();
            Mask = reader.ReadInt32();
            MaskR = reader.ReadInt32();
            MaskG = reader.ReadInt32();
            MaskB = reader.ReadInt32();
            MaskA = reader.ReadInt32();
            ExpressionName = reader.ReadFName();
        }
    }
}
