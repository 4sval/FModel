//////////////////////////////////////////////
// Apache 2.0  - 2003-2019
// Author : Derek Tremblay (derektremblay666@gmail.com)
//////////////////////////////////////////////

namespace WpfHexaEditor.Core.CharacterTable
{
    /// <summary>
    /// Type of DTE used in TBL
    /// </summary>
    public enum DteType
    {
        Invalid = -1,
        Ascii = 0,
        Japonais,
        DualTitleEncoding,
        MultipleTitleEncoding,
        EndLine,
        EndBlock
    }

    public enum DefaultCharacterTableType
    {
        Ascii,
        EbcdicWithSpecialChar,
        EbcdicNoSpecialChar
        //MACINTOSH
        //DOS/IBM-ASCII
    }
}