using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static PakReader.AssetReader;

namespace PakReader
{
    public class MeshExporter
    {
        public static void ExportMesh(BinaryWriter writer, CSkeletalMesh Mesh, CSkelMeshLod Lod)
        {
            VChunkHeader BoneHdr, InfHdr;

            int i, j;
            CVertexShare Share = new CVertexShare();

            // weld vertices
            // The code below differs from similar code for StaticMesh export: it relies on vertex weight
            // information to not perform occasional welding of vertices which has the same position and
            // normal, but belongs to different bones.
            Share.Prepare(Lod.Verts, Lod.NumVerts, 48);
            for (i = 0; i < Lod.NumVerts; i++)
            {
                CSkelMeshVertex S = Lod.Verts[i];
                // Here we relies on high possibility that vertices which should be shared between
                // triangles will have the same order of weights and bones (because most likely
                // these vertices were duplicated by copying). Doing more complicated comparison
                // will reduce performance with possibly reducing size of exported mesh by a few
                // more vertices.
                uint WeightsHash = S.PackedWeights;
                for (j = 0; j < S.Bone.Length; j++)
                    WeightsHash ^= (uint)(S.Bone[j] << j);
                Share.AddVertex(S.Position, S.Normal, WeightsHash);
            }

            ExportCommonMeshData(writer, Lod.Sections, Lod.Verts, Lod.Indices, Share);

            int numBones = Mesh.RefSkeleton.Length;

            BoneHdr = new VChunkHeader
            {
                DataCount = numBones,
                DataSize = 120
            };
            SAVE_CHUNK(writer, BoneHdr, "REFSKELT");
            for (i = 0; i < numBones; i++)
            {
                VBone B = new VBone { Name = new byte[64] };
                CSkelMeshBone S = Mesh.RefSkeleton[i];
                Extensions.StrCpy(B.Name, S.Name);
                // count NumChildren
                int NumChildren = 0;
                for (j = 0; j < numBones; j++)
                    if ((j != i) && (Mesh.RefSkeleton[j].ParentIndex == i))
                        NumChildren++;
                B.NumChildren = NumChildren;
                B.ParentIndex = S.ParentIndex;
                B.BonePos.Position = S.Position;
                B.BonePos.Orientation = S.Orientation;

		        B.BonePos.Orientation.Y *= -1;
		        B.BonePos.Orientation.W *= -1;
		        B.BonePos.Position.Y *= -1;

                B.Write(writer);
            }

            // count influences
            int NumInfluences = 0;
            for (i = 0; i < Share.Points.Count; i++)
            {
                int WedgeIndex = Share.VertToWedge[i];
                CSkelMeshVertex V = Lod.Verts[WedgeIndex];
                for (j = 0; j < 4; j++)
                {
                    if (V.Bone[j] < 0) break;
                    NumInfluences++;
                }
            }

            // write influences
            InfHdr = new VChunkHeader
            {
                DataCount = NumInfluences,
                DataSize = 12
            };
            SAVE_CHUNK(writer, InfHdr, "RAWWEIGHTS");
            for (i = 0; i < Share.Points.Count; i++)
            {
                int WedgeIndex = Share.VertToWedge[i];
                CSkelMeshVertex V = Lod.Verts[WedgeIndex];
                CVec4 UnpackedWeights = V.UnpackWeights();
                for (j = 0; j < 4; j++)
                {
                    if (V.Bone[j] < 0) break;
                    NumInfluences--;                // just for verification

                    VRawBoneInfluence I;
                    I.Weight = UnpackedWeights.v[j];
                    I.BoneIndex = V.Bone[j];
                    I.PointIndex = i;
                    I.Write(writer);
                }
            }
            if (NumInfluences != 0)
            {
                throw new FileLoadException("Did not write to all influences");
            }

            ExportExtraUV(writer, Lod.ExtraUV, Lod.NumVerts, Lod.NumTexCoords);
        }

