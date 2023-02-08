using System;
using System.Collections.Generic;
using System.Numerics;
using CUE4Parse_Conversion.Animations;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Views.Snooper.Shading;
using Serilog;

namespace FModel.Views.Snooper.Models.Animations;

public struct BoneIndice
{
    public int Index;
    public int ParentIndex;
}

public class Skeleton : IDisposable
{
    public readonly USkeleton UnrealSkeleton;
    public readonly bool IsLoaded;

    public readonly Dictionary<string, BoneIndice> BonesIndicesByLoweredName;
    public readonly Dictionary<int, Transform> BonesTransformByIndex;
    public readonly Dictionary<int, Matrix4x4> InvertedBonesMatrixByIndex;

    public Animation Anim;
    public bool HasAnim => Anim != null;

    public Skeleton()
    {
        BonesIndicesByLoweredName = new Dictionary<string, BoneIndice>();
        BonesTransformByIndex = new Dictionary<int, Transform>();
        InvertedBonesMatrixByIndex = new Dictionary<int, Matrix4x4>();
    }

    public Skeleton(FPackageIndex package, FReferenceSkeleton referenceSkeleton) : this()
    {
        UnrealSkeleton = package.Load<USkeleton>();
        IsLoaded = UnrealSkeleton != null;
        if (!IsLoaded) return;

        for (int boneIndex = 0; boneIndex < referenceSkeleton.FinalRefBoneInfo.Length; boneIndex++)
        {
            var info = referenceSkeleton.FinalRefBoneInfo[boneIndex];
            BonesIndicesByLoweredName[info.Name.Text.ToLower()] = new BoneIndice { Index = boneIndex, ParentIndex = info.ParentIndex };
        }

#if DEBUG
        for (int trackIndex = 0; trackIndex < UnrealSkeleton.ReferenceSkeleton.FinalRefBoneInfo.Length; trackIndex++)
        {
            var bone = UnrealSkeleton.ReferenceSkeleton.FinalRefBoneInfo[trackIndex];
            if (!BonesIndicesByLoweredName.TryGetValue(bone.Name.Text.ToLower(), out _))
                Log.Warning($"Bone Mismatch: {bone.Name.Text} ({trackIndex}) is not present in the mesh's skeleton");
        }
#endif

        foreach (var boneIndices in BonesIndicesByLoweredName.Values)
        {
            var bone = referenceSkeleton.FinalRefBonePose[boneIndices.Index];
            if (!BonesTransformByIndex.TryGetValue(boneIndices.Index, out var boneTransform))
            {
                boneTransform = new Transform
                {
                    Rotation = bone.Rotation,
                    Position = bone.Translation * Constants.SCALE_DOWN_RATIO,
                    Scale = bone.Scale3D
                };
            }

            if (!BonesTransformByIndex.TryGetValue(boneIndices.ParentIndex, out var parentTransform))
                parentTransform = new Transform { Relation = Matrix4x4.Identity };

            boneTransform.Relation = parentTransform.Matrix;
            Matrix4x4.Invert(boneTransform.Matrix, out var inverted);

            BonesTransformByIndex[boneIndices.Index] = boneTransform;
            InvertedBonesMatrixByIndex[boneIndices.Index] = inverted;
        }
    }

    public void SetAnimation(CAnimSet anim, bool rotationOnly)
    {
        Anim = new Animation(this, anim, rotationOnly);
    }

    public void SetUniform(Shader shader, float deltaSeconds = 0f, bool update = false)
    {
        if (!IsLoaded) return;
        if (!HasAnim)
        {
            foreach (var boneIndex in BonesTransformByIndex.Keys)
            {
                if (boneIndex >= Constants.MAX_BONE_UNIFORM)
                    break;
                shader.SetUniform($"uFinalBonesMatrix[{boneIndex}]", Matrix4x4.Identity);
            }
        }
        else
        {
            if (update) Anim.Update(deltaSeconds);
            foreach (var boneIndex in BonesTransformByIndex.Keys)
            {
                if (boneIndex >= Constants.MAX_BONE_UNIFORM)
                    break;
                if (!InvertedBonesMatrixByIndex.TryGetValue(boneIndex, out var invertMatrix))
                    throw new ArgumentNullException($"no inverse matrix for bone '{boneIndex}'");

                shader.SetUniform($"uFinalBonesMatrix[{boneIndex}]", invertMatrix * Anim.InterpolateBoneTransform(boneIndex));
            }
            if (update) Anim.CheckForNextSequence();
        }
    }

    public void Dispose()
    {
        BonesIndicesByLoweredName.Clear();
        BonesTransformByIndex.Clear();
        InvertedBonesMatrixByIndex.Clear();
        Anim?.Dispose();
    }
}
