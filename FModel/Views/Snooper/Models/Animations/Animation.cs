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
        CurrentTime = 1;
        CurrentAnimation = anim;
    }

    public Dictionary<int, Transform> CalculateBoneTransform(FMeshBoneInfo[] boneInfos, Dictionary<int, Transform> bonesTransformByIndex)
    {
        var ret = new Dictionary<int, Transform>();
        var sequence = CurrentAnimation.Sequences[0];
        for (int boneIndex = 0; boneIndex < boneInfos.Length; boneIndex++)
        {
            var boneOrientation = FQuat.Identity;
            var bonePosition = FVector.ZeroVector;
            var boneScale = FVector.OneVector;
            sequence.Tracks[boneIndex].GetBonePosition(CurrentTime, sequence.NumFrames, false, ref bonePosition, ref boneOrientation);
            if (CurrentTime < sequence.Tracks[boneIndex].KeyScale.Length)
                boneScale = sequence.Tracks[boneIndex].KeyScale[CurrentTime];

            if (!bonesTransformByIndex.TryGetValue(boneIndex, out var originalTransform))
                originalTransform = new Transform { Relation = Matrix4x4.Identity };

            if (!ret.TryGetValue(boneInfos[boneIndex].ParentIndex, out var parentTransform))
                parentTransform = new Transform { Relation = Matrix4x4.Identity };
            else boneOrientation.Conjugate();

            var boneTransform = new Transform
            {
                Relation = parentTransform.Matrix,
                Rotation = boneOrientation * originalTransform.Rotation * FQuat.Conjugate(boneOrientation),
                Position = bonePosition * Constants.SCALE_DOWN_RATIO,
                Scale = boneScale
            };

            // boneTransform.Rotation = originalTransform.Rotation * FQuat.Conjugate(originalTransform.Rotation) * parentTransform.Rotation
            boneTransform.Position -= boneTransform.Position - originalTransform.Position;

            // boneTransform.Rotation = originalTransform.Rotation * (boneTransform.Rotation * FQuat.Conjugate(originalTransform.Rotation));
            // boneTransform.Position = originalTransform.Position + (boneTransform.Position - originalTransform.Position);

            ret[boneIndex] = boneTransform;
        }

        return ret;
    }

    public void Dispose()
    {

    }
}
