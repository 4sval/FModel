using System;
using System.Collections.Generic;
using System.Numerics;
using CUE4Parse_Conversion.Animations;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Views.Snooper.Buffers;
using OpenTK.Graphics.OpenGL4;
using Serilog;

namespace FModel.Views.Snooper.Models.Animations;

public struct BoneIndice
{
    public int Index;
    public int ParentIndex;
}

public class Skeleton : IDisposable
{
    private int _handle;
    private BufferObject<Matrix4x4> _ssbo;

    public readonly USkeleton UnrealSkeleton;
    public readonly bool IsLoaded;

    public readonly Dictionary<string, BoneIndice> BonesIndicesByLoweredName;
    public readonly Dictionary<int, Transform> BonesTransformByIndex;
    public readonly Matrix4x4[] InvertedBonesMatrixByIndex;

    public Animation Anim;
    public bool HasAnim => Anim != null;

    public Skeleton()
    {
        BonesIndicesByLoweredName = new Dictionary<string, BoneIndice>();
        BonesTransformByIndex = new Dictionary<int, Transform>();
        InvertedBonesMatrixByIndex = Array.Empty<Matrix4x4>();
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

        InvertedBonesMatrixByIndex = new Matrix4x4[BonesIndicesByLoweredName.Count];
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

    public void Setup()
    {
        _handle = GL.CreateProgram();
        _ssbo = new BufferObject<Matrix4x4>(InvertedBonesMatrixByIndex.Length, BufferTarget.ShaderStorageBuffer);
    }

    public void Render(float deltaSeconds = 0f, bool update = false)
    {
        if (!IsLoaded) return;

        _ssbo.BindBufferBase(1);

        if (!HasAnim)
        {
            for (int boneIndex = 0; boneIndex < InvertedBonesMatrixByIndex.Length; boneIndex++)
            {
                _ssbo.Update(boneIndex, Matrix4x4.Identity);
            }
        }
        else
        {
            if (update) Anim.Update(deltaSeconds);
            for (int boneIndex = 0; boneIndex < InvertedBonesMatrixByIndex.Length; boneIndex++)
            {
                _ssbo.Update(boneIndex, InvertedBonesMatrixByIndex[boneIndex] * Anim.InterpolateBoneTransform(boneIndex));
            }
            if (update) Anim.CheckForNextSequence();
        }

        _ssbo.Unbind();
    }

    public void Dispose()
    {
        BonesIndicesByLoweredName.Clear();
        BonesTransformByIndex.Clear();
        Anim?.Dispose();

        _ssbo?.Dispose();
        GL.DeleteProgram(_handle);
    }
}
