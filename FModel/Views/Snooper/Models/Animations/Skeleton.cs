using System;
using System.Collections.Generic;
using System.Numerics;
using CUE4Parse_Conversion.Animations;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Views.Snooper.Shading;

namespace FModel.Views.Snooper.Models.Animations;

public class Skeleton : IDisposable
{
    public readonly USkeleton UnrealSkeleton;
    public readonly FReferenceSkeleton ReferenceSkeleton;
    public readonly Dictionary<string, int> BonesIndexByLoweredName;
    public readonly Dictionary<int, Transform> BonesTransformByIndex;
    public readonly Dictionary<int, Matrix4x4> InvertedBonesMatrixByIndex;
    public readonly bool IsLoaded;

    public Animation Anim;
    public bool HasAnim => Anim != null;

    public Skeleton()
    {
        BonesIndexByLoweredName = new Dictionary<string, int>();
        BonesTransformByIndex = new Dictionary<int, Transform>();
        InvertedBonesMatrixByIndex = new Dictionary<int, Matrix4x4>();
    }

    public Skeleton(FPackageIndex package, FReferenceSkeleton referenceSkeleton) : this()
    {
        UnrealSkeleton = package.Load<USkeleton>();
        IsLoaded = UnrealSkeleton != null;
        if (!IsLoaded) return;

        ReferenceSkeleton = referenceSkeleton;
        foreach ((var name, var boneIndex) in referenceSkeleton.FinalNameToIndexMap)
            BonesIndexByLoweredName[name.ToLower()] = boneIndex;

        foreach (var boneIndex in BonesIndexByLoweredName.Values)
        {
            var bone = ReferenceSkeleton.FinalRefBonePose[boneIndex];
            var parentIndex = ReferenceSkeleton.FinalRefBoneInfo[boneIndex].ParentIndex;

            if (!BonesTransformByIndex.TryGetValue(boneIndex, out var boneTransform))
            {
                boneTransform = new Transform
                {
                    Rotation = bone.Rotation,
                    Position = bone.Translation * Constants.SCALE_DOWN_RATIO,
                    Scale = bone.Scale3D
                };
            }

            if (!BonesTransformByIndex.TryGetValue(parentIndex, out var parentTransform))
                parentTransform = new Transform { Relation = Matrix4x4.Identity };

            boneTransform.Relation = parentTransform.Matrix;
            Matrix4x4.Invert(boneTransform.Matrix, out var inverted);

            BonesTransformByIndex[boneIndex] = boneTransform;
            InvertedBonesMatrixByIndex[boneIndex] = inverted;
        }
    }

    public void SetAnimation(CAnimSet anim, bool rotationOnly)
    {
        Anim = new Animation(this, anim, rotationOnly);
    }

    public void SetPoseUniform(Shader shader)
    {
        if (!IsLoaded) return;
        foreach (var boneIndex in BonesTransformByIndex.Keys)
        {
            if (boneIndex >= Constants.MAX_BONE_UNIFORM)
                break;
            shader.SetUniform($"uFinalBonesMatrix[{boneIndex}]", Matrix4x4.Identity);
        }
    }

    public void SetUniform(Shader shader, float deltaSeconds = 0f, bool update = false)
    {
        if (!IsLoaded) return;
        if (!HasAnim) SetPoseUniform(shader);
        else
        {
            if (update) Anim.Update(deltaSeconds);
            foreach (var boneIndex in BonesTransformByIndex.Keys)
            {
                if (boneIndex >= Constants.MAX_BONE_UNIFORM)
                    break;
                if (!InvertedBonesMatrixByIndex.TryGetValue(boneIndex, out var invertMatrix))
                    throw new ArgumentNullException($"no inverse matrix for bone '{boneIndex}'");
                if (!Anim.TrackIndexByBoneIndex.TryGetValue(boneIndex, out var trackIndex))
                    throw new ArgumentNullException($"no track index for bone '{boneIndex}'");

                shader.SetUniform($"uFinalBonesMatrix[{boneIndex}]", invertMatrix * Anim.InterpolateBoneTransform(trackIndex));
            }
        }
    }

    public void Dispose()
    {
        BonesIndexByLoweredName.Clear();
        BonesTransformByIndex.Clear();
        Anim?.Dispose();
    }
}
