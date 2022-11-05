using System.Numerics;
using CUE4Parse.UE4.Objects.Core.Math;

namespace FModel.Views.Snooper;

public class Transform
{
    public static Transform Identity
    {
        get => new ();
    }

    public Matrix4x4 Relation = Matrix4x4.Identity;
    public FVector Position = FVector.ZeroVector.ToMapVector();
    public FRotator Rotation = FRotator.ZeroRotator;
    public FVector Scale = FVector.OneVector.ToMapVector();

    public Matrix4x4 Matrix =>
        Matrix4x4.CreateScale(Scale) *
        Matrix4x4.CreateRotationX(Helper.DegreesToRadians(Rotation.Roll)) *
        Matrix4x4.CreateRotationY(Helper.DegreesToRadians(-Rotation.Yaw)) *
        Matrix4x4.CreateRotationZ(Helper.DegreesToRadians(Rotation.Pitch)) *
        Matrix4x4.CreateTranslation(Position) *
        Relation;
}
