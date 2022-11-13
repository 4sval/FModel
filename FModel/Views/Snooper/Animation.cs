using System;
using System.Numerics;
using CUE4Parse_Conversion.Animations;
using CUE4Parse.UE4.Objects.Core.Math;

namespace FModel.Views.Snooper;

public class Animation : IDisposable
{
    public float CurrentTime;
    public float DeltaTime;
    public CAnimSet CurrentAnimation;
    public Matrix4x4[] FinalBonesMatrix;

    public Animation(CAnimSet anim)
    {
        CurrentTime = 0f;
        CurrentAnimation = anim;

        FinalBonesMatrix = new Matrix4x4[anim.TrackBoneNames.Length];
        for (int i = 0; i < FinalBonesMatrix.Length; i++)
        {
            FinalBonesMatrix[i] = Matrix4x4.Identity;
        }
    }

    public void UpdateAnimation(float deltaTime)
    {
        DeltaTime = deltaTime;
        if (CurrentAnimation != null)
        {
            CurrentTime = deltaTime;
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

            boneOrientation *= CurrentAnimation.BonePositions[boneIndex].Orientation;
            bonePosition = boneOrientation.RotateVector(bonePosition);
            bonePosition *= Constants.SCALE_DOWN_RATIO;
            if (CurrentAnimation.TrackBoneNames[boneIndex].Text == "pelvis")
            {

            }

            FinalBonesMatrix[boneIndex] =
                Matrix4x4.CreateFromQuaternion(boneOrientation) *
                Matrix4x4.CreateTranslation(bonePosition.ToMapVector());
        }
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
