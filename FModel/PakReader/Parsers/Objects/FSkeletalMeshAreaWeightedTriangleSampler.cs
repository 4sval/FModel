using System;

namespace FModel.PakReader.Parsers.Objects
{
    /** Allows area weighted sampling of triangles on a skeletal mesh. */
    public readonly struct FSkeletalMeshAreaWeightedTriangleSampler : IUStruct
    {
        /*
        public readonly USkeletalMesh Owner;
        public readonly int[] TriangleIndices;
        public readonly int LODIndex;
        */

        internal FSkeletalMeshAreaWeightedTriangleSampler(PackageReader reader)
        {
            throw new NotImplementedException(string.Format(FModel.Properties.Resources.ParsingNotSupported,
                "FSkeletalMeshAreaWeightedTriangleSampler"));
        }
    }
}