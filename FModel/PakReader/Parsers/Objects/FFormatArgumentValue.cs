using System;
using System.Collections.Generic;

namespace PakReader.Parsers.Objects
{
    public readonly struct FFormatArgumentValue : IUStruct
    {
        public readonly EFormatArgumentType Type;
        public readonly object Value;

        internal FFormatArgumentValue(PackageReader reader)
        {
            Type = (EFormatArgumentType)reader.ReadByte();
            Value = Type switch
            {
                EFormatArgumentType.Text => new FText(reader),
                EFormatArgumentType.Int => reader.ReadInt64(),
                EFormatArgumentType.UInt => reader.ReadUInt64(),
                EFormatArgumentType.Double => reader.ReadDouble(),
                EFormatArgumentType.Float => reader.ReadFloat(),
                _ => throw new NotImplementedException(string.Format(FModel.Properties.Resources.ParsingNotSupported, Type)),
            };
        }

        public Dictionary<string, object> GetValue()
        {
            return new Dictionary<string, object>
            {
                ["Type"] = Type,
                ["Value"] = Value switch
                {
                    FText fText => fText.GetValue(),
                    _ => Value,
                }
            };
        }
    }
}
