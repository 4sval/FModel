using System;
using System.Collections.Generic;
using System.Linq;

namespace FModel.Extensions;

public static class AssetCategoryExtensions
{
    public const uint CategoryBase = 0x00010000;

    public static EAssetCategory GetBaseCategory(this EAssetCategory category)
    {
        return (EAssetCategory) ((uint) category & 0xFFFF0000);
    }

    public static bool IsOfCategory(this EAssetCategory item, EAssetCategory category)
    {
        return item.GetBaseCategory() == category.GetBaseCategory();
    }

    public static bool IsBaseCategory(this EAssetCategory category)
    {
        return category == category.GetBaseCategory();
    }

    public static IEnumerable<EAssetCategory> GetBaseCategories()
    {
        return Enum.GetValues<EAssetCategory>().Where(c => c.IsBaseCategory());
    }
}
