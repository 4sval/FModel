using System.Numerics;
using CUE4Parse_Conversion.Animations;
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
        TimePerFrame = 1.0f / sequence.Rate;
        StartTime = sequence.StartPos;
        Duration = sequence.AnimEndTime;
        EndTime = StartTime + Duration;
        EndFrame = (Duration / TimePerFrame).FloorToInt() - 1;
        LoopingCount = sequence.LoopingCount;
        IsAdditive = sequence.bAdditive;
    }

    public void DrawSequence(ImDrawListPtr drawList, float x, Vector2 p2, Vector2 timeStep, Vector2 timeRatio, float t)
    {
        var q1 = new Vector2(x + StartTime * timeRatio.X + t, p2.Y - timeStep.Y / 2.0f);
        var q2 = p2 with { X = x + EndTime * timeRatio.X - t - t };

        drawList.AddLine(new Vector2(q1.X, q2.Y), q1, 0x50FFFFFF, 1.0f);
        drawList.AddLine(q1, new Vector2(q2.X, q1.Y), 0x50FFFFFF, 1.0f);
        drawList.AddLine(new Vector2(q2.X, q1.Y), q2, 0x50FFFFFF, 1.0f);
    }
}
