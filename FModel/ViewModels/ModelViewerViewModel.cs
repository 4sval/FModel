using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using FModel.Framework;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using SharpDX;

namespace FModel.ViewModels
{
    public class ModelViewerViewModel : ViewModel
    {
        private EffectsManager _effectManager;
        public EffectsManager EffectManager
        {
            get => _effectManager;
            set => SetProperty(ref _effectManager, value);
        }
        
        private Camera _cam;
        public Camera Cam
        {
            get => _cam;
            set => SetProperty(ref _cam, value);
        }
        
        private Geometry3D _cubeMesh;
        public Geometry3D CubeMesh
        {
            get => _cubeMesh;
            set => SetProperty(ref _cubeMesh, value);
        }
        
        private Material _red;
        public Material Red
        {
            get => _red;
            set => SetProperty(ref _red, value);
        }

        public ModelViewerViewModel(UStaticMesh? mesh)
        {
            EffectManager = new DefaultEffectsManager();
            Cam = new PerspectiveCamera();
            if (mesh?.RenderData == null || mesh.RenderData.LODs.Length < 1) return;
            
            var builder = new MeshBuilder();
            for (var i = 0; i < mesh.RenderData.LODs.Length; i++)
            {
                if (mesh.RenderData.LODs[i] is not
                {
                    VertexBuffer: not null,
                    PositionVertexBuffer: not null,
                    ColorVertexBuffer: not null,
                    IndexBuffer: not null
                } srcLod) continue;
                
                var numVerts = srcLod.PositionVertexBuffer.Verts.Length;
                for (var j = 0; j < numVerts; j++)
                {
                    var suv = srcLod.VertexBuffer.UV[j];
                    builder.Positions.Add(new Vector3(srcLod.PositionVertexBuffer.Verts[j].X, srcLod.PositionVertexBuffer.Verts[j].Y, srcLod.PositionVertexBuffer.Verts[j].Z));
                    builder.Normals.Add(new Vector3(suv.Normal[2].X, suv.Normal[2].Y, suv.Normal[2].Z));
                }
                
                for (var j = 0; j < srcLod.IndexBuffer.Indices16.Length; j++)
                {
                    builder.TriangleIndices.Add(srcLod.IndexBuffer[j]);
                }
                break;
            }
            builder.Scale(0.05, 0.05, 0.05);
            CubeMesh = builder.ToMesh();
            Red = PhongMaterials.Red;
        }
        
        public ModelViewerViewModel(USkeletalMesh? mesh)
        {
            EffectManager = new DefaultEffectsManager();
            Cam = new PerspectiveCamera();
            if (mesh == null || mesh.LODModels?.Length < 1) return;
            
            var builder = new MeshBuilder();
            for (var i = 0; i < mesh.LODModels.Length; i++)
            {
                if (mesh.LODModels[i] is not { } srcLod) continue;
                
                var bUseVerticesFromSections = false;
                var vertexCount = srcLod.VertexBufferGPUSkin.GetVertexCount();
                if (vertexCount == 0 && srcLod.Sections.Length > 0 && srcLod.Sections[0].SoftVertices.Length > 0)
                {
                    bUseVerticesFromSections = true;
                    for (var j = 0; j < srcLod.Sections.Length; j++)
                    {
                        vertexCount += srcLod.Sections[i].SoftVertices.Length;
                    }
                }
                
                var chunkIndex = -1;
                var chunkVertexIndex = 0;
                long lastChunkVertex = -1;
                var vertBuffer = srcLod.VertexBufferGPUSkin;
                
                for (var j = 0; j < vertexCount; j++)
                {
                    while (j >= lastChunkVertex) // this will fix any issues with empty chunks or sections
                    {
                        // proceed to next chunk or section
                        if (srcLod.Chunks.Length > 0)
                        {
                            // pre-UE4.13 code: chunks
                            var c = srcLod.Chunks[++chunkIndex];
                            lastChunkVertex = c.BaseVertexIndex + c.NumRigidVertices + c.NumSoftVertices;
                        }
                        else
                        {
                            // UE4.13+ code: chunk information migrated to sections
                            var s = srcLod.Sections[++chunkIndex];
                            lastChunkVertex = s.BaseVertexIndex + s.NumVertices;
                        }
                        chunkVertexIndex = 0;
                    }
                    
                    FSkelMeshVertexBase v;
                    if (bUseVerticesFromSections)
                        v = srcLod.Sections[chunkIndex].SoftVertices[chunkVertexIndex++];
                    else if (!vertBuffer.bUseFullPrecisionUVs)
                        v = vertBuffer.VertsHalf[j];
                    else
                        v = vertBuffer.VertsFloat[j];
                    
                    builder.Positions.Add(new Vector3(v.Pos.X, v.Pos.Y, v.Pos.Z));
                    builder.Normals.Add(new Vector3(v.Normal[2].X, v.Normal[2].Y, v.Normal[2].Z));
                }
                
                for (var j = 0; j < srcLod.Indices.Indices16.Length; j++)
                {
                    builder.TriangleIndices.Add(srcLod.Indices.Indices16[j]);
                }
                break;
            }
            builder.Scale(0.05, 0.05, 0.05);
            CubeMesh = builder.ToMesh();
            Red = PhongMaterials.Red;
        }
    }
}