using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FModel.PakReader.Parsers.PropertyTagData;

namespace FModel.PakReader.Parsers.Class
{
    /// <summary>
    /// IReadOnlyDictionary<string, object> is only used to be able to iterate over properties
    /// The derived class must have a "readonly Dictionary<string, object>" of properties
    /// </summary>
    public interface IUExport : IReadOnlyDictionary<string, object>
    {
        
    }

    public static class IUExportExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetExport<T>(this IUExport export, params string[] names)
        {
            foreach (string name in names)
            {
                if (export != null && export.TryGetValue(name, out var obj) && obj is T)
                    return (T)obj;
            }
            return default;
        }

        public static Dictionary<string, object> GetJsonDict(this IUExport export)
        {
            if (export != null)
            {
                var ret = new Dictionary<string, object>(export.Count);
                foreach (var (key, value) in export)
                {
                    ret[key] = value switch
                    {
                        ByteProperty byteProperty => byteProperty.GetValue(),
                        BoolProperty boolProperty => boolProperty.GetValue(),
                        IntProperty intProperty => intProperty.GetValue(),
                        FloatProperty floatProperty => floatProperty.GetValue(),
                        ObjectProperty objectProperty => objectProperty.GetValue(),
                        NameProperty nameProperty => nameProperty.GetValue(),
                        DelegateProperty delegateProperty => delegateProperty.GetValue(),
                        DoubleProperty doubleProperty => doubleProperty.GetValue(),
                        ArrayProperty arrayProperty => arrayProperty.GetValue(),
                        StructProperty structProperty => structProperty.GetValue(),
                        StrProperty strProperty => strProperty.GetValue(),
                        TextProperty textProperty => textProperty.GetValue(),
                        InterfaceProperty interfaceProperty => interfaceProperty.GetValue(),
                        SoftObjectProperty softObjectProperty => softObjectProperty.GetValue(),
                        UInt64Property uInt64Property => uInt64Property.GetValue(),
                        UInt32Property uInt32Property => uInt32Property.GetValue(),
                        UInt16Property uInt16Property => uInt16Property.GetValue(),
                        Int64Property int64Property => int64Property.GetValue(),
                        Int16Property int16Property => int16Property.GetValue(),
                        Int8Property int8Property => int8Property.GetValue(),
                        MapProperty mapProperty => mapProperty.GetValue(),
                        SetProperty setProperty => setProperty.GetValue(),
                        EnumProperty enumProperty => enumProperty.GetValue(),
                        UObject uObject => uObject.GetJsonDict(),
                        _ => value,
                    };
                }
                return ret;
            }
            return null;
        }
    }
}
