using System;
using System.Collections.Generic;
using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class MapProperty : BaseProperty<IReadOnlyDictionary<BaseProperty, BaseProperty>>
    {
        // https://github.com/EpicGames/UnrealEngine/blob/7d9919ac7bfd80b7483012eab342cb427d60e8c9/Engine/Source/Runtime/CoreUObject/Private/UObject/PropertyMap.cpp#L243
        internal MapProperty(PackageReader reader, FPropertyTag tag)
        {
            Position = reader.Position;
            var NumKeysToRemove = reader.ReadInt32();
            if (NumKeysToRemove != 0)
            {
                // Let me know if you find a package that has a non-zero NumKeysToRemove value
                throw new NotImplementedException("Parsing of non-zero NumKeysToRemove maps aren't supported yet.");
            }

            var NumEntries = reader.ReadInt32();
            var dict = new Dictionary<BaseProperty, BaseProperty>(NumEntries);
            for (int i = 0; i < NumEntries; i++)
            {
                dict[ReadProperty(reader, tag, tag.InnerType, ReadType.MAP)] = ReadProperty(reader, tag, tag.ValueType, ReadType.MAP);
            }
            Value = dict;
        }
    }
}
