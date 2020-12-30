using FModel.PakReader.Parsers.Objects;

namespace FModel.PakReader.Parsers.PropertyTagData
{
    public sealed class EnumProperty : BaseProperty<FName>
    {
        internal EnumProperty(FPropertyTag tag)
        {
            Value = new FName(ByteToEnum(tag.EnumName.String, 0));
        }
        internal EnumProperty(PackageReader reader, FPropertyTag tag, ReadType readType)
        {
            Position = reader.Position;

            if (!(reader is IoPackageReader) || readType != ReadType.NORMAL)
            {
                Value = reader.ReadFName();
            }
            else
            {
                var byteValue = tag.EnumType.String == "IntProperty" ? reader.ReadInt32() : reader.ReadByte();
                Value = new FName(ByteToEnum(tag.EnumName.String, byteValue));
            }
        }

        private static string ByteToEnum(string enumName, int value)
        {
            if (enumName == null)
                return value.ToString();

            string result;

            if (Globals.EnumMappings.TryGetValue(enumName, out var values))
            {
                result = values.TryGetValue(value, out var member) ? string.Concat(enumName, "::", member) : string.Concat(enumName, "::", value);
            }
            else
            {
                result = string.Concat(enumName, "::", value);
            }

            return result;
        }

        public string GetValue() => Value.String;
    }
}
