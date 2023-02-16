using System;
using System.Linq;
using System.Numerics;
using CUE4Parse_Conversion.Animations;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.Utils;
using ImGuiNET;
using Serilog;

namespace FModel.Views.Snooper.Models.Animations;

public class Sequence : IDisposable
{
    public readonly string Name;
    public readonly float TimePerFrame;
    public readonly float StartTime;
    public readonly float Duration;
    public readonly float EndTime;
    public readonly int EndFrame;
    public readonly int LoopingCount;

    public readonly Transform[][] BonesTransform;

    private Sequence(CAnimSequence sequence)
    {
        Name = sequence.Name;
        TimePerFrame = 1.0f / sequence.Rate;
        StartTime = sequence.StartPos;
        Duration = sequence.AnimEndTime;
        EndTime = StartTime + Duration;
        EndFrame = (Duration / TimePerFrame).FloorToInt() - 1;
        LoopingCount = sequence.LoopingCount;
    }

    public Sequence(Skeleton skeleton, CAnimSet anim, CAnimSequence sequence, bool rotationOnly) : this(sequence)
    {
        BonesTransform = new Transform[skeleton.BoneCount][];
        foreach (var boneIndices in skeleton.BonesIndicesByLoweredName.Values)
        {
            var originalTransform = skeleton.BonesTransformByIndex[boneIndices.BoneIndex];
            BonesTransform[boneIndices.BoneIndex] = new Transform[sequence.NumFrames];

            if (!boneIndices.HasTrack)
            {
                for (int frame = 0; frame < BonesTransform[boneIndices.BoneIndex].Length; frame++)
                {
                    BonesTransform[boneIndices.BoneIndex][frame] = new Transform
                    {
                        Relation = originalTransform.LocalMatrix * BonesTransform[boneIndices.ParentTrackIndex][frame].Matrix
                    };
                }
            }
            else
            {
                var trackIndex = boneIndices.TrackIndex;
                for (int frame = 0; frame < BonesTransform[boneIndices.BoneIndex].Length; frame++)
                {
                    var boneOrientation = originalTransform.Rotation;
                    var bonePosition = originalTransform.Position;
                    var boneScale = originalTransform.Scale;

                    sequence.Tracks[trackIndex].GetBonePosition(frame, sequence.NumFrames, false, ref bonePosition, ref boneOrientation);
                    if (frame < sequence.Tracks[trackIndex].KeyScale.Length)
                        boneScale = sequence.Tracks[trackIndex].KeyScale[frame];

                    switch (anim.BoneModes[trackIndex])
                    {
                        case EBoneTranslationRetargetingMode.Skeleton when !rotationOnly:
                        {
                            var targetTransform = sequence.RetargetBasePose?[trackIndex] ?? anim.BonePositions[trackIndex];
                            bonePosition = targetTransform.Translation;
                            break;
                        }
                        case EBoneTranslationRetargetingMode.AnimationScaled when !rotationOnly:
                        {
                            var sourceTranslationLength = (originalTransform.Position / Constants.SCALE_DOWN_RATIO).Size();
                            if (sourceTranslationLength > UnrealMath.KindaSmallNumber)
                            {
                                var targetTranslationLength = sequence.RetargetBasePose?[trackIndex].Translation.Size() ?? anim.BonePositions[trackIndex].Translation.Size();
                                bonePosition.Scale(targetTranslationLength / sourceTranslationLength);
                            }
                            break;
                        }
                        case EBoneTranslationRetargetingMode.AnimationRelative when !rotationOnly:
                        {
                            // https://github.com/EpicGames/UnrealEngine/blob/cdaec5b33ea5d332e51eee4e4866495c90442122/Engine/Source/Runtime/Engine/Private/Animation/AnimationRuntime.cpp#L2586
                            var refPoseTransform  = sequence.RetargetBasePose?[trackIndex] ?? anim.BonePositions[trackIndex];
                            break;
                        }
                        case EBoneTranslationRetargetingMode.OrientAndScale when !rotationOnly:
                        {
                            var sourceSkelTrans = originalTransform.Position / Constants.SCALE_DOWN_RATIO;
                            var targetSkelTrans = sequence.RetargetBasePose?[trackIndex].Translation ?? anim.BonePositions[trackIndex].Translation;

                            if (!sourceSkelTrans.Equals(targetSkelTrans))
                            {
                                var sourceSkelTransLength = sourceSkelTrans.Size();
                                var targetSkelTransLength = targetSkelTrans.Size();
                                if (!UnrealMath.IsNearlyZero(sourceSkelTransLength * targetSkelTransLength))
                                {
                                    var sourceSkelTransDir = sourceSkelTrans / sourceSkelTransLength;
                                    var targetSkelTransDir = targetSkelTrans / targetSkelTransLength;

                                    var deltaRotation = FQuat.FindBetweenNormals(sourceSkelTransDir, targetSkelTransDir);
                                    var scale = targetSkelTransLength / sourceSkelTransLength;
                                    bonePosition = deltaRotation.RotateVector(bonePosition) * scale;
                                }
                            }
                            break;
                        }
                    }

                    // revert FixRotationKeys
                    if (trackIndex > 0) boneOrientation.Conjugate();
                    bonePosition *= Constants.SCALE_DOWN_RATIO;

                    BonesTransform[boneIndices.BoneIndex][frame] = new Transform
                    {
                        Relation = boneIndices.HasParentTrack ? BonesTransform[boneIndices.ParentTrackIndex][frame].Matrix : originalTransform.Relation,
                        Rotation = boneOrientation,
                        Position = rotationOnly ? originalTransform.Position : bonePosition,
                        Scale = boneScale
                    };
                }
            }
        }
    }

    public void Dispose()
    {

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
}
