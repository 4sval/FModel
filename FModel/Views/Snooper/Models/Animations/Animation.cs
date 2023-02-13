using System;
using System.Numerics;
using CUE4Parse_Conversion.Animations;
using CUE4Parse.Utils;
using ImGuiNET;

namespace FModel.Views.Snooper.Models.Animations;

public enum AnimSeparatorType
{
    InBetween,
    End
}

public class Animation : IDisposable
{
    public float ElapsedTime;
    public bool IsPaused;
    public readonly int TotalFrames;
    public readonly float TotalDuration;

    public int CurrentSequence;
    public readonly Sequence[] Sequences;
    public int SequencesCount => Sequences.Length;

    public Animation()
    {
        Reset();

        IsPaused = false;
        TotalFrames = 0;
        TotalDuration = 0.0f;
        Sequences = Array.Empty<Sequence>();
    }

    public Animation(Skeleton skeleton, CAnimSet anim, bool rotationOnly) : this()
    {
        Sequences = new Sequence[anim.Sequences.Count];
        for (int i = 0; i < Sequences.Length; i++)
        {
            Sequences[i] = new Sequence(anim.Sequences[i], skeleton, rotationOnly);

            TotalFrames += Sequences[i].MaxFrame;
            TotalDuration += Sequences[i].Duration;
        }

        if (Sequences.Length > 0)
            ElapsedTime = Sequences[0].StartTime;
    }

    public void Update(float deltaSeconds)
    {
        if (IsPaused) return;
        if (Sequences[CurrentSequence].IsComplete)
        {
            Sequences[CurrentSequence].Reset();
            CurrentSequence++;
        }
        if (CurrentSequence >= SequencesCount)
            Reset();

        ElapsedTime += Sequences[CurrentSequence].Update(deltaSeconds);
    }


    public Matrix4x4 InterpolateBoneTransform(int boneIndex)
    {
        // interpolate here
        return Sequences[CurrentSequence].BonesTransform[boneIndex][Sequences[CurrentSequence].Frame].Matrix;
    }

    private void Reset()
    {
        ElapsedTime = 0.0f;
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
        var ratio = canvasSize / TotalFrames;

        var drawList = ImGui.GetWindowDrawList();

        ImGui.InvisibleButton("timeline_canvas", canvasP1 with { Y = _timeBarHeight }, ImGuiButtonFlags.MouseButtonLeft);
        IsPaused = ImGui.IsItemActive();
        if (IsPaused && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            var mousePosCanvas = io.MousePos - canvasP0;
            ElapsedTime = Math.Clamp(mousePosCanvas.X / canvasSize.X * TotalDuration, 0, TotalDuration);
        }

        drawList.AddRectFilled(canvasP0, canvasP1 with { Y = canvasP0.Y + _timeBarHeight }, 0xFF181818);
        drawList.PushClipRect(canvasP0, canvasP1 with { Y = canvasP0.Y + _timeBarHeight }, true);
        {
            for (float x = 0; x < canvasSize.X; x += ratio.X * 10f)
            {
                drawList.AddLine(new Vector2(canvasP0.X + x, canvasP0.Y + _timeHeight + 2.5f), canvasP1 with { X = canvasP0.X + x }, 0xA0FFFFFF);
                drawList.AddText(fontPtr, 14, new Vector2(canvasP0.X + x + 4, canvasP0.Y + 7.5f), 0x50FFFFFF, (x / ratio.X).FloorToInt().ToString());
            }
        }
        drawList.PopClipRect();

        for (int i = 0; i < Sequences.Length; i++)
        {
            Sequences[i].DrawSequence(drawList, canvasP0, ratio, i);
        }

        DrawSeparator(drawList, canvasP0, canvasP1, ElapsedTime * ratio.X, AnimSeparatorType.InBetween);
        DrawSeparator(drawList, canvasP0, canvasP1, TotalDuration * ratio.X, AnimSeparatorType.End);

        // ImGui.Text($"{Sequences[CurrentSequence].Name} > {(CurrentSequence < SequencesCount - 1 ? Sequences[CurrentSequence + 1].Name : Sequences[0].Name)}");
        // ImGui.Text($"Frame: {Sequences[CurrentSequence].Frame}/{Sequences[CurrentSequence].MaxFrame}");
        // ImGui.Text($"Frame: {Frame}/{TotalFrames}");
        // ImGui.Text($"FPS: {Sequences[CurrentSequence].FramesPerSecond}");
    }

    private void DrawSeparator(ImDrawListPtr drawList, Vector2 origin, Vector2 destination, float time, AnimSeparatorType separatorType)
    {
        const int size = 5;

        Vector2 p1 = separatorType switch
        {
            AnimSeparatorType.InBetween => new Vector2(origin.X + time, origin.Y + _timeBarHeight),
            AnimSeparatorType.End => origin with { X = origin.X + time },
            _ => throw new ArgumentOutOfRangeException(nameof(separatorType), separatorType, null)
        };
        var p2 = new Vector2(p1.X, destination.Y);

        uint color = separatorType switch
        {
            AnimSeparatorType.InBetween => 0xFF6F6F6F,
            AnimSeparatorType.End => 0xFF2E3E82,
            _ => throw new ArgumentOutOfRangeException(nameof(separatorType), separatorType, null)
        };

        drawList.AddLine(p1, p2, color, 1f);
        switch (separatorType)
        {
            case AnimSeparatorType.InBetween:
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
