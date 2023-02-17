using System;
using System.Collections.Generic;
using System.Numerics;
using CUE4Parse_Conversion.Animations;
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
    public readonly Dictionary<string, BoneIndice> BonesIndicesByLoweredName;
    public readonly Dictionary<int, Transform> BonesTransformByIndex;

    private int _previousAnimationSequence;
    private int _previousSequenceFrame;
    private Transform[][][] _animatedBonesTransform;        // [sequence][bone][frame]
    private readonly Matrix4x4[] _invertedBonesMatrix;
    public int BoneCount => _invertedBonesMatrix.Length;
    public bool IsAnimated => _animatedBonesTransform.Length > 0;

    public Skeleton()
    {
        BonesIndicesByLoweredName = new Dictionary<string, BoneIndice>();
        BonesTransformByIndex = new Dictionary<int, Transform>();
        _animatedBonesTransform = Array.Empty<Transform[][]>();
        _invertedBonesMatrix = Array.Empty<Matrix4x4>();
    }

    public Skeleton(FReferenceSkeleton referenceSkeleton) : this()
    {
        for (int boneIndex = 0; boneIndex < referenceSkeleton.FinalRefBoneInfo.Length; boneIndex++)
        {
            var info = referenceSkeleton.FinalRefBoneInfo[boneIndex];

            var boneIndices = new BoneIndice { BoneIndex = boneIndex, ParentBoneIndex = info.ParentIndex };
            if (!boneIndices.IsRoot)
                boneIndices.LoweredParentBoneName =
                    referenceSkeleton.FinalRefBoneInfo[boneIndices.ParentBoneIndex].Name.Text.ToLower();

            BonesIndicesByLoweredName[info.Name.Text.ToLower()] = boneIndices;
        }

        _invertedBonesMatrix = new Matrix4x4[BonesIndicesByLoweredName.Count];
        foreach (var boneIndices in BonesIndicesByLoweredName.Values)
        {
            var bone = referenceSkeleton.FinalRefBonePose[boneIndices.BoneIndex];
            if (!BonesTransformByIndex.TryGetValue(boneIndices.BoneIndex, out var boneTransform))
            {
                boneTransform = new Transform
                {
                    Rotation = bone.Rotation,
                    Position = bone.Translation * Constants.SCALE_DOWN_RATIO,
                    Scale = bone.Scale3D
                };
            }

            if (!BonesTransformByIndex.TryGetValue(boneIndices.ParentBoneIndex, out var parentTransform))
                parentTransform = new Transform { Relation = Matrix4x4.Identity };

            boneTransform.Relation = parentTransform.Matrix;
            Matrix4x4.Invert(boneTransform.Matrix, out var inverted);


            BonesTransformByIndex[boneIndices.BoneIndex] = boneTransform;
            _invertedBonesMatrix[boneIndices.BoneIndex] = inverted;
        }
    }

    public void Animate(CAnimSet anim, bool rotationOnly)
    {
        TrackSkeleton(anim);

        _animatedBonesTransform = new Transform[anim.Sequences.Count][][];
        for (int s = 0; s < _animatedBonesTransform.Length; s++)
        {
            var sequence = anim.Sequences[s];
            _animatedBonesTransform[s] = new Transform[BoneCount][];
            foreach (var boneIndices in BonesIndicesByLoweredName.Values)
            {
                var originalTransform = BonesTransformByIndex[boneIndices.BoneIndex];
                _animatedBonesTransform[s][boneIndices.BoneIndex] = new Transform[sequence.NumFrames];

                if (!boneIndices.HasTrack)
                {
                    for (int frame = 0; frame < _animatedBonesTransform[s][boneIndices.BoneIndex].Length; frame++)
                    {
                        _animatedBonesTransform[s][boneIndices.BoneIndex][frame] = new Transform
                        {
                            Relation = originalTransform.LocalMatrix * _animatedBonesTransform[s][boneIndices.ParentTrackIndex][frame].Matrix
                        };
                    }
                }
                else
                {
                    var trackIndex = boneIndices.TrackIndex;
                    for (int frame = 0; frame < _animatedBonesTransform[s][boneIndices.BoneIndex].Length; frame++)
                    {
                        var boneOrientation = originalTransform.Rotation;
                        var bonePosition = originalTransform.Position;
                        var boneScale = originalTransform.Scale;

                        sequence.Tracks[trackIndex].GetBonePosition(frame, sequence.NumFrames, false, ref bonePosition, ref boneOrientation);
                        if (frame < sequence.Tracks[trackIndex].KeyScale.Length)
                            boneScale = sequence.Tracks[trackIndex].KeyScale[frame];

                        switch (anim.BoneModes[trackIndex])
                        {
                            case EBoneTranslationRetargetingMode.Skeleton when !rotationOnly:
                            {
                                var targetTransform = sequence.RetargetBasePose?[trackIndex] ?? anim.BonePositions[trackIndex];
                                bonePosition = targetTransform.Translation;
                                break;
                            }
                            case EBoneTranslationRetargetingMode.AnimationScaled when !rotationOnly:
                            {
                                var sourceTranslationLength = (originalTransform.Position / Constants.SCALE_DOWN_RATIO).Size();
                                if (sourceTranslationLength > UnrealMath.KindaSmallNumber)
                                {
                                    var targetTranslationLength = sequence.RetargetBasePose?[trackIndex].Translation.Size() ?? anim.BonePositions[trackIndex].Translation.Size();
                                    bonePosition.Scale(targetTranslationLength / sourceTranslationLength);
                                }
                                break;
                            }
                            case EBoneTranslationRetargetingMode.AnimationRelative when !rotationOnly:
                            {
                                // https://github.com/EpicGames/UnrealEngine/blob/cdaec5b33ea5d332e51eee4e4866495c90442122/Engine/Source/Runtime/Engine/Private/Animation/AnimationRuntime.cpp#L2586
                                var refPoseTransform  = sequence.RetargetBasePose?[trackIndex] ?? anim.BonePositions[trackIndex];
                                break;
                            }
                            case EBoneTranslationRetargetingMode.OrientAndScale when !rotationOnly:
                            {
                                var sourceSkelTrans = originalTransform.Position / Constants.SCALE_DOWN_RATIO;
                                var targetSkelTrans = sequence.RetargetBasePose?[trackIndex].Translation ?? anim.BonePositions[trackIndex].Translation;

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

                        // revert FixRotationKeys
                        if (trackIndex > 0) boneOrientation.Conjugate();
                        bonePosition *= Constants.SCALE_DOWN_RATIO;

                        _animatedBonesTransform[s][boneIndices.BoneIndex][frame] = new Transform
                        {
                            Relation = boneIndices.HasParentTrack ? _animatedBonesTransform[s][boneIndices.ParentTrackIndex][frame].Matrix : originalTransform.Relation,
                            Rotation = boneOrientation,
                            Position = rotationOnly ? originalTransform.Position : bonePosition,
                            Scale = boneScale
                        };
                    }
                }
            }
        }
    }

    private void TrackSkeleton(CAnimSet anim)
    {
        ResetAnimatedData();

        // tracked bones
        for (int trackIndex = 0; trackIndex < anim.TrackBonesInfo.Length; trackIndex++)
        {
            var info = anim.TrackBonesInfo[trackIndex];
            if (!BonesIndicesByLoweredName.TryGetValue(info.Name.Text.ToLower(), out var boneIndices))
                continue;

            boneIndices.TrackIndex = trackIndex;
            var parentTrackIndex = info.ParentIndex;
            if (parentTrackIndex < 0) continue;

            do
            {
                info = anim.TrackBonesInfo[parentTrackIndex];
                if (BonesIndicesByLoweredName.TryGetValue(info.Name.Text.ToLower(), out var parentBoneIndices) && parentBoneIndices.HasTrack)
                    boneIndices.ParentTrackIndex = parentBoneIndices.BoneIndex;
                else parentTrackIndex = info.ParentIndex;
            } while (!boneIndices.HasParentTrack);
        }

        // fix parent of untracked bones
        foreach ((var boneName, var boneIndices) in BonesIndicesByLoweredName)
        {
            if (boneIndices.IsRoot || boneIndices.HasTrack && boneIndices.HasParentTrack) // assuming root bone always has a track
                continue;

#if DEBUG
            Log.Warning($"{Name} Bone Mismatch: {boneName} ({boneIndices.BoneIndex}) was not present in the anim's target skeleton");
#endif

            var loweredParentBoneName = boneIndices.LoweredParentBoneName;
            do
            {
                var parentBoneIndices = BonesIndicesByLoweredName[loweredParentBoneName];
                if (parentBoneIndices.HasTrack) boneIndices.ParentTrackIndex = parentBoneIndices.BoneIndex;
                else loweredParentBoneName = parentBoneIndices.LoweredParentBoneName;
            } while (!boneIndices.HasParentTrack);
        }
    }

    public void ResetAnimatedData(bool full = false)
    {
        foreach (var boneIndices in BonesIndicesByLoweredName.Values)
        {
            boneIndices.TrackIndex = -1;
            boneIndices.ParentTrackIndex = -1;
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
        for (int boneIndex = 0; boneIndex < BoneCount; boneIndex++)
            _ssbo.Update(boneIndex, _invertedBonesMatrix[boneIndex] * _animatedBonesTransform[_previousAnimationSequence][boneIndex][_previousSequenceFrame].Matrix);
        _ssbo.Unbind();
    }

    public Matrix4x4 GetBoneMatrix(BoneIndice boneIndices)
    {
        return IsAnimated
            ? _animatedBonesTransform[_previousAnimationSequence][boneIndices.BoneIndex][_previousSequenceFrame].Matrix
            : BonesTransformByIndex[boneIndices.BoneIndex].Matrix;
    }

    public void Render()
    {
        _ssbo.BindBufferBase(1);
    }

    public void Dispose()
    {
        BonesIndicesByLoweredName.Clear();
        BonesTransformByIndex.Clear();

        _ssbo?.Dispose();
        GL.DeleteProgram(_handle);
    }
}
