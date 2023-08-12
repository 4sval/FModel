using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CUE4Parse_Conversion.Animations.PSA;
using CUE4Parse.UE4.Assets.Exports.Animation;
using FModel.Views.Snooper.Buffers;
using FModel.Views.Snooper.Shading;
using OpenTK.Graphics.OpenGL4;
using Serilog;

namespace FModel.Views.Snooper.Animations;

public class Skeleton : IDisposable
{
    private int _handle;
    private BufferObject<Matrix4x4> _rest;
    private BufferObject<Matrix4x4> _ssbo;

    public string Name;
    public bool IsAnimated { get; private set; }

    public readonly int BoneCount;
    public readonly Dictionary<string, Bone> BonesByLoweredName;

    public Skeleton()
    {
        BonesByLoweredName = new Dictionary<string, Bone>();
    }

    public Skeleton(FReferenceSkeleton referenceSkeleton) : this()
    {
        BoneCount = referenceSkeleton.FinalRefBoneInfo.Length;
        for (int boneIndex = 0; boneIndex < BoneCount; boneIndex++)
        {
            var info = referenceSkeleton.FinalRefBoneInfo[boneIndex];
            var boneTransform = new Transform
            {
                Rotation = referenceSkeleton.FinalRefBonePose[boneIndex].Rotation,
                Position = referenceSkeleton.FinalRefBonePose[boneIndex].Translation * Constants.SCALE_DOWN_RATIO,
                Scale = referenceSkeleton.FinalRefBonePose[boneIndex].Scale3D
            };

            var bone = new Bone(boneIndex, info.ParentIndex, boneTransform);
            if (!bone.IsRoot)
            {
                bone.LoweredParentName =
                    referenceSkeleton.FinalRefBoneInfo[bone.ParentIndex].Name.Text.ToLower();
                bone.Rest.Relation = BonesByLoweredName[bone.LoweredParentName].Rest.Matrix;
            }

            BonesByLoweredName[info.Name.Text.ToLower()] = bone;
        }
    }

    public void Animate(CAnimSet animation)
    {
        IsAnimated = true;
        ResetAnimatedData();

        // map bones
        for (int boneIndex = 0; boneIndex < animation.Skeleton.BoneCount; boneIndex++)
        {
            var info = animation.Skeleton.ReferenceSkeleton.FinalRefBoneInfo[boneIndex];
            if (!BonesByLoweredName.TryGetValue(info.Name.Text.ToLower(), out var bone))
                continue;

            bone.SkeletonIndex = boneIndex;
        }

        // find playable sequences
        for (int s = 0; s < animation.Sequences.Count; s++)
        {
            var sequence = animation.Sequences[s];
            foreach (var bone in BonesByLoweredName.Values.Where(bone => sequence.OriginalSequence.FindTrackForBoneIndex(bone.SkeletonIndex) >= 0))
            {
                bone.AnimatedBySequences.Add(s);
            }
            sequence.Retarget(animation.Skeleton);
        }

#if DEBUG
        foreach ((var boneName, var bone) in BonesByLoweredName)
        {
            if (bone.IsRoot || bone.IsMapped) // assuming root bone always is mapped
                continue;

            Log.Warning($"{Name} Bone Mismatch: {boneName} ({bone.Index}) was not present in the anim's target skeleton");
        }
#endif
    }

    public void ResetAnimatedData(bool full = false)
    {
        foreach (var bone in BonesByLoweredName.Values)
        {
            bone.SkeletonIndex = -1;
            bone.AnimatedBySequences.Clear();
        }

        if (!full) return;
        IsAnimated = false;
        _ssbo.UpdateRange(BoneCount, Matrix4x4.Identity);
    }

    public void Setup()
    {
        _handle = GL.CreateProgram();

        _rest = new BufferObject<Matrix4x4>(BoneCount, BufferTarget.ShaderStorageBuffer);
        foreach (var bone in BonesByLoweredName.Values)
        {
            _rest.Update(bone.Index, bone.Rest.Matrix);
        }
        _rest.Unbind();

        _ssbo = new BufferObject<Matrix4x4>(BoneCount, BufferTarget.ShaderStorageBuffer);
        _ssbo.UpdateRange(BoneCount, Matrix4x4.Identity);
    }

    public void UpdateAnimationMatrices(Animation animation, bool rotationOnly)
    {
        if (!IsAnimated) return;

        _ssbo.Bind();

        foreach (var bone in BonesByLoweredName.Values)
        {
            var boneRelation = bone.IsRoot ? bone.Rest.Relation : bone.Rest.LocalMatrix * _ssbo.Get(bone.ParentIndex);
            if (bone.IsAnimated)
            {
                var (s, f) = GetBoneFrameData(bone, animation);
                var sequence = animation.UnrealAnim.Sequences[s];
                var boneOrientation = bone.Rest.Rotation;
                var bonePosition = bone.Rest.Position;
                var boneScale = bone.Rest.Scale;

                sequence.Tracks[bone.SkeletonIndex].GetBoneTransform(f, sequence.NumFrames, ref boneOrientation, ref bonePosition, ref boneScale);
                if (!bone.IsRoot) boneRelation = _ssbo.Get(bone.ParentIndex);
                bonePosition = rotationOnly ? bone.Rest.Position : bonePosition * Constants.SCALE_DOWN_RATIO;

                _ssbo.Update(bone.Index, new Transform
                {
                    Relation = boneRelation,
                    Rotation = boneOrientation,
                    Position = bonePosition,
                    Scale = boneScale
                }.Matrix);
            }
            else _ssbo.Update(bone.Index, boneRelation);
        }

        _ssbo.Unbind();
    }

    private (int, float) GetBoneFrameData(Bone bone, Animation animation)
    {
        int s = -1;
        float f = 0.0f;

        void Get(Bone b)
        {
            foreach (var i in b.AnimatedBySequences)
            {
                s = i;
                if (animation.Framing.TryGetValue(s, out f))
                    break;
            }
        }

        Get(bone);
        if (s == -1)
        {
            var parent = BonesByLoweredName[bone.LoweredParentName];
            while (!parent.IsAnimated)
            {
                parent = BonesByLoweredName[parent.LoweredParentName];
            }
            Get(parent);
        }

        return (s, f);
    }

    public Matrix4x4 GetBoneMatrix(Bone bone)
    {
        _ssbo.Bind();
        var anim = _ssbo.Get(bone.Index);
        _ssbo.Unbind();

        return IsAnimated ? anim : bone.Rest.Matrix;
    }

    public void Render(Shader shader)
    {
        shader.SetUniform("uIsAnimated", IsAnimated);

        _ssbo.BindBufferBase(1);
        _rest.BindBufferBase(2);
    }

    public void Dispose()
    {
        BonesByLoweredName.Clear();

        _rest?.Dispose();
        _ssbo?.Dispose();
        GL.DeleteProgram(_handle);
    }
}
