using System;
using System.Numerics;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Objects.Core.Math;

namespace FModel.Views.Snooper.Models;

public class Socket : IDisposable
{
    public readonly string Name;
    public readonly string Bone;
    public readonly Transform Transform;

    public Socket(USkeletalMeshSocket socket, Transform transform)
    {
        Name = socket.SocketName.Text;
        Bone = socket.BoneName.Text;
        Transform = transform;
        // Transform.Relation = transform.Matrix;
        // Transform.Position = socket.RelativeRotation.RotateVector(socket.RelativeLocation.ToMapVector()) * Constants.SCALE_DOWN_RATIO;
        // Transform.Scale = socket.RelativeScale.ToMapVector();
    }

    public Socket(USkeletalMeshSocket socket, Vector3 position)
    {
        Name = socket.SocketName.Text;
        Bone = socket.BoneName.Text;
        Transform = Transform.Identity;
        var pos = position /*+ socket.RelativeRotation.RotateVector(socket.RelativeLocation)*/;
        Transform.Position = new FVector(pos.X, pos.Z, pos.Y) * Constants.SCALE_DOWN_RATIO;
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
