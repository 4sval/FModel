using System;
using CUE4Parse_Conversion.Animations;
using CUE4Parse.Utils;

namespace FModel.Views.Snooper.Models.Animations;

public class Sequence : IDisposable
{
    public int Frame;
    public float ElapsedTime;
    public readonly string Name;
    public readonly int MaxFrame;
    public readonly float FramesPerSecond;
    public readonly float StartPos;
    public readonly float AnimStartTime;
    public readonly float AnimEndTime;
    public readonly int LoopingCount;

    public float TimePerFrame => 1.0f / FramesPerSecond;
    public float EndPos => AnimEndTime / TimePerFrame;

    public readonly Transform[][] BonesTransform;

    public Sequence(CAnimSequence sequence, Skeleton skeleton, bool rotationOnly)
    {
        Frame = 0;
        ElapsedTime = 0.0f;
        Name = sequence.Name;
        MaxFrame = sequence.NumFrames - 1;
        FramesPerSecond = sequence.Rate;
        StartPos = sequence.StartPos;
        AnimStartTime = sequence.AnimStartTime;
        AnimEndTime = sequence.AnimEndTime;
        LoopingCount = sequence.LoopingCount;

        BonesTransform = new Transform[skeleton.BonesTransformByIndex.Count][];
        for (int trackIndex = 0; trackIndex < skeleton.UnrealSkeleton.ReferenceSkeleton.FinalRefBoneInfo.Length; trackIndex++)
        {
            var bone = skeleton.UnrealSkeleton.ReferenceSkeleton.FinalRefBoneInfo[trackIndex];
            if (!skeleton.BonesIndicesByLoweredName.TryGetValue(bone.Name.Text.ToLower(), out var boneIndices))
                continue;

            var originalTransform = skeleton.BonesTransformByIndex[boneIndices.Index];

            BonesTransform[boneIndices.Index] = new Transform[sequence.NumFrames];
            for (int frame = 0; frame < BonesTransform[boneIndices.Index].Length; frame++)
            {
                var boneOrientation = originalTransform.Rotation;
                var bonePosition = originalTransform.Position;
                var boneScale = originalTransform.Scale;

                sequence.Tracks[trackIndex].GetBonePosition(frame, sequence.NumFrames, false, ref bonePosition, ref boneOrientation);
                if (frame < sequence.Tracks[trackIndex].KeyScale.Length)
                    boneScale = sequence.Tracks[trackIndex].KeyScale[frame];

                // revert FixRotationKeys
                if (trackIndex > 0) boneOrientation.Conjugate();

                bonePosition *= Constants.SCALE_DOWN_RATIO;

                // switch (boneModes[trackIndex])
                // {
                //     case EBoneRetargetingMode.Animation:
                //     case EBoneRetargetingMode.Mesh:
                //     case EBoneRetargetingMode.AnimationScaled:
                //     case EBoneRetargetingMode.AnimationRelative:
                //     case EBoneRetargetingMode.OrientAndScale:
                //     case EBoneRetargetingMode.Count:
                //     default:
                //         break;
                // }

                BonesTransform[boneIndices.Index][frame] = new Transform
                {
                    Relation = boneIndices.ParentIndex >= 0 ? BonesTransform[boneIndices.ParentIndex][frame].Matrix : originalTransform.Relation,
                    Rotation = boneOrientation,
                    Position = rotationOnly ? originalTransform.Position : bonePosition,
                    Scale = boneScale
                };
            }
        }
    }

    public void Update(float deltaSeconds)
    {
        ElapsedTime += deltaSeconds / TimePerFrame;
        Frame = Math.Min(ElapsedTime.FloorToInt(), MaxFrame);
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
