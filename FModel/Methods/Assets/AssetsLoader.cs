using FModel.Methods.SyntaxHighlighter;
using FModel.Methods.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PakReader;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Windows.Media;

namespace FModel.Methods.Assets
{
    static class AssetsLoader
    {
        public static bool isRunning = false;
        public static string ExportType { get; set; }

        public static async Task LoadSelectedAsset()
        {
            new UpdateMyProcessEvents("", "").Update();
            FWindow.FMain.Button_Extract.IsEnabled = false;
            FWindow.FMain.Button_Stop.IsEnabled = true;
            FWindow.FMain.AssetPropertiesBox_Main.Text = string.Empty;
            FWindow.FMain.AssetPropertiesBox_Main.SyntaxHighlighting = ResourceLoader.LoadHighlightingDefinition("Json.xshd");
            FWindow.FMain.ImageBox_Main.Source = null;

            string[] selectedItems = FWindow.FMain.ListBox_Main.SelectedItems.OfType<string>().ToArray(); //store selected items, doesn't crash if user select another item while extracting
            string treePath = TreeViewUtility.GetFullPath(FWindow.TVItem); //never change in the loop, in case user wanna see other folders while extracting

            TasksUtility.CancellableTaskTokenSource = new CancellationTokenSource();
            CancellationToken cToken = TasksUtility.CancellableTaskTokenSource.Token;
            await Task.Run(() =>
            {
                isRunning = true;
                foreach (string item in selectedItems)
                {
                    cToken.ThrowIfCancellationRequested(); //if clicked on 'Stop' it breaks at the following item
                    FWindow.FMain.Dispatcher.InvokeAsync(() => //ui thread because if not, FCurrentAsset isn't updated in time to Auto Save a JSON Data for example
                    {
                        FWindow.FCurrentAsset = item;
                    });

                    LoadAsset(treePath + "/" + item);
                }

            }, cToken).ContinueWith(TheTask =>
            {
                TasksUtility.TaskCompleted(TheTask.Exception);
                TasksUtility.CancellableTaskTokenSource.Dispose();
                isRunning = false;
            });

            FWindow.FMain.Button_Extract.IsEnabled = true;
            FWindow.FMain.Button_Stop.IsEnabled = false;
        }
        public static async Task ExtractFoldersAndSub(string path)
        {
            new UpdateMyProcessEvents("", "").Update();
            FWindow.FMain.Button_Extract.IsEnabled = false;
            FWindow.FMain.Button_Stop.IsEnabled = true;
            FWindow.FMain.AssetPropertiesBox_Main.Text = string.Empty;
            FWindow.FMain.AssetPropertiesBox_Main.SyntaxHighlighting = ResourceLoader.LoadHighlightingDefinition("Json.xshd");
            FWindow.FMain.ImageBox_Main.Source = null;

            List<IEnumerable<string>> assetList = new List<IEnumerable<string>>();
            if (!string.IsNullOrEmpty(FWindow.FCurrentPAK))
            {
                IEnumerable<string> files = PAKEntries.PAKToDisplay[FWindow.FCurrentPAK]
                    .Where(x => x.Name.StartsWith(path + "/"))
                    .Select(x => x.Name);

                if (files != null) { assetList.Add(files); }
            }
            else
            {
                foreach (FPakEntry[] PAKsFileInfos in PAKEntries.PAKToDisplay.Values)
                {
                    IEnumerable<string> files = PAKsFileInfos
                        .Where(x => x.Name.StartsWith(path + "/"))
                        .Select(x => x.Name);

                    if (files != null) { assetList.Add(files); }
                }
            }

            TasksUtility.CancellableTaskTokenSource = new CancellationTokenSource();
            CancellationToken cToken = TasksUtility.CancellableTaskTokenSource.Token;
            await Task.Run(() =>
            {
                isRunning = true;
                foreach (IEnumerable<string> filesFromOnePak in assetList)
                {
                    foreach (string asset in filesFromOnePak.OrderBy(s => s))
                    {
                        cToken.ThrowIfCancellationRequested(); //if clicked on 'Stop' it breaks at the following item

                        string target;
                        if (asset.EndsWith(".uexp") || asset.EndsWith(".ubulk")) { continue; }
                        else if (!asset.EndsWith(".uasset"))
                        {
                            target = asset; //ini uproject locres etc
                        }
                        else
                        {
                            target = asset.Substring(0, asset.LastIndexOf(".")); //uassets
                        }

                        FWindow.FMain.Dispatcher.InvokeAsync(() => //ui thread because if not, FCurrentAsset isn't updated in time to Auto Save a JSON Data for example
                        {
                            FWindow.FCurrentAsset = Path.GetFileName(target);
                        });
                        LoadAsset(target);
                    }
                }

            }, cToken).ContinueWith(TheTask =>
            {
                TasksUtility.TaskCompleted(TheTask.Exception);
                TasksUtility.CancellableTaskTokenSource.Dispose();
                isRunning = false;
            });

            FWindow.FMain.Button_Extract.IsEnabled = true;
            FWindow.FMain.Button_Stop.IsEnabled = false;
        }
        public static async Task ExtractUpdateMode()
        {
            new UpdateMyProcessEvents("", "").Update();
            FWindow.FMain.MI_UpdateMode.IsEnabled = true;
            FWindow.FMain.Button_Extract.IsEnabled = false;
            FWindow.FMain.Button_Stop.IsEnabled = true;
            FWindow.FMain.AssetPropertiesBox_Main.Text = string.Empty;
            FWindow.FMain.AssetPropertiesBox_Main.SyntaxHighlighting = ResourceLoader.LoadHighlightingDefinition("Json.xshd");
            FWindow.FMain.ImageBox_Main.Source = null;

            List<IEnumerable<string>> assetList = new List<IEnumerable<string>>();
            foreach (FPakEntry[] PAKsFileInfos in PAKEntries.PAKToDisplay.Values)
            {
                IEnumerable<string> files = PAKsFileInfos
                    .Where(x => Forms.FModel_UpdateMode.AssetsEntriesDict.Any(c => bool.Parse(c.Value["isChecked"]) && x.Name.StartsWith(c.Value["Path"])))
                    .Select(x => x.Name);

                if (files != null) { assetList.Add(files); }
            }

            TasksUtility.CancellableTaskTokenSource = new CancellationTokenSource();
            CancellationToken cToken = TasksUtility.CancellableTaskTokenSource.Token;
            await Task.Run(() =>
            {
                isRunning = true;
                foreach (IEnumerable<string> filesFromOnePak in assetList)
                {
                    foreach (string asset in filesFromOnePak.OrderBy(s => s))
                    {
                        cToken.ThrowIfCancellationRequested(); //if clicked on 'Stop' it breaks at the following item

                        string target;
                        if (asset.EndsWith(".uexp") || asset.EndsWith(".ubulk")) { continue; }
                        else if (!asset.EndsWith(".uasset"))
                        {
                            target = asset; //ini uproject locres etc
                        }
                        else
                        {
                            target = asset.Substring(0, asset.LastIndexOf(".")); //uassets
                        }

                        FWindow.FMain.Dispatcher.InvokeAsync(() => //ui thread because if not, FCurrentAsset isn't updated in time to Auto Save a JSON Data for example
                        {
                            FWindow.FCurrentAsset = Path.GetFileName(target);
                        });
                        LoadAsset(target);
                    }
                }

            }, cToken).ContinueWith(TheTask =>
            {
                TasksUtility.TaskCompleted(TheTask.Exception);
                TasksUtility.CancellableTaskTokenSource.Dispose();
                isRunning = false;
            });

            FWindow.FMain.MI_Auto_Save_Images.IsChecked = false;
            FWindow.FMain.Button_Extract.IsEnabled = true;
            FWindow.FMain.Button_Stop.IsEnabled = false;
            new UpdateMyProcessEvents("All assets have been extracted successfully", "Success").Update();
        }

