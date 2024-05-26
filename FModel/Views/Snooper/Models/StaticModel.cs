using System.Numerics;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.PhysicsEngine;
using FModel.Views.Snooper.Shading;
using OpenTK.Graphics.OpenGL4;

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

        Box = staticMesh.BoundingBox * 1.5f * Constants.SCALE_DOWN_RATIO;
    }

    public StaticModel(UStaticMesh export, CStaticMesh staticMesh, Transform transform = null)
        : base(export, staticMesh.LODs[LodLevel], export.Materials, staticMesh.LODs[LodLevel].Verts, staticMesh.LODs.Count, transform)
    {
        if (export.BodySetup.TryLoad(out UBodySetup bodySetup) && bodySetup.AggGeom != null)
        {
            foreach (var convexElem in bodySetup.AggGeom.ConvexElems)
            {
                Collisions.Add(new Collision(convexElem));
            }
            foreach (var sphereElem in bodySetup.AggGeom.SphereElems)
            {
                Collisions.Add(new Collision(sphereElem));
            }
            foreach (var boxElem in bodySetup.AggGeom.BoxElems)
            {
                Collisions.Add(new Collision(boxElem));
            }
            foreach (var sphylElem in bodySetup.AggGeom.SphylElems)
            {
                Collisions.Add(new Collision(sphylElem));
            }
            foreach (var taperedCapsuleElem in bodySetup.AggGeom.TaperedCapsuleElems)
            {
                Collisions.Add(new Collision(taperedCapsuleElem));
            }
        }

        Box = staticMesh.BoundingBox * Constants.SCALE_DOWN_RATIO;
        for (int i = 0; i < export.Sockets.Length; i++)
        {
            if (export.Sockets[i].Load<UStaticMeshSocket>() is not { } socket) continue;
            Sockets.Add(new Socket(socket));
        }
    }

    public override void RenderCollision(Shader shader)
    {
        base.RenderCollision(shader);

        GL.Disable(EnableCap.CullFace);
        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
        foreach (var collision in Collisions)
        {
            collision.Render(shader, Matrix4x4.Identity);
        }
        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        GL.Enable(EnableCap.CullFace);
    }
}
