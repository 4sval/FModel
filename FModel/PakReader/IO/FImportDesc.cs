using FModel.PakReader.Parsers.Objects;

namespace FModel.PakReader.IO
{
    public class FImportDesc
    {
        public FName Name;
        public FPackageObjectIndex GlobalImportIndex;
        public FExportDesc Export;
    }
}