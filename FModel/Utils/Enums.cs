using System;
using System.Runtime.CompilerServices;

namespace FModel.Utils
{
    public static class Enums
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAnyFlags<T>(this T flags, T contains) where T : System.Enum, IConvertible
        {
            return (flags.ToInt32(null) & contains.ToInt32(null)) != 0;
        }
    }
}