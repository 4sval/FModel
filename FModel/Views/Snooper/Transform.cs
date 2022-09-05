using System.Numerics;
using CUE4Parse.UE4.Objects.Core.Math;

namespace FModel.Views.Snooper;

public class Transform
{
    public static Transform Identity
    {
        get => new ();
    }

    public FVector Position = FVector.ZeroVector;
    public FRotator Rotation = FRotator.ZeroRotator;
    public FVector Scale = FVector.OneVector;

    public Matrix4x4 Matrix =>
        Matrix4x4.Identity *
        Matrix4x4.CreateFromYawPitchRoll(
            Helper.DegreesToRadians(Rotation.Yaw),
            Helper.DegreesToRadians(Rotation.Pitch),
            Helper.DegreesToRadians(Rotation.Roll)) *
        Matrix4x4.CreateScale(Scale) *
        Matrix4x4.CreateTranslation(Position);
}
