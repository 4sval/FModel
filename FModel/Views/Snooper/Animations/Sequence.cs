using System.Numerics;
using CUE4Parse_Conversion.Animations.PSA;
using CUE4Parse.Utils;
using ImGuiNET;

namespace FModel.Views.Snooper.Animations;

public class Sequence
{
    public readonly string Name;
    public readonly float TimePerFrame;
    public readonly float StartTime;
    public readonly float Duration;
    public readonly float EndTime;
    public readonly int EndFrame;
    public readonly int LoopingCount;
    public readonly bool IsAdditive;

    public Sequence(CAnimSequence sequence)
    {
        Name = sequence.Name;
        TimePerFrame = 1.0f / sequence.FramesPerSecond;
        StartTime = sequence.StartPos;
        Duration = sequence.AnimEndTime;
        EndTime = StartTime + Duration;
        EndFrame = (Duration / TimePerFrame).FloorToInt() - 1;
        LoopingCount = sequence.LoopingCount;
        IsAdditive = sequence.IsAdditive;
    }

    public void DrawSequence(ImDrawListPtr drawList, ImFontPtr fontPtr, float x, Vector2 p2, Vector2 timeStep, Vector2 timeRatio, float t, bool animSelected)
    {
        var halfThickness = t / 2.0f;
        var q1 = new Vector2(x + StartTime * timeRatio.X + t + halfThickness, p2.Y - timeStep.Y / 2.0f);
        var q2 = p2 with { X = x + EndTime * timeRatio.X - t * 2.0f };

        drawList.PushClipRect(q1, q2 with { X = q2.X + t }, true);

        var lineColor = animSelected ? 0xA0FFFFFF : 0x50FFFFFF;
        drawList.AddLine(new Vector2(q1.X, q2.Y), q1, lineColor, 1.0f);
        drawList.AddLine(q1, new Vector2(q2.X, q1.Y), lineColor, 1.0f);
        drawList.AddLine(new Vector2(q2.X, q1.Y), q2, lineColor, 1.0f);

        if (IsAdditive)
            drawList.AddText(fontPtr, 12 * ImGui.GetWindowDpiScale(), new Vector2(q1.X + t, q1.Y + halfThickness), animSelected ? 0xFFFFFFFF : 0x50FFFFFF, "Is Additive");

        drawList.PopClipRect();
    }
}