        static void ExportCommonMeshData(BinaryWriter writer, CMeshSection[] Sections, CSkelMeshVertex[] Verts, CIndexBuffer Indices, CVertexShare Share)
        {
            VChunkHeader MainHdr = new VChunkHeader(), PtsHdr, WedgHdr, FacesHdr, MatrHdr;
            int i;

            // main psk header
            SAVE_CHUNK(writer, MainHdr, "ACTRHEAD");

            PtsHdr = new VChunkHeader
            {
                DataCount = Share.Points.Count,
                DataSize = 12
            };
            SAVE_CHUNK(writer, PtsHdr, "PNTS0000");
            for (i = 0; i < Share.Points.Count; i++)
            {
                FVector V = Share.Points[i];
                V.Y = -V.Y;
                V.Write(writer);
            }

            // get number of faces (some Gears3 meshes may have index buffer larger than needed)
            // get wedge-material mapping
            int numFaces = 0;
            int[] WedgeMat = new int[Verts.Length];
            for (i = 0; i < Sections.Length; i++)
            {
                CMeshSection Sec = Sections[i];
                numFaces += Sec.NumFaces;
                for (int j = 0; j < Sec.NumFaces * 3; j++)
                {
                    WedgeMat[Indices[j + Sec.FirstIndex]] = i;
                }
            }

            WedgHdr = new VChunkHeader
            {
                DataCount = Verts.Length,
                DataSize = 16
            };
            SAVE_CHUNK(writer, WedgHdr, "VTXW0000");
            for (i = 0; i < Verts.Length; i++)
            {
                CSkelMeshVertex S = Verts[i];
                VVertex W = new VVertex
                {
                    PointIndex = Share.WedgeToVert[i],
                    U = S.UV.U,
                    V = S.UV.V,
                    MatIndex = (byte)WedgeMat[i],
                    Reserved = 0,
                    Pad = 0
                };
                W.Write(writer);
            }

            if (Verts.Length <= 65536)
            {
                FacesHdr = new VChunkHeader
                {
                    DataCount = numFaces,
                    DataSize = 12
                };
                SAVE_CHUNK(writer, FacesHdr, "FACE0000");
                for (i = 0; i < Sections.Length; i++)
                {
                    CMeshSection Sec = Sections[i];
                    for (int j = 0; j < Sec.NumFaces; j++)
                    {
                        VTriangle16 T = new VTriangle16 { WedgeIndex = new ushort[3] };
                        for (int k = 0; k < 3; k++)
                        {
                            int idx = (int)Indices[Sec.FirstIndex + j * 3 + k];
                            if (idx < 0 || idx >= 65536)
                            {
                                throw new FileLoadException("Invalid section index");
                            }
                            T.WedgeIndex[k] = (ushort)idx;
                        }
                        T.MatIndex = (byte)i;
                        T.AuxMatIndex = 0;
                        T.SmoothingGroups = 1;
                        ushort tmp = T.WedgeIndex[0];
                        T.WedgeIndex[0] = T.WedgeIndex[1];
                        T.WedgeIndex[1] = tmp;
                        T.Write(writer);
                    }
                }
            }
            else
            {
                // pskx extension
                FacesHdr = new VChunkHeader
                {
                    DataCount = numFaces,
                    DataSize = 18
                };
                SAVE_CHUNK(writer, FacesHdr, "FACE3200");
                for (i = 0; i < Sections.Length; i++)
                {
                    CMeshSection Sec = Sections[i];
                    for (int j = 0; j < Sec.NumFaces; j++)
                    {
                        VTriangle32 T = new VTriangle32 { WedgeIndex = new int[3] };
                        for (int k = 0; k < 3; k++)
                        {
                            int idx = (int)Indices[Sec.FirstIndex + j * 3 + k];
                            T.WedgeIndex[k] = idx;
                        }
                        T.MatIndex = (byte)i;
                        T.AuxMatIndex = 0;
                        T.SmoothingGroups = 1;
                        int tmp = T.WedgeIndex[0];
                        T.WedgeIndex[0] = T.WedgeIndex[1];
                        T.WedgeIndex[1] = tmp;
                        T.Write(writer);
                    }
                }
            }

            MatrHdr = new VChunkHeader
            {
                DataCount = Sections.Length,
                DataSize = 88
            };
            SAVE_CHUNK(writer, MatrHdr, "MATT0000");
            for (i = 0; i < Sections.Length; i++)
            {
                VMaterial M = new VMaterial { MaterialName = new byte[64] };
                UUnrealMaterial Tex = Sections[i].Material;
                M.TextureIndex = i; // could be required for UT99
                //!! this will not handle (UMaterialWithPolyFlags->Material==NULL) correctly - will make MaterialName=="None"
                //!! (the same valid for md5mesh export)
                Tex = null;
                if (Tex != null)
                {
                    //Extensions.StrCpy(M.MaterialName, Tex.Name);
                    //ExportObject(Tex);
                }
                else
                {
                    Extensions.StrCpy(M.MaterialName, $"material_{i}");
                }
                M.Write(writer);
            }
        }

        static void ExportExtraUV(BinaryWriter writer, CMeshUVFloat[][] ExtraUV, int NumVerts, int NumTexCoords)
        {
            VChunkHeader UVHdr = new VChunkHeader
            {
                DataCount = NumVerts,
                DataSize = 8
            };

            for (int j = 1; j < NumTexCoords; j++)
            {
                byte[] chunkName = new byte[32];
                Extensions.StrCpy(chunkName, $"EXTRAUVS{j - 1}");
                SAVE_CHUNK(writer, UVHdr, chunkName);
            }
        }

        static void SAVE_CHUNK(BinaryWriter writer, VChunkHeader var, string name)
        {
            var.ChunkID = new byte[20];
            var.TypeFlag = 1999801;
            Extensions.StrCpy(var.ChunkID, name);
            var.Write(writer);
        }

        static void SAVE_CHUNK(BinaryWriter writer, VChunkHeader var, byte[] name)
        {
            var.ChunkID = new byte[20];
            var.TypeFlag = 1999801;
            Buffer.BlockCopy(name, 0, var.ChunkID, 0, name.Length > 20 ? 20 : name.Length);
            var.Write(writer);
        }
    }

    struct VBone
    {
        public byte[] Name; // 64 length char array
        //public uint Flags;
        public int NumChildren;
        public int ParentIndex; // 0 if this is the root bone.
        public VJointPosPsk BonePos;

        public void Write(BinaryWriter writer)
        {
            writer.Write(Name);
            //writer.Write(Flags);
            writer.Write(NumChildren);
            writer.Write(ParentIndex);
            BonePos.Write(writer);
        }
    }

