using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using FModel.Views.Snooper.Shading;

namespace FModel.Views.Snooper.Models;

public class StaticModel : UModel
{
    public StaticModel(UMaterialInterface unrealMaterial, CStaticMesh staticMesh) : base(unrealMaterial)
    {
        var lod = staticMesh.LODs[LodLevel];

        Indices = new uint[lod.Indices.Value.Length];
        for (int i = 0; i < Indices.Length; i++)
        {
            Indices[i] = (uint) lod.Indices.Value[i];
        }

        Vertices = new float[lod.NumVerts * VertexSize];
        for (int i = 0; i < lod.Verts.Length; i++)
        {
            var count = 0;
            var baseIndex = i * VertexSize;
            var vert = lod.Verts[i];
            Vertices[baseIndex + count++] = i;
            Vertices[baseIndex + count++] = vert.Position.X * Constants.SCALE_DOWN_RATIO;
            Vertices[baseIndex + count++] = vert.Position.Z * Constants.SCALE_DOWN_RATIO;
            Vertices[baseIndex + count++] = vert.Position.Y * Constants.SCALE_DOWN_RATIO;
            Vertices[baseIndex + count++] = vert.Normal.X;
            Vertices[baseIndex + count++] = vert.Normal.Z;
            Vertices[baseIndex + count++] = vert.Normal.Y;
            Vertices[baseIndex + count++] = vert.Tangent.X;
            Vertices[baseIndex + count++] = vert.Tangent.Z;
            Vertices[baseIndex + count++] = vert.Tangent.Y;
            Vertices[baseIndex + count++] = vert.UV.U;
            Vertices[baseIndex + count++] = vert.UV.V;
            Vertices[baseIndex + count++] = .5f;
        }

        Materials = new Material[1];
        Materials[0] = new Material(unrealMaterial) { IsUsed = true };

        Sections = new Section[1];
        Sections[0] = new Section(0, Indices.Length, 0);

        AddInstance(Transform.Identity);

        Box = staticMesh.BoundingBox * Constants.SCALE_DOWN_RATIO;
    }

    public StaticModel(UStaticMesh export, CStaticMesh staticMesh, Transform transform = null)
        : base(export, staticMesh.LODs[LodLevel], export.Materials, staticMesh.LODs[LodLevel].Verts, staticMesh.LODs.Count, transform)
    {
        Box = staticMesh.BoundingBox * Constants.SCALE_DOWN_RATIO;
        for (int i = 0; i < export.Sockets.Length; i++)
        {
            if (export.Sockets[i].Load<UStaticMeshSocket>() is not { } socket) continue;
            Sockets.Add(new Socket(socket));
        }
    }
}
