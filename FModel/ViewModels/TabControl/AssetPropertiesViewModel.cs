using FModel.Utils;
using System.Windows;
using FModel.PakReader;
using FModel.PakReader.IO;
using FModel.PakReader.Parsers.Objects;

namespace FModel.ViewModels.TabControl
{
    static class AssetPropertiesVm
    {
        public static readonly AssetPropertiesViewModel assetPropertiesViewModel = new AssetPropertiesViewModel();
        public static void Set(this AssetPropertiesViewModel vm, ReaderEntry entry)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                string ext = string.Join("   ", entry.GetExtension(), entry.Uexp?.GetExtension(), entry.Ubulk?.GetExtension());
                string offsets;
                string tSize;
                if (entry is FPakEntry pakEntry)
                {
                    offsets = string.Join("   ", "0x" + (pakEntry.Offset + pakEntry.StructSize).ToString("X2"),
                        entry.Uexp != null ? "0x" + (((FPakEntry)pakEntry.Uexp).Offset + pakEntry.StructSize).ToString("X2") : string.Empty,
                        entry.Ubulk != null ? "0x" + (((FPakEntry)pakEntry.Ubulk).Offset + pakEntry.StructSize).ToString("X2") : string.Empty);
                    tSize = Strings.GetReadableSize(pakEntry.Size + ((pakEntry.Uexp as FPakEntry)?.Size ?? 0) + ((pakEntry.Ubulk as FPakEntry)?.Size ?? 0));
                } else if (entry is FIoStoreEntry ioEntry)
                {
                    offsets = string.Join("   ", "0x" + (ioEntry.Offset).ToString("X2"),
                        entry.Uexp != null ? "0x" + (((FIoStoreEntry)ioEntry.Uexp).Offset).ToString("X2") : string.Empty,
                        entry.Ubulk != null ? "0x" + (((FIoStoreEntry)ioEntry.Ubulk).Offset).ToString("X2") : string.Empty);
                    tSize = Strings.GetReadableSize(ioEntry.Size + ((ioEntry.Uexp as FIoStoreEntry)?.Size ?? 0) + ((ioEntry.Ubulk as FIoStoreEntry)?.Size ?? 0));
                }
                else
                {
                    offsets = string.Empty;
                    tSize = string.Empty;
                }

                vm.AssetName = entry.GetNameWithExtension();
                vm.PartOf = entry.ContainerName;
                vm.IncludedExtensions = ext.TrimEnd();
                vm.Offsets = offsets.TrimEnd();
                vm.TotalSize = tSize;
                if (entry is FPakEntry c1)
                {
                    vm.IsEncrypted = c1.Encrypted ? Properties.Resources.Yes : Properties.Resources.No;
                    vm.CompMethod = ((ECompressionFlags)c1.CompressionMethodIndex).ToString();
                }
                else if (entry is FIoStoreEntry c2)
                {
                    vm.IsEncrypted = c2.Encrypted ? Properties.Resources.Yes : Properties.Resources.No;
                    vm.CompMethod = c2.CompressionMethodString;
                }
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
