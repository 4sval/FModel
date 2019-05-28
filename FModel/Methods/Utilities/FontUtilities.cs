using FModel.Properties;
using System;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace FModel
{
    class FontUtilities
    {
        public static PrivateFontCollection _pfc = new PrivateFontCollection();
        public static StringFormat _centeredString = new StringFormat();
        public static StringFormat _rightString = new StringFormat();
        public static StringFormat _centeredStringLine = new StringFormat();
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
            _pfc.AddMemoryFont(weirdData, _fontLength);

            _fontLength = Resources.BurbankBigCondensed_Black.Length;
            _fontdata = Resources.BurbankBigCondensed_Black;
            IntPtr weirdData2 = Marshal.AllocCoTaskMem(_fontLength);
            Marshal.Copy(_fontdata, 0, weirdData2, _fontLength);
            _pfc.AddMemoryFont(weirdData2, _fontLength);

            _centeredString.Alignment = StringAlignment.Center;
            _rightString.Alignment = StringAlignment.Far;
            _centeredStringLine.LineAlignment = StringAlignment.Center;
            _centeredStringLine.Alignment = StringAlignment.Center;
        }
    }
}
