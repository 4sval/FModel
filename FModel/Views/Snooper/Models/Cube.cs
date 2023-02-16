using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Objects.Core.Misc;
using FModel.Views.Snooper.Shading;

namespace FModel.Views.Snooper.Models;

public class Cube : Model
{
    public Cube(CStaticMesh mesh, FGuid guid, UMaterialInterface unrealMaterial) : base(unrealMaterial, guid)
    {
        var lod = mesh.LODs[0];

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
    }
}