    struct VJointPosPsk
    {
        public FQuat Orientation;
        public FVector Position;
        //public float Length;
        public FVector Size;

        public void Write(BinaryWriter writer)
        {
            Orientation.Write(writer);
            Position.Write(writer);
            //writer.Write(Length);
            Size.Write(writer);
        }
    }

    struct VRawBoneInfluence
    {
        public float Weight;
        public int PointIndex;
        public int BoneIndex;

        public void Write(BinaryWriter writer)
        {
            writer.Write(Weight);
            writer.Write(PointIndex);
            writer.Write(BoneIndex);
        }
    }

    struct VVertex
    {
        public int PointIndex; // int16, padded to int; used as int for large meshes
        public float U, V;
        public byte MatIndex;
        public byte Reserved;
        public short Pad; // not used

        public void Write(BinaryWriter writer)
        {
            writer.Write(PointIndex);
            writer.Write(U);
            writer.Write(V);
            writer.Write(MatIndex);
            writer.Write(Reserved);
            writer.Write(Pad);
        }
    }

    struct VTriangle16
    {
        public ushort[] WedgeIndex; // 3 length, Point to three vertices in the vertex list.
        public byte MatIndex; // Materials can be anything.
        public byte AuxMatIndex; // Second material (unused).
        public uint SmoothingGroups;

        public void Write(BinaryWriter writer)
        {
            writer.Write(WedgeIndex[0]);
            writer.Write(WedgeIndex[1]);
            writer.Write(WedgeIndex[2]);
            writer.Write(MatIndex);
            writer.Write(AuxMatIndex);
            writer.Write(SmoothingGroups);
        }
    }

    struct VTriangle32
    {
        public int[] WedgeIndex; // 3 length, Point to three vertices in the vertex list.
        public byte MatIndex; // Materials can be anything.
        public byte AuxMatIndex; // Second material (unused).
        public uint SmoothingGroups; // 32-bit flag for smoothing groups.

        public void Write(BinaryWriter writer)
        {
            writer.Write(WedgeIndex[0]);
            writer.Write(WedgeIndex[1]);
            writer.Write(WedgeIndex[2]);
            writer.Write(MatIndex);
            writer.Write(AuxMatIndex);
            writer.Write(SmoothingGroups);
        }
    }

    struct VMaterial
    {
        public byte[] MaterialName; // 64 length char array
        public int TextureIndex;
        //public uint PolyFrags;
        //public int AuxMaterial;
        //public uint AuxFlags;
        //public int LodBias;
        //public int LodStyle;

        public void Write(BinaryWriter writer)
        {
            writer.Write(MaterialName);
            writer.Write(TextureIndex);
            //writer.Write(PolyFrags);
            //writer.Write(AuxMaterial);
            //writer.Write(AuxFlags);
            //writer.Write(LodBias);
            //writer.Write(LodStyle);
        }
    }

    public struct CSkeletalMesh
    {
        public ExportObject OriginalMesh;
        public FBox BoundingBox;
        public FSphere BoundingSphere;
        public CVec3 MeshOrigin;
        public CVec3 MeshScale;
        public FRotator RotOrigin;
        public CSkelMeshBone[] RefSkeleton;
        public CSkelMeshLod[] Lods;
        public CSkelMeshSocket[] Sockets;

