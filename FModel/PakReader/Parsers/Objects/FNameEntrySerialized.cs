using System.Collections.Generic;
using System.IO;
using System.Text;
using FModel.PakReader.IO;
using FModel.Utils;

namespace FModel.PakReader.Parsers.Objects
{
    // The only values this contains from the original FNameEntrySerialized is the isWide (unused here since C# strings are always 16 bit anyway) and the Index (some typedef of an int which was unused anyway)
    // FNames are passed into a pool, but I don't think this has any impact or difference on the resolving of these values. I could make a Dictionary or Lookup for values having the same hash or something..?

    // FNameEntrySerialized is a class due to the value typing that C# has for structs. This is for memory performance to reduce duplicate strings in memory. Refrain from saving the FNameEntrySerialized's value (Name) and opt for a class instead
    public readonly struct FNameEntrySerialized
    {
        public readonly string Name;

        // The parser is basically the same as FString. Let me know if there are any breaking test cases here
        internal FNameEntrySerialized(BinaryReader reader)
        {
            Name = reader.ReadFString();
            // skip DummyHashes (case and non-case preserving hashes)
            reader.BaseStream.Position += 4;
        }

        public FNameEntrySerialized(string name)
        {
            Name = name;
        }

        public override string ToString() => Name;

        public static void LoadNameBatch(List<FNameEntrySerialized> outNames, List<ulong> outHashes, byte[] nameData, byte[] hashData)
        {
            using var nameReader = new BinaryReader(new MemoryStream(nameData));
            using var hashReader = new BinaryReader(new MemoryStream(hashData));

            var hashDataIt = hashReader.ReadUInt64();
            var hashVersion = hashDataIt.IntelOrder64();

            //var hashCount = (int) (hashReader.BaseStream.Length / sizeof(ulong) - 1);
            var hashCount = hashData.Length / sizeof(ulong) - 1;
            outNames.Capacity = hashCount;

            for (var i = 0; i < hashCount; i++)
            {
                outHashes.Add(hashReader.ReadUInt64());
                outNames.Add(LoadNameHeader(nameReader));
            }
        }

        private static FNameEntrySerialized LoadNameHeader(BinaryReader nameReader)
        {
            var header = new FSerializedNameHeader(nameReader);

            var length = (int)header.Length;
            return header.IsUtf16 ?
                new FNameEntrySerialized(Encoding.Unicode.GetString(nameReader.ReadBytes(length * 2))) :
                new FNameEntrySerialized(Encoding.UTF8.GetString(nameReader.ReadBytes(length)));
        }
    }
}
