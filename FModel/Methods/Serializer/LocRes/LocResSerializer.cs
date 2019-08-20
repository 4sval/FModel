using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FModel
{
    /*
     * Author: Asval
     * pretty sure it can be refactored
     * 
     * */
    static class LocResSerializer
    {
        private static byte[] _locResMagic = { 0x0E, 0x14, 0x74, 0x75, 0x67, 0x4A, 0x03, 0xFC, 0x4A, 0x15, 0x90, 0x9D, 0xC3, 0x37, 0x7F, 0x1B };
        private static long _localizedStringArrayOffset { get; set; }
        private static string[] _localizedStringArray { get; set; }
        private static string _namespacesString { get; set; }
        private static string _myKey { get; set; }
        public static Dictionary<string, dynamic> LocResDict { get; set; }

        public static void setLocRes(string filepath, bool addToCurrent = false)
        {
            if (!addToCurrent) { LocResDict = new Dictionary<string, dynamic>(); }
            _myKey = "";
            _namespacesString = "";
            _localizedStringArrayOffset = -1;

            using (BinaryReader reader = new BinaryReader(File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Encoding.GetEncoding(1252)))
            {
                byte[] MagicNumber = reader.ReadBytes(16);
                if (MagicNumber.SequenceEqual(_locResMagic))
                {
                    byte VersionNumber = reader.ReadByte();
                    if (VersionNumber == 2) //optimized
                    {
                        _localizedStringArrayOffset = reader.ReadInt64();
                        if (_localizedStringArrayOffset != -1)
                        {
                            long CurrentFileOffset = reader.BaseStream.Position;

                            reader.BaseStream.Seek(_localizedStringArrayOffset, SeekOrigin.Begin);
                            int arrayLength = reader.ReadInt32();

                            reader.BaseStream.Seek(_localizedStringArrayOffset, SeekOrigin.Begin);

                            _localizedStringArray = new string[arrayLength];
                            for (int i = 0; i < _localizedStringArray.Length; i++)
                            {
                                _localizedStringArray[i] = AssetReader.readCleanString(reader);
                            }

                            reader.BaseStream.Seek(CurrentFileOffset, SeekOrigin.Begin);

                            uint NamespaceCount = reader.ReadUInt32();
                            reader.ReadBytes(17);

                            for (uint i = 0; i < NamespaceCount; i++)
                            {
                                reader.ReadInt32();
                                readNamespaces(reader);
                            }
                        }
                    }
                    else { throw new ArgumentException("Unsupported LocRes version."); }
                }
                else { throw new ArgumentException("Wrong LocResMagic number."); }
            }
        }

        private static void readNamespaces(BinaryReader br)
        {
            if (br.BaseStream.Position > _localizedStringArrayOffset) { return; }

            int stringLength = br.ReadInt32();
            if (stringLength > 0)
            {
                _namespacesString = Encoding.GetEncoding(1252).GetString(br.ReadBytes(stringLength)).TrimEnd('\0');
            }
            else if (stringLength == 0)
            {
                _namespacesString = "";
            }
            else
            {
                byte[] data = br.ReadBytes((-1 - stringLength) * 2);
                br.ReadBytes(2);
                _namespacesString = Encoding.Unicode.GetString(data);
            }

            br.ReadInt32();
            int stringIndex = br.ReadInt32();
            if (stringIndex > _localizedStringArray.Length || stringIndex < 0)
            {
                if (!LocResDict.ContainsKey(_namespacesString))
                {
                    LocResDict[_namespacesString] = new Dictionary<string, string>();
                }

                long newOffset = br.BaseStream.Position - 8;
                br.BaseStream.Seek(newOffset, SeekOrigin.Begin);

                int KeyCount = br.ReadInt32();
                for (int i = 0; i < KeyCount; i++)
                {
                    _myKey = AssetReader.readCleanString(br);

                    br.ReadInt32();
                    stringIndex = br.ReadInt32();

                    LocResDict[_namespacesString][_myKey] = _localizedStringArray[stringIndex];
                }
            }
            else
            {
                if (!LocResDict.ContainsKey(_namespacesString))
                {
                    LocResDict[_namespacesString] = new Dictionary<string, string>();
                    LocResDict[_namespacesString] = _localizedStringArray[stringIndex];
                }
            }
        }
    }
}
