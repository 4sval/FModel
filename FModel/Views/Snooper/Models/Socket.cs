using System;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

namespace FModel.Views.Snooper.Models;

public class Socket : IDisposable
{
    public readonly string Name;
    public readonly string Bone;
    public readonly Transform Transform;

    public Socket(USkeletalMeshSocket socket)
    {
        Name = socket.SocketName.Text;
        Bone = socket.BoneName.Text;
        Transform = Transform.Identity;
        Transform.Rotation = socket.RelativeRotation.Quaternion();
        Transform.Position = socket.RelativeLocation * Constants.SCALE_DOWN_RATIO;
        Transform.Scale = socket.RelativeScale;
    }

    public Socket(USkeletalMeshSocket socket, Transform transform)
    {
        Name = socket.SocketName.Text;
        Bone = socket.BoneName.Text;
        Transform = transform;
        Transform.Relation = transform.Matrix;
        Transform.Rotation = socket.RelativeRotation.Quaternion();
        Transform.Position = socket.RelativeLocation * Constants.SCALE_DOWN_RATIO;
        Transform.Scale = socket.RelativeScale;
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
