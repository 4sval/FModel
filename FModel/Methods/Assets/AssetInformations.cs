using FModel.Methods.MessageBox;
using FModel.Methods.Utilities;
using PakReader;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;

namespace FModel.Methods.Assets
{
    static class AssetInformations
    {
        public static void OpenAssetInfos(bool isFromDataGrid = false)
        {
            string infos = GetAssetInfos(isFromDataGrid);
            if (DarkMessageBox.ShowYesNo(infos, FWindow.FCurrentAsset, "Copy Properties", "OK") == System.Windows.MessageBoxResult.Yes)
            {
                Clipboard.SetText(infos);

                new UpdateMyConsole(FWindow.FCurrentAsset, CColors.Blue).Append();
                new UpdateMyConsole("'s properties successfully copied", CColors.White, true).Append();
            }
        }

        private static string GetAssetInfos(bool isFromDataGrid = false)
        {
            StringBuilder sb = new StringBuilder();

            string fullPath = isFromDataGrid ? FWindow.FCurrentAsset : TreeViewUtility.GetFullPath(FWindow.TVItem) + "/" + FWindow.FCurrentAsset;
            DebugHelper.WriteLine("Assets: Gathering info about {0}", fullPath);
            PakReader.PakReader reader = AssetsUtility.GetPakReader(fullPath);
            if (reader != null)
            {
                List<FPakEntry> entriesList = AssetsUtility.GetPakEntries(fullPath);
                foreach (FPakEntry entry in entriesList)
                {
                    sb.Append(
                        "\n- PAK File:\t" + Path.GetFileName(reader.Name) +
                        "\n- Path:\t\t" + entry.Name +
                        "\n- Position:\t" + entry.Pos +
                        "\n- Size:\t\t" + AssetsUtility.GetReadableSize(entry.UncompressedSize) +
                        "\n- Encrypted:\t" + entry.Encrypted +
                        "\n"
                        );
                }
            }

            if (isFromDataGrid)
            {
                string selectedName = fullPath.Substring(fullPath.LastIndexOf("/") + 1);
                if (selectedName.EndsWith(".uasset"))
                {
                    selectedName = selectedName.Substring(0, selectedName.LastIndexOf('.'));
                }
                FWindow.FCurrentAsset = selectedName;
            }
            return sb.ToString();
        }
    }
}
