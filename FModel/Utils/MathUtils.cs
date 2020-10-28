using System.Runtime.CompilerServices;

namespace FModel.Utils
{
    public static class MathUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int DivideAndRoundUp(this int dividend, int divisor) => (dividend + divisor - 1) / divisor;
    }
}