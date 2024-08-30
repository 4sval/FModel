using System;
using System.Collections.Generic;
using System.Numerics;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.PhysicsEngine;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Views.Snooper.Animations;
using FModel.Views.Snooper.Buffers;
using FModel.Views.Snooper.Shading;
using OpenTK.Graphics.OpenGL4;

namespace FModel.Views.Snooper.Models;

public class SkeletalModel : UModel
{
    private BufferObject<float> _morphVbo;

    public readonly Skeleton Skeleton;
    public readonly List<Morph> Morphs;

    public bool HasMorphTargets => Morphs.Count > 0;

    public float MorphTime;

    public SkeletalModel(USkeletalMesh export, CSkeletalMesh skeletalMesh, Transform transform = null)
        : base(export, skeletalMesh.LODs[LodLevel], export.Materials, skeletalMesh.LODs[LodLevel].Verts, skeletalMesh.LODs.Count, transform)
    {
        Box = skeletalMesh.BoundingBox * Constants.SCALE_DOWN_RATIO;
        Skeleton = new Skeleton(export.ReferenceSkeleton);

        var sockets = new List<FPackageIndex>();
        sockets.AddRange(export.Sockets);
        if (export.Skeleton.TryLoad(out USkeleton skeleton))
        {
            Skeleton.Name = skeleton.Name;
            // Skeleton.Merge(skeleton.ReferenceSkeleton);
            sockets.AddRange(skeleton.Sockets);
        }

        for (int i = 0; i < sockets.Count; i++)
        {
            if (sockets[i].Load<USkeletalMeshSocket>() is not { } socket) continue;
            Sockets.Add(new Socket(socket));
        }

        if (export.PhysicsAsset.TryLoad(out UPhysicsAsset physicsAsset))
        {
            foreach (var skeletalBodySetup in physicsAsset.SkeletalBodySetups)
            {
                if (!skeletalBodySetup.TryLoad(out USkeletalBodySetup bodySetup) || bodySetup.AggGeom == null) continue;
                foreach (var convexElem in bodySetup.AggGeom.ConvexElems)
                {
                    Collisions.Add(new Collision(convexElem, bodySetup.BoneName));
                }
                foreach (var sphereElem in bodySetup.AggGeom.SphereElems)
                {
                    Collisions.Add(new Collision(sphereElem, bodySetup.BoneName));
                }
                foreach (var boxElem in bodySetup.AggGeom.BoxElems)
                {
                    Collisions.Add(new Collision(boxElem, bodySetup.BoneName));
                }
                foreach (var sphylElem in bodySetup.AggGeom.SphylElems)
                {
                    Collisions.Add(new Collision(sphylElem, bodySetup.BoneName));
                }
                foreach (var taperedCapsuleElem in bodySetup.AggGeom.TaperedCapsuleElems)
                {
                    Collisions.Add(new Collision(taperedCapsuleElem, bodySetup.BoneName));
                }
            }
        }

        Morphs = [];
        if (export.MorphTargets.Length == 0) return;

        export.PopulateMorphTargetVerticesData();

        foreach (var morph in export.MorphTargets)
        {
            if (!morph.TryLoad(out UMorphTarget morphTarget) || morphTarget.MorphLODModels.Length < 1 ||
                morphTarget.MorphLODModels[0].Vertices.Length < 1)
                continue;

            Morphs.Add(new Morph(Vertices, VertexSize, morphTarget));
        }
    }

    public SkeletalModel(USkeleton export, FBox box) : base(export)
    {
        Indices = [];
        Materials = [];
        Vertices = [];
        Sections = [];
        AddInstance(Transform.Identity);

        Box = box * Constants.SCALE_DOWN_RATIO;
        Morphs = new List<Morph>();
        Skeleton = new Skeleton(export.ReferenceSkeleton);
        Skeleton.Name = export.Name;

        for (int i = 0; i < export.Sockets.Length; i++)
        {
            if (export.Sockets[i].Load<USkeletalMeshSocket>() is not { } socket) continue;
            Sockets.Add(new Socket(socket));
        }
    }

    public override void Setup(Options options)
    {
        base.Setup(options);

        Skeleton.Setup();
        if (!HasMorphTargets) return;

        for (int morph = 0; morph < Morphs.Count; morph++)
        {
            Morphs[morph].Setup();
            if (morph == 0)
                _morphVbo = new BufferObject<float>(Morphs[morph].Vertices, BufferTarget.ArrayBuffer);
        }

        Vao.Bind();
        Vao.VertexAttributePointer(13, 3, VertexAttribPointerType.Float, Morph.VertexSize, 0); // morph position
        Vao.VertexAttributePointer(14, 3, VertexAttribPointerType.Float, Morph.VertexSize, 0); // morph tangent
        Vao.Unbind();
    }

    public override void RenderCollision(Shader shader)
    {
        base.RenderCollision(shader);

        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);
        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
        foreach (var collision in Collisions)
        {
            var boneMatrix = Matrix4x4.Identity;
            if (Skeleton.BonesByLoweredName.TryGetValue(collision.LoweredBoneName, out var bone))
                boneMatrix = Skeleton.GetBoneMatrix(bone);

            collision.Render(shader, boneMatrix);
        }
        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        GL.Enable(EnableCap.CullFace);
        GL.Enable(EnableCap.DepthTest);
    }

    public void Render(Shader shader)
    {
        shader.SetUniform("uMorphTime", MorphTime);
        Skeleton.Render(shader);
    }

    public void RenderBones(Shader shader)
    {
        shader.SetUniform("uInstanceMatrix", GetTransform().Matrix);
        Skeleton.RenderBones();
    }

    public void UpdateMorph(int index)
    {
        _morphVbo.Update(Morphs[index].Vertices);
    }

    public override void Dispose()
    {
        Skeleton?.Dispose();
        if (HasMorphTargets) _morphVbo.Dispose();
        foreach (var morph in Morphs)
        {
            morph?.Dispose();
        }
        Morphs.Clear();

        base.Dispose();
    }
}
