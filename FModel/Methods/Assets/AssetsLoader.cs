using FModel.Methods.SyntaxHighlighter;
using FModel.Methods.Utilities;
using PakReader;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FModel.Methods.Assets
{
    class AssetsLoader
    {
        public static async void LoadSelectedAsset()
        {
            FWindow.FMain.Button_Extract.IsEnabled = false;
            FWindow.FMain.Button_Stop.IsEnabled = true;
            FWindow.FMain.AssetPropertiesBox_Main.Text = string.Empty;
            FWindow.FMain.AssetPropertiesBox_Main.SyntaxHighlighting = ResourceLoader.LoadHighlightingDefinition("Json.xshd");
            FWindow.FMain.ImageBox_Main.Source = null;

            IList selectedItems = FWindow.FMain.ListBox_Main.SelectedItems;
            TasksUtility.CancellableTaskTokenSource = new CancellationTokenSource();
            CancellationToken cToken = TasksUtility.CancellableTaskTokenSource.Token;
            await Task.Run(() =>
            {
                foreach (object item in selectedItems)
                {
                    cToken.ThrowIfCancellationRequested(); //if clicked on 'Stop' it breaks at the following item

                    FWindow.FCurrentAsset = item.ToString();
                    LoadAsset();
                }

            }, cToken).ContinueWith(TheTask =>
            {
                TasksUtility.TaskCompleted(TheTask.Exception);
                TasksUtility.CancellableTaskTokenSource.Dispose();
            });

            FWindow.FMain.Button_Extract.IsEnabled = true;
            FWindow.FMain.Button_Stop.IsEnabled = false;
        }

        private static void LoadAsset()
        {
            PakReader.PakReader reader = AssetsUtility.GetPakReader();
            if (reader != null)
            {
                IEnumerable<FPakEntry> entriesList = AssetsUtility.GetPakEntries(reader);
                string jsonData = AssetsUtility.GetAssetJsonData(reader, entriesList, true);

                FWindow.FMain.Dispatcher.InvokeAsync(() =>
                {
                    FWindow.FMain.AssetPropertiesBox_Main.Text = jsonData;
                });
            }
        }
    }
}
