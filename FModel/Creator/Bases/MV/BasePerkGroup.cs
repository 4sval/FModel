using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.UObject;
using SkiaSharp;

namespace FModel.Creator.Bases.MV;

public class BasePerkGroup : UCreator
{
    private readonly List<BasePandaIcon> _perks;

    public BasePerkGroup(UObject uObject, EIconStyle style) : base(uObject, style)
    {
        _perks = new List<BasePandaIcon>();
    }

    public override void ParseForInfo()
    {
        if (Object.TryGetValue(out UScriptSet perks, "Perks")) // PORCO DIO WB USE ARRAYS!!!!!!
        {
            foreach (var perk in perks.Properties)
            {
                if (perk.GenericValue is not FPackageIndex packageIndex ||
                    !Utils.TryGetPackageIndexExport(packageIndex, out UObject export))
                    continue;

                var icon = new BasePandaIcon(export, Style);
                icon.ParseForInfo();
                _perks.Add(icon);
            }
        }
    }

    public override SKBitmap[] Draw()
    {
        var ret = new SKBitmap[_perks.Count];
        for (var i = 0; i < ret.Length; i++)
        {
            ret[i] = _perks[i].Draw()[0];
        }

        return ret;
    }
}
