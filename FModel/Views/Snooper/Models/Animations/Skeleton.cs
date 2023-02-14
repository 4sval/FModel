using System;
using System.Collections.Generic;
using System.Numerics;
using CUE4Parse_Conversion.Animations;
using CUE4Parse.UE4.Assets.Exports.Animation;
using FModel.Views.Snooper.Buffers;
using OpenTK.Graphics.OpenGL4;

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

    public string Name;
    public readonly Dictionary<string, BoneIndice> BonesIndicesByLoweredName;
    public readonly Dictionary<int, Transform> BonesTransformByIndex;
    public readonly int BoneCount;

    public Animation Anim;
    public bool HasAnim => Anim != null;

    public Skeleton()
    {
        BonesIndicesByLoweredName = new Dictionary<string, BoneIndice>();
        BonesTransformByIndex = new Dictionary<int, Transform>();
    }

    public Skeleton(FReferenceSkeleton referenceSkeleton) : this()
    {
        for (int boneIndex = 0; boneIndex < referenceSkeleton.FinalRefBoneInfo.Length; boneIndex++)
        {
            var info = referenceSkeleton.FinalRefBoneInfo[boneIndex];
            BonesIndicesByLoweredName[info.Name.Text.ToLower()] = new BoneIndice { Index = boneIndex, ParentIndex = info.ParentIndex };
        }

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
            BonesTransformByIndex[boneIndices.Index] = boneTransform;
        }

        BoneCount = BonesTransformByIndex.Count;
    }

    public void SetAnimation(CAnimSet anim, bool rotationOnly)
    {
        Anim = new Animation(this, anim, rotationOnly);
    }

    public void Setup()
    {
        _handle = GL.CreateProgram();

        _ssbo = new BufferObject<Matrix4x4>(BoneCount, BufferTarget.ShaderStorageBuffer);
        for (int boneIndex = 0; boneIndex < BoneCount; boneIndex++)
            _ssbo.Update(boneIndex, Matrix4x4.Identity);
        _ssbo.BindBufferBase(1);
    }

    public void UpdateMatrices(float deltaSeconds)
    {
        if (!HasAnim) return;

        _ssbo.BindBufferBase(1);

        Anim.Update(deltaSeconds);
        for (int boneIndex = 0; boneIndex < BoneCount; boneIndex++)
            _ssbo.Update(boneIndex, Anim.InterpolateBoneTransform(boneIndex));

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
