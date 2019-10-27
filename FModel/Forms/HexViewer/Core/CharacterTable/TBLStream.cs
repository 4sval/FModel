//////////////////////////////////////////////
// Apache 2.0  - 2003-2019
// Author : Derek Tremblay (derektremblay666@gmail.com)
//////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WpfHexaEditor.Core.Bytes;

namespace WpfHexaEditor.Core.CharacterTable
{
    /// <summary>
    /// Used to manage Thingy TBL file (entry=value)
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "<En attente>")]
    public sealed class TblStream : IDisposable
    {
        #region Global class variables        
        /// <summary>
        /// TBL file path
        /// </summary>
        private string _fileName = string.Empty;

        /// <summary>
        /// Represente the whole TBL file
        /// </summary>
        private Dictionary<string, Dte> _dteList = new Dictionary<string, Dte>();
        #endregion

        #region Constructors
        /// <summary>
        /// Constructeur permétant de charg?le fichier DTE
        /// </summary>
        public TblStream(string fileName) => FileName = fileName;

        /// <summary>
        /// Constructeur permétant de chargéle fichier DTE
        /// </summary>
        public TblStream() { }
        #endregion

        #region Indexer

        /// <summary>
        /// Indexer to work on the DTE contained in TBL in the manner of a table.
        /// </summary>
        public Dte this[string index]
        {
            get => _dteList[index];
            set => _dteList[index] = value;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Find entry in TBL file
        /// </summary>
        /// <param name="hex">Hex value to find match</param>
        /// <param name="showSpecialValue">Fin the Endblock and EndLine</param>
        public string FindMatch(string hex, bool showSpecialValue)
        {
            if (showSpecialValue)
            {
                if (_dteList.ContainsKey($"/{hex}")) return FModel.Properties.Resources.EndTagString; //"<end>";
                if (_dteList.ContainsKey($"*{hex}")) return FModel.Properties.Resources.LineTagString; //"<ln>";
            }

            return _dteList.ContainsKey(hex) 
                ? _dteList[hex].Value
                : "#";
        }

        /// <summary>
        /// Convert data to TBL string. 
        /// </summary>
        /// <returns>
        /// Return string converted to TBL string representation.
        /// Return null on error
        /// </returns>
        public string ToTblString(byte[] data)
        {
            if (data == null) return null;

            var sb = new StringBuilder();

            for (var i = 0; i < data.Length; i++)
            {
                if (i < data.Length - 1)
                {
                    var mte = FindMatch(ByteConverters.ByteToHex(data[i]) + ByteConverters.ByteToHex(data[i + 1]), true);

                    if (mte != "#")
                    {
                        sb.Append(mte);
                        continue;
                    }
                }

                sb.Append(FindMatch(ByteConverters.ByteToHex(data[i]), true));
            }

            return sb.ToString();
        }
        
        /// <summary>
        /// Close the TBL and clear object
        /// </summary>
        public void Close()
        {
            _fileName = string.Empty;
            _dteList.Clear();
        }

        /// <summary>
        /// Load the TBL file
        /// </summary>
        private void Load(string tblString)
        {
            //Variables
            char[] sepEndLine = {'\n'}; //end line char
            char[] sepEqual = {'='}; //equal separator char

            //build strings line
            var textFromString = new StringBuilder(tblString);
            textFromString.Insert(textFromString.Length, new[] { '\r', '\n' });
            var lines = textFromString.ToString().Split(sepEndLine);
            
            //Clear before loading
            _dteList.Clear();

            #region Fill dtelist dictionary 
            foreach (var line in lines)
            {
                var info = line.Split(sepEqual);

                Dte dte;
                try
                {
                    switch (info[0].Length)
                    {
                        case 2:
                            dte = info[1].Length == 2
                                ? new Dte(info[0], info[1].Substring(0, info[1].Length - 1), DteType.Ascii)
                                : new Dte(info[0], info[1].Substring(0, info[1].Length - 1),
                                    DteType.DualTitleEncoding);
                            break;
                        case 4: // >2
                            dte = new Dte(info[0], info[1].Substring(0, info[1].Length - 1),
                                DteType.MultipleTitleEncoding);
                            break;
                        default:
                            continue;
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    switch (info[0].Substring(0, 1))
                    {
                        case @"/":
                            dte = new Dte(info[0].Substring(0, info[0].Length - 1), string.Empty, DteType.EndBlock);
                            break;
                        case @"*":
                            dte = new Dte(info[0].Substring(0, info[0].Length - 1), string.Empty, DteType.EndLine);
                            break;
                        default:
                            continue;
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    //Occurs when two == are in the same line
                    dte = new Dte(info[0], "=", DteType.DualTitleEncoding);
                }

                _dteList.Add(dte.Entry, dte);
                
            }
            #endregion

            #region Load bookmark
            BookMarks.Clear();
            foreach (var line in lines)
            {
                try
                {
                    if (line.Substring(0, 1) == "(")
                    {
                        var fav = new BookMark();
                        var lineSplited = line.Split(')');
                        fav.Description = lineSplited[1].Substring(0, lineSplited[1].Length - 1);

                        lineSplited = line.Split('h');
                        fav.BytePositionInStream = 
                            ByteConverters.HexLiteralToLong(lineSplited[0].Substring(1, lineSplited[0].Length - 1)).position;
                        fav.Marker = ScrollMarker.TblBookmark;
                        BookMarks.Add(fav);
                    }
                }
                catch
                {
                    //Nothing to add if error
                }
            }
            #endregion
        }

        /// <summary>
        /// Load TBL file
        /// </summary>
        private void Load()
        {
            //ouverture du fichier
            if (!File.Exists(_fileName))
            {
                var fs = File.Create(_fileName);
                fs.Close();
            }

            StreamReader tblFile;
            try
            {
                tblFile = new StreamReader(_fileName, Encoding.ASCII);
            }
            catch
            {
                return;
            }

            if (tblFile.BaseStream.CanRead)
                Load(tblFile.ReadToEnd());

            tblFile.Close();
        }

        /// <summary>
        /// Save tbl file
        /// </summary>
        public void Save()
        {
            var myFile = new FileStream(_fileName, FileMode.Create, FileAccess.Write);
            var tblFile = new StreamWriter(myFile, Encoding.Unicode); //ASCII

            if (tblFile.BaseStream.CanWrite)
            {
                //Save tbl set
                foreach (var dte in _dteList)
                    if (dte.Value.Type != DteType.EndBlock && 
                        dte.Value.Type != DteType.EndLine)
                        tblFile.WriteLine(dte.Value.Entry + "=" + dte.Value);
                    else
                        tblFile.WriteLine(dte.Value.Entry);

                //Save bookmark
                tblFile.WriteLine();
                foreach (var mark in BookMarks)
                    tblFile.WriteLine(mark.ToString());

                //Add to line at end of file. Needed for some apps that using tbl file
                tblFile.WriteLine();
                tblFile.WriteLine();
            }

            //close file
            tblFile.Close();
        }

        /// <summary>
        /// Add a DTE/MTE in TBL
        /// </summary>
        public void Add(Dte dte) => _dteList.Add(dte.Entry, dte);

        /// <summary>
        /// Remove TBL entry
        /// </summary>
        /// <param name="dte"></param>
        public void Remove(Dte dte) => _dteList.Remove(dte.Entry);

        #endregion

        #region Property
        /// <summary>
        /// Get or set the File path to TBL
        /// </summary>
        public string FileName
        {
            get => _fileName;
            internal set
            {
                if (File.Exists(value))
                {
                    _fileName = value;
                    Load();
                }
                else
                    throw new FileNotFoundException();
            }
        }

        /// <summary>
        /// Get the count of DTE/MTE in the TBL
        /// </summary>
        public int Length => _dteList.Count;

        /// <summary>
        /// Get of set bookmarks
        /// </summary>
        public List<BookMark> BookMarks { get; set; } = new List<BookMark>();

        public int TotalDte => _dteList.Count(l => l.Value.Type == DteType.DualTitleEncoding);
        public int TotalMte => _dteList.Count(l => l.Value.Type == DteType.MultipleTitleEncoding);
        public int TotalAscii => _dteList.Count(l => l.Value.Type == DteType.Ascii);
        public int TotalInvalid => _dteList.Count(l => l.Value.Type == DteType.Invalid);
        public int TotalJaponais => _dteList.Count(l => l.Value.Type == DteType.Japonais);
        public int TotalEndLine => _dteList.Count(l => l.Value.Type == DteType.EndLine);
        public int TotalEndBlock => _dteList.Count(l => l.Value.Type == DteType.EndBlock);

        /// <summary>
        /// Get the end block char
        /// </summary>
        public string EndBlock
        {
            get
            {
                foreach (var dte in _dteList)
                    if (dte.Value.Type == DteType.EndBlock)
                        return dte.Value.Entry;

                return string.Empty;
            }
        }

        /// <summary>
        /// Get the end line char
        /// </summary>
        public string EndLine
        {
            get
            {
                foreach (var dte in _dteList)
                    if (dte.Value.Type == DteType.EndLine)
                        return dte.Value.Entry;

                return string.Empty;
            }
        }

        /// <summary>
        /// Enable/Disable Readonly on control.
        /// </summary>
        public bool AllowEdit { get; set; }

        #endregion

        #region Build default TBL

        public static TblStream CreateDefaultTbl(DefaultCharacterTableType type = DefaultCharacterTableType.Ascii)
        {
            var tbl = new TblStream();

            switch (type)
            {
                case DefaultCharacterTableType.Ascii:
                    for (byte i = 0; i < 255; i++)
                        tbl.Add(new Dte(ByteConverters.ByteToHex(i).ToUpper(), $"{ByteConverters.ByteToChar(i)}"));
                    break;
                case DefaultCharacterTableType.EbcdicWithSpecialChar:
                    tbl.Load(FModel.Properties.Resources.EBCDIC);
                    break;
                case DefaultCharacterTableType.EbcdicNoSpecialChar:
                    tbl.Load(FModel.Properties.Resources.EBCDIC_NoSpecialChar);
                    break;
            }

            tbl.AllowEdit = true;
            return tbl;
        }

        #endregion

        #region IDisposable Support

        private bool _disposedValue; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _dteList = null;
                }

                _disposedValue = true;
            }
        }

        public void Dispose() => Dispose(true);

        #endregion
    }
}