using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CUE4Parse_Conversion.Animations.PSA;
using CUE4Parse.UE4.Assets.Exports.Animation;
using FModel.Views.Snooper.Buffers;
using FModel.Views.Snooper.Shading;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using Serilog;

namespace FModel.Views.Snooper.Animations;

public class Skeleton : IDisposable
{
    private int _handle;
    private BufferObject<Matrix4x4> _rest;
    private BufferObject<Matrix4x4> _ssbo;
    private Matrix4x4[] _boneMatriceAtFrame;
    private readonly List<string> _breadcrumb;

    public string Name;
    public readonly Dictionary<string, Bone> BonesByLoweredName;

    public readonly int BoneCount;
    private int _additionalBoneCount;
    private int TotalBoneCount => BoneCount + _additionalBoneCount;

    public bool IsAnimated { get; private set; }
    public string SelectedBone;

    private const int _vertexSize = 12;
    private BufferObject<float> _vbo;
    private int _vaoHandle;

    public Skeleton()
    {
        BonesByLoweredName = new Dictionary<string, Bone>();
        _breadcrumb = new List<string>();
    }

    public Skeleton(FReferenceSkeleton referenceSkeleton) : this()
    {
        BoneCount = referenceSkeleton.FinalRefBoneInfo.Length;
        for (int boneIndex = 0; boneIndex < BoneCount; boneIndex++)
        {
            var info = referenceSkeleton.FinalRefBoneInfo[boneIndex];
            var boneName = info.Name.Text.ToLower();
            var bone = new Bone(boneIndex, info.ParentIndex, new Transform
            {
                Rotation = referenceSkeleton.FinalRefBonePose[boneIndex].Rotation,
                Position = referenceSkeleton.FinalRefBonePose[boneIndex].Translation * Constants.SCALE_DOWN_RATIO,
                Scale = referenceSkeleton.FinalRefBonePose[boneIndex].Scale3D
            });

            if (!bone.IsRoot)
            {
                bone.LoweredParentName = referenceSkeleton.FinalRefBoneInfo[bone.ParentIndex].Name.Text.ToLower();
                var parentBone = BonesByLoweredName[bone.LoweredParentName];

                bone.Rest.Relation = parentBone.Rest.Matrix;
                parentBone.LoweredChildNames.Add(boneName);
            }

            if (boneIndex == 0) SelectedBone = boneName;
            BonesByLoweredName[boneName] = bone;
        }
        _breadcrumb.Add(SelectedBone);
        _boneMatriceAtFrame = new Matrix4x4[BoneCount];
    }

    public void Merge(FReferenceSkeleton referenceSkeleton)
    {
        for (int boneIndex = 0; boneIndex < referenceSkeleton.FinalRefBoneInfo.Length; boneIndex++)
        {
            var info = referenceSkeleton.FinalRefBoneInfo[boneIndex];
            var boneName = info.Name.Text.ToLower();

            if (!BonesByLoweredName.TryGetValue(boneName, out var bone))
            {
                bone = new Bone(BoneCount + _additionalBoneCount, -1, new Transform
                {
                    Rotation = referenceSkeleton.FinalRefBonePose[boneIndex].Rotation,
                    Position = referenceSkeleton.FinalRefBonePose[boneIndex].Translation * Constants.SCALE_DOWN_RATIO,
                    Scale = referenceSkeleton.FinalRefBonePose[boneIndex].Scale3D
                }, true);

                if (!bone.IsRoot)
                {
                    bone.LoweredParentName = referenceSkeleton.FinalRefBoneInfo[info.ParentIndex].Name.Text.ToLower();
                    var parentBone = BonesByLoweredName[bone.LoweredParentName];

                    bone.ParentIndex = parentBone.Index;
                    bone.Rest.Relation = parentBone.Rest.Matrix;
                    parentBone.LoweredChildNames.Add(boneName);
                }

                BonesByLoweredName[boneName] = bone;
                _additionalBoneCount++;
            }
        }
        _boneMatriceAtFrame = new Matrix4x4[TotalBoneCount];
    }

    public void Setup()
    {
        _handle = GL.CreateProgram();

        _vaoHandle = GL.GenVertexArray();
        GL.BindVertexArray(_vaoHandle);


        _vbo = new BufferObject<float>(_vertexSize * BoneCount, BufferTarget.ArrayBuffer);

        var sf = sizeof(float);
        var half = _vertexSize / 2;
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, sf * half, sf * 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, sf * half, sf * 3);

        GL.BindVertexArray(0);

        _rest = new BufferObject<Matrix4x4>(BoneCount, BufferTarget.ShaderStorageBuffer);
        foreach (var bone in BonesByLoweredName.Values)
        {
            if (bone.IsVirtual) break;
            _rest.Update(bone.Index, bone.Rest.Matrix);
        }
        _rest.Unbind();

