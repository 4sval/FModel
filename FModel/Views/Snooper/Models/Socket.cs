using System;
using System.Collections.Generic;
using System.Numerics;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;

namespace FModel.Views.Snooper.Models;

public class Socket : IDisposable
{
    public readonly string Name;
    public readonly FName BoneName;
    public readonly Transform Transform;

    public readonly List<FGuid> AttachedModels;

    private Socket()
    {
        Transform = Transform.Identity;
        AttachedModels = new List<FGuid>();
    }

    public Socket(UStaticMeshSocket socket, Transform transform) : this()
    {
        Name = socket.SocketName.Text;
        Transform.Relation = transform.Matrix;
        Transform.Rotation = socket.RelativeRotation.Quaternion();
        Transform.Position = socket.RelativeLocation * Constants.SCALE_DOWN_RATIO;
        Transform.Scale = socket.RelativeScale;
    }

    public Socket(USkeletalMeshSocket socket, Transform transform) : this()
    {
        Name = socket.SocketName.Text;
        BoneName = socket.BoneName;
        Transform.Relation = transform.Matrix;
        Transform.Rotation = socket.RelativeRotation.Quaternion();
        Transform.Position = socket.RelativeLocation * Constants.SCALE_DOWN_RATIO;
        Transform.Scale = socket.RelativeScale;
    }

    public void UpdateSocketMatrix(Matrix4x4 matrix)
    {
        Transform.Relation = matrix;
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
