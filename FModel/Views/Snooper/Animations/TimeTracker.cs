using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace FModel.Views.Snooper.Animations;

public enum ETrackerType
{
    Start,
    Frame,
    InBetween,
    End
}

public class TimeTracker : IDisposable
{
    public bool IsPaused;
    public float ElapsedTime;
    public float MaxElapsedTime { get; private set; }

    private float _timeHeight = 10.0f;
    private float _timeBarHeight => _timeHeight * 2.0f;

    public TimeTracker()
    {
        Reset();
        SetMaxElapsedTime(0.01f);
    }

    public void Update(float deltaSeconds)
    {
        if (IsPaused) return;
        ElapsedTime += deltaSeconds;
        if (ElapsedTime >= MaxElapsedTime) Reset();
    }

    public void SetMaxElapsedTime(float maxElapsedTime)
    {
        MaxElapsedTime = MathF.Max(maxElapsedTime, MaxElapsedTime);
    }

    public void Reset()
    {
        IsPaused = false;
        ElapsedTime = 0.0f;
    }

    public void Dispose()
    {
        Reset();
    }

    public void ImGuiTimeline(ImFontPtr fontPtr, List<Animation> animations)
    {
        var io = ImGui.GetIO();
        var canvasP0 = ImGui.GetCursorScreenPos();
        var canvasSize = ImGui.GetContentRegionAvail();
        var canvasP1 = new Vector2(canvasP0.X + canvasSize.X, canvasP0.Y + canvasSize.Y);
        var timeRatio = canvasSize / MaxElapsedTime;

        var drawList = ImGui.GetWindowDrawList();

        ImGui.InvisibleButton("timeline_canvas", canvasP1 with { Y = _timeBarHeight }, ImGuiButtonFlags.MouseButtonLeft);
        IsPaused = ImGui.IsItemActive();
        if (IsPaused && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            var mousePosCanvas = io.MousePos - canvasP0;
            ElapsedTime = Math.Clamp(mousePosCanvas.X / canvasSize.X * MaxElapsedTime, 0.01f, MaxElapsedTime);
            foreach (var animation in animations)
            {
                animation.TimeCalculation(ElapsedTime);
            }
        }

        drawList.AddRectFilled(canvasP0, canvasP1 with { Y = canvasP0.Y + _timeBarHeight }, 0xFF181818);
        drawList.PushClipRect(canvasP0, canvasP1 with { Y = canvasP0.Y + _timeBarHeight }, true);
        {
            for (float x = 0; x < canvasSize.X; x += timeRatio.X * MaxElapsedTime / canvasSize.X * 50.0f)
            {
                drawList.AddLine(new Vector2(canvasP0.X + x, canvasP0.Y + _timeHeight + 2.5f), canvasP1 with { X = canvasP0.X + x }, 0xA0FFFFFF);
                drawList.AddText(fontPtr, 14, new Vector2(canvasP0.X + x + 4, canvasP0.Y + 7.5f), 0x50FFFFFF, $"{x / timeRatio.X:F1}s");
            }
        }
        drawList.PopClipRect();

        // for (int i = 0; i < Sequences.Length; i++)
        // {
        //     Sequences[i].DrawSequence(drawList, canvasP0.X, canvasP0.Y + _timeBarHeight, timeRatio, i, i == CurrentSequence ? 0xFF0000FF : 0xFF175F17);
        // }

        DrawSeparator(drawList, canvasP0, canvasP1, ElapsedTime * timeRatio.X, ETrackerType.Frame);
        // DrawSeparator(drawList, canvasP0, canvasP1, EndTime * timeRatio.X, ETrackerType.End);
    }

    private void DrawSeparator(ImDrawListPtr drawList, Vector2 origin, Vector2 destination, float time, ETrackerType separatorType)
    {
        const int size = 5;

        Vector2 p1 = separatorType switch
        {
            ETrackerType.Frame => new Vector2(origin.X + time, origin.Y + _timeBarHeight),
            ETrackerType.End => origin with { X = origin.X + time },
            _ => throw new ArgumentOutOfRangeException(nameof(separatorType), separatorType, null)
        };
        var p2 = new Vector2(p1.X, destination.Y);

        uint color = separatorType switch
        {
            ETrackerType.Frame => 0xFF6F6F6F,
            ETrackerType.End => 0xFF2E3E82,
            _ => throw new ArgumentOutOfRangeException(nameof(separatorType), separatorType, null)
        };

        drawList.AddLine(p1, p2, color, 1f);
        switch (separatorType)
        {
            case ETrackerType.Frame:
                color = 0xFF30478C;
                var xl = p1.X - size;
                var xr = p1.X + size;
                var yb = origin.Y + _timeBarHeight - _timeHeight / 2.0f;

                drawList.AddQuadFilled(origin with { X = xl }, origin with { X = xr }, new Vector2(xr, yb), new Vector2(xl, yb), color);
                drawList.AddTriangleFilled(new Vector2(xl, yb), new Vector2(xr, yb), p1, color);
                break;
            case ETrackerType.End:
                drawList.AddTriangleFilled(p1, p1 with { X = p1.X - size }, p1 with { Y = p1.Y + size }, color);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(separatorType), separatorType, null);
        }
    }
}
