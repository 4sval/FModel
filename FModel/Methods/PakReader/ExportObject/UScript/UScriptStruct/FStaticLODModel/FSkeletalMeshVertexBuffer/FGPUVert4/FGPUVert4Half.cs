namespace PakReader
{
    public struct FGPUVert4Half
    {
        public FVector Pos;
        public FPackedNormal[] Normal; // 3 length
        public FSkinWeightInfo Infs;

        public FMeshUVHalf[] UV; // 4 length
    }
}
