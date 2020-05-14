using System;
using System.Collections.Generic;
using System.Reflection;

namespace PakReader.Parsers
{
    static class ReflectionHelper
    {
        readonly static Dictionary<Type, (Type BaseType, Func<object, object> Setter)> PropertyCache = new Dictionary<Type, (Type, Func<object, object>)>();

        static class New<T>
        {
            public static readonly Func<T> Instance = GetInstance();
            public static readonly IReadOnlyDictionary<(string Name, Type Type), Action<object, object>> ActionMap = GetActionMap();

            static Func<T> GetInstance()
            {
                var constructor = typeof(T).GetConstructor(Type.EmptyTypes);
                return () => (T)constructor.Invoke(Array.Empty<object>());
            }

            static IReadOnlyDictionary<(string Name, Type Type), Action<object, object>> GetActionMap()
            {
                var dict = new Dictionary<(string Name, Type Type), Action<object, object>>();

                var Fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var Properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                for (int i = 0; i < Properties.Length; i++)
                {
                    dict[((Properties[i].GetCustomAttribute<UPropAttribute>()?.Name ?? Properties[i].Name).ToLowerInvariant(), Properties[i].PropertyType)] = Properties[i].SetValue;
                }
                for (int i = 0; i < Fields.Length; i++)
                {
                    dict[((Fields[i].GetCustomAttribute<UPropAttribute>()?.Name ?? Fields[i].Name).ToLowerInvariant(), Fields[i].FieldType)] = Fields[i].SetValue;
                }

                return dict;
            }
        }

        internal static T NewInstance<T>() => New<T>.Instance();
        internal static IReadOnlyDictionary<(string Name, Type Type), Action<object, object>> GetActionMap<T>() => New<T>.ActionMap;

        internal static (Type BaseType, Func<object, object> Getter) GetPropertyInfo(Type property)
        {
            if (!PropertyCache.TryGetValue(property, out var info))
            {
                var baseType = property.BaseType;
                // PropertyCache[property] = info = (baseType.GenericTypeArguments[0], baseType.GetField("Value").GetValue);
                // Currently don't have this because of array/map/set property schenanigans
                PropertyCache[property] = info = (property, (prop) => prop);
            }
            return info;
        }
    }
}
