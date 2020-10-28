using System;
using Newtonsoft.Json;

namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FText
    {
        [JsonIgnore]
        public readonly ETextFlag Flags;
        public readonly FTextHistory Text;

        internal FText(ETextFlag flags, FTextHistory text)
        {
            Flags = flags;
            Text = text;
        }

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
            // https://github.com/EpicGames/UnrealEngine/blob/283e412aa843210f2d6e9ed0236861cf749b3429/Engine/Source/Runtime/Core/Private/Internationalization/TextHistory.h
            // https://github.com/EpicGames/UnrealEngine/blob/bf95c2cbc703123e08ab54e3ceccdd47e48d224a/Engine/Source/Runtime/Core/Private/Internationalization/TextHistory.cpp
            switch (HistoryType)
            {
                case ETextHistoryType.Base:
                    Text = new FTextHistory.Base(reader);
                    break;
                case ETextHistoryType.AsDateTime:
                    Text = new FTextHistory.DateTime(reader);
                    break;
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
                case ETextHistoryType.AsTime:
                    Text = new FTextHistory.AsTime(reader);
                    break;
                case ETextHistoryType.AsDate:
                    Text = new FTextHistory.AsDate(reader);
                    break;
                case ETextHistoryType.ArgumentFormat:
                    Text = new FTextHistory.ArgumentDataFormat(reader);
                    break;
                case ETextHistoryType.Transform:
                    Text = new FTextHistory.Transform(reader);
                    break;
                case ETextHistoryType.TextGenerator:
                    throw new NotImplementedException(string.Format(FModel.Properties.Resources.ParsingNotSupported, HistoryType));
                default:
                    Text = new FTextHistory.None(reader);
                    break;
            }
        }

        public object GetValue()
        {
            return Text switch
            {
                FTextHistory.DateTime dateTime => dateTime.GetValue(),
                FTextHistory.OrderedFormat orderedFormat => orderedFormat.GetValue(),
                FTextHistory.FormatNumber formatNumber => formatNumber.GetValue(),
                FTextHistory.StringTableEntry stringTableEntry => stringTableEntry.GetValue(),
                FTextHistory.AsTime asTime => asTime.GetValue(),
                FTextHistory.AsDate asDate => asDate.GetValue(),
                FTextHistory.ArgumentDataFormat argumentDataFormat => argumentDataFormat.GetValue(),
                FTextHistory.Transform transform => transform.GetValue(),
                FTextHistory.None none => none.CultureInvariantString,
                _ => Text
            };
        }
    }
}
