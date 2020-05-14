using PakReader;
using PakReader.Pak;
using PakReader.Parsers.Objects;
using System.Collections.Generic;
using System.Windows;

namespace FModel.ViewModels.TabControl
{
    static class PakPropertiesVm
    {
        public static readonly PakPropertiesViewModel pakPropertiesViewModel = new PakPropertiesViewModel();
        public static void Set(this PakPropertiesViewModel vm, PakFileReader pakFileReader)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                vm.PakName = pakFileReader.FileName;
                vm.Version = ((int)pakFileReader.Info.Version).ToString();
                vm.MountPoint = pakFileReader.MountPoint;
                vm.AesKey = pakFileReader.AesKey.ToStringKey();
                vm.Guid = pakFileReader.Info.EncryptionKeyGuid.Hex;
                vm.FileCount = (pakFileReader as IReadOnlyDictionary<string, FPakEntry>).Count.ToString();
            });
        }
        public static void Reset(this PakPropertiesViewModel vm)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                vm.PakName = string.Empty;
                vm.Version = string.Empty;
                vm.MountPoint = string.Empty;
                vm.AesKey = string.Empty;
                vm.Guid = string.Empty;
                vm.FileCount = string.Empty;
            });
        }
    }

    public class PakPropertiesViewModel : PropertyChangedBase
    {
        private string _pakName;
        public string PakName
        {
            get { return _pakName; }

            set { this.SetProperty(ref this._pakName, value); }
        }
        private string _version;
        public string Version
        {
            get { return _version; }

            set { this.SetProperty(ref this._version, value); }
        }
        private string _mountPoint;
        public string MountPoint
        {
            get { return _mountPoint; }

            set { this.SetProperty(ref this._mountPoint, value); }
        }
        private string _aesKey;
        public string AesKey
        {
            get { return _aesKey; }

            set { this.SetProperty(ref this._aesKey, value); }
        }
        private string _guid;
        public string Guid
        {
            get { return _guid; }

            set { this.SetProperty(ref this._guid, value); }
        }
        private string _fileCount;
        public string FileCount
        {
            get { return _fileCount; }

            set { this.SetProperty(ref this._fileCount, value); }
        }
    }
}
