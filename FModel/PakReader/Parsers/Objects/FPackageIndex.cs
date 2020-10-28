using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;

namespace FModel.PakReader.Parsers.Objects
{
    /**
     * Wrapper for index into a ULnker's ImportMap or ExportMap.
     * Values greater than zero indicate that this is an index into the ExportMap.  The
     * actual array index will be (FPackageIndex - 1).
     *
     * Values less than zero indicate that this is an index into the ImportMap. The actual
     * array index will be (-FPackageIndex - 1)
     */
    public readonly struct FPackageIndex
    {
        public readonly int Index;
        public FObjectResource Resource
        {
            get
            {
                if (!IsNull)
                {
                    if (Reader is LegacyPackageReader legacyReader)
                    {
                        if (IsImport && AsImport < legacyReader.ImportMap.Length)
                        {
                            return legacyReader.ImportMap[AsImport];
                        }

                        if (IsExport && AsExport < legacyReader.ExportMap.Length)
                        {
                            return legacyReader.ExportMap[AsExport];
                        }
                    }
                    else if (Reader is IoPackageReader ioReader)
                    {
                        if (IsImport && AsImport < ioReader.FakeImportMap.Count)
                        {
                            return ioReader.FakeImportMap[AsImport];
                        }

                        if (IsExport && AsExport < ioReader.ExportMap.Length)
                        {
                            return new FObjectExport(ioReader, AsExport);
                        }

                        Debugger.Break();
                    }
                }
                return null;
            }
        }

        private readonly PackageReader Reader;

        internal FPackageIndex(PackageReader reader)
        {
            Index = reader.ReadInt32();
            Reader = reader;
        }
        
        internal FPackageIndex(PackageReader reader, int index)
        {
            Index = index;
            Reader = reader;
        }

        public object GetValue()
        {
            if (Resource != null)
            {
                return new Dictionary<string, object>
                {
                    ["ObjectName"] = Resource.ObjectName.String,
                    ["OuterIndex"] = Resource.OuterIndex.GetValue()
                };
            }
            return Index;
        }

        [JsonIgnore]
        public bool IsNull => Index == 0;
        [JsonIgnore]
        public bool IsImport => Index < 0;
        [JsonIgnore]
        public bool IsExport => Index > 0;

        // Original names were ToImport and ToExport but I prefer "As" to "To" for properties
        [JsonIgnore]
        public int AsImport => -Index - 1;
        [JsonIgnore]
        public int AsExport => Index - 1;
    }
}
