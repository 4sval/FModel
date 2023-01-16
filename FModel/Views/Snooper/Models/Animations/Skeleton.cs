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
    public readonly Dictionary<string, int> BonesIndexByName;
    public Dictionary<int, Transform> BonesTransformByIndex;
    public Dictionary<int, Transform> AnimBonesTransformByIndex;
    public readonly bool IsLoaded;

    public Animation Anim;

    public Skeleton()
    {
        BonesIndexByName = new Dictionary<string, int>();
        BonesTransformByIndex = new Dictionary<int, Transform>();
        AnimBonesTransformByIndex = new Dictionary<int, Transform>();
    }

    public Skeleton(FPackageIndex package, FReferenceSkeleton referenceSkeleton, Transform transform) : this()
    {
        UnrealSkeleton = package.Load<USkeleton>();
        IsLoaded = UnrealSkeleton != null;
        if (!IsLoaded) return;

        ReferenceSkeleton = referenceSkeleton ?? UnrealSkeleton.ReferenceSkeleton;
        BonesIndexByName = ReferenceSkeleton.FinalNameToIndexMap;
        UpdateBoneMatrices(transform.Matrix);
    }

    public void SetAnimation(CAnimSet anim)
    {
        Anim = new Animation(anim);
    }

    public void UpdateBoneMatrices(Matrix4x4 matrix)
    {
        if (!IsLoaded) return;
        foreach (var boneIndex in BonesIndexByName.Values)
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
                parentTransform = new Transform { Relation = matrix };

            boneTransform.Relation = parentTransform.Matrix;
            BonesTransformByIndex[boneIndex] = AnimBonesTransformByIndex[boneIndex] = boneTransform;
        }
    }

    public void SetUniform(Shader shader)
    {
        if (!IsLoaded || Anim == null) return;
        AnimBonesTransformByIndex = Anim.CalculateBoneTransform(ReferenceSkeleton.FinalRefBoneInfo, BonesTransformByIndex);
        foreach ((int boneIndex, Transform boneTransform) in AnimBonesTransformByIndex)
        {
            shader.SetUniform($"uFinalBonesMatrix[{boneIndex}]", boneTransform.Matrix);
        }
    }

    public void Dispose()
    {
        BonesIndexByName.Clear();
        BonesTransformByIndex.Clear();
        Anim?.Dispose();
    }
}
