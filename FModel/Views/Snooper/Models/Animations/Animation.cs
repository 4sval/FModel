using System;
using System.Collections.Generic;
using System.Numerics;
using CUE4Parse_Conversion.Animations;
using CUE4Parse.Utils;

namespace FModel.Views.Snooper.Models.Animations;

public class Animation : IDisposable
{
    public int Frame;
    public float ElapsedTime;
    public readonly int MaxFrame;
    public readonly float FramesPerSecond;
    public readonly Dictionary<int, int> TrackIndexByBoneIndex;
    public readonly Transform[][] BoneTransforms;

    public float TimePerFrame => 1.0f / FramesPerSecond;

    public Animation(Skeleton skeleton, CAnimSet anim)
    {
        Frame = 0;
        ElapsedTime = 0;
        TrackIndexByBoneIndex = new Dictionary<int, int>();

        var sequence = anim.Sequences[0];
        MaxFrame = sequence.NumFrames;
        FramesPerSecond = sequence.Rate;

        BoneTransforms = new Transform[skeleton.UnrealSkeleton.ReferenceSkeleton.FinalRefBoneInfo.Length][];
        for (var trackIndex = 0; trackIndex < BoneTransforms.Length; trackIndex++)
        {
            BoneTransforms[trackIndex] = new Transform[MaxFrame];

            var bone = skeleton.UnrealSkeleton.ReferenceSkeleton.FinalRefBoneInfo[trackIndex];
            if (!skeleton.BonesIndexByLoweredName.TryGetValue(bone.Name.Text.ToLower(), out var boneIndex))
                continue;
            if (!skeleton.BonesTransformByIndex.TryGetValue(boneIndex, out var originalTransform))
                throw new ArgumentNullException($"no transform for bone '{boneIndex}'");

            TrackIndexByBoneIndex[boneIndex] = trackIndex;
            var boneOrientation = originalTransform.Rotation;
            var bonePosition = originalTransform.Position;
            var boneScale = originalTransform.Scale;

            for (var frame = 0; frame < BoneTransforms[trackIndex].Length; frame++)
            {
                sequence.Tracks[trackIndex].GetBonePosition(frame, MaxFrame, false, ref bonePosition, ref boneOrientation);
                if (frame < sequence.Tracks[trackIndex].KeyScale.Length)
                    boneScale = sequence.Tracks[trackIndex].KeyScale[frame];

                // revert FixRotationKeys
                if (trackIndex > 0) boneOrientation.Conjugate();

                bonePosition *= Constants.SCALE_DOWN_RATIO;

                switch (anim.BoneModes[trackIndex])
                {
                    case EBoneRetargetingMode.Animation:
                    case EBoneRetargetingMode.Mesh:
                    case EBoneRetargetingMode.AnimationScaled:
                    case EBoneRetargetingMode.AnimationRelative:
                    case EBoneRetargetingMode.OrientAndScale:
                    case EBoneRetargetingMode.Count:
                    default:
                        break;
                }

                BoneTransforms[trackIndex][frame] = new Transform
                {
                    Relation = bone.ParentIndex >= 0 ? BoneTransforms[bone.ParentIndex][frame].Matrix : originalTransform.Relation,
                    Rotation = boneOrientation,
                    Position = bonePosition,
                    Scale = boneScale
                };
            }
        }
    }

    public Matrix4x4 InterpolateBoneTransform(int trackIndex)
    {
        Frame = ElapsedTime.FloorToInt() % MaxFrame; // interpolate here
        return BoneTransforms[trackIndex][Frame].Matrix;
    }

    public void Dispose()
    {
        TrackIndexByBoneIndex.Clear();
    }
}
