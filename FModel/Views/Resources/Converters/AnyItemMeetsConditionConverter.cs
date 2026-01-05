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

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not IEnumerable items || Conditions.Count == 0)
            return false;

        return items.OfType<GameFileViewModel>().Any(item => Conditions.All(c => c.Matches(item)));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public interface IItemCondition
{
    bool Matches(GameFileViewModel item);
}

public class ItemCategoryCondition : IItemCondition
{
    public EAssetCategory Category { get; set; }

    public bool Matches(GameFileViewModel item)
    {
        return item != null && item.AssetCategory.IsOfCategory(Category);
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
