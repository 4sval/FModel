using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace FModel.Extensions
{
    public static class CollectionExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Next<T>(this IList<T> collection, T value)
        {
            var i = collection.IndexOf(value) + 1;
            return i >= collection.Count ? collection.First() : collection[i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Next<T>(this IList<T> collection, int index)
        {
            var i = index + 1;
            return i >= collection.Count ? collection.First() : collection[i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Previous<T>(this IList<T> collection, T value)
        {
            var i = collection.IndexOf(value) - 1;
            return i < 0 ? collection.Last() : collection[i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Previous<T>(this IList<T> collection, int index)
        {
            var i = index - 1;
            return i < 0 ? collection.Last() : collection[i];
        }
    }
}