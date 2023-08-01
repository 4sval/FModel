using System;
using System.Collections.Generic;
using System.Numerics;
using CUE4Parse_Conversion.Animations.PSA;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.Utils;
using ImGuiNET;

namespace FModel.Views.Snooper.Animations;

public class Animation : IDisposable
{
    private readonly UObject _export;
    private readonly CAnimSet _animSet;

    public readonly string Path;
    public readonly string Name;
    public readonly Sequence[] Sequences;
    public readonly float StartTime;                // Animation Start Time
    public readonly float EndTime;                  // Animation End Time
    public readonly float TotalElapsedTime;         // Animation Max Time
    public readonly string TargetSkeleton;

    public int CurrentSequence;
    public int FrameInSequence;                     // Current Sequence's Frame to Display
    public int NextFrameInSequence;
    public float LerpAmount;

    public string Label =>
        $"Retarget: {TargetSkeleton}\nSequences: {CurrentSequence + 1}/{Sequences.Length}\nFrames: {FrameInSequence}/{Sequences[CurrentSequence].EndFrame}";
    public bool IsActive;
    public bool IsSelected;

    public readonly List<FGuid> AttachedModels;

    public Animation(UObject export)
    {
        _export = export;
        Path = _export.GetPathName();
        Name = _export.Name;
        Sequences = Array.Empty<Sequence>();
        AttachedModels = new List<FGuid>();
    }

    public Animation(UObject export, CAnimSet animSet) : this(export)
    {
        _animSet = animSet;
        TargetSkeleton = _animSet.Skeleton.Name;

        Sequences = new Sequence[_animSet.Sequences.Count];
        for (int i = 0; i < Sequences.Length; i++)
        {
            Sequences[i] = new Sequence(_animSet.Sequences[i]);

            EndTime = Sequences[i].EndTime;
            TotalElapsedTime += _animSet.Sequences[i].NumFrames * Sequences[i].TimePerFrame;
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

        var exactFrameAtThisTime = (elapsedTime - lastEndTime) / Sequences[CurrentSequence].TimePerFrame;
        FrameInSequence = Math.Min(exactFrameAtThisTime.FloorToInt(), Sequences[CurrentSequence].EndFrame);
        NextFrameInSequence = Math.Min(FrameInSequence + 1, Sequences[CurrentSequence].EndFrame);
        LerpAmount = Math.Clamp(exactFrameAtThisTime - FrameInSequence, 0, 1);
    }

    private void Reset()
    {
        FrameInSequence = 0;
        NextFrameInSequence = 0;
        LerpAmount = 0.0f;
        CurrentSequence = 0;
    }

    public void Dispose()
    {
        AttachedModels.Clear();
    }

    public void ImGuiAnimation(Snooper s, Save saver, ImDrawListPtr drawList, ImFontPtr fontPtr, Vector2 timelineP0, Vector2 treeP0, Vector2 timeStep, Vector2 timeRatio, float y, float t, int i)
    {
        var name = $"{Name}##{i}";
        var p1 = new Vector2(timelineP0.X + StartTime * timeRatio.X + t, y + t);
        var p2 = new Vector2(timelineP0.X + EndTime * timeRatio.X - t, y + timeStep.Y - t);

        ImGui.SetCursorScreenPos(p1);
        ImGui.InvisibleButton($"timeline_sequencetracker_{name}", new Vector2(EndTime * timeRatio.X - t, timeStep.Y - t), ImGuiButtonFlags.MouseButtonLeft);
        IsActive = ImGui.IsItemActive();
        IsSelected = s.Renderer.Options.SelectedAnimation == i;
        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
        {
            s.Renderer.Options.SelectAnimation(i);
        }
        Popup(s, saver, i);

        drawList.AddRectFilled(p1, p2, IsSelected ? 0xFF48B048 : 0xFF175F17, 5.0f, ImDrawFlags.RoundCornersTop);
        for (int j = 0; j < Sequences.Length; j++)
        {
            Sequences[j].DrawSequence(drawList, fontPtr, timelineP0.X, p2, timeStep, timeRatio, t, IsSelected);
        }

        ImGui.SetCursorScreenPos(treeP0 with { Y = p1.Y });
        if (ImGui.Selectable(name, s.Renderer.Options.SelectedAnimation == i, ImGuiSelectableFlags.SpanAllColumns, new Vector2(p1.X - treeP0.X, timeStep.Y - t - t)))
        {
            s.Renderer.Options.SelectAnimation(i);
        }
        Popup(s, saver, i);
    }

    private void Popup(Snooper s, Save saver, int i)
    {
        SnimGui.Popup(() =>
        {
            s.Renderer.Options.SelectAnimation(i);
            if (ImGui.BeginMenu("Animate"))
            {
                foreach ((var guid, var model) in s.Renderer.Options.Models)
                {
                    var selected = AttachedModels.Contains(guid);
                    if (ImGui.MenuItem(model.Name, null, selected, (model.HasSkeleton && !model.Skeleton.IsAnimated) || selected))
                    {
                        if (selected) AttachedModels.Remove(guid); else AttachedModels.Add(guid);
                        model.Skeleton.ResetAnimatedData(true);
                        if (!selected) model.Skeleton.Animate(_animSet, s.Renderer.AnimateWithRotationOnly);
                    }
                }
                ImGui.EndMenu();
            }
            if (ImGui.MenuItem("Save"))
            {
                s.WindowShouldFreeze(true);
                saver.Value = s.Renderer.Options.TrySave(_export, out saver.Label, out saver.Path);
                s.WindowShouldFreeze(false);
            }
            ImGui.Separator();
            if (ImGui.MenuItem("Copy Path to Clipboard")) ImGui.SetClipboardText(Path);
        });
    }
}
