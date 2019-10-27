//////////////////////////////////////////////
// Apache 2.0  - 2018
// Author : Derek Tremblay (derektremblay666@gmail.com)
//////////////////////////////////////////////

using System;
using System.Windows.Media;
using WpfHexaEditor.Core.Bytes;

namespace WpfHexaEditor.Core.Xcbb
{
    /// <summary>
    /// IMPLEMENTATION NOT COMPLETED
    /// Used to create block of custom colors background 
    /// </summary>
    /// TODO : Add programated positon..
    public class CustomBackgroundBlock
    {
        private long _length;

        public CustomBackgroundBlock() { }

        public CustomBackgroundBlock(long start, long length, SolidColorBrush color, string description)
        {
            StartOffset = start;
            Length = length;
            Color = color;
            Description = description;
        }

        public CustomBackgroundBlock(long start, long length, SolidColorBrush color)
        {
            StartOffset = start;
            Length = length;
            Color = color;
        }

        public CustomBackgroundBlock(string start, long length, SolidColorBrush color)
        {
            var srt = ByteConverters.HexLiteralToLong(start);

            StartOffset = srt.success ? srt.position : throw new Exception("Can't convert this string to long");
            Length = length;
            Color = color;
        }

        public CustomBackgroundBlock(string start, long length, SolidColorBrush color, string description)
        {
            var srt = ByteConverters.HexLiteralToLong(start);

            StartOffset = srt.success ? srt.position : throw new Exception("Can't convert this string to long");
            Length = length;
            Color = color;
            Description = description;
        }

        /// <summary>
        /// Get or set the start offset
        /// </summary>
        public long StartOffset { get; set; }

        /// <summary>
        /// Get the stop offset
        /// </summary>
        public long StopOffset => StartOffset + Length - 1;

        /// <summary>
        /// Get or set the lenght of background block
        /// </summary>
        public long Length
        {
            get => _length;
            set => _length = value > 0 ? value : 1;
        }

        /// <summary>
        /// Description of background block
        /// </summary>
        public string Description { get; set; }

        public SolidColorBrush Color { get; set; } = Brushes.Transparent;
    }
}
