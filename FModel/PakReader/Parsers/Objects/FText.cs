using Newtonsoft.Json;
using System;

namespace PakReader.Parsers.Objects
{
    public readonly struct FText
    {
        [JsonIgnore]
        public readonly ETextFlag Flags;
        public readonly FTextHistory Text;

        // https://github.com/EpicGames/UnrealEngine/blob/7d9919ac7bfd80b7483012eab342cb427d60e8c9/Engine/Source/Runtime/Core/Private/Internationalization/Text.cpp#L769
        internal FText(PackageReader reader)
        {
            Flags = (ETextFlag)reader.ReadUInt32();

            // "Assuming" the reader/archive is persistent
            Flags &= ETextFlag.ConvertedProperty | ETextFlag.InitializedFromString;

            // Execute if UE4 version is at least VER_UE4_FTEXT_HISTORY

            // The type is serialized during the serialization of the history, during deserialization we need to deserialize it and create the correct history
            var HistoryType = (ETextHistoryType)reader.ReadSByte();

            // Create the history class based on the serialized type
            switch (HistoryType)
            {
                case ETextHistoryType.Base:
                    Text = new FTextHistory.Base(reader);
                    break;
                case ETextHistoryType.AsDateTime:
                    Text = new FTextHistory.DateTime(reader);
                    break;
                // https://github.com/EpicGames/UnrealEngine/blob/bf95c2cbc703123e08ab54e3ceccdd47e48d224a/Engine/Source/Runtime/Core/Private/Internationalization/TextHistory.cpp
                // https://github.com/EpicGames/UnrealEngine/blob/bf95c2cbc703123e08ab54e3ceccdd47e48d224a/Engine/Source/Runtime/Core/Private/Internationalization/TextData.h
                case ETextHistoryType.NamedFormat:
                case ETextHistoryType.OrderedFormat:
                    Text = new FTextHistory.OrderedFormat(reader);
                    break;
                case ETextHistoryType.AsNumber:
                case ETextHistoryType.AsPercent:
                case ETextHistoryType.AsCurrency:
                    Text = new FTextHistory.FormatNumber(reader);
                    break;
                case ETextHistoryType.StringTableEntry:
                    Text = new FTextHistory.StringTableEntry(reader);
                    break;
                case ETextHistoryType.ArgumentFormat:
                case ETextHistoryType.AsDate:
                case ETextHistoryType.AsTime:
                case ETextHistoryType.Transform:
                case ETextHistoryType.TextGenerator:
                    throw new NotImplementedException(string.Format(FModel.Properties.Resources.ParsingNotSupported, HistoryType));
                default:
                    Text = new FTextHistory.None(reader);
                    break;
            }
        }
    }
}
