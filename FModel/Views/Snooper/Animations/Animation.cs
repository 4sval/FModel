using System;
using System.Collections.Generic;
using System.Numerics;
using CUE4Parse_Conversion.Animations;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.Utils;
using ImGuiNET;

namespace FModel.Views.Snooper.Animations;

public class Animation : IDisposable
{
    public readonly UObject Export;
    public readonly CAnimSet AnimSet;
    public readonly string Path;
    public readonly string Name;
    public readonly Sequence[] Sequences;
    public readonly float StartTime;                // Animation Start Time
    public readonly float EndTime;                  // Animation End Time
    public readonly float TotalElapsedTime;         // Animation Max Time
    public readonly string TargetSkeleton;

    public int CurrentSequence;
    public int FrameInSequence;                     // Current Sequence's Frame to Display

    public string Label =>
        $"Retarget: {TargetSkeleton}\nSequences: {CurrentSequence + 1}/{Sequences.Length}\nFrames: {FrameInSequence}/{Sequences[CurrentSequence].EndFrame}";
    public bool IsActive;
    public bool IsSelected;

    public readonly List<FGuid> AttachedModels;

    public Animation(UObject export)
    {
        Export = export;
        Path = Export.GetPathName();
        Name = Export.Name;
        Sequences = Array.Empty<Sequence>();
        AttachedModels = new List<FGuid>();
    }

    public Animation(UObject export, CAnimSet animSet) : this(export)
    {
        AnimSet = animSet;
        TargetSkeleton = AnimSet.OriginalAnim.Name;

        Sequences = new Sequence[AnimSet.Sequences.Count];
        for (int i = 0; i < Sequences.Length; i++)
        {
            Sequences[i] = new Sequence(AnimSet.Sequences[i]);

            EndTime = Sequences[i].EndTime;
            TotalElapsedTime += AnimSet.Sequences[i].NumFrames * Sequences[i].TimePerFrame;
        }

        if (Sequences.Length > 0)
            StartTime = Sequences[0].StartTime;
    }

    public Animation(UObject export, CAnimSet animSet, params FGuid[] animatedModels) : this(export, animSet)
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

    public void ImGuiAnimation(Snooper s, Save saver, ImDrawListPtr drawList, Vector2 timelineP0, Vector2 treeP0, Vector2 treeP1, Vector2 timeStep, Vector2 timeRatio, float y, float t, int i)
    {
        var p1 = new Vector2(timelineP0.X + StartTime * timeRatio.X + t, y + t);
        var p2 = new Vector2(timelineP0.X + EndTime * timeRatio.X - t, y + timeStep.Y - t);

        ImGui.SetCursorScreenPos(p1);
        ImGui.InvisibleButton($"timeline_sequencetracker_{Name}##{i}", new Vector2(EndTime * timeRatio.X - t, timeStep.Y - t), ImGuiButtonFlags.MouseButtonLeft);
        IsActive = ImGui.IsItemActive();
        IsSelected = s.Renderer.Options.SelectedAnimation == i;
        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
        {
            s.Renderer.Options.SelectedAnimation = i;
        }
        SnimGui.Popup(() =>
        {
            s.Renderer.Options.SelectedAnimation = i;
            if (ImGui.BeginMenu("Animate"))
            {
                foreach ((var guid, var model) in s.Renderer.Options.Models)
                {
                    if (ImGui.MenuItem(model.Name, model.HasSkeleton && !AttachedModels.Contains(guid)))
                    {
                        AttachedModels.Add(guid);
                        model.Skeleton.ResetAnimatedData(true);
                        model.Skeleton.Animate(AnimSet, s.Renderer.AnimateWithRotationOnly);
                    }
                }
                ImGui.EndMenu();
            }
            if (ImGui.Selectable("Save"))
            {
                s.WindowShouldFreeze(true);
                saver.Value = s.Renderer.Options.TrySave(Export, out saver.Label, out saver.Path);
                s.WindowShouldFreeze(false);
            }
            ImGui.Separator();
            if (ImGui.Selectable("Copy Path to Clipboard")) ImGui.SetClipboardText(Path);
        });

        drawList.AddRectFilled(p1, p2, IsSelected ? 0xFF48B048 : 0xFF175F17, 5.0f, ImDrawFlags.RoundCornersTop);
        for (int j = 0; j < Sequences.Length; j++)
        {
            Sequences[j].DrawSequence(drawList, timelineP0.X, p2, timeStep, timeRatio, t);
        }

        drawList.PushClipRect(treeP0 with { Y = p1.Y }, treeP1 with { Y = p2.Y }, true);
        drawList.AddText(treeP0 with { Y = y + timeStep.Y / 4.0f }, 0xFFFFFFFF, Name);
        drawList.PopClipRect();
    }
}
