//////////////////////////////////////////////
// Apache 2.0  - 2016-2019
// Author : Derek Tremblay (derektremblay666@gmail.com)
//////////////////////////////////////////////

using WpfHexaEditor.Core.Interfaces;

namespace WpfHexaEditor.Core.Bytes
{
    public class ByteModified : IByteModified
    {
        #region Constructor

        /// <summary>
        /// Default contructor
        /// </summary>
        public ByteModified() { }

        /// <summary>
        /// complete contructor
        /// </summary>
        public ByteModified(byte? val, ByteAction action, long bytePositionInStream, long undoLength)
        {
            Byte = val;
            Action = action;
            BytePositionInStream = bytePositionInStream;
            Length = undoLength;
        }

        #endregion constructor

        #region properties

        /// <summary>
        /// Byte mofidied
        /// </summary>
        public byte? Byte { get; set; }

        /// <summary>
        /// Action have made in this byte
        /// </summary>
        public ByteAction Action { get; set; } = ByteAction.Nothing;

        /// <summary>
        /// Get of Set te position in file
        /// </summary>
        public long BytePositionInStream { get; set; } = -1;

        /// <summary>
        /// Number of byte to undo when this byte is reach
        /// </summary>
        public long Length { get; set; } = 1;

        #endregion properties

        #region Methods

        /// <summary>
        /// Check if the object is valid and data can be used for action
        /// </summary>
        public bool IsValid => BytePositionInStream > -1 && Action != ByteAction.Nothing && Byte != null;

        /// <summary>
        /// String representation of byte
        /// </summary>
        public override string ToString() =>
            $"ByteModified - Action:{Action} Position:{BytePositionInStream} Byte:{Byte}";

        /// <summary>
        /// Clear object
        /// </summary>
        public void Clear()
        {
            Byte = null;
            Action = ByteAction.Nothing;
            BytePositionInStream = -1;
            Length = 1;
        }

        /// <summary>
        /// Copy Current instance to another
        /// </summary>
        /// <returns></returns>
        public ByteModified GetCopy() => new ByteModified
        {
            Action = Action,
            Byte = Byte,
            Length = Length,
            BytePositionInStream = BytePositionInStream
        };

        /// <summary>
        /// Get if bytemodified is valid
        /// </summary>
        public static bool CheckIsValid(ByteModified byteModified) => byteModified != null && byteModified.IsValid;

        #endregion Methods
        
    }
}