using AutoUpdaterDotNET;
using FModel.Logger;
using FModel.Utils;
using FModel.Windows.CustomNotifier;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace FModel.Grabber.Cdn
{
    static class CdnData
    {
        public static readonly bool bInternet = NetworkInterface.GetIsNetworkAvailable();

        public static async Task<CdnResponse> GetData()
        {
            if (bInternet)
                return await Endpoints.GetJsonEndpoint<CdnResponse>(Endpoints.FMODEL_JSON).ConfigureAwait(false);
            else
            {
                Globals.gNotifier.ShowCustomMessage("CDN", Properties.Resources.NoInternet, "/FModel;component/Resources/wifi-strength-off.ico");
                DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[CDN]", "No internet");
                return null;
            }
        }
    }

    public class CdnResponse
    {
        public Dictionary<string, Backup[]> Backups { get; set; }
        public Dictionary<string, GlobalMessage[]> GlobalMessages { get; set; }
        public FUpdater Updater { get; set; }
    }
    public class Backup
    {
        public string Header { get; set; }
        public string DownloadUrl { get; set; }
        public double Size { get; set; }
    }
    public class GlobalMessage
    {
        public string Message { get; set; }
        public string Color { get; set; }
        public bool NewLine { get; set; }
    }
    public class FUpdater
    {
        public string Version { get; set; }
        public string Url { get; set; }
        public string Changelog { get; set; }
        public Mandatory Mandatory { get; set; }
    }
}
