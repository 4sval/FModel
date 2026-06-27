using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Dto;

namespace FModel.ViewModels;

public class StreamingLevelFilterViewModel
{
    public string WorldName { get; }
    public int TotalCount { get; }
    public List<ActorNodeVm> Children { get; } = [];

    public StreamingLevelFilterViewModel(StreamingLevelFilterArgs args)
    {
        WorldName = args.WorldName;

        foreach (var actor in args.Actors)
        {
            Children.Add(new ActorNodeVm(actor));
        }

        var worldLevels = new ActorNodeVm("Streaming Levels") { IsExpanded = true };
        foreach (var level in args.StreamingLevels)
        {
            worldLevels.Children.Add(new StreamingLevelNodeVm(level));
        }
        if (worldLevels.Children.Count > 0) Children.Add(worldLevels);

        TotalCount = CountLevels(args.Actors) + args.StreamingLevels.Count;
    }

    public void SkipAll()
    {
        foreach (var child in Children)
        {
            child.IsChecked = false;
        }
    }

    private int CountLevels(IReadOnlyList<ActorDto> actors)
    {
        var n = 0;
        foreach (var a in actors) n += CountFromActor(a);
        return n;
    }

    private int CountFromActor(ActorDto actor)
    {
        var n = actor.StreamingLevels?.Count ?? 0;
        if (actor.RootComponent is { } comp) n += CountFromComponent(comp);
        return n;
    }

    private int CountFromComponent(SceneComponentDto comp)
    {
        var n = 0;
        foreach (var a in comp.AttachedActors) n += CountFromActor(a);
        foreach (var c in comp.Children) n += CountFromComponent(c);
        return n;
    }
}

public class ActorNodeVm : TreeNodeVm
{
    public override string Name { get; }
    public bool IsExpanded { get; set; }
    public ObservableCollection<TreeNodeVm> Children { get; } = [];

    private static int _batch;

    public ActorNodeVm(string name)
    {
        Name = name;
        Children.CollectionChanged += (_, e) =>
        {
            if (e.NewItems is null) return;

            foreach (TreeNodeVm child in e.NewItems)
            {
                child.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(IsChecked) && _batch == 0)
                        OnPropertyChanged(nameof(IsChecked));
                };
            }
        };
    }

    public ActorNodeVm(ActorDto actor) : this(actor.Name)
    {
        if (actor.StreamingLevels is { Count: > 0 } streamingLevels)
            foreach (var level in streamingLevels)
                Children.Add(new StreamingLevelNodeVm(level));

        CollectFromComponent(actor.RootComponent);
    }

    private ActorNodeVm(SceneComponentDto component) : this(component.Name)
    {
        CollectFromComponent(component);
    }

    private void NotifyIntermediates()
    {
        foreach (var child in Children.OfType<ActorNodeVm>())
        {
            child.NotifyIntermediates();
            child.OnPropertyChanged(nameof(IsChecked));
        }
    }

    private void CollectFromComponent(SceneComponentDto? comp)
    {
        if (comp is null) return;
        foreach (var actor in comp.AttachedActors)
            Children.Add(new ActorNodeVm(actor));
        foreach (var component in comp.Children)
            Children.Add(new ActorNodeVm(component));
    }

    private IEnumerable<StreamingLevelNodeVm> AllLevels()
    {
        foreach (var child in Children)
        {
            switch (child)
            {
                case StreamingLevelNodeVm sl:
                    yield return sl;
                    break;
                case ActorNodeVm actor:
                {
                    foreach (var l in actor.AllLevels())
                    {
                        yield return l;
                    }
                    break;
                }
            }
        }
    }

    public bool? IsChecked
    {
        get
        {
            var all = AllLevels().ToList();
            if (all.Count == 0) return false;

            var trueCount = all.Count(l => l.IsChecked);
            if (trueCount == all.Count) return true;
            if (trueCount == 0) return false;
            return null;
        }
        set
        {
            var v = value ?? false;
            _batch++;
            foreach (var l in AllLevels())
                l.IsChecked = v;
            NotifyIntermediates();
            _batch--;
            OnPropertyChanged();
        }
    }
}

public class StreamingLevelNodeVm(StreamingLevel level) : TreeNodeVm
{
    public override string Name { get; } = level.World.Name;

    public bool IsChecked
    {
        get => level.IsPersistent;
        set
        {
            level.IsPersistent = value;
            OnPropertyChanged();
        }
    }
}

public abstract class TreeNodeVm : INotifyPropertyChanged
{
    public abstract string Name { get; }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
