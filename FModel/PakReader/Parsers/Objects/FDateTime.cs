using System;
using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FDateTime : IUStruct
    {
        // might add more helper methods here

        /** Holds the ticks in 100 nanoseconds resolution since January 1, 0001 A.D. */
        public readonly long Ticks;
        public readonly string Date;

        internal FDateTime(BinaryReader reader)
        {
            Ticks = reader.ReadInt64();
            Date = new DateTime(Ticks).ToString("F");
        }
    }
}
