using System.Collections.Generic;

namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FFormatArgumentData : IUStruct
    {
        public readonly string ArgumentName;
        public readonly EFormatArgumentType ArgumentValueType;
        public readonly object ArgumentValue;

        public FFormatArgumentData(PackageReader reader)
        {
            ArgumentName = reader.ReadFString();
            ArgumentValueType = (EFormatArgumentType)reader.ReadByte();
            ArgumentValue = ArgumentValueType switch
            {
                EFormatArgumentType.Int => reader.ReadInt32(),
                EFormatArgumentType.Float => reader.ReadFloat(),
                EFormatArgumentType.Text => new FText(reader),
                EFormatArgumentType.Gender => (ETextGender)reader.ReadByte(),
                _ => null,
            };
        }

        public Dictionary<string, object> GetValue()
        {
            return new Dictionary<string, object>
            {
                ["ArgumentName"] = ArgumentName,
                ["ArgumentValueType"] = ArgumentValueType,
                ["ArgumentValue"] = ArgumentValue
            };
        }
    }
}