        public CSkeletalMesh(USkeletalMesh mesh)
        {
            OriginalMesh = mesh;

            // convert bounds
            BoundingSphere = new FSphere
            {
                R = mesh.Bounds.sphere_radius / 2
            };
            BoundingBox = new FBox
            {
                Min = mesh.Bounds.origin - mesh.Bounds.box_extend,
                Max = mesh.Bounds.origin + mesh.Bounds.box_extend
            };

            // MeshScale, MeshOrigin, RotOrigin are removed in UE4
            //!! NOTE: MeshScale is integrated into RefSkeleton.RefBonePose[0].Scale3D.
            //!! Perhaps rotation/translation are integrated too!
            MeshOrigin = new CVec3 { v = new float[] { 0, 0, 0 } };
            RotOrigin = new FRotator { pitch = 0, roll = 0, yaw = 0 };
            MeshScale = new CVec3 { v = new float[] { 1, 1, 1 } };

            // convert LODs
            Lods = new CSkelMeshLod[mesh.LODModels.Length];
            for (int i = 0; i < Lods.Length; i++)
            {
                var SrcLod = mesh.LODModels[i];
                if (SrcLod.Indices.Indices16.Length == 0 && SrcLod.Indices.Indices32.Length == 0)
                {
                    // No indicies in this lod
                    continue;
                }

                int NumTexCoords = SrcLod.NumTexCoords;
                if (NumTexCoords > 8)
                {
                    throw new FileLoadException($"SkeletalMesh has too many ({NumTexCoords}) UV sets");
                }

                CSkelMeshLod Lod = new CSkelMeshLod
                {
                    NumTexCoords = NumTexCoords,
                    HasNormals = true,
                    HasTangents = true,
                };

                // get vertex count and determine vertex source
                int VertexCount = SrcLod.VertexBufferGPUSkin.GetVertexCount();

                //bool bUseVerticesFromSections = false;
                // if (VertexCount == 0 && SrcLod.Sections.Length > 0 && SrcLod.Sections[0].SoftVertices.Count > 0)
                // above is used for editor assets, but there are no chunks for soft vertices

                Lod.AllocateVerts(VertexCount);

                int chunkIndex = -1;
                int lastChunkVertex = -1;
                //int chunkVertexIndex = 0;

                ushort[] BoneMap = null;

                for (int j = 0; j < VertexCount; j++)
                {
                    while (j >= lastChunkVertex) // this will fix any issues with empty chunks or sections
                    {
                        // UE4.13+ code: chunk information migrated to sections
                        FSkelMeshSection S = SrcLod.Sections[++chunkIndex];
                        lastChunkVertex = (int)(S.base_vertex_index + S.num_vertices);
                        BoneMap = S.bone_map;
                        //chunkVertexIndex = 0;
                    }

                    // get vertex from GPU skin
                    FSkelMeshVertexBase V;

                    if (!SrcLod.VertexBufferGPUSkin.bUseFullPrecisionUVs)
                    {
                        FGPUVert4Half V0 = SrcLod.VertexBufferGPUSkin.VertsHalf[j];
                        FMeshUVHalf[] SrcUV = V0.UV;
                        V = new FSkelMeshVertexBase
                        {
                            Infs = V0.Infs,
                            Normal = V0.Normal,
                            Pos = V0.Pos
                        };
                        // UV: convert half -> float
                        Lod.Verts[j].UV = (FMeshUVFloat)SrcUV[0];
                        for (int TexCoordIndex = 1; TexCoordIndex < NumTexCoords; TexCoordIndex++)
                        {
                            Lod.ExtraUV[TexCoordIndex - 1][j] = (FMeshUVFloat)SrcUV[TexCoordIndex];
                        }
                    }
                    else
                    {
                        FGPUVert4Float V0 = SrcLod.VertexBufferGPUSkin.VertsFloat[j];
                        FMeshUVFloat[] SrcUV = V0.UV;
                        V = new FSkelMeshVertexBase
                        {
                            Infs = V0.Infs,
                            Normal = V0.Normal,
                            Pos = V0.Pos
                        };
                        // UV: convert half -> float
                        Lod.Verts[j].UV = SrcUV[0];
                        for (int TexCoordIndex = 1; TexCoordIndex < NumTexCoords; TexCoordIndex++)
                        {
                            Lod.ExtraUV[TexCoordIndex - 1][j] = SrcUV[TexCoordIndex];
                        }
                    }
                    Lod.Verts[j].Position = V.Pos;
                    Lod.Verts[j].UnpackNormals(V.Normal);

                    // convert influences
                    Lod.Verts[j].Bone = new short[4];
                    int k2 = 0;
                    uint PackedWeights = 0;
                    for (int k = 0; k < 4; k++)
                    {
                        int BoneIndex = V.Infs.bone_index[k];
                        byte BoneWeight = V.Infs.bone_weight[k];
                        if (BoneWeight == 0) continue; // skip this influence (but do not stop the loop!)
                        PackedWeights |= (uint)(BoneWeight << (k2 * 8));
                        Lod.Verts[j].Bone[k2] = (short)BoneMap[BoneIndex];
                        k2++;
                    }
                    Lod.Verts[j].PackedWeights = PackedWeights;
                    if (k2 < 4) Lod.Verts[j].Bone[k2] = -1; // mark end of list
                }

                // indices
                Lod.Indices.Initialize(SrcLod.Indices.Indices16, SrcLod.Indices.Indices32);

                // sections
                Lod.Sections = new CMeshSection[SrcLod.Sections.Length];

                FSkeletalMeshLODInfo Info = mesh.LODInfo[i];

                for (int j = 0; j < SrcLod.Sections.Length; j++)
                {
                    FSkelMeshSection S = SrcLod.Sections[j];
                    CMeshSection Dst = new CMeshSection();

                    // remap material for LOD
                    int MaterialIndex = S.material_index;
                    if (MaterialIndex >= 0 && MaterialIndex < Info.LODMaterialMap.Length)
                        MaterialIndex = Info.LODMaterialMap[MaterialIndex];

                    if (S.material_index < mesh.Materials.Length)
                        Dst.Material = new UUnrealMaterial();// mesh.Materials[MaterialIndex].Material;
                    // -> TODO: actually get the object from the pak

                    Dst.FirstIndex = (int)S.base_index;
                    Dst.NumFaces = (int)S.num_triangles;
                    Lod.Sections[j] = Dst;
                }

                Lods[i] = Lod;
            }

            // copy skeleton
            int NumBones = mesh.RefSkeleton.ref_bone_info.Length;
            RefSkeleton = new CSkelMeshBone[NumBones];
            for(int i = 0; i < NumBones; i++)
            {
                FMeshBoneInfo B = mesh.RefSkeleton.ref_bone_info[i];
                FTransform T = mesh.RefSkeleton.ref_bone_pose[i];
                CSkelMeshBone Dst = new CSkelMeshBone
                {
                    Name = B.name,
                    ParentIndex = B.parent_index,
                    Position = T.translation,
                    Orientation = T.rotation
                };
                // fix skeleton; all bones but 0
                if (i >= 1)
                    Dst.Orientation.Conjugate();
                RefSkeleton[i] = Dst;
            }
            
            Sockets = null; // dunno where this is set

            FinalizeMesh();
        }

