using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.IO.Objects;
using FModel.ViewModels;

namespace FModel.Views.Resources.Converters;

/// <summary>
/// TODO: migrate legacy view models to use GameFileViewModel instead of GameFile, then remove this converter
/// for example <see cref="TabItem"/> or <see cref="SearchViewModel"/>
/// </summary>
public class GameFileMeetsConditionConverter : IValueConverter
{
    public Collection<IGameFileCondition> Conditions { get; } = [];

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var gameFile = value switch
        {
            GameFile file => file,
            TabItem tabItem => tabItem.Entry,
            _ => null
        };
        if (gameFile is null || Conditions.Count == 0) return false;

        return Conditions.All(c => c.Matches(gameFile));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
public interface IGameFileCondition
{
    bool Matches(GameFile item);
}

public class GameFileIsUePackageCondition : IGameFileCondition
{
    public bool Matches(GameFile item)
    {
        return item?.IsUePackage ?? false;
    }
}

public class GameFileIsIoStoreCondition : IGameFileCondition
{
    public bool Matches(GameFile item)
    {
        return item is FIoStoreEntry;
    }
}
