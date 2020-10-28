namespace FModel.Utils
{
    public static class ByteOrderSwap
    {
        public static ulong IntelOrder64(this ulong value)
        {
            value = ((value << 8) & 0xFF00FF00FF00FF00UL ) | ((value >> 8) & 0x00FF00FF00FF00FFUL);
            value = ((value << 16) & 0xFFFF0000FFFF0000UL ) | ((value >> 16) & 0x0000FFFF0000FFFFUL);
            return (value << 32) | (value >> 32);
        }
    }
}