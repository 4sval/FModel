using System;
using System.Collections.Generic;
using System.Numerics;
using CUE4Parse_Conversion.Animations;
using CUE4Parse.UE4.Objects.Core.Math;

namespace FModel.Views.Snooper.Models.Animations;

public class Animation : IDisposable
{
    public float CurrentTime;
    public float DeltaTime;
    public CAnimSet CurrentAnimation;
    public Transform[] FinalBonesMatrix;

    public Animation(CAnimSet anim, Dictionary<string, int> nameToIndex, Dictionary<int, Transform> indexToTransform)
    {
        CurrentTime = 0f;
        CurrentAnimation = anim;

        FinalBonesMatrix = new Transform[anim.TrackBoneNames.Length];
        for (int i = 0; i < FinalBonesMatrix.Length; i++)
        {
            if (!nameToIndex.TryGetValue(anim.TrackBoneNames[i].Text, out var boneIndex) ||
                !indexToTransform.TryGetValue(boneIndex, out var boneTransform))
            {
                boneTransform = Transform.Identity;
            }

            FinalBonesMatrix[i] = Transform.Identity;
            FinalBonesMatrix[i].Relation = boneTransform.Matrix;
        }
    }

    public void UpdateAnimation(float deltaTime)
    {
        DeltaTime = deltaTime;
        if (CurrentAnimation != null)
        {
            // CurrentTime = deltaTime;
            CalculateBoneTransform();
        }
    }

    public void CalculateBoneTransform()
    {
        var sequence = CurrentAnimation.Sequences[0];
        for (int boneIndex = 0; boneIndex < FinalBonesMatrix.Length; boneIndex++)
        {
            var boneOrientation = FQuat.Identity;
            var bonePosition = FVector.ZeroVector;
            sequence.Tracks[boneIndex].GetBonePosition(CurrentTime, sequence.NumFrames, false, ref bonePosition, ref boneOrientation);

            FinalBonesMatrix[boneIndex].Rotation = boneOrientation;
            FinalBonesMatrix[boneIndex].Position = bonePosition * Constants.SCALE_DOWN_RATIO;
        }
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
