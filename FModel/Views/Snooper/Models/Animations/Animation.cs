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
        BoneTransforms = new Transform[skeleton.BonesTransformByIndex.Count][];
        for (var boneIndex = 0; boneIndex < BoneTransforms.Length; boneIndex++)
        {
            var parentIndex = skeleton.ReferenceSkeleton.FinalRefBoneInfo[boneIndex].ParentIndex;
            if (!skeleton.BonesTransformByIndex.TryGetValue(boneIndex, out var originalTransform))
                throw new ArgumentNullException("no transform for bone " + boneIndex);

            var boneOrientation = originalTransform.Rotation;
            var bonePosition = originalTransform.Position;
            var boneScale = originalTransform.Scale;

            BoneTransforms[boneIndex] = new Transform[sequence.NumFrames];
            for (var frame = 0; frame < BoneTransforms[boneIndex].Length; frame++)
            {
                sequence.Tracks[boneIndex].GetBonePosition(frame, sequence.NumFrames, false, ref bonePosition, ref boneOrientation);
                if (CurrentTime < sequence.Tracks[boneIndex].KeyScale.Length)
                    boneScale = sequence.Tracks[boneIndex].KeyScale[CurrentTime];

                boneOrientation.W *= -1;
                bonePosition *= Constants.SCALE_DOWN_RATIO;

                BoneTransforms[boneIndex][frame] = new Transform
                {
                    Relation = parentIndex >= 0 ? BoneTransforms[parentIndex][frame].Matrix : originalTransform.Relation,
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
