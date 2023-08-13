using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;

namespace FModel.Views.Snooper.Models;

public struct SocketAttachementInfo
{
    public FGuid Guid;
    public int Instance;
}

public class Socket : IDisposable
{
    public readonly string Name;
    public readonly FName BoneName;
    public readonly Transform Transform;
    public readonly bool IsVirtual;

    public readonly List<SocketAttachementInfo> AttachedModels;
    public bool IsDaron => AttachedModels.Count > 0;

    private Socket()
    {
        Transform = Transform.Identity;
        AttachedModels = new List<SocketAttachementInfo>();
    }

    public Socket(string name, FName boneName, Transform transform, bool isVirtual) : this()
    {
        Name = name;
        BoneName = boneName;
        Transform = transform;
        IsVirtual = isVirtual;
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
        BoneName = socket.BoneName;
        Transform.Rotation = socket.RelativeRotation.Quaternion();
        Transform.Position = socket.RelativeLocation * Constants.SCALE_DOWN_RATIO;
        Transform.Scale = socket.RelativeScale;
    }

    public void Dispose()
    {
        AttachedModels.Clear();
    }
}