        public void FinalizeMesh()
        {
            for (int i = 0; i < Lods.Length; i++)
            {
                Lods[i].BuildNormals();
            }
            SortBones();

            // fix bone weights
            for (int i = 0; i < Lods.Length; i++)
            {
                var lod = Lods[i];
                for (int j = 0; j < lod.NumVerts; j++)
                {
                    byte[] UnpackedWeights = BitConverter.GetBytes(lod.Verts[j].PackedWeights);

                    bool ShouldFix = false;
                    for (int k = 0; k < 4; k++)
                    {
                        int Bone = lod.Verts[j].Bone[k];
                        if (Bone < 0) break;
                        if (UnpackedWeights[k] == 0)
                        {
                            // remove zero weight
                            ShouldFix = true;
                            continue;
                        }
                        // remove duplicated influences, if any
                        for (int l = 0; l < k; l++)
                        {
                            if (lod.Verts[j].Bone[l] == Bone)
                            {
                                // add l's weight to k, and set l's weight to 0
                                int NewWeight = UnpackedWeights[k] + UnpackedWeights[l];
                                if (NewWeight > 255) NewWeight = 255;
                                UnpackedWeights[k] = (byte)(NewWeight & 0xFF);
                                UnpackedWeights[l] = 0;
                                ShouldFix = true;
                            }
                        }
                    }

                    if (ShouldFix)
                    {
                        for (int k = 3; k >= 0; k--) // iterate in reverse order for correct removal of '0' followed by '0'
                        {
                            if (UnpackedWeights[k] == 0)
                            {
                                if (k < 3)
                                {
                                    Buffer.BlockCopy(UnpackedWeights, k + 1, UnpackedWeights, k, 3 - k);
                                    Buffer.BlockCopy(lod.Verts[j].Bone, k+1, lod.Verts[j].Bone, k, (3 - k) * sizeof(short));
                                }
                                // remove last weight item
                                UnpackedWeights[3] = 0;
                                lod.Verts[j].Bone[3] = -1;
                            }
                        }
                        // pack weights back to vertex
                        lod.Verts[j].PackedWeights = BitConverter.ToUInt32(UnpackedWeights, 0);
                    }

                    // Check for requirement of renormalizing weights
                    int TotalWeight = 0;
                    int NumInfluences;
                    for (NumInfluences = 0; NumInfluences < 4; NumInfluences++)
                    {
                        int Bone = lod.Verts[j].Bone[NumInfluences];
                        if (Bone < 0) break;
                        TotalWeight += UnpackedWeights[NumInfluences];
                    }
                    if (TotalWeight != 255)
                    {
                        // Do renormalization
                        float Scale = 255.0f / TotalWeight;
                        TotalWeight = 0;
                        for (int k = 0; k < NumInfluences; k++)
                        {
                            UnpackedWeights[k] = (byte)Math.Round(UnpackedWeights[k] * Scale);
                            TotalWeight += UnpackedWeights[k];
                        }
                        // There still could be TotalWeight which differs slightly from value 255.
                        // Adjust first bone weight to make sum matching 255. Assume that the first
                        // weight is largest one (it is true at least for UE4), so this adjustment
                        // won't be noticeable.
                        int Delta = 255 - TotalWeight;
                        UnpackedWeights[0] += (byte)Delta;

                        lod.Verts[j].PackedWeights = BitConverter.ToUInt32(UnpackedWeights, 0);
                    }
                }
            }
        }

