using System;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Settings;

namespace FModel.Extensions;

public static class CUE4ParseExtensions
{
    public class LoadPackageResult
    {
        // more than 1 export per page currently break the inner package navigation feature
        // if you have 1k exports per page, at page 2, you click on export index 932
        // it will find the export index 932 in the current page, which would realistically be 1932
        // fix would be to use InclusiveStart and ExclusiveEnd to determine the page the export index is in
        // giving the document access to this would fix the issue and we could re-use Package instead of reloading it but it's quite a bit of work atm

        private const int PaginationThreshold = 5000;
        private const int MaxExportPerPage = 1;

        public IPackage Package;
        public int RequestedIndex;

        public bool IsPaginated => Package.ExportMapLength >= PaginationThreshold;

        /// <summary>
        /// index of the first export on the current page
        /// this index is the starting point for additional data preview
        ///
        /// it can be >0 even if <see cref="IsPaginated"/> is false if we want to focus data preview on a specific export
        /// in this case, we will display all exports but only the focused one will be checked for data preview
        /// </summary>
        public int InclusiveStart => Math.Max(0, RequestedIndex - RequestedIndex % MaxExportPerPage);
        /// <summary>
        /// last exclusive export index of the current page
        /// </summary>
        public int ExclusiveEnd => IsPaginated
            ? Math.Min(InclusiveStart + MaxExportPerPage, Package.ExportMapLength)
            : Package.ExportMapLength;
        public int PageSize => ExclusiveEnd - InclusiveStart;

        public string TabTitleExtra => IsPaginated ? $"Export{(PageSize > 1 ? "s" : "")} {InclusiveStart}{(PageSize > 1 ? $"-{ExclusiveEnd - 1}" : "")} of {Package.ExportMapLength - 1}" : null;

        /// <summary>
        /// display all exports unless paginated
        /// </summary>
        /// <param name="save">if we save the data we will display all exports even if <see cref="IsPaginated"/> is true</param>
        /// <returns></returns>
        public object GetDisplayData(bool save = false) => !save && IsPaginated
            ? Package.GetExports(InclusiveStart, PageSize)
            : Package.GetExports();
    }

    public static LoadPackageResult GetLoadPackageResult(this IFileProvider provider, GameFile file, string objectName = null)
    {
        var result = new LoadPackageResult { Package = provider.LoadPackage(file) };
        if (result.IsPaginated || (result.Package.HasFlags(EPackageFlags.PKG_ContainsMap) && UserSettings.Default.PreviewWorlds)) // focus on UWorld if it's a map we want to preview
        {
            result.RequestedIndex = result.Package.GetExportIndex(file.NameWithoutExtension);
            if (objectName != null)
            {
                result.RequestedIndex = int.TryParse(objectName, out var index) ? index : result.Package.GetExportIndex(objectName);
            }
        }

        return result;
    }
}
