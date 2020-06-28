using PakReader.Parsers.Class;
using PakReader.Parsers.Objects;
using System.Collections.Generic;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class StructProperty : BaseProperty<IUStruct>
    {
        internal StructProperty(PackageReader reader, FPropertyTag tag)
        {
            Position = reader.Position;
            Value = new UScriptStruct(reader, tag.StructName).Struct;
        }

        public object GetValue()
        {
            if (Value is UObject obj)
            {
                var ret = new Dictionary<string, object>(obj.Count);
                foreach (KeyValuePair<string, object> KvP in obj)
                {
                    if (KvP.Value == null)
                        ret[KvP.Key] = null;
                    else
                        ret[KvP.Key] = KvP.Value.GetType().Name switch
                        {
                            "ByteProperty" => ((ByteProperty)KvP.Value).GetValue(),
                            "BoolProperty" => ((BoolProperty)KvP.Value).GetValue(),
                            "IntProperty" => ((IntProperty)KvP.Value).GetValue(),
                            "FloatProperty" => ((FloatProperty)KvP.Value).GetValue(),
                            "ObjectProperty" => ((ObjectProperty)KvP.Value).GetValue(),
                            "NameProperty" => ((NameProperty)KvP.Value).GetValue(),
                            "DoubleProperty" => ((DoubleProperty)KvP.Value).GetValue(),
                            "ArrayProperty" => ((ArrayProperty)KvP.Value).GetValue(),
                            "StructProperty" => ((StructProperty)KvP.Value).GetValue(),
                            "StrProperty" => ((StrProperty)KvP.Value).GetValue(),
                            "TextProperty" => ((TextProperty)KvP.Value).GetValue(),
                            "InterfaceProperty" => ((InterfaceProperty)KvP.Value).GetValue(),
                            "SoftObjectProperty" => ((SoftObjectProperty)KvP.Value).GetValue(),
                            "UInt64Property" => ((UInt64Property)KvP.Value).GetValue(),
                            "UInt32Property" => ((UInt32Property)KvP.Value).GetValue(),
                            "UInt16Property" => ((UInt16Property)KvP.Value).GetValue(),
                            "Int64Property" => ((Int64Property)KvP.Value).GetValue(),
                            "Int16Property" => ((Int16Property)KvP.Value).GetValue(),
                            "Int8Property" => ((Int8Property)KvP.Value).GetValue(),
                            "MapProperty" => ((MapProperty)KvP.Value).GetValue(),
                            "SetProperty" => ((SetProperty)KvP.Value).GetValue(),
                            "EnumProperty" => ((EnumProperty)KvP.Value).GetValue(),
                            _ => KvP.Value,
                        };
                }
                return ret;
            }
            else if (Value is FGameplayTagContainer gTags)
            {
                var ret = new string[gTags.GameplayTags.Length];
                for (int i = 0; i < ret.Length; i++)
                {
                    ret[i] = gTags.GameplayTags[i].String;
                }
                return ret;
            }
            else if (Value is FGuid guid)
            {
                return guid.Hex;
            }
            return Value;
        }
    }
}
