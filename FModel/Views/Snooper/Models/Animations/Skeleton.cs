using System;
using System.Collections.Generic;
using System.Numerics;
using CUE4Parse_Conversion.Animations;
using CUE4Parse.UE4.Assets.Exports.Animation;
using FModel.Views.Snooper.Buffers;
using OpenTK.Graphics.OpenGL4;
using Serilog;

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

            var boneIndices = new BoneIndice { BoneIndex = boneIndex, ParentBoneIndex = info.ParentIndex };
            if (!boneIndices.IsRoot)
                boneIndices.LoweredParentBoneName =
                    referenceSkeleton.FinalRefBoneInfo[boneIndices.ParentBoneIndex].Name.Text.ToLower();

            BonesIndicesByLoweredName[info.Name.Text.ToLower()] = boneIndices;
        }

        InvertedBonesMatrix = new Matrix4x4[BonesIndicesByLoweredName.Count];
        foreach (var boneIndices in BonesIndicesByLoweredName.Values)
        {
            var bone = referenceSkeleton.FinalRefBonePose[boneIndices.BoneIndex];
            if (!BonesTransformByIndex.TryGetValue(boneIndices.BoneIndex, out var boneTransform))
            {
                boneTransform = new Transform
                {
                    Rotation = bone.Rotation,
                    Position = bone.Translation * Constants.SCALE_DOWN_RATIO,
                    Scale = bone.Scale3D
                };
            }

            if (!BonesTransformByIndex.TryGetValue(boneIndices.ParentBoneIndex, out var parentTransform))
                parentTransform = new Transform { Relation = Matrix4x4.Identity };

            boneTransform.Relation = parentTransform.Matrix;
            Matrix4x4.Invert(boneTransform.Matrix, out var inverted);


            BonesTransformByIndex[boneIndices.BoneIndex] = boneTransform;
            InvertedBonesMatrix[boneIndices.BoneIndex] = inverted;
        }
    }

    public void SetAnimation(CAnimSet anim, bool rotationOnly)
    {
        TrackSkeleton(anim);
        Anim = new Animation(this, anim, rotationOnly);
    }

    private void TrackSkeleton(CAnimSet anim)
    {
        // reset
        foreach (var boneIndices in BonesIndicesByLoweredName.Values)
        {
            boneIndices.TrackIndex = -1;
            boneIndices.ParentTrackIndex = -1;
        }

        // tracked bones
        for (int trackIndex = 0; trackIndex < anim.TrackBonesInfo.Length; trackIndex++)
        {
            var info = anim.TrackBonesInfo[trackIndex];
            if (!BonesIndicesByLoweredName.TryGetValue(info.Name.Text.ToLower(), out var boneIndices))
                continue;

            boneIndices.TrackIndex = trackIndex;
            var parentTrackIndex = info.ParentIndex;
            if (parentTrackIndex < 0) continue;

            do
            {
                info = anim.TrackBonesInfo[parentTrackIndex];
                if (BonesIndicesByLoweredName.TryGetValue(info.Name.Text.ToLower(), out var parentBoneIndices) && parentBoneIndices.HasTrack)
                    boneIndices.ParentTrackIndex = parentBoneIndices.BoneIndex;
                else parentTrackIndex = info.ParentIndex;
            } while (!boneIndices.HasParentTrack);
        }

        // fix parent of untracked bones
        foreach ((var boneName, var boneIndices) in BonesIndicesByLoweredName)
        {
            if (boneIndices.IsRoot || boneIndices.HasTrack && boneIndices.HasParentTrack) // assuming root bone always has a track
                continue;

#if DEBUG
            Log.Warning($"Bone Mismatch: {boneName} ({boneIndices.BoneIndex}) was not present in the anim's target skeleton");
#endif

            var loweredParentBoneName = boneIndices.LoweredParentBoneName;
            do
            {
                var parentBoneIndices = BonesIndicesByLoweredName[loweredParentBoneName];
                if (parentBoneIndices.HasTrack) boneIndices.ParentTrackIndex = parentBoneIndices.BoneIndex;
                else loweredParentBoneName = parentBoneIndices.LoweredParentBoneName;
            } while (!boneIndices.HasParentTrack);
        }
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
