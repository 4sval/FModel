using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace FModel.Views.Resources.Converters;

public class AssetTypeToBoolConverter : IValueConverter
{
    public static readonly AssetTypeToBoolConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not EAssetCategory assetType || parameter is not string expectedType)
            return true;
        if (assetType is EAssetCategory.All)
            return true;

        return expectedType
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(t => string.Equals(t, assetType.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
