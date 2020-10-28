namespace FModel.PakReader.Parsers.Objects
{
    public class FObjectResource
    {
        public FName ObjectName { get; protected set; }
        public FPackageIndex OuterIndex { get; protected set; }

        public FObjectResource()
        {
            
        }

        public FObjectResource(FName objectName, FPackageIndex outerIndex)
        {
            ObjectName = objectName;
            OuterIndex = outerIndex;
        }
    }
}
