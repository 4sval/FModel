using System;
using System.Numerics;
using CUE4Parse_Conversion.Animations;
using ImGuiNET;

namespace FModel.Views.Snooper.Models.Animations;

public class Animation : IDisposable
{
    public int CurrentSequence;
    public readonly Sequence[] Sequences;
    public int SequencesCount => Sequences.Length;

    public Animation()
    {
        Sequences = Array.Empty<Sequence>();
    }

    public Animation(Skeleton skeleton, CAnimSet anim, bool rotationOnly) : this()
    {
        Sequences = new Sequence[anim.Sequences.Count];
        for (int i = 0; i < Sequences.Length; i++)
        {
            Sequences[i] = new Sequence(anim.Sequences[i], skeleton, rotationOnly);
        }
    }

    public void Update(float deltaSeconds)
    {
        Sequences[CurrentSequence].Update(deltaSeconds);
    }

    public Matrix4x4 InterpolateBoneTransform(int boneIndex)
    {
        // interpolate here
        return Sequences[CurrentSequence].BonesTransform[boneIndex][Sequences[CurrentSequence].Frame].Matrix;
    }

    public void CheckForNextSequence()
    {
        if (Sequences[CurrentSequence].ElapsedTime > Sequences[CurrentSequence].EndPos)
        {
            Sequences[CurrentSequence].ElapsedTime = 0;
            Sequences[CurrentSequence].Frame = 0;
            CurrentSequence++;
        }

        if (CurrentSequence >= SequencesCount)
        {
            CurrentSequence = 0;
        }
    }

    public void ImGuiTimeline()
    {
        var canvasP0 = ImGui.GetCursorScreenPos();
        var canvasSize = ImGui.GetContentRegionAvail();
        var canvasP1 = new Vector2(canvasP0.X + canvasSize.X, canvasP0.Y + canvasSize.Y);
        var ratio = canvasSize / Sequences[CurrentSequence].MaxFrame;

        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(canvasP0, canvasP1, 0xFF242424);

        var l1 = new Vector2(canvasP0.X + Sequences[CurrentSequence].Frame * ratio.X, canvasP0.Y);
        var l2 = new Vector2(l1.X, canvasP1.Y);
        drawList.AddLine(l1, l2, 0xFF0000FF, 2f);

        ImGui.Text($"{Sequences[CurrentSequence].Name} > {(CurrentSequence < SequencesCount - 1 ? Sequences[CurrentSequence + 1].Name : Sequences[0].Name)}");
        ImGui.Text($"Frame: {Sequences[CurrentSequence].Frame}/{Sequences[CurrentSequence].MaxFrame}");
        ImGui.Text($"FPS: {Sequences[CurrentSequence].FramesPerSecond}");
    }

    public void Dispose()
    {

    }
}
