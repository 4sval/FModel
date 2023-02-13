using System;
using System.Numerics;
using CUE4Parse_Conversion.Animations;
using CUE4Parse.Utils;
using ImGuiNET;

namespace FModel.Views.Snooper.Models.Animations;

public class Sequence : IDisposable
{
    public int Frame;
    public float ElapsedTime;
    public readonly string Name;
    public readonly int MaxFrame;
    public readonly float TimePerFrame;
    public readonly float StartTime;
    public readonly float Duration;
    public readonly float EndTime;
    public readonly int LoopingCount;

    public readonly Transform[][] BonesTransform;

    public bool IsComplete => ElapsedTime > Duration;
    // public bool IsComplete => Frame >= MaxFrame;

    public Sequence(CAnimSequence sequence, Skeleton skeleton, bool rotationOnly)
    {
        Reset();

        Name = sequence.Name;
        MaxFrame = sequence.NumFrames - 1;
        TimePerFrame = 1.0f / sequence.Rate;
        StartTime = sequence.StartPos / TimePerFrame;
        Duration = sequence.AnimEndTime / TimePerFrame;
        EndTime = StartTime + Duration;
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

    public float Update(float deltaSeconds)
    {
        var delta = deltaSeconds / TimePerFrame;
        ElapsedTime += delta;
        Frame = Math.Min(ElapsedTime.FloorToInt(), MaxFrame);
        return delta;
    }

    public void Reset()
    {
        ElapsedTime = 0.0f;
        Frame = 0;
    }


    private readonly float _height = 20.0f;
    public void DrawSequence(ImDrawListPtr drawList, Vector2 origin, Vector2 ratio, int index)
    {
        var height = _height * (index % 2) + _height;
        var p1 = new Vector2(origin.X + StartTime * ratio.X, origin.Y + height);
        var p2 = new Vector2(origin.X + EndTime * ratio.X, origin.Y + height + _height);
        drawList.AddRectFilled(p1, p2, 0xFF175F17);
        drawList.AddText(p1 with { X = p1.X + 2.5f }, 0xFF000000, Name);
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
