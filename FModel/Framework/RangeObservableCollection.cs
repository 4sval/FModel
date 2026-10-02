using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace FModel.Framework;

public sealed class RangeObservableCollection<T> : ObservableCollection<T>
{
    private bool _suppressNotification;

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!_suppressNotification)
            base.OnCollectionChanged(e);
    }

    public void AddRange(IEnumerable<T> list)
    {
        ArgumentNullException.ThrowIfNull(list);

        _suppressNotification = true;

        foreach (var item in list)
            Add(item);

        _suppressNotification = false;
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public void ReplaceRange(IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        var items = collection as IList<T> ?? [.. collection];

        CheckReentrancy();

        Items.Clear();

        foreach (var item in items)
            Items.Add(item);

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public void SetSuppressionState(bool state)
    {
        _suppressNotification = state;
    }

    public void InvokeOnCollectionChanged(NotifyCollectionChangedAction changedAction = NotifyCollectionChangedAction.Reset)
    {
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(changedAction));
    }
}
