using SkiaSharp;

using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows;

namespace FModel.Extensions
{
    public static class ClipboardExtensions
    {
        public static void SetClipboardImage(byte[] pngBytes, string fileName = null)
        {
            Clipboard.Clear();
            var data = new DataObject();
            using var pngMs = new MemoryStream(pngBytes);
            using var image = Image.FromStream(pngMs);
            // As standard bitmap, without transparency support
            data.SetData(DataFormats.Bitmap, image, true);
            // As PNG. Gimp will prefer this over the other two
            data.SetData("PNG", pngMs, false);
            // As DIB. This is (wrongly) accepted as ARGB by many applications
            using var dibMemStream = new MemoryStream(ConvertToDib(pngBytes));
            data.SetData(DataFormats.Dib, dibMemStream, false);
            // Optional fileName
            if (!string.IsNullOrEmpty(fileName))
            {
                var htmlFragment = GenerateHTMLFragment($"<img src=\"{fileName}\"/>");
                data.SetData(DataFormats.Html, htmlFragment);
            }
            // The 'copy=true' argument means the MemoryStreams can be safely disposed after the operation
            Clipboard.SetDataObject(data, true);
        }

        private static byte[] ConvertToDib(byte[] pngBytes = null)
        {
            byte[] bm32bData;
            int width, height;

            {
                using var skBmp = SKBitmap.Decode(pngBytes);
                width = skBmp.Width;
                height = skBmp.Height;
                using var rotated = new SKBitmap(new SKImageInfo(width, height, skBmp.ColorType));
                using var canvas = new SKCanvas(rotated);
                canvas.Scale(1, -1, 0, height / 2.0f);
                canvas.DrawBitmap(skBmp, SKPoint.Empty);
                canvas.Flush();
                bm32bData = rotated.Bytes;
            }

            // BITMAPINFOHEADER struct for DIB.
            const int hdrSize = 0x28;
            var fullImage = new byte[hdrSize + 12 + bm32bData.Length];
            //Int32 biSize;
            WriteIntToByteArray(fullImage, 0x00, 4, true, hdrSize);
            //Int32 biWidth;
            WriteIntToByteArray(fullImage, 0x04, 4, true, (uint)width);
            //Int32 biHeight;
            WriteIntToByteArray(fullImage, 0x08, 4, true, (uint)height);
            //Int16 biPlanes;
            WriteIntToByteArray(fullImage, 0x0C, 2, true, 1);
            //Int16 biBitCount;
            WriteIntToByteArray(fullImage, 0x0E, 2, true, 32);
            //BITMAPCOMPRESSION biCompression = BITMAPCOMPRESSION.BITFIELDS;
            WriteIntToByteArray(fullImage, 0x10, 4, true, 3);
            //Int32 biSizeImage;
            WriteIntToByteArray(fullImage, 0x14, 4, true, (uint)bm32bData.Length);
            // These are all 0. Since .net clears new arrays, don't bother writing them.
            //Int32 biXPelsPerMeter = 0;
            //Int32 biYPelsPerMeter = 0;
            //Int32 biClrUsed = 0;
            //Int32 biClrImportant = 0;

            // The aforementioned "BITFIELDS": colour masks applied to the Int32 pixel value to get the R, G and B values.
            WriteIntToByteArray(fullImage, hdrSize + 0, 4, true, 0x00FF0000);
            WriteIntToByteArray(fullImage, hdrSize + 4, 4, true, 0x0000FF00);
            WriteIntToByteArray(fullImage, hdrSize + 8, 4, true, 0x000000FF);
            Buffer.BlockCopy(bm32bData, 0, fullImage, hdrSize + 12, bm32bData.Length);
            return fullImage;
        }

        private static void WriteIntToByteArray(byte[] data, int startIndex, int bytes, bool littleEndian, uint value)
        {
            var lastByte = bytes - 1;

            if (data.Length < startIndex + bytes)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), "Data array is too small to write a " + bytes + "-byte value at offset " + startIndex + ".");
            }

            for (var index = 0; index < bytes; index++)
            {
                var offs = startIndex + (littleEndian ? index : lastByte - index);
                data[offs] = (byte)(value >> 8 * index & 0xFF);
            }
        }

        private static string GenerateHTMLFragment(string html)
        {
            var sb = new StringBuilder();

            const string header = "Version:0.9\r\nStartHTML:<<<<<<<<<1\r\nEndHTML:<<<<<<<<<2\r\nStartFragment:<<<<<<<<<3\r\nEndFragment:<<<<<<<<<4\r\n";
            const string startHTML = "<html>\r\n<body>\r\n";
            const string startFragment = "<!--StartFragment-->";
            const string endFragment = "<!--EndFragment-->";
            const string endHTML = "\r\n</body>\r\n</html>";

            sb.Append(header);

            var startHTMLLength = header.Length;
            var startFragmentLength = startHTMLLength + startHTML.Length + startFragment.Length;
            var endFragmentLength = startFragmentLength + Encoding.UTF8.GetByteCount(html);
            var endHTMLLength = endFragmentLength + endFragment.Length + endHTML.Length;

            sb.Replace("<<<<<<<<<1", startHTMLLength.ToString("D10"));
            sb.Replace("<<<<<<<<<2", endHTMLLength.ToString("D10"));
            sb.Replace("<<<<<<<<<3", startFragmentLength.ToString("D10"));
            sb.Replace("<<<<<<<<<4", endFragmentLength.ToString("D10"));

            sb.Append(startHTML);
            sb.Append(startFragment);
            sb.Append(html);
            sb.Append(endFragment);
            sb.Append(endHTML);

            return sb.ToString();
        }
    }
}