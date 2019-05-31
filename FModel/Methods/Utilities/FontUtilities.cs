using FModel.Properties;
using System;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace FModel
{
    static class FontUtilities
    {
        public static readonly PrivateFontCollection pfc = new PrivateFontCollection();
        public static readonly StringFormat centeredString = new StringFormat();
        public static readonly StringFormat rightString = new StringFormat();
        public static readonly StringFormat leftString = new StringFormat();
        public static readonly StringFormat centeredStringLine = new StringFormat();
        private static int _fontLength { get; set; }
        private static byte[] _fontdata { get; set; }

        /// <summary>
        /// Register font in Resources to the PrivateFontCollection to use them in IconGenerator or other stuff
        /// </summary>
        public static void SetFont()
        {
            _fontLength = Resources.BurbankBigCondensed_Bold.Length;
            _fontdata = Resources.BurbankBigCondensed_Bold;
            IntPtr weirdData = Marshal.AllocCoTaskMem(_fontLength);
            Marshal.Copy(_fontdata, 0, weirdData, _fontLength);
            pfc.AddMemoryFont(weirdData, _fontLength);

            _fontLength = Resources.BurbankBigCondensed_Black.Length;
            _fontdata = Resources.BurbankBigCondensed_Black;
            IntPtr weirdData2 = Marshal.AllocCoTaskMem(_fontLength);
            Marshal.Copy(_fontdata, 0, weirdData2, _fontLength);
            pfc.AddMemoryFont(weirdData2, _fontLength);

            centeredString.Alignment = StringAlignment.Center;
            rightString.Alignment = StringAlignment.Far;
            leftString.Alignment = StringAlignment.Near;

            centeredStringLine.LineAlignment = StringAlignment.Center;
            centeredStringLine.Alignment = StringAlignment.Center;
        }
    }
}
