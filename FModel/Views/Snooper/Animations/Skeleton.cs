using System;
using System.Collections.Generic;
using System.Numerics;
using CUE4Parse_Conversion.Animations.PSA;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.Core.Math;
using FModel.Views.Snooper.Buffers;
using OpenTK.Graphics.OpenGL4;
using Serilog;

namespace FModel.Views.Snooper.Animations;

public class Skeleton : IDisposable
{
    private int _handle;
    private BufferObject<Matrix4x4> _ssbo;

    public string Name;
    public readonly Dictionary<string, Bone> BonesByLoweredName;

    private int _previousAnimationSequence;
    private int _previousSequenceFrame;
    private Transform[][][] _animatedBonesTransform;        // [sequence][bone][frame]
    private readonly Matrix4x4[] _invertedBonesMatrix;
    public int BoneCount => _invertedBonesMatrix.Length;
    public bool IsAnimated => _animatedBonesTransform.Length > 0;

    public Skeleton()
    {
        BonesByLoweredName = new Dictionary<string, Bone>();
        _animatedBonesTransform = Array.Empty<Transform[][]>();
        _invertedBonesMatrix = Array.Empty<Matrix4x4>();
    }

    public Skeleton(FReferenceSkeleton referenceSkeleton) : this()
    {
        _invertedBonesMatrix = new Matrix4x4[referenceSkeleton.FinalRefBoneInfo.Length];
        for (int boneIndex = 0; boneIndex < _invertedBonesMatrix.Length; boneIndex++)
        {
            var info = referenceSkeleton.FinalRefBoneInfo[boneIndex];
            var boneTransform = new Transform
            {
                Rotation = referenceSkeleton.FinalRefBonePose[boneIndex].Rotation,
                Position = referenceSkeleton.FinalRefBonePose[boneIndex].Translation * Constants.SCALE_DOWN_RATIO,
                Scale = referenceSkeleton.FinalRefBonePose[boneIndex].Scale3D
            };

            var bone = new Bone(boneIndex, info.ParentIndex, boneTransform);
            if (!bone.IsRoot)
            {
                bone.LoweredParentName =
                    referenceSkeleton.FinalRefBoneInfo[bone.ParentIndex].Name.Text.ToLower();
                bone.Rest.Relation = BonesByLoweredName[bone.LoweredParentName].Rest.Matrix;
            }

            BonesByLoweredName[info.Name.Text.ToLower()] = bone;

            Matrix4x4.Invert(boneTransform.Matrix, out var inverted);
            _invertedBonesMatrix[bone.Index] = inverted;
        }
    }

    public void Animate(CAnimSet anim, bool rotationOnly)
    {
        MapSkeleton(anim);

        _animatedBonesTransform = new Transform[anim.Sequences.Count][][];
        for (int s = 0; s < _animatedBonesTransform.Length; s++)
        {
            var sequence = anim.Sequences[s];
            _animatedBonesTransform[s] = new Transform[BoneCount][];
            foreach (var bone in BonesByLoweredName.Values)
            {
                _animatedBonesTransform[s][bone.Index] = new Transform[sequence.NumFrames];

                var skeletonBoneIndex = bone.SkeletonIndex;
                if (sequence.OriginalSequence.FindTrackForBoneIndex(skeletonBoneIndex) < 0)
                {
                    bone.IsAnimated |= false;
                    for (int frame = 0; frame < _animatedBonesTransform[s][bone.Index].Length; frame++)
                    {
                        _animatedBonesTransform[s][bone.Index][frame] = new Transform
                        {
                            Relation = bone.IsRoot ? bone.Rest.Relation :
                                bone.Rest.LocalMatrix * _animatedBonesTransform[s][bone.ParentIndex][frame].Matrix
                        };
                    }
                }
                else
                {
                    bone.IsAnimated |= true;
                    for (int frame = 0; frame < _animatedBonesTransform[s][bone.Index].Length; frame++)
                    {
                        var boneOrientation = bone.Rest.Rotation;
                        var bonePosition = bone.Rest.Position;
                        var boneScale = bone.Rest.Scale;

                        sequence.Tracks[skeletonBoneIndex].GetBoneTransform(frame, sequence.NumFrames, ref boneOrientation, ref bonePosition, ref boneScale);

                        switch (anim.Skeleton.BoneTree[skeletonBoneIndex])
                        {
                            case EBoneTranslationRetargetingMode.Skeleton when !rotationOnly:
                            {
                                var targetTransform = sequence.RetargetBasePose?[skeletonBoneIndex] ?? anim.Skeleton.ReferenceSkeleton.FinalRefBonePose[skeletonBoneIndex];
                                bonePosition = targetTransform.Translation;
                                break;
                            }
                            case EBoneTranslationRetargetingMode.AnimationScaled when !rotationOnly:
                            {
                                var sourceTranslationLength = (bone.Rest.Position / Constants.SCALE_DOWN_RATIO).Size();
                                if (sourceTranslationLength > UnrealMath.KindaSmallNumber)
                                {
                                    var targetTranslationLength = sequence.RetargetBasePose?[skeletonBoneIndex].Translation.Size() ?? anim.Skeleton.ReferenceSkeleton.FinalRefBonePose[skeletonBoneIndex].Translation.Size();
                                    bonePosition.Scale(targetTranslationLength / sourceTranslationLength);
                                }
                                break;
                            }
                            case EBoneTranslationRetargetingMode.AnimationRelative when !rotationOnly:
                            {
                                // can't tell if it's working or not
                                var sourceSkelTrans = bone.Rest.Position / Constants.SCALE_DOWN_RATIO;
                                var refPoseTransform  = sequence.RetargetBasePose?[skeletonBoneIndex] ?? anim.Skeleton.ReferenceSkeleton.FinalRefBonePose[skeletonBoneIndex];

                                boneOrientation = boneOrientation * FQuat.Conjugate(bone.Rest.Rotation) * refPoseTransform.Rotation;
                                bonePosition += refPoseTransform.Translation - sourceSkelTrans;
                                boneScale *= refPoseTransform.Scale3D * bone.Rest.Scale;
                                boneOrientation.Normalize();
                                break;
                            }
                            case EBoneTranslationRetargetingMode.OrientAndScale when !rotationOnly:
                            {
                                var sourceSkelTrans = bone.Rest.Position / Constants.SCALE_DOWN_RATIO;
                                var targetSkelTrans = sequence.RetargetBasePose?[skeletonBoneIndex].Translation ?? anim.Skeleton.ReferenceSkeleton.FinalRefBonePose[skeletonBoneIndex].Translation;

                                if (!sourceSkelTrans.Equals(targetSkelTrans))
                                {
                                    var sourceSkelTransLength = sourceSkelTrans.Size();
                                    var targetSkelTransLength = targetSkelTrans.Size();
                                    if (!UnrealMath.IsNearlyZero(sourceSkelTransLength * targetSkelTransLength))
                                    {
                                        var sourceSkelTransDir = sourceSkelTrans / sourceSkelTransLength;
                                        var targetSkelTransDir = targetSkelTrans / targetSkelTransLength;

                                        var deltaRotation = FQuat.FindBetweenNormals(sourceSkelTransDir, targetSkelTransDir);
                                        var scale = targetSkelTransLength / sourceSkelTransLength;
                                        bonePosition = deltaRotation.RotateVector(bonePosition) * scale;
                                    }
                                }
                                break;
                            }
                        }

                        _animatedBonesTransform[s][bone.Index][frame] = new Transform
                        {
                            Relation = bone.IsRoot ? bone.Rest.Relation : _animatedBonesTransform[s][bone.ParentIndex][frame].Matrix,
                            Rotation = boneOrientation,
                            Position = rotationOnly ? bone.Rest.Position : bonePosition * Constants.SCALE_DOWN_RATIO,
                            Scale = boneScale
                        };
                    }
                }
            }
        }
    }

