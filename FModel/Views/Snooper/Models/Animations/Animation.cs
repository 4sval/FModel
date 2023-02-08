using System;
using System.Numerics;
using CUE4Parse_Conversion.Animations;
using CUE4Parse.Utils;
using ImGuiNET;

namespace FModel.Views.Snooper.Models.Animations;

public class Animation : IDisposable
{
    public float ElapsedTime;

    public int CurrentSequence;
    public readonly Sequence[] Sequences;
    public int SequencesCount => Sequences.Length;

    public Animation()
    {
        ElapsedTime = 0;
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
        ElapsedTime += deltaSeconds / Sequences[CurrentSequence].TimePerFrame;
        Sequences[CurrentSequence].Frame = ElapsedTime.FloorToInt() % Sequences[CurrentSequence].MaxFrame;
    }

    public Matrix4x4 InterpolateBoneTransform(int trackIndex)
    {
        // interpolate here
        return Sequences[CurrentSequence].BonesTransform[trackIndex][Sequences[CurrentSequence].Frame].Matrix;
    }

    public void ImGuiTimeline()
    {
        ImGui.BeginDisabled(SequencesCount < 2);
        ImGui.DragInt("Sequence", ref CurrentSequence, 1, 0, Sequences.Length - 1, Sequences[CurrentSequence].Name);
        ImGui.EndDisabled();
        ImGui.Text($"Frame: {Sequences[CurrentSequence].Frame}/{Sequences[CurrentSequence].MaxFrame}");
        ImGui.Text($"FPS: {Sequences[CurrentSequence].FramesPerSecond}");
    }

    public void Dispose()
    {

    }
}
