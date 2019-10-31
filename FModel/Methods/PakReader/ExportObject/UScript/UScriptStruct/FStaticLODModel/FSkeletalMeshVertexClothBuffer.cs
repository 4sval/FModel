using System.IO;

namespace PakReader
{
    public struct FSkeletalMeshVertexClothBuffer
    {
        public ulong[] cloth_index_mapping;

        public FSkeletalMeshVertexClothBuffer(BinaryReader reader)
        {
            var flags = new FStripDataFlags(reader);

            if (!flags.server_data_stripped)
            {
                // umodel: https://github.com/gildor2/UModel/blob/9a1fe8c77d136f018ba18c9e5c445fdcc5f374ae/Unreal/UnMesh4.cpp#L924
                //         https://github.com/gildor2/UModel/blob/39c635c13d61616297fb3e47f33e3fc20259626e/Unreal/UnCoreSerialize.cpp#L320
                // ue4: https://github.com/EpicGames/UnrealEngine/blob/master/Engine/Source/Runtime/Engine/Private/SkeletalMeshLODRenderData.cpp#L758
                //      https://github.com/EpicGames/UnrealEngine/blob/master/Engine/Source/Runtime/Engine/Public/Rendering/SkeletalMeshLODRenderData.h#L119

                int elem_size = reader.ReadInt32(); // umodel has this, might want to actually serialize this like how ue4 has it
                int count = reader.ReadInt32();
                reader.BaseStream.Seek(elem_size * count, SeekOrigin.Current);

                cloth_index_mapping = reader.ReadTArray(() => reader.ReadUInt64());
            }

            cloth_index_mapping = null;
        }
    }
}
