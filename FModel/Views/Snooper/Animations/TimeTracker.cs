using System;
using System.Collections.Generic;
using System.Numerics;
using FModel.Views.Snooper.Shading;
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
    public bool IsActive;
    public float ElapsedTime;
    public float MaxElapsedTime;

    public TimeTracker()
    {
        Reset();
    }

    public void Update(float deltaSeconds)
    {
        if (IsPaused || IsActive) return;
        ElapsedTime += deltaSeconds;
        if (ElapsedTime >= MaxElapsedTime) Reset(false);
    }

    public void SafeSetElapsedTime(float elapsedTime)
    {
        ElapsedTime = Math.Clamp(elapsedTime, 0.0f, MaxElapsedTime);
    }

    public void SafeSetMaxElapsedTime(float maxElapsedTime)
    {
        MaxElapsedTime = MathF.Max(maxElapsedTime, MaxElapsedTime);
    }

    public void Reset(bool doMet = true)
    {
        IsPaused = false;
        ElapsedTime = 0.0f;
        if (doMet) MaxElapsedTime = 0.01f;
    }

    public void Dispose()
    {
        Reset();
    }

    private const float _margin = 5.0f;
    private const float _thickness = 2.0f;
    private const float _timeHeight = 10.0f;
    private float _timeBarHeight => _timeHeight * 2.0f;
    private readonly Vector2 _timeStep = new (50, 25);
    private readonly Vector2 _buttonSize = new (14.0f);
    private readonly string[] _icons = { "tl_forward", "tl_pause", "tl_rewind" };

    public void ImGuiTimeline(Snooper s, Save saver, Dictionary<string, Texture> icons, List<Animation> animations, Vector2 outliner, ImFontPtr fontPtr)
    {
        var treeP0 = ImGui.GetCursorScreenPos();
        var canvasSize = ImGui.GetContentRegionAvail();
        var canvasMaxY = MathF.Max(canvasSize.Y, _timeBarHeight + _timeStep.Y * animations.Count);
        ImGui.BeginChild("timeline_child", canvasSize with { Y = canvasMaxY });

        var timelineP1 = new Vector2(treeP0.X + canvasSize.X, treeP0.Y + canvasMaxY);
        var treeP1 = timelineP1 with { X = treeP0.X + outliner.X };

        var timelineP0 = treeP0 with { X = treeP1.X + _thickness };
        var timelineSize = timelineP1 - timelineP0;
        var timeRatio = timelineSize / MaxElapsedTime;

        var drawList = ImGui.GetWindowDrawList();
        drawList.PushClipRect(treeP0, timelineP1, true);
        drawList.AddRectFilled(treeP0, treeP1, 0xFF1F1C1C);
        drawList.AddRectFilled(timelineP0, timelineP1 with { Y = timelineP0.Y + _timeBarHeight }, 0xFF141414);
        drawList.AddRectFilled(timelineP0 with { Y = timelineP0.Y + _timeBarHeight }, timelineP1, 0xFF242424);
        drawList.AddLine(new Vector2(treeP1.X, treeP0.Y), treeP1, 0xFF504545, _thickness);
        drawList.AddLine(treeP0 with { Y = timelineP0.Y + _timeBarHeight }, timelineP1 with { Y = timelineP0.Y + _timeBarHeight }, 0x50504545, _thickness);

        // adding margin
        treeP0.X += _margin;
        treeP1.X -= _margin;

        // control buttons
        for (int i = 0; i < _icons.Length; i++)
        {
            var x = _buttonSize.X * 2.0f * i;
            ImGui.SetCursorScreenPos(treeP0 with { X = treeP1.X - x - _buttonSize.X * 2.0f + _thickness });
            if (ImGui.ImageButton($"timeline_actions_{_icons[i]}", icons[i == 1 ? IsPaused ? "tl_play" : "tl_pause" : _icons[i]].GetPointer(), _buttonSize))
            {
                switch (i)
                {
                    case 0:
                        SafeSetElapsedTime(ElapsedTime + _timeStep.X / timeRatio.X);
                        break;
                    case 1:
                        IsPaused = !IsPaused;
                        break;
                    case 2:
                        SafeSetElapsedTime(ElapsedTime - _timeStep.X / timeRatio.X);
                        break;
                }
            }
        }

        drawList.AddText(treeP0 with { Y = treeP0.Y + _thickness }, 0xA0FFFFFF, $"{ElapsedTime:F1}/{MaxElapsedTime:F1} seconds");

        ImGui.SetCursorScreenPos(timelineP0);
        ImGui.InvisibleButton("timeline_timetracker_canvas", timelineSize with { Y = _timeBarHeight }, ImGuiButtonFlags.MouseButtonLeft);
        IsActive = ImGui.IsItemActive();
        if (IsActive && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            var mousePosCanvas = ImGui.GetIO().MousePos - timelineP0;
            SafeSetElapsedTime(mousePosCanvas.X / timelineSize.X * MaxElapsedTime);
            foreach (var animation in animations)
            {
                animation.TimeCalculation(ElapsedTime);
            }
        }

        {   // draw time + time grid
            for (float x = 0; x < timelineSize.X; x += _timeStep.X)
            {
                var cursor = timelineP0.X + x;
                drawList.AddLine(new Vector2(cursor, timelineP0.Y + _timeHeight + 2.5f), new Vector2(cursor, timelineP0.Y + _timeBarHeight), 0xA0FFFFFF);
                drawList.AddLine(new Vector2(cursor, timelineP0.Y + _timeBarHeight), timelineP1 with { X = cursor }, 0x28C8C8C8);
                drawList.AddText(fontPtr, 14, new Vector2(cursor + 4, timelineP0.Y + 7.5f), 0x50FFFFFF, $"{x / timeRatio.X:F1}s");
            }

            for (float y = _timeBarHeight; y < timelineSize.Y; y += _timeStep.Y)
            {
                drawList.AddLine(timelineP0 with { Y = timelineP0.Y + y }, timelineP1 with { Y = timelineP0.Y + y }, 0x28C8C8C8);
            }
        }

        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.0f, 0.5f));
        for (int i = 0; i < animations.Count; i++)
        {
            var y = timelineP0.Y + _timeBarHeight + _timeStep.Y * i;
            animations[i].ImGuiAnimation(s, saver, drawList, timelineP0, treeP0, treeP1, _timeStep, timeRatio, y, _thickness, i);
            DrawSeparator(drawList, timelineP0, y + _timeStep.Y, animations[i].EndTime * timeRatio.X, ETrackerType.End);
        }
        ImGui.PopStyleVar();

        for (int i = 0; i < animations.Count; i++)
        {
            var y = timelineP0.Y + _timeBarHeight + _timeStep.Y * i;
            for (int j = 0; j < animations[i].Sequences.Length - 1; j++)
            {
                DrawSeparator(drawList, timelineP0, y + _timeStep.Y - _thickness, animations[i].Sequences[j].EndTime * timeRatio.X - 1.0f, ETrackerType.InBetween);
            }
        }

        DrawSeparator(drawList, timelineP0, timelineP1.Y, ElapsedTime * timeRatio.X, ETrackerType.Frame);

        drawList.PopClipRect();
        ImGui.EndChild();
    }

    private void DrawSeparator(ImDrawListPtr drawList, Vector2 origin, float y, float time, ETrackerType separatorType)
    {
        float size = separatorType switch
        {
            ETrackerType.Frame => 5,
            ETrackerType.End => 5,
            ETrackerType.InBetween => 7.5f,
            _ => throw new ArgumentOutOfRangeException(nameof(separatorType), separatorType, null)
        };

        Vector2 p1 = separatorType switch
        {
            ETrackerType.Frame => new Vector2(origin.X + time, origin.Y + _timeBarHeight),
            ETrackerType.End => origin with { X = origin.X + time },
            ETrackerType.InBetween => origin with { X = origin.X + time },
            _ => throw new ArgumentOutOfRangeException(nameof(separatorType), separatorType, null)
        };
        var p2 = p1 with { Y = y };

        uint color = separatorType switch
        {
            ETrackerType.Frame => 0xFF6F6F6F,
            ETrackerType.End => 0xFF2E3E82,
            ETrackerType.InBetween => 0xA0FFFFFF,
            _ => throw new ArgumentOutOfRangeException(nameof(separatorType), separatorType, null)
        };

        switch (separatorType)
        {
            case ETrackerType.Frame:
                color = 0xFF30478C;
                var xl = p1.X - size;
                var xr = p1.X + size;
                var yb = origin.Y + _timeBarHeight - _timeHeight / 2.0f;

                drawList.AddLine(p1, p2, color, 1f);
                drawList.AddQuadFilled(origin with { X = xl }, origin with { X = xr }, new Vector2(xr, yb), new Vector2(xl, yb), color);
                drawList.AddTriangleFilled(new Vector2(xl, yb), new Vector2(xr, yb), p1, color);
                break;
            case ETrackerType.End:
                drawList.AddLine(p1, p2, color, 1f);
                drawList.AddTriangleFilled(p1, p1 with { X = p1.X - size }, p1 with { Y = p1.Y + size }, color);
                break;
            case ETrackerType.InBetween:
                p1.Y += _timeBarHeight;
                drawList.AddLine(p1, p2, color, 1f);
                drawList.AddTriangleFilled(p1, new Vector2(p1.X - size / 2.0f, p1.Y - size), new Vector2(p1.X + size / 2.0f, p1.Y - size), color);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(separatorType), separatorType, null);
        }
    }
}