        public static void LoadAsset(string assetPath)
        {
            PakReader.PakReader reader = AssetsUtility.GetPakReader(assetPath);
            if (reader != null)
            {
                List<FPakEntry> entriesList = AssetsUtility.GetPakEntries(assetPath);
                string jsonData = AssetsUtility.GetAssetJsonData(reader, entriesList, true);
                FWindow.FMain.Dispatcher.InvokeAsync(() =>
                {
                    FWindow.FMain.AssetPropertiesBox_Main.Text = jsonData;
                });

                if (AssetsUtility.IsValidJson(jsonData))
                {
                    dynamic AssetData = JsonConvert.DeserializeObject(jsonData);
                    JToken AssetMainToken;
                    if (jsonData.StartsWith("[") && jsonData.EndsWith("]"))
                    {
                        JArray AssetArray = JArray.FromObject(AssetData);
                        AssetMainToken = AssetArray[0];
                    }
                    else if (jsonData.StartsWith("{") && jsonData.EndsWith("}"))
                    {
                        AssetMainToken = AssetData;
                    }
                    else
                    {
                        AssetMainToken = null;
                    }

                    
                    if (AssetMainToken != null && AssetMainToken["export_type"] != null && AssetMainToken["properties"] != null)
                    {
                        ExportType = AssetMainToken["export_type"].Value<string>();
                        DrawingVisual VisualImage = null;
                        switch (ExportType)
                        {
                            case "AthenaBackpackItemDefinition":
                            case "AthenaBattleBusItemDefinition":
                            case "AthenaCharacterItemDefinition":
                            case "AthenaConsumableEmoteItemDefinition":
                            case "AthenaSkyDiveContrailItemDefinition":
                            case "AthenaDanceItemDefinition":
                            case "AthenaEmojiItemDefinition":
                            case "AthenaGliderItemDefinition":
                            case "AthenaItemWrapDefinition":
                            case "AthenaLoadingScreenItemDefinition":
                            case "AthenaMusicPackItemDefinition":
                            case "AthenaPetCarrierItemDefinition":
                            case "AthenaPickaxeItemDefinition":
                            case "AthenaSprayItemDefinition":
                            case "AthenaToyItemDefinition":
                            case "AthenaVictoryPoseItemDefinition":
                            case "FortBannerTokenType":
                            case "AthenaGadgetItemDefinition":
                            case "FortWeaponRangedItemDefinition":
                            case "FortWeaponMeleeItemDefinition":
                            case "FortWeaponMeleeDualWieldItemDefinition":
                            case "FortIngredientItemDefinition":
                            case "FortVariantTokenType":
                            case "FortAmmoItemDefinition":
                            case "FortHeroType":
                            case "FortDefenderItemDefinition":
                            case "FortContextTrapItemDefinition":
                            case "FortTrapItemDefinition":
                            case "FortCardPackItemDefinition":
                            case "FortPlaysetGrenadeItemDefinition":
                            case "FortConsumableAccountItemDefinition":
                            case "FortBadgeItemDefinition":
                            case "FortCurrencyItemDefinition":
                            case "FortConversionControlItemDefinition":
                            case "FortHomebaseNodeItemDefinition":
                            case "FortPersonalVehicleItemDefinition":
                            case "FortCampaignHeroLoadoutItemDefinition":
                            case "FortNeverPersistItemDefinition":
                            case "FortPersistentResourceItemDefinition":
                            case "FortResourceItemDefinition":
                            case "FortGadgetItemDefinition":
                            case "FortStatItemDefinition":
                            case "FortTokenType":
                            case "FortDailyRewardScheduleTokenDefinition":
                            case "FortWorkerType":
                            case "FortConditionalResourceItemDefinition":
                            case "FortAwardItemDefinition":
                            case "FortChallengeBundleScheduleDefinition":
                            case "FortAbilityKit":
                            case "FortSchematicItemDefinition":
                            case "FortAccoladeItemDefinition":
                                VisualImage = IconCreator.IconCreator.DrawNormalIconKThx(AssetMainToken["properties"].Value<JArray>());
                                break;
                            case "FortChallengeBundleItemDefinition":
                                VisualImage = IconCreator.IconCreator.DrawChallengeKThx(AssetMainToken["properties"].Value<JArray>(), assetPath);
                                break;
                        }
                        if (VisualImage != null) { ImagesUtility.LoadImageAfterExtraction(VisualImage); }
                    }
                }
            }
        }
    }
}
