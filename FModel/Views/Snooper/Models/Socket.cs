using System;
using System.Collections.Generic;
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
