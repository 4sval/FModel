namespace PakReader
{
    public struct FSkeletalMeshVertexBuffer
    {
        public int NumTexCoords;
        public FVector MeshExtension;
        public FVector MeshOrigin;
        public bool bUseFullPrecisionUVs;
        public bool bExtraBoneInfluences;
        public FGPUVert4Half[] VertsHalf;
        public FGPUVert4Float[] VertsFloat;

        public int GetVertexCount()
        {
            if (VertsHalf != null && VertsHalf.Length != 0)
            {
                return VertsHalf.Length;
            }
            else if (VertsFloat != null && VertsFloat.Length != 0)
            {
                return VertsFloat.Length;
            }
            return 0;
        }
    }
}
