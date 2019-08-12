using csharp_wick;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace FModel
{
    class UpdateMyConsole
    {
        private string _textToDisplay;
        private Color _displayedColor;
        private bool _newLine;
        private HorizontalAlignment _hAlign;
        public UpdateMyConsole(string textToDisplay, Color displayedColor, bool newLine = false, HorizontalAlignment hAlign = HorizontalAlignment.Left)
        {
            _textToDisplay = textToDisplay;
            _displayedColor = displayedColor;
            _newLine = newLine;
            _hAlign = hAlign;
        }

        public void AppendToConsole()
        {
            App.MainFormToUse.AppendTextToConsole(_textToDisplay, _displayedColor, _newLine, _hAlign);
        }
    }
    class UpdateMyState
    {
        private string _textToDisplay;
        private string _stateText;
        public UpdateMyState(string textToDisplay, string stateText)
        {
            _textToDisplay = textToDisplay;
            _stateText = stateText;
        }

        public void ChangeProcessState()
        {
            App.MainFormToUse.UpdateProcessState(_textToDisplay, _stateText);
        }
    }

    static class ThePak
    {
        public static List<PaksEntry> mainPaksList { get; set; }
        public static List<PaksEntry> dynamicPaksList { get; set; }
        public static string CurrentUsedItem { get; set; }

        public static Dictionary<string, string> AllpaksDictionary { get; set; }
        public static Dictionary<string, PakExtractor> PaksExtractorDictionary { get; set; }
        public static Dictionary<PakExtractor, string[]> PaksFileArrayDictionary { get; set; }

        /// <summary>
        /// read the GUID of a the param, it's basically just reading some bytes at the end of a pak file, but it's useful to tell if the pak is dynamically encrypted
        /// "0-0-0-0" for normal pak
        /// "123456789-9876543210-741085209630-015975306482" for dynamic pak (these numbers are an example)
        /// </summary>
        /// <param name="pakPath"> path to the pak file to get its GUID </param>
        /// <returns> the guid </returns>
        public static string ReadPakGuid(string pakPath)
        {
            using (BinaryReader reader = new BinaryReader(File.Open(pakPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                reader.BaseStream.Seek(reader.BaseStream.Length - 61 - 160, SeekOrigin.Begin);
                uint g1 = reader.ReadUInt32();
                reader.BaseStream.Seek(reader.BaseStream.Length - 57 - 160, SeekOrigin.Begin);
                uint g2 = reader.ReadUInt32();
                reader.BaseStream.Seek(reader.BaseStream.Length - 53 - 160, SeekOrigin.Begin);
                uint g3 = reader.ReadUInt32();
                reader.BaseStream.Seek(reader.BaseStream.Length - 49 - 160, SeekOrigin.Begin);
                uint g4 = reader.ReadUInt32();

                string guid = g1 + "-" + g2 + "-" + g3 + "-" + g4;
                return guid;
            }
        }

        /// <summary>
        /// read the pak version (v8 28.05.2019), it's basically just reading one byte at the end of a pak file
        /// </summary>
        /// <param name="pakPath"> path to the pak file to get its version </param>
        /// <returns> the version </returns>
        public static string ReadPakVersion(string pakPath)
        {
            using (BinaryReader reader = new BinaryReader(File.Open(pakPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                reader.BaseStream.Seek(reader.BaseStream.Length - 40 - 160, SeekOrigin.Begin);
                uint version = reader.ReadUInt32();

                return version.ToString();
            }
        }
    }

    static class App
    {
        public static MainWindow MainFormToUse = null;
        public static string DefaultOutputPath { get; set; }
    }

    static class Checking
    {
        public static bool WasFeatured { get; set; }
        public static bool UmWorking { get; set; }
        public static string BackupFileName { get; set; }
        public static string ExtractedFilePath { get; set; }
        public static bool DifferenceFileExists = false;
        public static string currentSelectedNodePartialPath { get; set; }
    }

    public struct PaksEntry : IEquatable<PaksEntry>
    {
        internal PaksEntry(string myPak, string myPakGuid)
        {
            thePak = myPak;
            thePakGuid = myPakGuid;
        }
        public string thePak { get; set; }
        public string thePakGuid { get; set; }

        bool IEquatable<PaksEntry>.Equals(PaksEntry other)
        {
            throw new NotImplementedException();
        }
    }

    public struct AESEntry : IEquatable<AESEntry>
    {
        internal AESEntry(string myPak, string myKey)
        {
            thePak = myPak;
            theKey = myKey;
        }
        public string thePak { get; set; }
        public string theKey { get; set; }

        bool IEquatable<AESEntry>.Equals(AESEntry other)
        {
            throw new NotImplementedException();
        }
    }
}
