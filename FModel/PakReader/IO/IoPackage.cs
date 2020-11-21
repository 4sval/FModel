using System.IO;
using FModel.PakReader.Parsers;
using FModel.PakReader.Parsers.Class;
using FModel.PakReader.Parsers.Objects;
using Newtonsoft.Json;

namespace FModel.PakReader.IO
{
    public class IoPackage : Package
    {
        private readonly byte[] UAsset;
        private readonly byte[] UBulk;
        private IoPackageReader _reader;
        private readonly string _jsonData = null;
        
        internal IoPackage(byte[] asset, byte[] bulk)
        {
            UAsset = asset;
            UBulk = bulk;
        }

        public IoPackageReader Reader
        {
            get
            {
                if (_reader == null)
                {
                    var asset = new MemoryStream(UAsset);
                    var bulk = UBulk != null ? new MemoryStream(UBulk) : null;
                    asset.Position = 0;
                    if (bulk != null)
                        bulk.Position = 0;

                    return _reader = new IoPackageReader(asset, bulk, Globals.GlobalData, true);
                }

                return _reader;
            }
        }

        public override string JsonData
        {
            get
            {
                if (string.IsNullOrEmpty(_jsonData))
                {
                    var ret = new JsonExport[Exports.Length];
                    for (int i = 0; i < ret.Length; i++)
                    {
                        ret[i] = new JsonExport
                        {
                            ExportType = ExportTypes[i].String,
                            ExportValue = (FModel.EJsonType)FModel.Properties.Settings.Default.AssetsJsonType switch
                            {
                                FModel.EJsonType.Default => Exports[i].GetJsonDict(),
                                _ => Exports[i]
                            }
                        };
                    }
#if DEBUG
                    return JsonConvert.SerializeObject(ret, Formatting.Indented); 
#else
                    return _jsonData = JsonConvert.SerializeObject(ret, Formatting.Indented);
#endif
                }
                return _jsonData;
            }
        }
        public override FName[] ExportTypes => Reader.DataExportTypes;
        public override IUExport[] Exports => Reader.DataExports;
    }
}