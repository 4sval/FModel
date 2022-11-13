using System;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Objects.UObject;

namespace FModel.Views.Snooper;

public class Skeleton : IDisposable
{
    public readonly USkeleton RefSkel;
    public readonly bool IsLoaded;
    public readonly Socket[] Sockets;

    public Animation Anim;

    public Skeleton(FPackageIndex package)
    {
        RefSkel = package.Load<USkeleton>();
        if (RefSkel == null) return;

        IsLoaded = true;
        Sockets = new Socket[RefSkel.Sockets.Length];
        for (int i = 0; i < Sockets.Length; i++)
        {
            if (RefSkel.Sockets[i].Load<USkeletalMeshSocket>() is not { } socket ||
                !RefSkel.ReferenceSkeleton.FinalNameToIndexMap.TryGetValue(socket.BoneName.Text, out var boneIndex))
                continue;

            var t = RefSkel.ReferenceSkeleton.FinalRefBonePose[boneIndex];
            var transform = Transform.Identity;
            transform.Position = t.Translation.ToMapVector() * Constants.SCALE_DOWN_RATIO;
            transform.Rotation = t.Rotator();
            transform.Scale = t.Scale3D.ToMapVector();

            Sockets[i] = new Socket(socket, transform);
        }
    }

    public void SetUniform(Shader shader)
    {
        if (!IsLoaded) return;
        for (var i = 0; i < Anim?.FinalBonesMatrix.Length; i++)
        {
            shader.SetUniform($"uFinalBonesMatrix[{i}]", Anim.FinalBonesMatrix[i]);
        }
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
