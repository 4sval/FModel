using FModel.Properties;
using System;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
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

            _fontLength = Resources.BurbankBigCondensed_Black_vJapanese.Length;
            _fontdata = Resources.BurbankBigCondensed_Black_vJapanese;
            IntPtr weirdData3 = Marshal.AllocCoTaskMem(_fontLength);
            Marshal.Copy(_fontdata, 0, weirdData3, _fontLength);
            pfc.AddMemoryFont(weirdData3, _fontLength);

            centeredString.Alignment = StringAlignment.Center;
            rightString.Alignment = StringAlignment.Far;
            leftString.Alignment = StringAlignment.Near;

            centeredStringLine.LineAlignment = StringAlignment.Center;
            centeredStringLine.Alignment = StringAlignment.Center;
        }

        /// <summary>
        /// https://stackoverflow.com/questions/19674743/dynamically-resizing-font-to-fit-space-while-using-graphics-drawstring/19674954
        /// </summary>
        /// <param name="g"></param>
        /// <param name="longString"></param>
        /// <param name="Room"></param>
        /// <param name="PreferedFont"></param>
        /// <returns></returns>
        public static Font FindFont(Graphics g, string longString, Size Room, Font PreferedFont)
        {
            SizeF RealSize = g.MeasureString(longString, PreferedFont);
            float HeightScaleRatio = Room.Height / RealSize.Height;
            float WidthScaleRatio = Room.Width / RealSize.Width;

            float ScaleRatio = (HeightScaleRatio < WidthScaleRatio)
               ? ScaleRatio = HeightScaleRatio
               : ScaleRatio = WidthScaleRatio;

            float ScaleFontSize = PreferedFont.Size * ScaleRatio;

            return new Font(PreferedFont.FontFamily, ScaleFontSize);
        }

        public static void ConvertToTtf(string file)
        {
            if (File.Exists(Path.ChangeExtension(file, ".ttf"))) File.Delete(Path.ChangeExtension(file, ".ttf"));

            File.Move(file, Path.ChangeExtension(file, ".ttf"));
            new UpdateMyState(ThePak.CurrentUsedItem + " successfully converted to a font", "Success").ChangeProcessState();
        }
    }
}
