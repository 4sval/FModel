using FModel.Utils;
using PakReader.Parsers.Objects;
using System.Windows;

namespace FModel.ViewModels.TabControl
{
    static class AssetPropertiesVm
    {
        public static readonly AssetPropertiesViewModel assetPropertiesViewModel = new AssetPropertiesViewModel();
        public static void Set(this AssetPropertiesViewModel vm, FPakEntry entry)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                string ext = string.Join("   ", entry.GetExtension(), entry.Uexp?.GetExtension(), entry.Ubulk?.GetExtension());
                string offsets = string.Join("   ", "0x" + (entry.Offset + entry.StructSize).ToString("X2"),
                    entry.Uexp != null ? "0x" + (entry.Uexp.Offset + entry.StructSize).ToString("X2") : string.Empty,
                    entry.Ubulk != null ? "0x" + (entry.Ubulk.Offset + entry.StructSize).ToString("X2") : string.Empty);
                string tSize = Strings.GetReadableSize(entry.Size + (entry.Uexp?.Size ?? 0) + (entry.Ubulk?.Size ?? 0));

                vm.AssetName = entry.GetNameWithExtension();
                vm.PartOf = entry.PakFileName;
                vm.IncludedExtensions = ext.TrimEnd();
                vm.Offsets = offsets.TrimEnd();
                vm.TotalSize = tSize;
                vm.IsEncrypted = entry.Encrypted ? Properties.Resources.Yes : Properties.Resources.No;
                vm.CompMethod = ((ECompressionFlags)entry.CompressionMethodIndex).ToString();
            });
        }
        public static void Reset(this AssetPropertiesViewModel vm)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                vm.AssetName = string.Empty;
                vm.PartOf = string.Empty;
                vm.IncludedExtensions = string.Empty;
                vm.Offsets = string.Empty;
                vm.TotalSize = string.Empty;
                vm.IsEncrypted = string.Empty;
                vm.CompMethod = string.Empty;
            });
        }
    }

    public class AssetPropertiesViewModel : PropertyChangedBase
    {
        private string _assetName;
        public string AssetName
        {
            get { return _assetName; }

            set { this.SetProperty(ref this._assetName, value); }
        }
        private string _partOf;
        public string PartOf
        {
            get { return _partOf; }

            set { this.SetProperty(ref this._partOf, value); }
        }
        private string _includedExtensions;
        public string IncludedExtensions
        {
            get { return _includedExtensions; }

            set { this.SetProperty(ref this._includedExtensions, value); }
        }
        private string _offsets;
        public string Offsets
        {
            get { return _offsets; }

            set { this.SetProperty(ref this._offsets, value); }
        }
        private string _totalSize;
        public string TotalSize
        {
            get { return _totalSize; }

            set { this.SetProperty(ref this._totalSize, value); }
        }
        private string _isEncrypted;
        public string IsEncrypted
        {
            get { return _isEncrypted; }

            set { this.SetProperty(ref this._isEncrypted, value); }
        }
        private string _compMethod;
        public string CompMethod
        {
            get { return _compMethod; }

            set { this.SetProperty(ref this._compMethod, value); }
        }
    }
}
