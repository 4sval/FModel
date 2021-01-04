using FModel.PakReader.Parsers.Objects;
using System.Linq;

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

        private static string ByteToEnum(string enumName, int index)
        {
            if (enumName == null)
                return index.ToString();

            var enumProp = Globals.Usmap.Enums.FirstOrDefault(x => x.Name == enumName);
            if (!enumProp.Equals(default))
                return index >= enumProp.Names.Length ? string.Concat(enumName, "::", index) : string.Concat(enumName, "::", enumProp.Names[index]);
            else
                return string.Concat(enumName, "::", index);
        }

        public string GetValue() => Value.String;
    }
}
