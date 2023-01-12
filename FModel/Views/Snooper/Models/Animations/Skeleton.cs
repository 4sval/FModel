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
    public readonly Dictionary<string, int> BonesIndexByName;
    public readonly Dictionary<int, Transform> BonesTransformByIndex;
    public readonly bool IsLoaded;

    public Animation Anim;

    public Skeleton()
    {
        BonesIndexByName = new Dictionary<string, int>();
        BonesTransformByIndex = new Dictionary<int, Transform>();
    }

    public Skeleton(FPackageIndex package, Transform transform) : this()
    {
        UnrealSkeleton = package.Load<USkeleton>();
        if (UnrealSkeleton == null) return;

        BonesIndexByName = UnrealSkeleton.ReferenceSkeleton.FinalNameToIndexMap;
        BonesTransformByIndex = new Dictionary<int, Transform>();
        UpdateBoneMatrices(transform.Matrix);

        IsLoaded = true;
    }

    public void SetAnimation(CAnimSet anim)
    {
        Anim = new Animation(anim, BonesIndexByName, BonesTransformByIndex);
    }

    public void UpdateBoneMatrices(Matrix4x4 matrix)
    {
        foreach (var boneIndex in BonesIndexByName.Values)
        {
            var bone = UnrealSkeleton.ReferenceSkeleton.FinalRefBonePose[boneIndex];
            var parentIndex = UnrealSkeleton.ReferenceSkeleton.FinalRefBoneInfo[boneIndex].ParentIndex;

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
            BonesTransformByIndex[boneIndex] = boneTransform;
        }
    }

    public void SetUniform(Shader shader)
    {
        if (!IsLoaded) return;
        for (var i = 0; i < Anim?.FinalBonesMatrix.Length; i++)
        {
            shader.SetUniform($"uFinalBonesMatrix[{i}]", Anim.FinalBonesMatrix[i].Matrix);
        }
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
