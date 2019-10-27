//////////////////////////////////////////////
// Apache 2.0  - 2016-2018
// Author : Derek Tremblay (derektremblay666@gmail.com)
//////////////////////////////////////////////

using WpfHexaEditor.Core.Bytes;

namespace WpfHexaEditor.Core
{
    /// <summary>
    /// BookMark class
    /// </summary>
    public sealed class BookMark
    {
        public ScrollMarker Marker { get; set; } = ScrollMarker.Nothing;
        public long BytePositionInStream { get; set; }
        public string Description { get; set; } = string.Empty;

        public BookMark() { }

        public BookMark(string description, long position)
        {
            BytePositionInStream = position;
            Description = description;
        }

        public BookMark(string description, long position, ScrollMarker marker)
        {
            BytePositionInStream = position;
            Description = description;
            Marker = marker;
        }

        /// <summary>
        /// String representation
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"({ByteConverters.LongToHex(BytePositionInStream)}h){Description}";
    }
}