using System;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

namespace FModel.Views.Snooper;

public class Socket : IDisposable
{
    public readonly string Name;
    public readonly string Bone;
    public readonly Transform Transform;

    public Socket(USkeletalMeshSocket socket, Transform relation)
    {
        Name = socket.SocketName.Text;
        Bone = socket.BoneName.Text;
        Transform = Transform.Identity;
        Transform.Relation = relation.Matrix;
        Transform.Position = socket.RelativeLocation.ToMapVector() * Constants.SCALE_DOWN_RATIO;
        Transform.Rotation = socket.RelativeRotation;
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
