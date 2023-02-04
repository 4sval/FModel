using System;
using System.Numerics;
using CUE4Parse_Conversion.Animations;
using CUE4Parse.UE4.Objects.Core.Math;

namespace FModel.Views.Snooper.Models.Animations;

public class Animation : IDisposable
{
    public int CurrentTime;
    public readonly int MaxTime;
    public readonly Transform[][] BoneTransforms;

    public Animation(Skeleton skeleton, CAnimSet anim)
    {
        CurrentTime = 0;

        var sequence = anim.Sequences[0];
        MaxTime = sequence.NumFrames - 1;
        BoneTransforms = new Transform[skeleton.UnrealSkeleton.ReferenceSkeleton.FinalRefBoneInfo.Length][];
        for (var trackIndex = 0; trackIndex < BoneTransforms.Length; trackIndex++)
        {
            var bone = skeleton.UnrealSkeleton.ReferenceSkeleton.FinalRefBoneInfo[trackIndex];
            if (!skeleton.BonesIndexByLoweredName.TryGetValue(bone.Name.Text.ToLower(), out var boneIndex))
            {
                BoneTransforms[trackIndex] = new Transform[sequence.NumFrames];
                continue;
            }
            if (!skeleton.BonesTransformByIndex.TryGetValue(boneIndex, out var originalTransform))
                throw new ArgumentNullException($"no transform for bone '{boneIndex}'");

            var boneOrientation = originalTransform.Rotation;
            var bonePosition = originalTransform.Position;
            var boneScale = originalTransform.Scale;

            BoneTransforms[trackIndex] = new Transform[sequence.NumFrames];
            for (var frame = 0; frame < BoneTransforms[trackIndex].Length; frame++)
            {
                sequence.Tracks[trackIndex].GetBonePosition(frame, sequence.NumFrames, false, ref bonePosition, ref boneOrientation);
                if (CurrentTime < sequence.Tracks[trackIndex].KeyScale.Length)
                    boneScale = sequence.Tracks[trackIndex].KeyScale[CurrentTime];

                // revert FixRotationKeys
                if (trackIndex > 0) boneOrientation.Conjugate();

                bonePosition *= Constants.SCALE_DOWN_RATIO;

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

    public void Dispose()
    {

    }
}
