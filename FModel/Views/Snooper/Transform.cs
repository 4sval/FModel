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
    public FVector Position = FVector.ZeroVector;
    public FQuat Rotation = FQuat.Identity;
    public FVector Scale = FVector.OneVector;

    private Matrix4x4? _saved;
    public Matrix4x4 LocalMatrix => Matrix4x4.CreateScale(Scale.X, Scale.Z, Scale.Y) *
                                    Matrix4x4.CreateFromQuaternion(Quaternion.Normalize(new Quaternion(Rotation.X, Rotation.Z, Rotation.Y, -Rotation.W))) *
                                    Matrix4x4.CreateTranslation(Position.X, Position.Z, Position.Y);
    public Matrix4x4 Matrix => LocalMatrix * Relation;

    public void Save()
    {
        _saved = LocalMatrix;
    }

    public void ModifyLocal(Matrix4x4 matrix)
    {
        Matrix4x4.Decompose(matrix, out var scale, out var rotation, out var position);

        Scale.X = scale.X;
        Scale.Y = scale.Z;
        Scale.Z = scale.Y;
        Rotation.X = rotation.X;
        Rotation.Y = rotation.Z;
        Rotation.Z = rotation.Y;
        Rotation.W = -rotation.W;
        Position.X = position.X;
        Position.Z = position.Y;
        Position.Y = position.Z;
    }

    public void Reset()
    {
        if (!_saved.HasValue) return;
        ModifyLocal(_saved.Value);
    }

    public override string ToString() => Matrix.Translation.ToString();
}
