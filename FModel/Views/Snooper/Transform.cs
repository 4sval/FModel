using System.Numerics;

namespace FModel.Views.Snooper;

public class Transform
{
    public static Transform Identity
    {
        get => new ();
    }

    public Vector3 Position = Vector3.Zero;
    public Vector3 Rotation = Vector3.Zero;
    public Vector3 Scale = Vector3.One;

    public Matrix4x4 Matrix =>
        Matrix4x4.Identity *
        Matrix4x4.CreateFromYawPitchRoll(
            Helper.DegreesToRadians(Rotation.Y),
            Helper.DegreesToRadians(Rotation.X),
            Helper.DegreesToRadians(Rotation.Z)) *
        Matrix4x4.CreateScale(Scale) *
        Matrix4x4.CreateTranslation(Position);
}
