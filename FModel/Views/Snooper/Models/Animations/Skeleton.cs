using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Views.Snooper.Shading;

namespace FModel.Views.Snooper.Models.Animations;

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
            if (RefSkel.Sockets[i].Load<USkeletalMeshSocket>() is not { } socket) continue;

            if (!RefSkel.ReferenceSkeleton.FinalNameToIndexMap.TryGetValue(socket.BoneName.Text, out var boneIndex))
            {
                Sockets[i] = new Socket(socket);
            }
            else
            {
                var transforms = new List<Transform>();
                while (boneIndex > -1)
                {
                    var bone = RefSkel.ReferenceSkeleton.FinalRefBonePose[boneIndex];
                    boneIndex = RefSkel.ReferenceSkeleton.FinalRefBoneInfo[boneIndex].ParentIndex;

                    transforms.Add(new Transform
                    {
                        Rotation = bone.Rotation,
                        Position = bone.Translation * Constants.SCALE_DOWN_RATIO,
                        Scale = bone.Scale3D
                    });
                }

                for (int j = transforms.Count - 2; j > -1; j--)
                {
                    transforms[j].Relation *= transforms[j + 1].Matrix;
                }

                Sockets[i] = new Socket(socket, transforms[0]);
            }
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
