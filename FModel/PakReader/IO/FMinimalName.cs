using System.IO;

namespace FModel.PakReader.IO
{
    public readonly struct FMinimalName
    {
        public readonly FNameEntryId Index;
        public readonly int Number; // #define NAME_NO_NUMBER_INTERNAL	0

        public FMinimalName(BinaryReader reader)
        {
            Index = new FNameEntryId(reader);
            Number = reader.ReadInt32();
        }

        public FMinimalName(FNameEntryId inIndex, int inNumber)
        {
            Index = inIndex;
            Number = inNumber;
        }
    }
}
