namespace PakReader
{
    public struct FGPUVert4Float
    {
        public FVector Pos;
        public FPackedNormal[] Normal; // 3 length
        public FSkinWeightInfo Infs;

        public FMeshUVFloat[] UV; // 4 length
    }
}
