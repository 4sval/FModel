namespace PakReader.Parsers.Objects
{
    public readonly struct FSkeletalMeshSamplingLODBuiltData : IUStruct
    {
        /** Area weighted sampler for the whole mesh at this LOD.*/
        public readonly FSkeletalMeshAreaWeightedTriangleSampler AreaWeightedTriangleSampler;

        internal FSkeletalMeshSamplingLODBuiltData(PackageReader reader)
        {
            AreaWeightedTriangleSampler = new FSkeletalMeshAreaWeightedTriangleSampler(reader);
        }
    }
}
