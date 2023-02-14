using System;
using System.Numerics;
using CUE4Parse_Conversion.Animations;
using CUE4Parse.Utils;
using ImGuiNET;

namespace FModel.Views.Snooper.Models.Animations;

public enum AnimSeparatorType
{
    Start,
    Frame,
    InBetween,
    End
}

public class Animation : IDisposable
{
    public float ElapsedTime;                       // Animation Elapsed Time
    public int FrameInSequence;                     // Current Sequence's Frame to Display

    public bool IsPaused;
    public readonly float EndTime;                  // Animation End Time
    public readonly float TotalElapsedTime;         // Animation Max Time

    public int CurrentSequence;
    public readonly Sequence[] Sequences;
    public int SequencesCount => Sequences.Length;

    public Animation()
    {
        Reset();

        IsPaused = false;
        EndTime = 0.0f;
        TotalElapsedTime = 0.0f;
        Sequences = Array.Empty<Sequence>();
    }

    public Animation(Skeleton skeleton, CAnimSet anim, bool rotationOnly) : this()
    {
        Sequences = new Sequence[anim.Sequences.Count];
        for (int i = 0; i < Sequences.Length; i++)
        {
            Sequences[i] = new Sequence(anim.Sequences[i], skeleton, rotationOnly);

            TotalElapsedTime += anim.Sequences[i].NumFrames * Sequences[i].TimePerFrame;
            EndTime = Sequences[i].EndTime;
        }

        if (Sequences.Length > 0)
            ElapsedTime = Sequences[0].StartTime;
    }

    public void Update(float deltaSeconds)
    {
        if (IsPaused) return;

        ElapsedTime += deltaSeconds;
        TimeCalculation();
    }

    public Matrix4x4 InterpolateBoneTransform(int boneIndex)
    {
        // interpolate here
        return Sequences[CurrentSequence].BonesTransform[boneIndex][FrameInSequence].Matrix;
    }

    private void TimeCalculation()
    {
        for (int i = 0; i < Sequences.Length; i++)
        {
            if (ElapsedTime < Sequences[i].EndTime && ElapsedTime >= Sequences[i].StartTime)
            {
                CurrentSequence = i;
                break;
            }
        }
        if (ElapsedTime >= TotalElapsedTime) Reset();

        var lastEndTime = 0.0f;
        for (int s = 0; s < CurrentSequence; s++)
            lastEndTime = Sequences[s].EndTime;

        FrameInSequence = Math.Min(((ElapsedTime - lastEndTime) / Sequences[CurrentSequence].TimePerFrame).FloorToInt(), Sequences[CurrentSequence].UsableEndFrame);
    }

    private void Reset()
    {
        ElapsedTime = 0.0f;
        FrameInSequence = 0;
        CurrentSequence = 0;
    }

    private float _timeHeight = 10.0f;
    private float _timeBarHeight => _timeHeight * 2.0f;
    public void ImGuiTimeline(ImFontPtr fontPtr)
    {
        var io = ImGui.GetIO();
        var canvasP0 = ImGui.GetCursorScreenPos();
        var canvasSize = ImGui.GetContentRegionAvail();
        var canvasP1 = new Vector2(canvasP0.X + canvasSize.X, canvasP0.Y + canvasSize.Y);
        var timeRatio = canvasSize / TotalElapsedTime;

        var drawList = ImGui.GetWindowDrawList();

        ImGui.InvisibleButton("timeline_canvas", canvasP1 with { Y = _timeBarHeight }, ImGuiButtonFlags.MouseButtonLeft);
        IsPaused = ImGui.IsItemActive();
        if (IsPaused && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            var mousePosCanvas = io.MousePos - canvasP0;
            ElapsedTime = Math.Clamp(mousePosCanvas.X / canvasSize.X * TotalElapsedTime, 0, TotalElapsedTime);
            TimeCalculation();
        }

        drawList.AddRectFilled(canvasP0, canvasP1 with { Y = canvasP0.Y + _timeBarHeight }, 0xFF181818);
        drawList.PushClipRect(canvasP0, canvasP1 with { Y = canvasP0.Y + _timeBarHeight }, true);
        {
            for (float x = 0; x < canvasSize.X; x += timeRatio.X * TotalElapsedTime / canvasSize.X * 50.0f)
            {
                drawList.AddLine(new Vector2(canvasP0.X + x, canvasP0.Y + _timeHeight + 2.5f), canvasP1 with { X = canvasP0.X + x }, 0xA0FFFFFF);
                drawList.AddText(fontPtr, 14, new Vector2(canvasP0.X + x + 4, canvasP0.Y + 7.5f), 0x50FFFFFF, $"{x / timeRatio.X:F1}s");
            }
        }
        drawList.PopClipRect();

        for (int i = 0; i < Sequences.Length; i++)
        {
            Sequences[i].DrawSequence(drawList, canvasP0.X, canvasP0.Y + _timeBarHeight, timeRatio, i, i == CurrentSequence ? 0xFF0000FF : 0xFF175F17);
        }

        DrawSeparator(drawList, canvasP0, canvasP1, ElapsedTime * timeRatio.X, AnimSeparatorType.Frame);
        DrawSeparator(drawList, canvasP0, canvasP1, EndTime * timeRatio.X, AnimSeparatorType.End);
    }

    private void DrawSeparator(ImDrawListPtr drawList, Vector2 origin, Vector2 destination, float time, AnimSeparatorType separatorType)
    {
        const int size = 5;

        Vector2 p1 = separatorType switch
        {
            AnimSeparatorType.Frame => new Vector2(origin.X + time, origin.Y + _timeBarHeight),
            AnimSeparatorType.End => origin with { X = origin.X + time },
            _ => throw new ArgumentOutOfRangeException(nameof(separatorType), separatorType, null)
        };
        var p2 = new Vector2(p1.X, destination.Y);

        uint color = separatorType switch
        {
            AnimSeparatorType.Frame => 0xFF6F6F6F,
            AnimSeparatorType.End => 0xFF2E3E82,
            _ => throw new ArgumentOutOfRangeException(nameof(separatorType), separatorType, null)
        };

        drawList.AddLine(p1, p2, color, 1f);
        switch (separatorType)
        {
            case AnimSeparatorType.Frame:
                color = 0xFF30478C;
                var xl = p1.X - size;
                var xr = p1.X + size;
                var yb = origin.Y + _timeBarHeight - _timeHeight / 2.0f;

                drawList.AddQuadFilled(origin with { X = xl }, origin with { X = xr }, new Vector2(xr, yb), new Vector2(xl, yb), color);
                drawList.AddTriangleFilled(new Vector2(xl, yb), new Vector2(xr, yb), p1, color);
                break;
            case AnimSeparatorType.End:
                drawList.AddTriangleFilled(p1, p1 with { X = p1.X - size }, p1 with { Y = p1.Y + size }, color);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(separatorType), separatorType, null);
        }
    }

    public void Dispose()
    {

    }
}
