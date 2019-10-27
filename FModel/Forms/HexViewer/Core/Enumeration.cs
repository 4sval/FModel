//////////////////////////////////////////////
// Apache 2.0  - 2016-2018
// Author : Derek Tremblay (derektremblay666@gmail.com)
//////////////////////////////////////////////

using System;

namespace WpfHexaEditor.Core
{
    /// <summary>
    /// ByteAction used for ByteModified class
    /// </summary>
    public enum ByteAction
    {
        Nothing,
        Added,
        Deleted,
        Modified,

        /// <summary>
        /// Used in ByteProvider for get list
        /// </summary>
        All
    }

    /// <summary>
    /// Used for coloring mode of selection
    /// </summary>
    public enum FirstColor
    {
        HexByteData,
        StringByteData
    }

    /// <summary>
    /// Mode of Copy/Paste
    /// </summary>
    public enum CopyPasteMode
    {
        Byte,
        HexaString,
        AsciiString,
        TblString,
        CSharpCode,
        VbNetCode,
        JavaCode,
        CCode,
        FSharpCode,
        PascalCode
    }

    /// <summary>
    /// Used with Copy to code fonction for language are similar to C.
    /// </summary>
    internal enum CodeLanguage
    {
        C,
        CSharp,
        Java,
        FSharp,
        Vbnet,
        Pascal
    }

    /// <summary>
    /// Used for check label are selected et next label to select...
    /// </summary>
    public enum KeyDownLabel
    {
        FirstChar,
        SecondChar,

        //ThirdChar,
        NextPosition
    }

    public enum ByteToString
    {
        /// <summary>
        /// Build-in convertion mode. (recommended)
        /// </summary>
        ByteToCharProcess,

        /// <summary>
        /// System.Text.Encoding.ASCII string encoder
        /// </summary>
        AsciiEncoding
    }

    /// <summary>
    /// Scrollbar marker
    /// </summary>
    public enum ScrollMarker
    {
        Nothing,
        SearchHighLight,
        Bookmark,
        SelectionStart,
        ByteModified,
        ByteDeleted,
        TblBookmark
    }

    /// <summary>
    /// Type are opened in byteprovider
    /// </summary>
    ///[Obsolete("The ByteProviderStreamType is low extensible for variety of stream source,and will be removed in next release.")]
    public enum ByteProviderStreamType
    {
        File,
        MemoryStream,
        Nothing
    }

    /// <summary>
    /// Type of character are used
    /// </summary>
    public enum CharacterTableType
    {
        Ascii,
        TblFile
    }

    /// <summary>
    /// Used for control the speed of mouse wheel
    /// </summary>
    public enum MouseWheelSpeed
    {
        VerySlow = 1,
        Slow = 3,
        Normal = 5,
        Fast = 7,
        VeryFast = 9,
        System
    }

    /// <summary>
    /// IByteControl spacer width
    /// </summary>
    public enum ByteSpacerWidth
    {
        VerySmall = 1,
        Small = 3,
        Normal = 6,
        Large = 9,
        VeryLarge = 12
    }

    public enum ByteSpacerGroup
    {
        TwoByte = 2,
        FourByte = 4,
        SixByte = 6,
        EightByte = 8
    }

    public enum ByteSpacerPosition
    {
        HexBytePanel,
        StringBytePanel,
        Both,
        Nothing
    }

    public enum ByteSpacerVisual
    {
        Empty,
        Line,
        Dash
    }

    /// <summary>
    /// Used with the view mode of HexByte, header or position.
    /// </summary>
    public enum DataVisualType
    {
        Hexadecimal,    //Editable
        Decimal         //Not editable
        //Binary        //Editable
    }

    /// <summary>
    /// Used to select the visual of the offset panel
    /// </summary>
    public enum OffSetPanelType
    {
        OffsetOnly,
        LineOnly,
        Both
    }

    /// <summary>
    /// Used to fix the wigth of the offset panel
    /// </summary>
    public enum OffSetPanelFixedWidth
    {
        Dynamic,
        Fixed
    }

    /// <summary>
    /// Use mode of the caret
    /// </summary>
    public enum CaretMode
    {
        Insert,
        Overwrite
    }
}