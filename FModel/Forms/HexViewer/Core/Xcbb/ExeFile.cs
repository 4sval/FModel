using System.Collections.Generic;
using System.Windows.Media;
using WpfHexaEditor.Core.Bytes;
using static FModel.Properties.Resources;

namespace WpfHexaEditor.Core.Xcbb
{
    /// <summary>
    /// IMPLEMENTATON NOT COMPLETED - FOR TESTING CONCEPT ONLY
    /// Window executable file custom background block.
    /// Will be implemented in XML file (XCBB)...
    /// Will de deleted soon in futher commit
    /// </summary>
    /// TODO : Localize string
    public class ExeFile
    {
        public List<CustomBackgroundBlock> GetCustomBackgroundBlock() =>
            new List<CustomBackgroundBlock>
            {
                new CustomBackgroundBlock(0, 2, Brushes.BlueViolet,         CBB_EXEFile_MagicNumberString),
                new CustomBackgroundBlock(2, 2, Brushes.Brown,              CBB_EXEFile_BytesInLastBlockString),
                new CustomBackgroundBlock(4, 2, Brushes.SeaGreen,           CBB_EXEFile_NumberOfBlockInFileBlockString),
                new CustomBackgroundBlock(6, 2, Brushes.CadetBlue,          CBB_EXEFile_NumberOfRelocationEntriesString),
                new CustomBackgroundBlock(8, 2, Brushes.DarkGoldenrod,      CBB_EXEFile_NumberOfRelocationEntriesString),
                new CustomBackgroundBlock("0x0A", 2, Brushes.Coral,         CBB_EXEFile_NumberOfHeaderParagraphAdditionalMemoryString),
                new CustomBackgroundBlock("0x0C", 2, Brushes.HotPink,       CBB_EXEFile_MaxNumberOfHeaderParagraphAdditionalMemoryString),
                new CustomBackgroundBlock("0x0E", 2, Brushes.Cyan,          CBB_EXEFile_RelativeValueOfStackSegmentString),
                new CustomBackgroundBlock("0x10", 2, Brushes.IndianRed,     CBB_EXEFile_InitialValueOfSPRegisterString),
                new CustomBackgroundBlock("0x12", 2, Brushes.LimeGreen,     CBB_EXEFile_WordChecksumString),
                new CustomBackgroundBlock("0x14", 2, Brushes.PaleTurquoise, CBB_EXEFile_InitialValueOfIPRegisterString),
                new CustomBackgroundBlock("0x16", 2, Brushes.DarkOrange,    CBB_EXEFile_InitialValueOfCSRegisterString),
                new CustomBackgroundBlock("0x18", 2, Brushes.Chartreuse,    CBB_EXEFile_OffsetOfTheFirstRelocationItemString),
                new CustomBackgroundBlock("0x1A", 2, Brushes.DarkSeaGreen,  CBB_EXEFile_OverlayNumberString),
            };

        /// <summary>
        /// Detect if is a PE file and create the backgroung based on file data
        /// </summary>
        /// TODO : complete with other various custombackground based on file
        public List<CustomBackgroundBlock> GetCustomBackgroundBlock(ByteProvider provider)
        {
            if (ByteProvider.CheckIsOpen(provider))
            {
                //Added only if is a PE file...
                if (ByteConverters.ByteToHex(provider.GetCopyData(0, 1, true)) == "4D 5A")
                {
                    //Load default
                    var list = GetCustomBackgroundBlock();

                    //Add CBB : This program cannot be run in DOS mode
                    list.Add(new CustomBackgroundBlock("0x4E", 38, Brushes.PaleVioletRed, CBB_EXEFile_NotDOSProgramString));

                    return list;
                }
            }

            return new List<CustomBackgroundBlock>();

        }
    }
}