using FModel.PakReader.Parsers.Objects;

namespace FModel.PakReader.Parsers.PropertyTagData
{
    public sealed class EnumProperty : BaseProperty<FName>
    {
        internal EnumProperty(FPropertyTag tag)
        {
            Value = new FName(ByteToEnum(tag.EnumName.String, 0));
        }
        internal EnumProperty(PackageReader reader, FPropertyTag tag)
        {
            Position = reader.Position;

            if (reader is IoPackageReader)
            {
                object byteValue = tag.EnumType.String == "IntProperty" ? reader.ReadInt32() : reader.ReadByte();
                Value = new FName(ByteToEnum(tag.EnumName.String, byteValue));
            }
            else
            {
                Value = reader.ReadFName();
            }
        }

        private static string ByteToEnum(string enumName, object value)
        {
            string result;

            if (enumName == null)
                return value.ToString();

            if (Globals.EnumMappings.TryGetValue(enumName, out var values))
            {
                result = values.TryGetValue((int)value, out var member) ? string.Concat(enumName, "::", member) : string.Concat(enumName, "::", value);
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
