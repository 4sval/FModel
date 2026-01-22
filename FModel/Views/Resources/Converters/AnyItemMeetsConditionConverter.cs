using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using CUE4Parse.UE4.IO.Objects;
using FModel.Extensions;
using FModel.ViewModels;

namespace FModel.Views.Resources.Converters;

public class AnyItemMeetsConditionConverter : IValueConverter
{
    public Collection<IItemCondition> Conditions { get; } = [];

    /// <summary>
    /// Determines how multiple conditions are evaluated. Default is 'And'.
    /// </summary>
    public EConditionMode ConditionMode { get; set; } = EConditionMode.And;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not IEnumerable items || Conditions.Count == 0)
            return false;

        Func<GameFileViewModel, bool> predicate = ConditionMode switch
        {
            EConditionMode.And => item => Conditions.All(condition => condition.Matches(item)),
            EConditionMode.Or => item => Conditions.Any(condition => condition.Matches(item)),
            _ => throw new ArgumentOutOfRangeException()
        };

        return items.OfType<GameFileViewModel>().Any(predicate);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    public enum EConditionMode
    {
        And,
        Or
    }
}

public interface IItemCondition
{
    bool Matches(GameFileViewModel item);
}

public class ItemActionCondition : IItemCondition
{
    public EBulkType Action { get; set; }

    public bool Matches(GameFileViewModel item)
    {
        return item != null && item.AssetActions.HasFlag(Action);
    }
}

public class ItemCategoryCondition : IItemCondition
{
    public EAssetCategory Category { get; set; }

    public bool Matches(GameFileViewModel item)
    {
        if (item == null) return false;

        // if the specified category is a base category, check if the item's category is derived from it
        if (Category.IsBaseCategory())
        {
            return item.AssetCategory.IsOfCategory(Category);
        }

        // if the specified category is a targeted non-base category, check for exact match
        return item.AssetCategory == Category;
    }
}

public class ItemIsUePackageCondition : IItemCondition
{
    public bool Matches(GameFileViewModel item)
    {
        return item?.Asset?.IsUePackage ?? false;
    }
}

public class ItemIsIoStoreCondition : IItemCondition
{
    public bool Matches(GameFileViewModel item)
    {
        return item?.Asset is FIoStoreEntry;
    }
}