        public void SortBones()
        {
            int NumBones = RefSkeleton.Length;
            int i;

            // prepare CBoneSortHelper
            if (NumBones >= 1024 * 3)
            {
                throw new FileLoadException("Too many bones");
            }

            CBoneSortHelper helper = new CBoneSortHelper();
            for (i = 0; i < NumBones; i++)
            {
                helper.Bones[i] = new CBoneSortHelper.Proxy
                {
                    Bone = RefSkeleton[i],
                    Parent = (i > 0) ? helper.Bones[RefSkeleton[i].ParentIndex] : null
                };
                helper.SortedBones[i] = helper.Bones[i];
            }

            // sort bones
            helper.SortBoneArray(NumBones);

            // build remap table
            int[] Remap = new int[1024 * 3];
            int[] RemapBack = new int[1024 * 3];
            for (i = 0; i < NumBones; i++)
            {
                CBoneSortHelper.Proxy P = helper.SortedBones[i];
                int OldIndex = Array.IndexOf(helper.Bones, P);
                Remap[OldIndex] = i;
                RemapBack[i] = OldIndex;
            }

            // build new RefSkeleton
            CSkelMeshBone[] NewSkeleton = new CSkelMeshBone[NumBones];
            for (i = 0; i < NumBones; i++)
            {
                NewSkeleton[i] = RefSkeleton[RemapBack[i]];
                int oldParent = NewSkeleton[i].ParentIndex;
                NewSkeleton[i].ParentIndex = (oldParent > 0) ? Remap[oldParent] : 0;
            }
            Array.Copy(NewSkeleton, RefSkeleton, NumBones);

            // remap bone influences
            for (int lod = 0; lod < Lods.Length; lod++)
            {
                CSkelMeshLod L = Lods[lod];
                int V = 0;
                for (i = 0; i < L.NumVerts; i++, V++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        int Bone = L.Verts[V].Bone[j];
                        if (Bone < 0) break;
                        L.Verts[V].Bone[j] = (short)Remap[Bone];
                    }
                }
            }
        }
    }

    class CBoneSortHelper
    {
        public class Proxy
        {
            public Proxy Parent;
            public CSkelMeshBone Bone;
        }

        public Proxy[] Bones = new Proxy[1024 * 3];		//!! rename or pass to SortBones()
        public Proxy[] SortedBones = new Proxy[1024 * 3];
        int NumBones;
        int NumSortedBones;

        public void SortBoneArray(int NumBones)
        {
            this.NumBones = NumBones;
            NumSortedBones = 1;
            SortedBones[0] = Bones[0];
            SortRecursive(0);
        }

        void SortRecursive(int ParentIndex)
        {
            Proxy Parent = Bones[ParentIndex];
            for (int i = 0; i < NumBones; i++)
            {
                Proxy Bone = Bones[i];
                if (Bone.Parent == Parent)
                {
                    if (NumSortedBones >= NumBones) throw new FileLoadException("Loop in skeleton");
                    SortedBones[NumSortedBones++] = Bone;
                    SortRecursive(i);
                }
            }
        }
    }

    struct FSkelMeshVertexBase
    {
        public FVector Pos;
        public FPackedNormal[] Normal; // 3 length
        public FSkinWeightInfo Infs;
    }

    public struct FBox
    {
        public FVector Min, Max;
        public byte IsValid;
    }

    public struct FSphere
    {
        public float X;
        public float Y;
        public float Z;
        public float R;
    }

    class VChunkHeader
    {
        public byte[] ChunkID; // text identifier, 20 length char array
        public int TypeFlag; // version: 1999801 or 2003321
        public int DataSize; // sizeof(type)
        public int DataCount; // number of array elements

        public void Write(BinaryWriter writer)
        {
            writer.Write(ChunkID);
            writer.Write(TypeFlag);
            writer.Write(DataSize);
            writer.Write(DataCount);
        }
    }

    public struct CVec3
    {
        public float[] v; // 3 length

        public float Normalize()
        {
            float length = (float)Math.Sqrt(Dot(this, this));
            if (length != 0) Scale(1 / length);
            return length;
        }

        public void Scale(float scale)
        {
            v[0] *= scale;
            v[1] *= scale;
            v[2] *= scale;
        }

        public void VectorMA(float scale, CVec3 b)
        {
            v[0] += scale * b.v[0];
            v[1] += scale * b.v[1];
            v[2] += scale * b.v[2];
        }

        public static CVec3 operator -(CVec3 a, CVec3 b)
        {
            return new CVec3
            {
                v = new float[]
                {
                    a.v[0] - b.v[0],
                    a.v[1] - b.v[1],
                    a.v[2] - b.v[2]
                }
            };
        }

        public static CVec3 operator +(CVec3 a, CVec3 b)
        {
            return new CVec3
            {
                v = new float[]
                {
                    a.v[0] + b.v[0],
                    a.v[1] + b.v[1],
                    a.v[2] + b.v[2]
                }
            };
        }

        public static CVec3 Cross(CVec3 a, CVec3 b) => new CVec3
        {
            v = new float[]
            {
                a.v[1] * b.v[2] - a.v[2] * b.v[1],
                a.v[2] * b.v[0] - a.v[0] * b.v[2],
                a.v[0] * b.v[1] - a.v[1] * b.v[0]
            }
        };

        public static float Dot(CVec3 a, CVec3 b) =>
            a.v[0] * b.v[0] + a.v[1] * b.v[1] + a.v[2] * b.v[2];
    }

    public struct CVec4
    {
        public float[] v; // 4 length

        public static CVec4 operator -(CVec4 a, CVec4 b)
        {
            return new CVec4
            {
                v = new float[]
                {
                    a.v[0] - b.v[0],
                    a.v[1] - b.v[1],
                    a.v[2] - b.v[2],
                    a.v[3] - b.v[3]
                }
            };
        }

        public static CVec4 operator +(CVec4 a, CVec4 b)
        {
            return new CVec4
            {
                v = new float[]
                {
                    a.v[0] + b.v[0],
                    a.v[1] + b.v[1],
                    a.v[2] + b.v[2],
                    a.v[3] + b.v[3]
                }
            };
        }

        public static implicit operator CVec3(CVec4 me) => new CVec3
        {
            v = new float[] {me.v[0], me.v[1], me.v[2] }
        };

        public static implicit operator FVector(CVec4 me) => new FVector
        {
            X = me.v[0],
            Y = me.v[1],
            Z = me.v[2]
        };
    }

    public struct CQuat
    {
        public float x, y, z, w;

        public void Conjugate()
        {
            x = -x;
            y = -y;
            z = -z;
        }
    }

    public struct CSkelMeshBone
    {
        public string Name;
        public int ParentIndex;
        public CVec3 Position;
        public CQuat Orientation;
    }

    public struct CSkelMeshLod
    {
        // generic properties
        public int NumTexCoords;
        public bool HasNormals;
        public bool HasTangents;

        // geometry
        public CMeshSection[] Sections;
        public int NumVerts;
        public CMeshUVFloat[][] ExtraUV; // 1st dem is 7 length
        public CIndexBuffer Indices;

        // impl
        public CSkelMeshVertex[] Verts;

        public void BuildNormals()
        {
            if (HasNormals) return;

            int i, j;

            // Find vertices to share.
            // We are using very simple algorithm here: to share all vertices with the same position
            // independently on normals of faces which share this vertex.
            CVec3[] tmpNorm = new CVec3[NumVerts];					// really will use Points.Num() items, which value is smaller than NumVerts
            CVertexShare Share = new CVertexShare();
            Share.Prepare(Verts, NumVerts, 48);
            for (i = 0; i < NumVerts; i++)
            {
                CPackedNormal NullVec;
                NullVec.Data = 0;
                Share.AddVertex(Verts[i].Position, NullVec);
            }

            for (i = 0; i < Indices.Length / 3; i++)
            {
                CSkelMeshVertex[] V = new CSkelMeshVertex[3];
                CVec3[] N = new CVec3[3];
                for (j = 0; j < 3; j++)
                {
                    int idx = (int)Indices[i * 3 + j]; // index in Verts[]
                    V[j] = Verts[idx];
                    N[j] = tmpNorm[Share.WedgeToVert[idx]]; // remap to shared verts
                }

                // compute edges
                CVec3[] D = new CVec3[]
                {
                    V[1].Position - V[0].Position,
                    V[2].Position - V[1].Position,
                    V[0].Position - V[2].Position
                };
                // compute face normal
                CVec3 norm = CVec3.Cross(D[1], D[0]);
                norm.Normalize();
                // compute angles
                for (j = 0; j < 3; j++) D[j].Normalize();
                float[] angle = new float[]
                {
                    (float)Math.Acos(-CVec3.Dot(D[0], D[2])),
                    (float)Math.Acos(-CVec3.Dot(D[0], D[1])),
                    (float)Math.Acos(-CVec3.Dot(D[1], D[2]))
                };
                // add normals for triangle verts
                for (j = 0; j < 3; j++)
                {
                    N[j].VectorMA(angle[j], norm);
                }
                
                for (j = 0; j < 3; j++)
                {
                    int idx = (int)Indices[i * 3 + j];
                    Verts[idx] = V[j];
                    tmpNorm[Share.WedgeToVert[idx]] = N[j];
                }
            }

            // TODO: add "hard angle threshold" - do not share vertex between faces when angle between them
            // is too large.

            // normalize shared normals ...
            for (i = 0; i < Share.Points.Count; i++)
                tmpNorm[i].Normalize();

            // ... then place ("unshare") normals to Verts
            for (i = 0; i < NumVerts; i++)
                Verts[i].Normal.Pack(tmpNorm[Share.WedgeToVert[i]]);

            HasNormals = true;
        }

        public void AllocateVerts(int Count)
        {
            if (Verts != null)
            {
                throw new FileLoadException("Verts must be null to allocate");
            }
            Verts = new CSkelMeshVertex[Count];
            ExtraUV = new CMeshUVFloat[4][];
            for(int i = 0; i < 4; i++)
            {
                ExtraUV[i] = new CMeshUVFloat[Count];
            }
            NumVerts = Count;
        }
    }

    public struct CSkelMeshSocket
    {
        public string Name;
        public string Bone;
        public CCoords Transform;
    }

    public struct CCoords
    {
        public CVec3 origin;
        public CVec3 axis;
    }

    public struct CMeshSection
    {
        public UUnrealMaterial Material;
        public int FirstIndex;
        public int NumFaces;
    }

    public class UUnrealMaterial : ExportObject // dummy obj atm
    {

    }

    public struct CMeshUVFloat
    {
        public float U, V;
    }

    public struct CIndexBuffer
    {
        public ushort[] Indices16;
        public uint[] Indices32;

        public bool Is32 => Indices32 != null && Indices32.Length != 0;

        public void Initialize(ushort[] Idx16, uint[] Idx32)
        {
            if (Idx32 != null && Idx32.Length != 0)
            {
                Indices32 = new uint[Idx32.Length];
                Buffer.BlockCopy(Idx32, 0, Indices32, 0, Idx32.Length * sizeof(uint));
            }
            else
            {
                Indices16 = new ushort[Idx16.Length];
                Buffer.BlockCopy(Idx16, 0, Indices16, 0, Idx16.Length * sizeof(ushort));
            }
        }

        public int Length => Is32 ? Indices32.Length : Indices16.Length;

        public uint this[int i] => Is32 ? Indices32[i] : Indices16[i];
    }

    public struct CPackedNormal : IEquatable<CPackedNormal>
    {
        public uint Data;

        public float W
        {
            get => ((byte)(Data >> 24)) / 127.0f;
            set => Data = (Data & 0xFFFFFF) | ((uint)Math.Round(value * 127.0f) << 24);
        }

        public bool Equals(CPackedNormal other) => other.Data == Data;

        //public static bool operator ==(CPackedNormal a, CPackedNormal b) => a.Equals(b);

        //public static bool operator !=(CPackedNormal a, CPackedNormal b) => !a.Equals(b);

        public void Pack(CVec3 Unpacked)
        {
            Data = (uint)((byte)Math.Round(Unpacked.v[0] * 127)
                + ((byte)Math.Round(Unpacked.v[1] * 127) << 8)
                + ((byte)Math.Round(Unpacked.v[2] * 127) << 16));
        }

        public static implicit operator CPackedNormal(FPackedNormal me) => new CPackedNormal
        {
            Data = me.Data ^ 0x80808080
        };
    }

    public struct CSkelMeshVertex
    {
        public CVec4 Position;
        public CPackedNormal Normal;
        public CPackedNormal Tangent;
        public CMeshUVFloat UV;

        public uint PackedWeights;
        public short[] Bone; // 4 length

        public void UnpackNormals(FPackedNormal[] SrcNormal)
        {
            Tangent = SrcNormal[0];
            Normal = SrcNormal[2];

            if (SrcNormal[1].Data != 0)
            {
                FVector Tangent = SrcNormal[0];
                FVector Binormal = SrcNormal[1];
                FVector Normal = SrcNormal[2];
                CVec3 ComputedBinormal = CVec3.Cross(Normal, Tangent);
                float Sign = CVec3.Dot(Binormal, ComputedBinormal);
                this.Normal.W = Sign > 0 ? 1f : -1f;
            }
        }

        public CVec4 UnpackWeights()
        {
            float Scale = 1 / 255f;
            return new CVec4
            {
                v = new float[]
                {
                    (PackedWeights & 0xFF) * Scale,
                    ((PackedWeights >> 8) & 0xFF) * Scale,
                    ((PackedWeights >> 16) & 0xFF) * Scale,
                    ((PackedWeights >> 24) & 0xFF) * Scale,
                }
            };
        }
    }

    struct CVertexShare
    {
        public List<FVector> Points;
        public List<FPackedNormal> Normals;
        public List<uint> ExtraInfos;
        public List<int> WedgeToVert;
        public int[] VertToWedge;
        public int WedgeIndex;

        // hashing
        public FVector Mins, Maxs;
        public FVector Extents;
        public int[] Hash; // 16384 length
        public int[] HashNext;

        public void Prepare(CSkelMeshVertex[] Verts, int NumVerts, int VertexSize)
        {
            WedgeIndex = 0;
            Points = new List<FVector>(NumVerts);
            Normals = new List<FPackedNormal>(NumVerts);
            ExtraInfos = new List<uint>(NumVerts);
            WedgeToVert = new List<int>(NumVerts);
            VertToWedge = new int[NumVerts];

            // compute bounds for better hashing
            ComputeBounds(Verts, ref Mins, ref Maxs);
            Extents = Maxs - Mins;
            Extents.X += 1;
            Extents.Y += 1;
            Extents.Z += 1;
            // initialize Hash and HashNext with -1
            HashNext = new int[NumVerts];
            Hash = new int[16384];
            for(int i = 0; i < NumVerts; i++)
            {
                HashNext[i] = -1;
            }
            for (int i = 0; i < 16384; i++)
            {
                Hash[i] = -1;
            }
        }

        public void ComputeBounds(CSkelMeshVertex[] Data, ref FVector Mins, ref FVector Maxs, bool UpdateBounds = false)
        {
            if (Data.Length == 0)
            {
                if (!UpdateBounds)
                {
                    Mins = new FVector();
                    Maxs = new FVector();
                }
                return;
            }

            int offset = 0;
            if (!UpdateBounds)
            {
                Mins = Maxs = Data[0].Position;
                offset = 1;
            }

            for(; offset < Data.Length; offset++)
            {
                var d = Data[offset].Position;
                if (d.v[0] < Mins.X) Mins.X = d.v[0];
                if (d.v[0] > Maxs.X) Maxs.X = d.v[0];
                if (d.v[1] < Mins.Y) Mins.Y = d.v[1];
                if (d.v[1] > Maxs.Y) Maxs.Y = d.v[1];
                if (d.v[2] < Mins.Z) Mins.Z = d.v[2];
                if (d.v[2] > Maxs.Z) Maxs.Z = d.v[2];
            }
        }

        public int AddVertex(CVec3 Pos, CPackedNormal Normal, uint ExtraInfo = 0)
        {
            int PointIndex = -1;

            Normal.Data &= 0xFFFFFF;		// clear W component which is used for binormal computation

            // compute hash
            int h = (int)(Math.Floor(
                ((Pos.v[0] - Mins.X) / Extents.X +
                 (Pos.v[1] - Mins.Y) / Extents.Y +
                 (Pos.v[2] - Mins.Z) / Extents.Z)
                 * Hash.Length / 3 * 16)		// multiply to 16 for better spreading inside Hash array
                % Hash.Length);
            
            // find point with the same position and normal
            /*for (PointIndex = Hash[h]; PointIndex >= 0; PointIndex = HashNext[PointIndex])
            {
                if (Points[PointIndex] == Pos && Normals[PointIndex] == Normal && ExtraInfos[PointIndex] == ExtraInfo)
                {
                    break;      // found it
                }
            }*/

            if (PointIndex == -1)
            {
                // point was not found - create it
                Points.Add(Pos);
                Normals.Add(Normal);
                ExtraInfos.Add(ExtraInfo);
                PointIndex = Points.Count - 1;

                // add to hash
                HashNext[PointIndex] = Hash[h];
                Hash[h] = PointIndex;
            }

            // remember vertex <-> wedge map
            WedgeToVert.Add(PointIndex);
            VertToWedge[PointIndex] = WedgeIndex++;

            return PointIndex;
        }
    }
}
