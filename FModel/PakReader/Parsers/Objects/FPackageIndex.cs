using Newtonsoft.Json;
using System.Collections.Generic;

namespace PakReader.Parsers.Objects
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
        [JsonIgnore]
        public readonly int Index;
        public FObjectResource Resource
        {
            get
            {
                if (!IsNull)
                {
                    if (IsImport && AsImport < Reader.ImportMap.Length)
                        return Reader.ImportMap[AsImport];
                    else if (IsImport && AsExport < Reader.ExportMap.Length)
                        return Reader.ExportMap[AsExport];
                }
                return null;
            }
        }

        readonly PackageReader Reader;

        internal FPackageIndex(PackageReader reader)
        {
            Index = reader.ReadInt32();
            Reader = reader;
        }

        public object GetValue()
        {
            if (Resource != null)
            {
                var ret = new Dictionary<string, object>
                {
                    ["ObjectName"] = Resource.ObjectName.String,
                    ["OuterIndex"] = Resource.OuterIndex.GetValue()
                };
                return ret;
            }
            return null;
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
