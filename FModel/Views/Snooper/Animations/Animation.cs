using System;
using System.Collections.Generic;
using CUE4Parse_Conversion.Animations;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.Utils;

namespace FModel.Views.Snooper.Animations;

public class Animation : IDisposable
{
    public readonly Sequence[] Sequences;
    public readonly float EndTime;                  // Animation End Time
    public readonly float TotalElapsedTime;         // Animation Max Time

    public int CurrentSequence;
    public int FrameInSequence;                     // Current Sequence's Frame to Display

    public readonly List<FGuid> AttachedModels;

    public Animation()
    {
        Sequences = Array.Empty<Sequence>();
        AttachedModels = new List<FGuid>();
    }

    public Animation(CAnimSet animSet) : this()
    {
        Sequences = new Sequence[animSet.Sequences.Count];
        for (int i = 0; i < Sequences.Length; i++)
        {
            Sequences[i] = new Sequence(animSet.Sequences[i]);

            TotalElapsedTime += animSet.Sequences[i].NumFrames * Sequences[i].TimePerFrame;
            EndTime = Sequences[i].EndTime;
        }

        // if (Sequences.Length > 0)
        //     Tracker.ElapsedTime = Sequences[0].StartTime;
    }

    public Animation(CAnimSet animSet, params FGuid[] animatedModels) : this(animSet)
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
}