        _ssbo = new BufferObject<Matrix4x4>(TotalBoneCount, BufferTarget.ShaderStorageBuffer);
        _ssbo.UpdateRange(Matrix4x4.Identity);
    }

    public void Animate(CAnimSet animation)
    {
        IsAnimated = true;
        ResetAnimatedData();

        // map bones
        for (int boneIndex = 0; boneIndex < animation.Skeleton.BoneCount; boneIndex++)
        {
            var info = animation.Skeleton.ReferenceSkeleton.FinalRefBoneInfo[boneIndex];
            if (!BonesByLoweredName.TryGetValue(info.Name.Text.ToLower(), out var bone))
                continue;

            bone.SkeletonIndex = boneIndex;
        }

        // find playable sequences
        for (int s = 0; s < animation.Sequences.Count; s++)
        {
            var sequence = animation.Sequences[s];
            foreach (var bone in BonesByLoweredName.Values.Where(bone => sequence.OriginalSequence.FindTrackForBoneIndex(bone.SkeletonIndex) >= 0))
            {
                bone.AnimatedBySequences.Add(s);
            }
            sequence.RetargetTracks(animation.Skeleton);
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
            bone.AnimatedBySequences.Clear();
        }

        if (!full) return;
        IsAnimated = false;
        _ssbo.UpdateRange(Matrix4x4.Identity);
    }

    public void UpdateAnimationMatrices(Animation animation, bool rotationOnly)
    {
        if (!IsAnimated) return;

        _ssbo.Bind();
        foreach (var bone in BonesByLoweredName.Values)
        {
            var boneMatrix = bone.IsRoot ? bone.Rest.Relation : bone.Rest.LocalMatrix * _boneMatriceAtFrame[bone.ParentIndex];
            if (bone.IsAnimated)
            {
                var (s, f) = GetBoneFrameData(bone, animation);
                var sequence = animation.UnrealAnim.Sequences[s];
                var boneOrientation = bone.Rest.Rotation;
                var bonePosition = bone.Rest.Position;
                var boneScale = bone.Rest.Scale;

                sequence.Tracks[bone.SkeletonIndex].GetBoneTransform(f, sequence.NumFrames, ref boneOrientation, ref bonePosition, ref boneScale);
                if (!bone.IsRoot) boneMatrix = _boneMatriceAtFrame[bone.ParentIndex];
                bonePosition = rotationOnly ? bone.Rest.Position : bonePosition * Constants.SCALE_DOWN_RATIO;

                boneMatrix = new Transform
                {
                    Relation = boneMatrix,
                    Rotation = boneOrientation,
                    Position = bonePosition,
                    Scale = boneScale
                }.Matrix;
            }

            _ssbo.Update(bone.Index, boneMatrix);
            _boneMatriceAtFrame[bone.Index] = boneMatrix;
        }
        _ssbo.Unbind();
    }

    public void UpdateVertices()
    {
        _vbo.Bind();
        foreach (var (boneName, bone) in BonesByLoweredName)
        {
            Matrix4x4 boneMatrix;
            Matrix4x4 parentBoneMatrix;
            if (IsAnimated)
            {
                boneMatrix = _boneMatriceAtFrame[bone.Index];
                parentBoneMatrix = _boneMatriceAtFrame[bone.ParentIndex];
            }
            else
            {
                boneMatrix = bone.Rest.Matrix;
                parentBoneMatrix = bone.IsRoot ? boneMatrix : BonesByLoweredName[bone.LoweredParentName].Rest.Matrix;
            }

            var count = 0;
            var baseIndex = bone.Index * _vertexSize;
            _vbo.Update(baseIndex + count++, boneMatrix.Translation.X);
            _vbo.Update(baseIndex + count++, boneMatrix.Translation.Y);
            _vbo.Update(baseIndex + count++, boneMatrix.Translation.Z);
            _vbo.Update(baseIndex + count++, 1.0f);
            _vbo.Update(baseIndex + count++, boneName == SelectedBone ? 0.0f : 1.0f);
            _vbo.Update(baseIndex + count++, boneName == SelectedBone ? 0.0f : 1.0f);
            _vbo.Update(baseIndex + count++, parentBoneMatrix.Translation.X);
            _vbo.Update(baseIndex + count++, parentBoneMatrix.Translation.Y);
            _vbo.Update(baseIndex + count++, parentBoneMatrix.Translation.Z);
            _vbo.Update(baseIndex + count++, 1.0f);
            _vbo.Update(baseIndex + count++, bone.LoweredParentName == SelectedBone ? 0.0f : 1.0f);
            _vbo.Update(baseIndex + count++, bone.LoweredParentName == SelectedBone ? 0.0f : 1.0f);
        }
        _vbo.Unbind();
    }

    private (int, float) GetBoneFrameData(Bone bone, Animation animation)
    {
        int s = -1;
        float f = 0.0f;

        void Get(Bone b)
        {
            foreach (var i in b.AnimatedBySequences)
            {
                s = i;
                if (animation.Framing.TryGetValue(s, out f))
                    break;
            }
        }

        Get(bone);
        if (s == -1)
        {
            var parent = BonesByLoweredName[bone.LoweredParentName];
            while (!parent.IsAnimated)
            {
                parent = BonesByLoweredName[parent.LoweredParentName];
            }
            Get(parent);
        }

        return (s, f);
    }

    public Matrix4x4 GetBoneMatrix(Bone bone) => IsAnimated ? _boneMatriceAtFrame[bone.Index] : bone.Rest.Matrix;

    public void Render(Shader shader)
    {
        shader.SetUniform("uIsAnimated", IsAnimated);

        _ssbo.BindBufferBase(1);
        _rest.BindBufferBase(2);
    }

    public void RenderBones()
    {
        GL.Disable(EnableCap.DepthTest);

        GL.BindVertexArray(_vaoHandle);
        GL.DrawArrays(PrimitiveType.Lines, 0, _vbo.Size);
        GL.DrawArrays(PrimitiveType.Points, 0, _vbo.Size);
        GL.BindVertexArray(0);

        GL.Enable(EnableCap.DepthTest);
    }

    public void ImGuiBoneBreadcrumb()
    {
        var p1 = ImGui.GetCursorScreenPos();
        var canvasSize = ImGui.GetContentRegionAvail() with { Y = 20 };
        var p2 = p1 + canvasSize;
        ImGui.BeginChild("skeleton_breadcrumb", canvasSize);

        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(p1, p2, 0xFF242424);

        var x = p1.X;
        var y = p1.Y + (p2.Y - p1.Y) / 2;
        for (int i = Math.Min(_breadcrumb.Count - 1, 5); i >= 0; i--)
        {
            var boneName = _breadcrumb[i];
            var size = ImGui.CalcTextSize(boneName);
            var position = new Vector2(x + 5, y - size.Y / 2f);

            ImGui.SetCursorScreenPos(position);
            if (ImGui.InvisibleButton($"breakfast_{boneName}", size, ImGuiButtonFlags.MouseButtonLeft))
            {
                SelectedBone = boneName;
                _breadcrumb.RemoveRange(0, i);
                break;
            }

            drawList.AddText(position, i == 0 || ImGui.IsItemHovered() ? 0xFFFFFFFF : 0xA0FFFFFF, boneName);
            x += size.X + 7.5f;
            drawList.AddText(position with { X = x }, 0xA0FFFFFF, ">");
            x += 7.5f;
        }

        ImGui.EndChild();
    }

    public void ImGuiBoneHierarchy()
    {
        foreach (var name in BonesByLoweredName[SelectedBone].LoweredChildNames)
        {
            DrawBoneTree(name, BonesByLoweredName[name]);
        }
    }

    private void DrawBoneTree(string boneName, Bone bone)
    {
        ImGui.PushID(bone.Index);
        ImGui.TableNextRow();
        ImGui.TableNextColumn();

        var flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanFullWidth;
        // if (boneName == SelectedBone) flags |= ImGuiTreeNodeFlags.Selected;
        if (bone.IsVirtual) flags |= ImGuiTreeNodeFlags.Leaf;
        else if (!bone.IsDaron) flags |= ImGuiTreeNodeFlags.Bullet;

        ImGui.SetNextItemOpen(bone.LoweredChildNames.Count <= 1, ImGuiCond.Appearing);
        var open = ImGui.TreeNodeEx(boneName, flags);
        if (ImGui.IsItemClicked() && !ImGui.IsItemToggledOpen() && bone.IsDaron)
        {
            SelectedBone = boneName;
            _breadcrumb.Clear();
            do
            {
                _breadcrumb.Add(boneName);
                boneName = BonesByLoweredName[boneName].LoweredParentName;
            } while (boneName != null);
        }

        ImGui.TableNextColumn();
        ImGui.TextColored(ImGui.ColorConvertU32ToFloat4(bone.Color), bone.SkeletonIndex.ToString());

        if (open)
        {
            foreach (var name in bone.LoweredChildNames)
            {
                DrawBoneTree(name, BonesByLoweredName[name]);
            }
            ImGui.TreePop();
        }
        ImGui.PopID();
    }

    public void Dispose()
    {
        BonesByLoweredName.Clear();

        _rest?.Dispose();
        _ssbo?.Dispose();
        GL.DeleteProgram(_handle);
    }
}
