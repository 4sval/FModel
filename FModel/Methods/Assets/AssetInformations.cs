using FModel.Methods.MessageBox;
using FModel.Methods.Utilities;
using PakReader;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace FModel.Methods.Assets
{
    class AssetInformations
    {
        public static void OpenAssetInfos()
        {
            string infos = GetAssetInfos();
            if (DarkMessageBox.ShowYesNo(infos, FWindow.FCurrentAsset, "Copy Properties", "OK") == System.Windows.MessageBoxResult.Yes)
            {
                Clipboard.SetText(infos);

                new UpdateMyConsole(FWindow.FCurrentAsset, CColors.Blue).Append();
                new UpdateMyConsole("'s properties successfully copied", CColors.White, true).Append();
            }
        }

        private static string GetAssetInfos()
        {
            StringBuilder sb = new StringBuilder();
            PakReader.PakReader reader = AssetsUtility.GetPakReader();

            if (reader != null)
            {
                IEnumerable<FPakEntry> entriesList = AssetsUtility.GetPakEntries(reader);

                foreach (FPakEntry entry in entriesList)
                {
                    sb.Append(
                        "\n- PAK File:\t" + reader.Name +
                        "\n- Path:\t\t" + entry.Name +
                        "\n- Position:\t" + entry.Pos +
                        "\n- Size:\t\t" + entry.Size +
                        "\n- Encrypted:\t" + entry.Encrypted +
                        "\n"
                        );
                }
            }

            return sb.ToString();
        }
    }
}
