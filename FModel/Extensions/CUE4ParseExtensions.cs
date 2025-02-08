using System;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Assets;

namespace FModel.Extensions;

public static class CUE4ParseExtensions
{
    public class LoadPackageResult
    {
        private const int PaginationThreshold = 5000;
        private const int MaxExportPerPage = 1;

        public IPackage Package;
        public bool IsPaginated => Package.ExportMapLength >= PaginationThreshold;

        public int InclusiveStart;
        public int ExclusiveEnd => IsPaginated
            ? Math.Min(InclusiveStart + MaxExportPerPage, Package.ExportMapLength)
            : Package.ExportMapLength;

        private int CurrentPage => (InclusiveStart + 1) / MaxExportPerPage;
        private int LastPage => Math.Max(1, (Package.ExportMapLength + MaxExportPerPage - 1) / MaxExportPerPage);
        private int PageSize => ExclusiveEnd - InclusiveStart;

        public string TabTitleExtra => IsPaginated ? $"Page {CurrentPage}/{LastPage}" : null;

        public object GetDisplayData(bool save = false) => !save ? Package.GetExports(InclusiveStart, PageSize) : Package.GetExports();
    }

    public static LoadPackageResult GetLoadPackageResult(this IFileProvider provider, GameFile file, string objectName = null)
    {
        var result = new LoadPackageResult { Package = provider.LoadPackage(file) };
        if (result.IsPaginated)
        {
            result.InclusiveStart = result.Package.GetExportIndex(file.NameWithoutExtension);
            if (objectName != null)
            {
                result.InclusiveStart = int.TryParse(objectName, out var index) ? index : result.Package.GetExportIndex(objectName);
            }
        }

        return result;
    }
}
