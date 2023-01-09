using System;
using System.Collections.Generic;
using System.Numerics;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace FModel.Views.Snooper.Models;

public class Socket : IDisposable
{
    public readonly string Name;
    public readonly string Bone;
    public readonly Transform Transform;

    public readonly List<FGuid> AttachedModels;

    private Socket()
    {
        Bone = "None";
        Transform = Transform.Identity;
        AttachedModels = new List<FGuid>();
    }

    public Socket(UStaticMeshSocket socket) : this()
    {
        Name = socket.SocketName.Text;
        Transform.Rotation = socket.RelativeRotation.Quaternion();
        Transform.Position = socket.RelativeLocation * Constants.SCALE_DOWN_RATIO;
        Transform.Scale = socket.RelativeScale;
    }

    public Socket(USkeletalMeshSocket socket) : this()
    {
        Name = socket.SocketName.Text;
        Bone = socket.BoneName.Text;
        Transform.Rotation = socket.RelativeRotation.Quaternion();
        Transform.Position = socket.RelativeLocation * Constants.SCALE_DOWN_RATIO;
        Transform.Scale = socket.RelativeScale;
    }

    public Socket(USkeletalMeshSocket socket, Transform transform) : this()
    {
        Name = socket.SocketName.Text;
        Bone = socket.BoneName.Text;
        Transform.Relation = transform.Matrix;
        Transform.Rotation = socket.RelativeRotation.Quaternion();
        Transform.Position = socket.RelativeLocation * Constants.SCALE_DOWN_RATIO;
        Transform.Scale = socket.RelativeScale;
    }

    public void UpdateSocketMatrix(Matrix4x4 delta)
    {
        // TODO: support for rotation and scale
        Transform.Relation.Translation += delta.Translation;
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
