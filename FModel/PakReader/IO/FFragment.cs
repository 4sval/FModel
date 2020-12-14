namespace FModel.PakReader.IO
{
    public readonly struct FFragment
    {
        public const uint SkipMax = 127;
        public const uint ValueMax = 127;

        public const uint SkipNumMask = 0x007fu;
        public const uint HasZeroMask = 0x0080u;
        public const int ValueNumShift = 9;
        public const uint IsLastMask  = 0x0100u;
        
        public readonly byte SkipNum; // Number of properties to skip before values
        public readonly bool HasAnyZeroes;
        public readonly byte ValueNum;  // Number of subsequent property values stored
        public readonly bool IsLast; // Is this the last fragment of the header?

        public FFragment(ushort packed)
        {
            SkipNum = (byte) (packed & SkipNumMask);
            HasAnyZeroes = (packed & HasZeroMask) != 0;
            ValueNum = (byte) (packed >> ValueNumShift);
            IsLast = (packed & IsLastMask) != 0;
        }
    }
}