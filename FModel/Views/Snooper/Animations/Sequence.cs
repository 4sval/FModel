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

    public Sequence(CAnimSequence sequence)
    {
        Name = sequence.Name;
        TimePerFrame = 1.0f / sequence.Rate;
        StartTime = sequence.StartPos;
        Duration = sequence.AnimEndTime;
        EndTime = StartTime + Duration;
        EndFrame = (Duration / TimePerFrame).FloorToInt() - 1;
        LoopingCount = sequence.LoopingCount;
    }

    private readonly float _height = 20.0f;
    public void DrawSequence(ImDrawListPtr drawList, float x, float y, Vector2 ratio, int index, uint col)
    {
        var height = _height * (index % 2);
        var p1 = new Vector2(x + StartTime * ratio.X, y + height);
        var p2 = new Vector2(x + EndTime * ratio.X, y + height + _height);
        drawList.PushClipRect(p1, p2, true);
        drawList.AddRectFilled(p1, p2, col);
        drawList.AddText(p1 with { X = p1.X + 2.5f }, 0xFF000000, Name);
        drawList.PopClipRect();
    }
}
