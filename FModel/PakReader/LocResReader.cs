using System;
using System.Collections.Generic;
using System.IO;
using PakReader.Parsers.Objects;

namespace PakReader
{
    public class LocResReader
    {
        static readonly FGuid Magic = new FGuid(0x7574140E, 0xFC034A67, 0x9D90154A, 0x1B7F37C3);

        public readonly Dictionary<string, Dictionary<string, string>> Entries = new Dictionary<string, Dictionary<string, string>>();

        public LocResReader(string path) : this(File.OpenRead(path)) { }

        public LocResReader(Stream stream) : this(new BinaryReader(stream)) { }

        public LocResReader(BinaryReader reader)
        {
            var MagicNumber = new FGuid(reader);

            var VersionNumber = Version.LEGACY;
            if (MagicNumber == Magic)
            {
                VersionNumber = (Version)reader.ReadByte();
            }
            else
            {
                // Legacy LocRes files lack the magic number, assume that's what we're dealing with, and seek back to the start of the file
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
            }

            if (VersionNumber > Version.LATEST)
            {
                throw new IOException($"LocRes file is too new to be loaded! (File Version: {(byte)VersionNumber}, Loader Version: {(byte)LocMetaReader.Version.LATEST})");
            }

            // Read the localized string array
            FTextLocalizationResourceString[] LocalizedStringArray = Array.Empty<FTextLocalizationResourceString>();
            if (VersionNumber >= Version.COMPACT)
            {
                long LocalizedStringArrayOffset = -1; // INDEX_NONE
                LocalizedStringArrayOffset = reader.ReadInt64();

                if (LocalizedStringArrayOffset != -1)
                {
                    if (VersionNumber >= Version.OPTIMIZED)
                    {
                        long CurrentFileOffset = reader.BaseStream.Position;
                        reader.BaseStream.Seek(LocalizedStringArrayOffset, SeekOrigin.Begin);
                        LocalizedStringArray = reader.ReadTArray(() => new FTextLocalizationResourceString(reader));
                        reader.BaseStream.Seek(CurrentFileOffset, SeekOrigin.Begin);
                    }
                    else
                    {
                        string[] TmpLocalizedStringArray;

                        long CurrentFileOffset = reader.BaseStream.Position;
                        reader.BaseStream.Seek(LocalizedStringArrayOffset, SeekOrigin.Begin);
                        TmpLocalizedStringArray = reader.ReadTArray(() => reader.ReadFString());
                        reader.BaseStream.Seek(CurrentFileOffset, SeekOrigin.Begin);

                        LocalizedStringArray = new FTextLocalizationResourceString[TmpLocalizedStringArray.Length];
                        for (int i = 0; i < TmpLocalizedStringArray.Length; i++)
                        {
                            LocalizedStringArray[i] = new FTextLocalizationResourceString(TmpLocalizedStringArray[i], -1);
                        }
                    }
                }
            }

            // Read entries count
            if (VersionNumber >= Version.OPTIMIZED)
            {
                uint EntriesCount = reader.ReadUInt32();
                // No need for initializer
                // Link: https://github.com/EpicGames/UnrealEngine/blob/7d9919ac7bfd80b7483012eab342cb427d60e8c9/Engine/Source/Runtime/Core/Private/Internationalization/TextLocalizationResource.cpp#L266
            }

            // Read namespace count
            uint NamespaceCount = reader.ReadUInt32();

            for (uint i = 0; i < NamespaceCount; i++)
            {
                // Read namespace
                if (VersionNumber >= Version.OPTIMIZED)
                {
                    reader.ReadUInt32(); // StrHash
                }
                var Namespace = reader.ReadFString();

                var Entries = new Dictionary<string, string>();

                // Read key count
                uint KeyCount = reader.ReadUInt32();

                for (uint j = 0; j < KeyCount; j++)
                {
                    // Read key
                    if (VersionNumber >= Version.OPTIMIZED)
                    {
                        reader.ReadUInt32(); // StrHash
                    }
                    string Key = reader.ReadFString();

                    reader.ReadUInt32(); // SourceStringHash

                    string EntryLocalizedString;
                    if (VersionNumber >= Version.COMPACT)
                    {
                        int LocalizedStringIndex = reader.ReadInt32();

                        if (LocalizedStringArray.Length > LocalizedStringIndex)
                        {
                            // Steal the string if possible
                            ref var LocalizedString = ref LocalizedStringArray[LocalizedStringIndex];
                            if (LocalizedString.RefCount == 1)
                            {
                                EntryLocalizedString = LocalizedString.String;
                                LocalizedString.RefCount--;
                            }
                            else
                            {
                                EntryLocalizedString = LocalizedString.String;
                                if (LocalizedString.RefCount != -1)
                                {
                                    LocalizedString.RefCount--;
                                }
                            }
                        }
                        else
                        {
                            throw new IOException($"LocRes has an invalid localized string index for namespace '{Namespace}' and key '{Key}'. This entry will have no translation.");
                        }
                    }
                    else
                    {
                        EntryLocalizedString = reader.ReadFString();
                    }
                    Entries.Add(Key, EntryLocalizedString);
                }
                this.Entries.Add(Namespace, Entries);
            }
        }

        public string this[string ns, string key] => Entries[ns][key];
        public bool TryGetValue(string ns, string key, out string value)
        {
            value = null;
            return Entries.TryGetValue(ns, out var nsret) && nsret.TryGetValue(key, out value);
        }

        public enum Version : byte
        {
            /** Legacy format file - will be missing the magic number. */
            LEGACY = 0,
            /** Compact format file - strings are stored in a LUT to avoid duplication. */
            COMPACT,
            /** Optimized format file - namespaces/keys are pre-hashed, we know the number of elements up-front, and the number of references for each string in the LUT (to allow stealing). */
            OPTIMIZED,

            LATEST_PLUS_ONE,
            LATEST = LATEST_PLUS_ONE - 1
        }

        struct FTextLocalizationResourceString
        {
            public readonly string String;
            public int RefCount;

            internal FTextLocalizationResourceString(BinaryReader reader)
            {
                String = reader.ReadFString();
                RefCount = reader.ReadInt32();
            }

            internal FTextLocalizationResourceString(string str, int refCount)
            {
                String = str;
                RefCount = refCount;
            }
        }
    }
}
