using System.Collections;
using System.Collections.Generic;
using System.IO;
using FModel.Utils;

namespace FModel.PakReader.IO
{
    public class FUnversionedHeader
    {
        public List<FFragment> Fragments = new List<FFragment>();
        public BitArray ZeroMask;
        public readonly bool HasNonZeroValues;
        public bool HasValues => HasNonZeroValues | (ZeroMask.Count > 0);
        
        public FUnversionedHeader(BinaryReader reader)
        {
            FFragment fragment;
            int zeroMaskNum = 0;
            uint unmaskedNum = 0;
            do
            {
                fragment = new FFragment(reader.ReadUInt16());
                
                Fragments.Add(fragment);
                if (fragment.HasAnyZeroes)
                    zeroMaskNum += fragment.ValueNum;
                else
                    unmaskedNum += fragment.ValueNum;
            } while (!fragment.IsLast);

            if (zeroMaskNum > 0)
            {
                LoadZeroMaskData(reader, zeroMaskNum, out ZeroMask);
                HasNonZeroValues = unmaskedNum > 0 || ZeroMask.Contains(false);
            }
            else
            {
                ZeroMask = new BitArray(new int[8]);
                HasNonZeroValues = unmaskedNum > 0;
            }
        }

        private static void LoadZeroMaskData(BinaryReader reader, int numBits, out BitArray data)
        {
            if (numBits <= 8)
            {
                data = new BitArray(new[] { reader.ReadByte() });
            }
            else if (numBits <= 16)
            {
                data = new BitArray(new []{ (int) reader.ReadUInt16() });
            }
            else
            {
                var num = numBits.DivideAndRoundUp(32);
                var intData = new int[num];
                for (int idx = 0; idx < num; idx++)
                {
                    intData[idx] = reader.ReadInt32();
                }
                data = new BitArray(intData);
            }
        }
    }
}