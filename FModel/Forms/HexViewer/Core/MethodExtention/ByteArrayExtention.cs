//////////////////////////////////////////////
// Apache 2.0  - 2016-2019
// Author : Derek Tremblay (derektremblay666@gmail.com)
//////////////////////////////////////////////

using System.Collections.Generic;
using System.Linq;

namespace WpfHexaEditor.Core.MethodExtention
{
    /// <summary>
    /// Extention methodes for find match in byte[]
    /// </summary>
    public static class ByteArrayExtention
    {
        /// <summary>
        /// Finds all index of byte find
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<En attente>")]
        public static IEnumerable<long> FindIndexOf(this byte[] self, byte[] candidate)
        {
            if (!IsEmptyLocate(self, candidate))
                for (var i = 0; i < self.Length; i++)
                {
                    if (!IsMatch(self, i, candidate))
                        continue;

                    yield return i;
                }
        }

        /// <summary>
        /// Check if match is finded
        /// </summary>
        private static bool IsMatch(byte[] array, long position, byte[] candidate) =>
            candidate.Length <= array.Length - position && !candidate.Where((t, i) => array[position + i] != t).Any();

        /// <summary>
        /// Check if can find
        /// </summary>
        private static bool IsEmptyLocate(byte[] array, byte[] candidate) => array == null
                                                                             || candidate == null
                                                                             || array.Length == 0
                                                                             || candidate.Length == 0
                                                                             || candidate.Length > array.Length;
    }
}