using System;
using System.Numerics;
using CUE4Parse_Conversion.Animations;
using CUE4Parse.Utils;
using ImGuiNET;

namespace FModel.Views.Snooper.Models.Animations;

public class Sequence : IDisposable
{
    public readonly string Name;
    public readonly int MaxFrame;
    public readonly float TimePerFrame;
    public readonly float StartTime;
    public readonly float Duration;
    public readonly float EndTime;
    public readonly int EndFrame;
    public readonly int LoopingCount;

    public int UsableEndFrame => EndFrame - 1;

    public readonly Transform[][] BonesTransform;

    public Sequence(CAnimSequence sequence, Skeleton skeleton, bool rotationOnly)
    {
        Name = sequence.Name;
        MaxFrame = sequence.NumFrames - 1;
        TimePerFrame = 1.0f / sequence.Rate;
        StartTime = sequence.StartPos;
        Duration = sequence.AnimEndTime;
        EndTime = StartTime + Duration;
        EndFrame = (Duration / TimePerFrame).FloorToInt();
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


    private readonly float _height = 20.0f;
    public void DrawSequence(ImDrawListPtr drawList, float x, float y, Vector2 ratio, int index, uint col)
    {
        var height = _height * (index % 2);
        var p1 = new Vector2(x + StartTime * ratio.X, y + height);
        var p2 = new Vector2(x + EndTime * ratio.X, y + height + _height);
        drawList.PushClipRect(p1, p2, true);
        drawList.AddRectFilled(p1, p2, col);
        drawList.AddText(p1 with { X = p1.X + 2.5f }, 0xFF000000, Name);
        drawList.PopClipRect();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
