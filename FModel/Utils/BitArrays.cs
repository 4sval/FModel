using System.Collections;

namespace FModel.Utils
{
    public static class BitArrays
    {

        public static bool Contains(this BitArray array, bool search)
        {
            for (var i = 0; i < array.Count; i++)
            {
                if (array[i])
                    return true;
            }

            return false;
        }
    }
}