using System;
using System.Collections.Generic;
using System.Numerics;
using CUE4Parse_Conversion.Animations;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.Utils;
using ImGuiNET;

namespace FModel.Views.Snooper.Animations;

public class Animation : IDisposable
{
    public readonly string Name;
    public readonly Sequence[] Sequences;
    public readonly float StartTime;                // Animation Start Time
    public readonly float EndTime;                  // Animation End Time
    public readonly float TotalElapsedTime;         // Animation Max Time
    public readonly string TargetSkeleton;

    public int CurrentSequence;
    public int FrameInSequence;                     // Current Sequence's Frame to Display

    public bool IsActive;
    public bool IsSelected;

    public readonly List<FGuid> AttachedModels;

    public Animation()
    {
        Sequences = Array.Empty<Sequence>();
        AttachedModels = new List<FGuid>();
    }

    public Animation(string name, CAnimSet animSet) : this()
    {
        Name = name;
        TargetSkeleton = animSet.OriginalAnim.Name;

        Sequences = new Sequence[animSet.Sequences.Count];
        for (int i = 0; i < Sequences.Length; i++)
        {
            Sequences[i] = new Sequence(animSet.Sequences[i]);

            EndTime = Sequences[i].EndTime;
            TotalElapsedTime += animSet.Sequences[i].NumFrames * Sequences[i].TimePerFrame;
        }

        if (Sequences.Length > 0)
            StartTime = Sequences[0].StartTime;
    }

    public Animation(string name, CAnimSet animSet, params FGuid[] animatedModels) : this(name, animSet)
    {
        AttachedModels.AddRange(animatedModels);
    }

    public void TimeCalculation(float elapsedTime)
    {
        for (int i = 0; i < Sequences.Length; i++)
        {
            if (elapsedTime < Sequences[i].EndTime && elapsedTime >= Sequences[i].StartTime)
            {
                CurrentSequence = i;
                break;
            }
        }
        if (elapsedTime >= TotalElapsedTime) Reset();

        var lastEndTime = 0.0f;
        for (int s = 0; s < CurrentSequence; s++)
            lastEndTime = Sequences[s].EndTime;

        FrameInSequence = Math.Min(((elapsedTime - lastEndTime) / Sequences[CurrentSequence].TimePerFrame).FloorToInt(), Sequences[CurrentSequence].EndFrame);
    }

    private void Reset()
    {
        FrameInSequence = 0;
        CurrentSequence = 0;
    }

    public void Dispose()
    {
        AttachedModels.Clear();
    }

    public void ImGuiAnimation(ImDrawListPtr drawList, Vector2 timelineP0, Vector2 treeP0, Vector2 treeP1, Vector2 timeStep, Vector2 timeRatio, float y, float t)
    {
        var p1 = new Vector2(timelineP0.X + StartTime * timeRatio.X + t, y + t);
        var p2 = new Vector2(timelineP0.X + EndTime * timeRatio.X - t, y + timeStep.Y - t);

        ImGui.SetCursorScreenPos(p1);
        ImGui.InvisibleButton($"timeline_sequencetracker_{Name}", new Vector2(EndTime * timeRatio.X - t, timeStep.Y - t), ImGuiButtonFlags.MouseButtonLeft);
        IsActive = ImGui.IsItemActive();

        drawList.AddRectFilled(p1, p2, IsSelected ? 0xFF48B048 : 0xFF175F17, 5.0f, ImDrawFlags.RoundCornersTop);
        drawList.PushClipRect(p1, p2, true);
        drawList.AddText(p1, 0xFF000000, $"{TargetSkeleton} - {CurrentSequence + 1}/{Sequences.Length} - {FrameInSequence}/{Sequences[CurrentSequence].EndFrame}");
        drawList.PopClipRect();

        drawList.PushClipRect(treeP0 with { Y = p1.Y }, treeP1 with { Y = p2.Y }, true);
        drawList.AddText(treeP0 with { Y = y + timeStep.Y / 4.0f }, 0xFFFFFFFF, Name);
        drawList.PopClipRect();
    }
}