    private void MapSkeleton(CAnimSet anim)
    {
        ResetAnimatedData();

        // map bones
        for (int boneIndex = 0; boneIndex < anim.Skeleton.BoneCount; boneIndex++)
        {
            var info = anim.Skeleton.ReferenceSkeleton.FinalRefBoneInfo[boneIndex];
            if (!BonesByLoweredName.TryGetValue(info.Name.Text.ToLower(), out var bone))
                continue;

            bone.SkeletonIndex = boneIndex;
            bone.IsAnimated = false;
        }

#if DEBUG
        foreach ((var boneName, var bone) in BonesByLoweredName)
        {
            if (bone.IsRoot || bone.IsMapped) // assuming root bone always is mapped
                continue;

            Log.Warning($"{Name} Bone Mismatch: {boneName} ({bone.Index}) was not present in the anim's target skeleton");
        }
#endif
    }

    public void ResetAnimatedData(bool full = false)
    {
        foreach (var bone in BonesByLoweredName.Values)
        {
            bone.SkeletonIndex = -1;
            bone.IsAnimated = false;
        }

        if (!full) return;
        _animatedBonesTransform = Array.Empty<Transform[][]>();
        _ssbo.UpdateRange(BoneCount, Matrix4x4.Identity);
    }

    public void Setup()
    {
        _handle = GL.CreateProgram();

        _ssbo = new BufferObject<Matrix4x4>(BoneCount, BufferTarget.ShaderStorageBuffer);
        _ssbo.UpdateRange(BoneCount, Matrix4x4.Identity);
    }

    public void UpdateAnimationMatrices(int currentSequence, int frameInSequence)
    {
        if (!IsAnimated) return;

        _previousAnimationSequence = currentSequence;
        if (_previousSequenceFrame == frameInSequence) return;
        _previousSequenceFrame = frameInSequence;

        _ssbo.Bind();
        for (int boneIndex = 0; boneIndex < BoneCount; boneIndex++) // interpolate here
            _ssbo.Update(boneIndex, _invertedBonesMatrix[boneIndex] * _animatedBonesTransform[_previousAnimationSequence][boneIndex][_previousSequenceFrame].Matrix);
        _ssbo.Unbind();
    }

    public Matrix4x4 GetBoneMatrix(Bone bone)
    {
        return IsAnimated
            ? _animatedBonesTransform[_previousAnimationSequence][bone.Index][_previousSequenceFrame].Matrix
            : bone.Rest.Matrix;
    }

    public void Render()
    {
        _ssbo.BindBufferBase(1);
    }

    public void Dispose()
    {
        BonesByLoweredName.Clear();

        _ssbo?.Dispose();
        GL.DeleteProgram(_handle);
    }
}
