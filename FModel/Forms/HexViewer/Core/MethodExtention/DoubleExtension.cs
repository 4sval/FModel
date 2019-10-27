//////////////////////////////////////////////
// Apache 2.0  - 2016-2018
// Author : Derek Tremblay (derektremblay666@gmail.com)
//////////////////////////////////////////////

using System;

namespace WpfHexaEditor.Core.MethodExtention
{
    public static class DoubleExtension
    {
        public static double Round(this double s, int digit = 2) => Math.Round(s, digit);
    }
}