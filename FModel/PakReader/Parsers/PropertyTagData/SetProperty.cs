using System;
using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class SetProperty : BaseProperty<BaseProperty[]>
    {
        // https://github.com/EpicGames/UnrealEngine/blob/bf95c2cbc703123e08ab54e3ceccdd47e48d224a/Engine/Source/Runtime/CoreUObject/Private/UObject/PropertySet.cpp#L216
        internal SetProperty(PackageReader reader, FPropertyTag tag)
        {
            Position = reader.Position;
            var NumKeysToRemove = reader.ReadInt32();
            if (NumKeysToRemove != 0)
            {
                // Let me know if you find a package that has a non-zero NumKeysToRemove value
                throw new NotImplementedException("Parsing of non-zero NumKeysToRemove sets aren't supported yet.");
            }

            var NumEntries = reader.ReadInt32();
            Value = new BaseProperty[NumEntries];
            for (int i = 0; i < NumEntries; i++)
            {
                Value[i] = ReadProperty(reader, tag, tag.InnerType, ReadType.ARRAY);
            }
        }
    }
}
