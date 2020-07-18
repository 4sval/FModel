using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FModel.PakReader
{
    /// <summary>
    /// http://wiki.xentax.com/index.php/Wwise_SoundBank_(*.bnk)
    /// don't copy paste pls, try to understand what you write from this
    /// DATASection holds the .wem files
    /// STIDSection holds the .wem files name in a dictionary where the key is the .wem file id from DATASection
    /// i see no use (for FModel) in other sections atm
    /// </summary>
    public class WwiseReader
    {
        private const uint _AKPK_ID = 0x4B504B41;
        private const uint _BKHD_ID = 0x44484B42;
        private const uint _INIT_ID = 0x54494E49;
        private const uint _DIDX_ID = 0x58444944;
        private const uint _DATA_ID = 0x41544144;
        private const uint _HIRC_ID = 0x43524948;
        //private const uint _RIFF_ID = 0x46464952;
        private const uint _STID_ID = 0x44495453;
        private const uint _STMG_ID = 0x474D5453;
        private const uint _ENVS_ID = 0x53564E45;
        private const uint _PLAT_ID = 0x54414C50;
        public Dictionary<string, byte[]> AudioFiles;

        public WwiseReader(BinaryReader reader)
        {
            AKPKSection akpkSection = null;
            DIDXSection didxSection = null;
            DATASection dataSection = null;
            HIRCSection hircSection = null;
            STIDSection stidSection = null;
            //STMGSection stmgSection = null;
            PLATSection platSection = null;
            AudioFiles = new Dictionary<string, byte[]>();

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                uint SectionIdentifier = reader.ReadUInt32();
                uint SectionLength = reader.ReadUInt32();
                long Position = reader.BaseStream.Position;
                switch (SectionIdentifier)
                {
                    case _AKPK_ID:
                        akpkSection = new AKPKSection(reader);
                        break;
                    case _BKHD_ID:
                        BKHDSection _A = new BKHDSection(reader);
                        break;
                    case _INIT_ID:
                        INITSection _B = new INITSection(reader);
                        break;
                    case _DIDX_ID:
                        didxSection = new DIDXSection(reader, Position + SectionLength);
                        break;
                    case _DATA_ID:
                        if (didxSection != null) dataSection = new DATASection(reader, Position, didxSection);
                        break;
                    case _HIRC_ID:
                        hircSection = new HIRCSection(reader);
                        break;
                    case _STID_ID:
                        stidSection = new STIDSection(reader);
                        break;
                    case _STMG_ID:
                        //stmgSection = new STMGSection(reader); //broken
                        break;
                    case _ENVS_ID:
                        break;
                    case _PLAT_ID:
                        platSection = new PLATSection(reader);
                        break;
#if DEBUG
                    default:
                        System.Diagnostics.Debug.WriteLine($"Unknown section 0x{SectionIdentifier:X} at {Position - sizeof(uint) - sizeof(uint)}");
                        break;
#endif
                }

                if (reader.BaseStream.Position != Position + SectionLength)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"Didn't read 0x{SectionIdentifier:X} correctly (at {reader.BaseStream.Position}, should be {Position + SectionLength})");
#endif
                    reader.BaseStream.Seek(Position + SectionLength, SeekOrigin.Begin);
                }
            }

            if (didxSection != null && dataSection != null && didxSection.WemFilesRef.Count == dataSection.WemFiles.Count)
            {
                for (int i = 0; i < didxSection.WemFilesRef.Count; i++)
                {
                    string key = $"{didxSection.WemFilesRef[i].Id}.wem";
                    if (stidSection != null && stidSection.SoundBanks.TryGetValue(didxSection.WemFilesRef[i].Id, out string name))
                        key = name;

                    AudioFiles[key] = dataSection.WemFiles[i];
                }
            }
            if (akpkSection != null)
            {
                foreach (var folder in akpkSection.Folders)
                    foreach (var entry in folder.Entries)
                        if (!entry.IsSoundBank)
                        {
                            string key = $"{entry.Path.ToUpper()}_{entry.NameHash}.wem";
                            if (stidSection != null && stidSection.SoundBanks.TryGetValue(entry.NameHash, out string name))
                                key = name;

                            reader.BaseStream.Seek(entry.Offset, SeekOrigin.Begin);
                            AudioFiles[key] = reader.ReadBytes(Convert.ToInt32(entry.Size));
                        }
            }
            // valorant event sound uses the HIRCSection but i don't understand how to get the actual audio from it atm
        }

        /// <summary>
        /// https://github.com/Nibre/HaloWwise/blob/master/HaloWwise/PackManager.cs
        /// </summary>
        public class AKPKSection
        {
            public uint FolderListLength;
            public uint BankTableLength;
            public uint SoundTableLength;
            public uint FolderNumber;
            public Folder[] Folders;

            public AKPKSection(BinaryReader reader)
            {
                reader.ReadUInt32(); // Skip
                FolderListLength = reader.ReadUInt32();
                BankTableLength = reader.ReadUInt32();
                SoundTableLength = reader.ReadUInt32();
                reader.ReadUInt32(); // Skip

                FolderNumber = reader.ReadUInt32();
                Folders = new Folder[FolderNumber];
                /* idk how to explain how to do it in another way */
                for (int i = 0; i < Folders.Length; i++)
                {
                    Folders[i] = new Folder(reader);
                }
                for (int i = 0; i < Folders.Length; i++)
                {
                    Folders[i].SetName(reader);
                }

                for (int i = 0; i < Folders.Length; i++)
                {
                    uint EntryLength = reader.ReadUInt32();
                    Folders[Folders[i].Id].Entries = new Folder.Entry[EntryLength];
                    for (int j = 0; j < Folders[Folders[i].Id].Entries.Length; j++)
                    {
                        Folder.Entry entry = new Folder.Entry(reader);
                        entry.Path = Folders[entry.FolderID].Name;

                        long rememberMe = reader.BaseStream.Position;
                        reader.BaseStream.Seek(entry.Offset, SeekOrigin.Begin);
                        entry.IsSoundBank = reader.ReadUInt32() == _BKHD_ID;
                        reader.BaseStream.Seek(rememberMe, SeekOrigin.Begin);

                        Folders[Folders[i].Id].Entries[j] = entry;
                    }
                }
            }

            public class Folder
            {
                public uint NameOffset;
                public uint Id;
                public string Name;
                public Entry[] Entries;

                public Folder(BinaryReader reader)
                {
                    NameOffset = reader.ReadUInt32();
                    Id = reader.ReadUInt32();
                }

                public void SetName(BinaryReader reader)
                {
                    StringBuilder builder = new StringBuilder();
                    while (true)
                    {
                        char a = reader.ReadChar();
                        reader.ReadChar(); // always a null byte
                        if (a == 0x0)
                            break;
                        else
                            builder.Append(a);
                    }
                    Name = builder.ToString().Trim();
                }

                public class Entry
                {
                    public uint NameHash;
                    public uint FolderID;
                    public uint Size;
                    public uint OffsetMultiplier;
                    public uint Offset;
                    public string Path;
                    public bool IsSoundBank;

                    public Entry(BinaryReader reader)
                    {
                        NameHash = reader.ReadUInt32();
                        OffsetMultiplier = reader.ReadUInt32();
                        Size = reader.ReadUInt32();
                        Offset = reader.ReadUInt32() * OffsetMultiplier;
                        FolderID = reader.ReadUInt32();
                    }
                }
            }
        }

        public class BKHDSection
        {
            public uint Version;
            public uint Id;

            public BKHDSection(BinaryReader reader)
            {
                Version = reader.ReadUInt32();
                Id = reader.ReadUInt32();
            }
        }

        /// <summary>
        /// this is 100% guessed
        /// </summary>
        public class INITSection
        {
            public uint ObjectNumber;
            public InitObject[] Objects;

            public INITSection(BinaryReader reader)
            {
                ObjectNumber = reader.ReadUInt32();
                Objects = new InitObject[ObjectNumber];
                for (int i = 0; i < Objects.Length; i++)
                {
                    Objects[i] = new InitObject(reader);
                }
            }

            public class InitObject
            {
                public ushort UnknownA;
                public ushort UnknownB;
                public string Object;

                public InitObject(BinaryReader reader)
                {
                    UnknownA = reader.ReadUInt16();
                    UnknownB = reader.ReadUInt16();
                    uint length = reader.ReadUInt32();
                    Object = Encoding.UTF8.GetString(reader.ReadBytes(Convert.ToInt32(length)).AsSpan(..^1));
                }
            }
        }

        public class DIDXSection
        {
            public List<WemObject> WemFilesRef;

            public DIDXSection(BinaryReader reader, long length)
            {
                WemFilesRef = new List<WemObject>();
                while (reader.BaseStream.Position < length)
                {
                    WemFilesRef.Add(new WemObject(reader));
                }
            }

            public class WemObject
            {
                public uint Id;
                public uint Offset;
                public uint Length;

                public WemObject(BinaryReader reader)
                {
                    Id = reader.ReadUInt32();
                    Offset = reader.ReadUInt32();
                    Length = reader.ReadUInt32();
                }
            }
        }

        public class DATASection
        {
            public List<byte[]> WemFiles;

            public DATASection(BinaryReader reader, long position, DIDXSection didxSection)
            {
                WemFiles = new List<byte[]>(didxSection.WemFilesRef.Count);
                foreach (var fileRef in didxSection.WemFilesRef)
                {
                    reader.BaseStream.Seek(position + fileRef.Offset, SeekOrigin.Begin);
                    WemFiles.Add(reader.ReadBytes(Convert.ToInt32(fileRef.Length)));
                }
            }
        }

        public class HIRCSection
        {
            public uint ObjectNumber;
            public WwiseObject[] Objects;

            public HIRCSection(BinaryReader reader)
            {
                ObjectNumber = reader.ReadUInt32();
                Objects = new WwiseObject[ObjectNumber];
                for (int i = 0; i < Objects.Length; i++)
                {
                    Objects[i] = new WwiseObject(reader);
                }
            }

            public class WwiseObject
            {
                public WwiseObjectType Type;
                public uint Length;
                public uint Id;
                public byte[] AdditionalData;

                public WwiseObject(BinaryReader reader)
                {
                    Type = (WwiseObjectType)reader.ReadByte();
                    Length = reader.ReadUInt32();
                    Id = reader.ReadUInt32();

                    AdditionalData = Type switch
                    {
                        _ => reader.ReadBytes(Convert.ToInt32(Length - sizeof(uint))),
                    };
                }

                public enum WwiseObjectType : byte
                {
                    Settings,
                    SoundSFXVoice,
                    EventAction,
                    Event,
                    SequenceContainer,
                    SwitchContainer,
                    AudioBus,
                    BlendContainer,
                    MusicSegment,
                    MusicTrack,
                    MusicSwitchContainer,
                    MusicPlaylistContainer,
                    Attenuation,
                    DialogueEvent,
                    MotionBus,
                    MotionFX,
                    Effect,
                    AuxiliaryBus
                }
            }
        }

        public class STIDSection
        {
            public uint SoundBankNumber;
            public Dictionary<uint, string> SoundBanks;

            public STIDSection(BinaryReader reader)
            {
                reader.ReadUInt32();
                SoundBankNumber = reader.ReadUInt32();
                SoundBanks = new Dictionary<uint, string>(Convert.ToInt32(SoundBankNumber));
                for (int i = 0; i < SoundBankNumber; i++)
                {
                    SoundBanks[reader.ReadUInt32()] = reader.ReadString();
                }
            }
        }

        public class STMGSection
        {
            public float VolumeThreshold;
            public ushort MaxVoiceInstances;
            public ushort StateGroupNumber;
            public StateGroupObject[] StateGroups;
            public uint SwitchGroupNumber;
            public SwitchGroupObject[] SwitchGroups;
            public uint GameParameterNumber;
            public GameParameterObject[] GameParameters;

            public STMGSection(BinaryReader reader)
            {
                VolumeThreshold = reader.ReadSingle();
                MaxVoiceInstances = reader.ReadUInt16();
                StateGroupNumber = reader.ReadUInt16();
                StateGroups = new StateGroupObject[Convert.ToInt32(StateGroupNumber)];
                for (int i = 0; i < StateGroups.Length; i++)
                {
                    StateGroups[i] = new StateGroupObject(reader);
                }
                SwitchGroupNumber = reader.ReadUInt32();
                SwitchGroups = new SwitchGroupObject[SwitchGroupNumber];
                for (int i = 0; i < SwitchGroups.Length; i++)
                {
                    SwitchGroups[i] = new SwitchGroupObject(reader);
                }
                GameParameterNumber = reader.ReadUInt32();
                GameParameters = new GameParameterObject[GameParameterNumber];
                for (int i = 0; i < GameParameters.Length; i++)
                {
                    GameParameters[i] = new GameParameterObject(reader);
                }
            }

            public class StateGroupObject
            {
                public uint StateId;
                public uint DefaultTransitionTime;
                public uint CustomTransitionTimeNumber;
                public CustomTransitionTimeObject[] CustomTransitionTimes;

                public StateGroupObject(BinaryReader reader)
                {
                    StateId = reader.ReadUInt32();
                    DefaultTransitionTime = reader.ReadUInt32();
                    CustomTransitionTimeNumber = reader.ReadUInt32();
                    CustomTransitionTimes = new CustomTransitionTimeObject[CustomTransitionTimeNumber];
                    for (int i = 0; i < CustomTransitionTimes.Length; i++)
                    {
                        CustomTransitionTimes[i] = new CustomTransitionTimeObject(reader);
                    }
                }

                public class CustomTransitionTimeObject
                {
                    public uint FromId;
                    public uint ToId;
                    public uint TransitionTime;

                    public CustomTransitionTimeObject(BinaryReader reader)
                    {
                        FromId = reader.ReadUInt32();
                        ToId = reader.ReadUInt32();
                        TransitionTime = reader.ReadUInt32();
                    }
                }
            }

            public class SwitchGroupObject
            {
                public uint SwitchId;
                public uint GameParameterId;
                public uint PointNumber;
                public PointObject[] Points;

                public SwitchGroupObject(BinaryReader reader)
                {
                    SwitchId = reader.ReadUInt32();
                    GameParameterId = reader.ReadUInt32();
                    PointNumber = reader.ReadUInt32();
                    Points = new PointObject[PointNumber];
                    for (int i = 0; i < Points.Length; i++)
                    {
                        Points[i] = new PointObject(reader);
                    }
                }

                public class PointObject
                {
                    public float GameParameterValue;
                    public uint SwitchId;
                    public uint CurveShape;

                    public PointObject(BinaryReader reader)
                    {
                        GameParameterValue = reader.ReadSingle();
                        SwitchId = reader.ReadUInt32();
                        CurveShape = reader.ReadUInt32();
                    }
                }
            }

            public class GameParameterObject
            {
                public uint Id;
                public float DefaultValue;

                public GameParameterObject(BinaryReader reader)
                {
                    Id = reader.ReadUInt32();
                    DefaultValue = reader.ReadSingle();
                }
            }
        }

        public class PLATSection
        {
            public string Platform;

            public PLATSection(BinaryReader reader)
            {
                uint length = reader.ReadUInt32();
                Platform = Encoding.UTF8.GetString(reader.ReadBytes(Convert.ToInt32(length)).AsSpan(..^1));
            }
        }
    }
}
