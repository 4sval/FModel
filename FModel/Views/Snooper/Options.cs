using System;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace FModel.Views.Snooper;

public class Options
{
    public FGuid SelectedModel { get; private set; }
    public int SelectedModelInstance;

    public int SelectedSection { get; private set; }
    public int SelectedMorph { get; private set; }

    public Options()
    {
        Reset();
    }

    public void Reset()
    {
        SelectedModel = Guid.Empty;
        SelectedModelInstance = 0;
        SelectedSection = 0;
        SelectedMorph = 0;
    }

    public void SelectModel(FGuid guid)
    {
        SelectedModel = guid;
        SelectedModelInstance = 0;
        SelectedSection = 0;
        SelectedMorph = 0;
    }

    public void SelectSection(int index)
    {
        SelectedSection = index;
    }

    public void SelectMorph(int index, Model model)
    {
        SelectedMorph = index;
        model.UpdateMorph(SelectedMorph);
    }

    public bool TryGetSection(Model model, out Section section)
    {
        if (SelectedSection >= 0 && SelectedSection < model.Sections.Length)
            section = model.Sections[SelectedSection]; else section = null;
        return section != null;
    }
}
