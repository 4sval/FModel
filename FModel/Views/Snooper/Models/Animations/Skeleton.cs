using System;
using System.Collections.Generic;
using System.Numerics;
using CUE4Parse_Conversion.Animations;
using CUE4Parse.UE4.Assets.Exports.Animation;
using FModel.Views.Snooper.Buffers;
using OpenTK.Graphics.OpenGL4;

namespace FModel.Views.Snooper.Models.Animations;

public class Skeleton : IDisposable
{
    private int _handle;
    private BufferObject<Matrix4x4> _ssbo;

    public string Name;
    public readonly Dictionary<string, BoneIndice> BonesIndicesByLoweredName;
    public readonly Dictionary<int, Transform> BonesTransformByIndex;
    public readonly Matrix4x4[] InvertedBonesMatrix;
    public int BoneCount => InvertedBonesMatrix.Length;

    public Animation Anim;
    public bool HasAnim => Anim != null;

    public Skeleton()
    {
        BonesIndicesByLoweredName = new Dictionary<string, BoneIndice>();
        BonesTransformByIndex = new Dictionary<int, Transform>();
        InvertedBonesMatrix = Array.Empty<Matrix4x4>();
    }

    public Skeleton(FReferenceSkeleton referenceSkeleton) : this()
    {
        for (int boneIndex = 0; boneIndex < referenceSkeleton.FinalRefBoneInfo.Length; boneIndex++)
        {
            var info = referenceSkeleton.FinalRefBoneInfo[boneIndex];
            BonesIndicesByLoweredName[info.Name.Text.ToLower()] = new BoneIndice { Index = boneIndex, ParentIndex = info.ParentIndex };
        }

        InvertedBonesMatrix = new Matrix4x4[BonesIndicesByLoweredName.Count];
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
            InvertedBonesMatrix[boneIndices.Index] = inverted;
        }
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
    }

    private int _previousFrame;
    public void UpdateMatrices(float deltaSeconds)
    {
        if (!HasAnim) return;

        Anim.Update(deltaSeconds);
        if (_previousFrame == Anim.FrameInSequence) return;
        _previousFrame = Anim.FrameInSequence;

        _ssbo.Bind();
        for (int boneIndex = 0; boneIndex < BoneCount; boneIndex++)
            _ssbo.Update(boneIndex, InvertedBonesMatrix[boneIndex] * Anim.InterpolateBoneTransform(boneIndex));
        _ssbo.Unbind();
    }

    public void Render()
    {
        _ssbo.BindBufferBase(1);
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
