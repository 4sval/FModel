using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FModel.Converter
{
    static class UnrealEngineDataToOgg
    {
        private static List<int> SearchBytePattern(byte[] pattern, byte[] bytes)
        {
            List<int> positions = new List<int>();
            int patternLength = pattern.Length;
            int totalLength = bytes.Length;
            byte firstMatchByte = pattern[0];
            for (int i = 0; i < totalLength; i++)
            {
                if (firstMatchByte == bytes[i] && totalLength - i >= patternLength)
                {
                    byte[] match = new byte[patternLength];
                    Array.Copy(bytes, i, match, 0, patternLength);
                    if (match.SequenceEqual(pattern))
                    {
                        positions.Add(i);
                        i += patternLength - 1;
                    }
                }
            }
            return positions;
        }
        private static bool TryFindAndReplace<T>(T[] source, T[] pattern, T[] replacement, out T[] newArray)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            if (pattern == null) { throw new ArgumentNullException(nameof(pattern)); }
            if (replacement == null) { throw new ArgumentNullException(nameof(replacement)); }

            newArray = null;
            if (pattern.Length > source.Length) { return false; }

            for (var start = 0; start < source.Length - pattern.Length + 1; start += 1)
            {
                var segment = new ArraySegment<T>(source, start, pattern.Length);
                if (segment.SequenceEqual(pattern))
                {
                    newArray = replacement.Concat(source.Skip(start + pattern.Length)).ToArray();
                    return true;
                }
            }
            return false;
        }
        private static byte[] Combine(params byte[][] arrays)
        {
            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }

        /// <summary>
        /// if no ubulk: if the uexp contains "OggS", once oggFind is found delete everything from the start to the pattern (excluding the pattern)
        ///              then just delete the last 4 bytes "Áƒ*ž" and all the empty bytes a then end (to avoid increasing file length if there's no sound)
        /// if ubulk: do same as no ubulk but add the ubulk bytes after uexpToDelete is found
        ///           uexpToDelete is a pattern to find to delete everything from this pattern to the end of the uexp file (including the pattern)
        /// </summary>
        /// <param name="file"></param>
        /// <returns> path to the ogg sound file </returns>
        public static string ConvertToOgg(string file)
        {
            byte[] oggFind = { 0x4F, 0x67, 0x67, 0x53 };
            byte[] oggOutNewArray;

            FileInfo isUbulkFound = new DirectoryInfo(Path.GetDirectoryName(file)).GetFiles(Path.GetFileNameWithoutExtension(file) + "*.ubulk", SearchOption.AllDirectories).FirstOrDefault();
            if (isUbulkFound == null)
            {
                if (File.ReadAllText(file).Contains("OggS"))
                {
                    byte[] src = File.ReadAllBytes(file);
                    TryFindAndReplace(src, oggFind, oggFind, out oggOutNewArray); //MAKE THE ARRAY START AT PATTERN POSITION

                    byte[] tmp = new byte[oggOutNewArray.Length - 4];
                    Array.Copy(oggOutNewArray, tmp, tmp.Length); //DELETE LAST 4 BYTES

                    int i = tmp.Length - 7;
                    while (tmp[i] == 0)
                    {
                        --i;
                    }
                    byte[] bar = new byte[i + 1];
                    Array.Copy(tmp, bar, i + 1); //DELETE EMPTY BYTES AT THE END

                    File.WriteAllBytes(App.DefaultOutputPath + "\\Sounds\\" + Path.GetFileNameWithoutExtension(file) + ".ogg", bar);
                }
            }
            else
            {
                if (File.ReadAllText(file).Contains("OggS"))
                {
                    byte[] uexpToDelete = { 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x04, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x05, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x04, 0x00 };
                    byte[] src = File.ReadAllBytes(file);
                    byte[] srcUbulk = File.ReadAllBytes(Path.GetDirectoryName(file) + "\\" + isUbulkFound);

                    List<int> positions = SearchBytePattern(uexpToDelete, src);
                    int lengthToDelete = src.Length - positions[0];

                    TryFindAndReplace(src, oggFind, oggFind, out oggOutNewArray); //MAKE THE ARRAY START AT PATTERN POSITION

                    byte[] tmp = new byte[oggOutNewArray.Length - lengthToDelete];
                    Array.Copy(oggOutNewArray, tmp, Math.Max(0, tmp.Length)); //DELETE LAST BYTES WHEN _uexpToDelete IS FOUND

                    byte[] tmp2 = Combine(tmp, srcUbulk); //ADD UBULK ARRAY TO UEXP ARRAY

                    int i = tmp2.Length - 1;
                    while (tmp2[i] == 0)
                    {
                        --i;
                    }
                    byte[] bar = new byte[i + 1];
                    Array.Copy(tmp2, bar, i + 1); //DELETE EMPTY BYTES AT THE END

                    File.WriteAllBytes(App.DefaultOutputPath + "\\Sounds\\" + Path.GetFileNameWithoutExtension(file) + ".ogg", bar);
                }
            }
            return App.DefaultOutputPath + "\\Sounds\\" + Path.GetFileNameWithoutExtension(file) + ".ogg";
        }
    }
}
