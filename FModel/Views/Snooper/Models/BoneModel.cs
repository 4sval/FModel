using CUE4Parse_Conversion.Meshes.PSK;
using FModel.Views.Snooper.Shading;
using OpenTK.Graphics.OpenGL4;

namespace FModel.Views.Snooper.Models;

public class BoneModel : UModel
{
    public BoneModel(CStaticMesh staticMesh)
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
            Vertices[baseIndex + count++] = vert.Position.X * Constants.SCALE_DOWN_RATIO;
            Vertices[baseIndex + count++] = vert.Position.Z * Constants.SCALE_DOWN_RATIO;
            Vertices[baseIndex + count++] = vert.Position.Y * Constants.SCALE_DOWN_RATIO;
        }

        Materials = new Material[1];
        Materials[0] = new Material { IsUsed = true };

        Sections = new Section[1];
        Sections[0] = new Section(0, Indices.Length, 0);

        Box = staticMesh.BoundingBox * Constants.SCALE_DOWN_RATIO;
    }

    public override void Render(Shader shader, bool outline = false)
    {
        GL.Disable(EnableCap.DepthTest);

        Vao.Bind();
        foreach (var section in Sections)
        {
            GL.DrawElementsInstanced(PrimitiveType.LineStrip, section.FacesCount, DrawElementsType.UnsignedInt, section.FirstFaceIndexPtr, TransformsCount);
        }
        Vao.Unbind();

        GL.Enable(EnableCap.DepthTest);
    }
}
