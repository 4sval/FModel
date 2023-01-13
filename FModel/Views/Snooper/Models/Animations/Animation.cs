using System;
using System.Collections.Generic;
using System.Numerics;
using CUE4Parse_Conversion.Animations;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.Core.Math;

namespace FModel.Views.Snooper.Models.Animations;

public class Animation : IDisposable
{
    public int CurrentTime;
    public float DeltaTime;
    public CAnimSet CurrentAnimation;

    public Animation(CAnimSet anim)
    {
        CurrentTime = 0;
        CurrentAnimation = anim;
    }

    public void UpdateAnimation(FMeshBoneInfo[] boneInfos, ref Dictionary<int, Transform> bonesTransformByIndex)
    {
        if (CurrentAnimation != null)
        {
            // CurrentTime = deltaTime;
            CalculateBoneTransform(boneInfos, ref bonesTransformByIndex);
        }
    }

    public void CalculateBoneTransform(FMeshBoneInfo[] boneInfos, ref Dictionary<int, Transform> bonesTransformByIndex)
    {
        var sequence = CurrentAnimation.Sequences[0];
        for (int boneIndex = 0; boneIndex < boneInfos.Length; boneIndex++)
        {
            var boneOrientation = FQuat.Identity;
            var bonePosition = FVector.ZeroVector;
            var boneScale = FVector.OneVector;
            sequence.Tracks[boneIndex].GetBonePosition(CurrentTime, sequence.NumFrames, false, ref bonePosition, ref boneOrientation);
            if (CurrentTime < sequence.Tracks[boneIndex].KeyScale.Length)
                boneScale = sequence.Tracks[boneIndex].KeyScale[CurrentTime];

            if (!bonesTransformByIndex.TryGetValue(boneInfos[boneIndex].ParentIndex, out var parentTransform))
                parentTransform = new Transform { Relation = Matrix4x4.Identity };

            bonesTransformByIndex[boneIndex] = new Transform
            {
                Relation = parentTransform.Matrix,
                Rotation = boneOrientation,
                Position = bonePosition * Constants.SCALE_DOWN_RATIO,
                Scale = boneScale
            };
        }
    }

    public void Dispose()
    {

    }
}
