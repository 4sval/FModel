using System;
using System.Collections.Generic;
using System.Numerics;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Objects.Core.Math;
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

            var transform = Transform.Identity;
            var matrix = Matrix4x4.Identity;
            while (boneIndex > -1)
            {
                var bone = RefSkel.ReferenceSkeleton.FinalRefBonePose[boneIndex];
                boneIndex = RefSkel.ReferenceSkeleton.FinalRefBoneInfo[boneIndex].ParentIndex;
                var parentBone = RefSkel.ReferenceSkeleton.FinalRefBonePose[boneIndex < 0 ? 0 : boneIndex];

                var orig_loc = bone.Translation;
                parentBone.Rotation.Conjugate();
                orig_loc = parentBone.Rotation.RotateVector(orig_loc);

                var orig_quat = bone.Rotation;
                orig_quat *= parentBone.Rotation;
                orig_quat.Conjugate();

                var p_rotated = orig_quat * orig_loc;
                orig_quat.Conjugate();
                p_rotated *= orig_quat;

                matrix *=
                    Matrix4x4.CreateFromQuaternion(orig_quat) *
                    Matrix4x4.CreateTranslation(p_rotated);

                // Console.WriteLine(matrix.Translation);
            }
            // for (int j = 0; j <= boneIndex; j++)
            // {
            //     var t = RefSkel.ReferenceSkeleton.FinalRefBonePose[j];
            //     var r = RefSkel.ReferenceSkeleton.FinalRefBonePose[j - (j == 0 ? 0 : 1)].Rotation;
            //     r.Conjugate();
            //     matrix *= Matrix4x4.CreateFromQuaternion(r) * Matrix4x4.CreateTranslation(t.Translation);
            //
            //     Console.WriteLine($@"{t.Translation}");
            //     transform.Relation *= matrix;
            // }

            Sockets[i] = new Socket(socket, matrix.Translation);
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
