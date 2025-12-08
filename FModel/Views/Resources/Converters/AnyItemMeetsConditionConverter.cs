using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using FModel.Extensions;
using FModel.ViewModels;

namespace FModel.Views.Resources.Converters;

public class AnyItemMeetsConditionConverter : IValueConverter
{
    public IItemCondition Condition { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not IEnumerable items || Condition == null)
            return false;

        return items.OfType<GameFileViewModel>().Any(item => Condition.Matches(item));
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
