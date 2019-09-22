using FModel.Methods.SyntaxHighlighter;
using FModel.Methods.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

                if (AssetsUtility.IsValidJson(jsonData))
                {
                    dynamic AssetData = JsonConvert.DeserializeObject(jsonData);
                    JToken AssetMainToken = null;
                    if (jsonData.StartsWith("[") && jsonData.EndsWith("]"))
                    {
                        JArray AssetArray = JArray.FromObject(AssetData);
                        AssetMainToken = AssetArray[0];
                    }
                    else if (jsonData.StartsWith("{") && jsonData.EndsWith("}"))
                    {
                        AssetMainToken = AssetData;
                    }

                    switch (AssetMainToken != null ? AssetMainToken["export_type"] != null ? AssetMainToken["export_type"].Value<string>() : string.Empty : string.Empty)
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
                            IconCreator.IconCreator.DrawTest(AssetMainToken["properties"].Value<JArray>());
                            break;
                        case "FortWeaponRangedItemDefinition":
                        case "FortWeaponMeleeItemDefinition":
                        case "FortIngredientItemDefinition":
                            break;
                        case "FortVariantTokenType":
                            break;
                        case "FortAmmoItemDefinition":
                            break;
                        case "FortHeroType":
                            break;
                        case "FortDefenderItemDefinition":
                            break;
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
                            break;
                        case "FortChallengeBundleItemDefinition":
                            break;
                        case "FortSchematicItemDefinition":
                            break;
                        case "SoundWave":
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }
}
