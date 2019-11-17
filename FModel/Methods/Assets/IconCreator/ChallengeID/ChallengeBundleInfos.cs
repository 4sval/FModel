using FModel.Methods.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PakReader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FModel.Methods.Assets.IconCreator.ChallengeID
{
    class ChallengeBundleInfos
    {
        public static List<BundleInfosEntry> BundleData { get; set; }

        public static void GetBundleData(JArray AssetProperties)
        {
            BundleData = new List<BundleInfosEntry>();

            JArray bundleDataArray = AssetsUtility.GetPropertyTagText<JArray>(AssetProperties, "QuestInfos", "data");
            if (bundleDataArray != null)
            {
                foreach (JToken data in bundleDataArray)
                {
                    if (data["struct_name"] != null && data["struct_type"] != null && string.Equals(data["struct_name"].Value<string>(), "FortChallengeBundleQuestEntry"))
                    {
                        JArray dataPropertiesArray = data["struct_type"]["properties"].Value<JArray>();
                        if (dataPropertiesArray != null)
                        {
                            JToken questDefinitionToken = AssetsUtility.GetPropertyTagText<JToken>(dataPropertiesArray, "QuestDefinition", "asset_path_name");
                            if (questDefinitionToken != null)
                            {
                                string path = FoldersUtility.FixFortnitePath(questDefinitionToken.Value<string>());
                                new UpdateMyProcessEvents(System.IO.Path.GetFileNameWithoutExtension(path), "Waiting").Update();
                                GetQuestData(dataPropertiesArray, path);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// this is kinda complex but at the end it only gets quest files names, counts, rewards, rewards quantity and the unlock type of the quests
        /// and repeat the process if he find stage quests
        /// </summary>
        /// <param name="BundleProperties"></param>
        /// <param name="assetPath"></param>
        private static void GetQuestData(JArray BundleProperties, string assetPath)
        {
            PakReader.PakReader reader = AssetsUtility.GetPakReader(assetPath);
            if (reader != null)
            {
                List<FPakEntry> entriesList = AssetsUtility.GetPakEntries(assetPath);
                string jsonData = AssetsUtility.GetAssetJsonData(reader, entriesList);

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

                    if (AssetMainToken != null)
                    {
                        JArray AssetProperties = AssetMainToken["properties"].Value<JArray>();
                        if (AssetProperties != null)
                        {
                            string questDescription = string.Empty;
                            long questCount = 0;
                            string unlockType = string.Empty;
                            string rewardPath = string.Empty;
                            string rewardQuantity = string.Empty;

                            //this come from the bundle properties array not the quest properties array
                            JToken questUnlockTypeToken = AssetsUtility.GetPropertyTag<JToken>(BundleProperties, "QuestUnlockType");
                            if (questUnlockTypeToken != null)
                            {
                                unlockType = questUnlockTypeToken.Value<string>();
                            }

                            //objectives array to catch the quest description and quest count
                            JArray objectivesDataArray = AssetsUtility.GetPropertyTagText<JArray>(AssetProperties, "Objectives", "data");
                            if (objectivesDataArray != null)
                            {
                                if (objectivesDataArray[0]["struct_name"] != null && objectivesDataArray[0]["struct_type"] != null && string.Equals(objectivesDataArray[0]["struct_name"].Value<string>(), "FortMcpQuestObjectiveInfo"))
                                {
                                    JArray objectivesDataProperties = objectivesDataArray[0]["struct_type"]["properties"].Value<JArray>();

                                    //this description come from the main quest array (not the objectives array)
                                    JToken description_namespace = AssetsUtility.GetPropertyTagText<JToken>(AssetProperties, "Description", "namespace");
                                    JToken description_key = AssetsUtility.GetPropertyTagText<JToken>(AssetProperties, "Description", "key");
                                    JToken description_source_string = AssetsUtility.GetPropertyTagText<JToken>(AssetProperties, "Description", "source_string");
                                    if (description_namespace != null && description_key != null && description_source_string != null)
                                    {
                                        questDescription = AssetTranslations.SearchTranslation(description_namespace.Value<string>(), description_key.Value<string>(), description_source_string.Value<string>());
                                    }
                                    else
                                    {
                                        //this description come from the objectives quest array
                                        description_namespace = AssetsUtility.GetPropertyTagText<JToken>(objectivesDataProperties, "Description", "namespace");
                                        description_key = AssetsUtility.GetPropertyTagText<JToken>(objectivesDataProperties, "Description", "key");
                                        description_source_string = AssetsUtility.GetPropertyTagText<JToken>(objectivesDataProperties, "Description", "source_string");
                                        if (description_namespace != null && description_key != null && description_source_string != null)
                                        {
                                            questDescription = AssetTranslations.SearchTranslation(description_namespace.Value<string>(), description_key.Value<string>(), description_source_string.Value<string>());
                                        }
                                    }

                                    if (objectivesDataProperties != null)
                                    {
                                        JToken countToken = AssetsUtility.GetPropertyTag<JToken>(objectivesDataProperties, "Count");
                                        if (countToken != null)
                                        {
                                            questCount = countToken.Value<long>();
                                            JToken objectiveCompletionCountToken = AssetsUtility.GetPropertyTag<JToken>(AssetProperties, "ObjectiveCompletionCount");
                                            if (objectiveCompletionCountToken != null)
                                            {
                                                questCount = objectiveCompletionCountToken.Value<long>();
                                            }
                                        }
                                    }
                                }
                            }

                            //rewards array to catch the reward name (not path) and the quantity
                            JArray rewardsDataArray = AssetsUtility.GetPropertyTagText<JArray>(AssetProperties, "Rewards", "data");
                            JArray hiddenRewardsDataArray = AssetsUtility.GetPropertyTagText<JArray>(AssetProperties, "HiddenRewards", "data");
                            if (rewardsDataArray != null)
                            {
                                if (rewardsDataArray[0]["struct_name"] != null && rewardsDataArray[0]["struct_type"] != null && string.Equals(rewardsDataArray[0]["struct_name"].Value<string>(), "FortItemQuantityPair"))
                                {
                                    try
                                    {
                                        JToken targetChecker = null;
                                        JToken targetQuantity = null;

                                        // It's individual, other challenges with tokens must be displayed?
                                        // It's checked if it remains a token because it can change in the future
                                        bool isToken = rewardsDataArray.Where(x => string.Equals(x["struct_type"]["properties"][0]["tag_data"]["struct_type"]["properties"][0]["tag_data"]["struct_type"]["properties"][0]["tag_data"].Value<string>(), "Token")).Any();
                                        if (string.Equals(System.IO.Path.GetFileName(assetPath), "Quest_S11_AlterEgo_08") && isToken)
                                        {
                                            targetChecker = rewardsDataArray.FirstOrDefault()["struct_type"]["properties"][0]["tag_data"]["struct_type"]["properties"][1]["tag_data"];
                                            targetQuantity = rewardsDataArray.FirstOrDefault()["struct_type"]["properties"][1]["tag_data"];
                                        }
                                        else
                                        {
                                            //checking the whole array for the reward
                                            //ignoring all Quest and Token until he find the reward
                                            targetChecker = rewardsDataArray.Where(x =>
                                            !string.Equals(x["struct_type"]["properties"][0]["tag_data"]["struct_type"]["properties"][0]["tag_data"]["struct_type"]["properties"][0]["tag_data"].Value<string>(), "Quest") &&
                                            !string.Equals(x["struct_type"]["properties"][0]["tag_data"]["struct_type"]["properties"][0]["tag_data"]["struct_type"]["properties"][0]["tag_data"].Value<string>(), "Token"))
                                            .FirstOrDefault()["struct_type"]["properties"][0]["tag_data"]["struct_type"]["properties"][1]["tag_data"];

                                            //checking the whole array for the reward quantity
                                            //ignoring all Quest and Token until he find the reward quantity
                                            targetQuantity = rewardsDataArray.Where(x =>
                                            !string.Equals(x["struct_type"]["properties"][0]["tag_data"]["struct_type"]["properties"][0]["tag_data"]["struct_type"]["properties"][0]["tag_data"].Value<string>(), "Quest") &&
                                            !string.Equals(x["struct_type"]["properties"][0]["tag_data"]["struct_type"]["properties"][0]["tag_data"]["struct_type"]["properties"][0]["tag_data"].Value<string>(), "Token"))
                                            .FirstOrDefault()["struct_type"]["properties"][1]["tag_data"];
                                        }

                                        if (targetChecker != null)
                                        {
                                            //this will catch the full path if asset exists to be able to grab his PakReader and List<FPakEntry>
                                            string primaryAssetNameFullPath = AssetEntries.AssetEntriesDict.Where(x => x.Key.ToLowerInvariant().Contains("/" + targetChecker.Value<string>().ToLowerInvariant() + ".uasset")).Select(d => d.Key).FirstOrDefault();
                                            if (!string.IsNullOrEmpty(primaryAssetNameFullPath))
                                            {
                                                rewardPath = primaryAssetNameFullPath.Substring(0, primaryAssetNameFullPath.LastIndexOf(".", StringComparison.InvariantCultureIgnoreCase));
                                            }

                                            if (targetQuantity != null)
                                            {
                                                rewardQuantity = targetQuantity.Value<string>();
                                            }

                                            BundleInfosEntry currentData = new BundleInfosEntry(questDescription, questCount, unlockType, rewardPath, rewardQuantity);
                                            if (!BundleData.Any(item => item.TheQuestDescription.Equals(currentData.TheQuestDescription, StringComparison.InvariantCultureIgnoreCase) && item.TheQuestCount == currentData.TheQuestCount))
                                            {
                                                BundleData.Add(currentData);
                                            }
                                        }
                                        else
                                        {
                                            BundleInfosEntry currentData = new BundleInfosEntry(questDescription, questCount, unlockType, "", "");
                                            if (!BundleData.Any(item => item.TheQuestDescription.Equals(currentData.TheQuestDescription, StringComparison.InvariantCultureIgnoreCase) && item.TheQuestCount == currentData.TheQuestCount))
                                            {
                                                BundleData.Add(currentData);
                                            }
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        if (hiddenRewardsDataArray != null)
                                        {
                                            if (hiddenRewardsDataArray[0]["struct_name"] != null && hiddenRewardsDataArray[0]["struct_type"] != null && string.Equals(hiddenRewardsDataArray[0]["struct_name"].Value<string>(), "FortHiddenRewardQuantityPair"))
                                            {
                                                JArray hiddenRewardPropertiesArray = hiddenRewardsDataArray[0]["struct_type"]["properties"].Value<JArray>();
                                                if (hiddenRewardPropertiesArray != null)
                                                {
                                                    JToken templateIdToken = AssetsUtility.GetPropertyTag<JToken>(hiddenRewardPropertiesArray, "TemplateId");
                                                    if (templateIdToken != null)
                                                    {
                                                        rewardPath = templateIdToken.Value<string>();
                                                    }

                                                    //reward quantity (if 1, this won't be displayed)
                                                    JToken hiddenQuantityToken = AssetsUtility.GetPropertyTag<JToken>(hiddenRewardPropertiesArray, "Quantity");
                                                    if (hiddenQuantityToken != null)
                                                    {
                                                        rewardQuantity = hiddenQuantityToken.Value<string>();
                                                    }

                                                    BundleInfosEntry currentData = new BundleInfosEntry(questDescription, questCount, unlockType, rewardPath, rewardQuantity);
                                                    if (!BundleData.Any(item => item.TheQuestDescription.Equals(currentData.TheQuestDescription, StringComparison.InvariantCultureIgnoreCase) && item.TheQuestCount == currentData.TheQuestCount))
                                                    {
                                                        BundleData.Add(currentData);
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            BundleInfosEntry currentData = new BundleInfosEntry(questDescription, questCount, unlockType, "", "");
                                            if (!BundleData.Any(item => item.TheQuestDescription.Equals(currentData.TheQuestDescription, StringComparison.InvariantCultureIgnoreCase) && item.TheQuestCount == currentData.TheQuestCount))
                                            {
                                                BundleData.Add(currentData);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                BundleInfosEntry currentData = new BundleInfosEntry(questDescription, questCount, unlockType, "", "");
                                if (!BundleData.Any(item => item.TheQuestDescription.Equals(currentData.TheQuestDescription, StringComparison.InvariantCultureIgnoreCase) && item.TheQuestCount == currentData.TheQuestCount))
                                {
                                    BundleData.Add(currentData);
                                }
                            }

                            //catch stage AFTER adding the current quest to the list
                            if (rewardsDataArray != null)
                            {
                                foreach (JToken token in rewardsDataArray)
                                {
                                    JToken targetChecker = token["struct_type"]["properties"][0]["tag_data"]["struct_type"]["properties"][0]["tag_data"]["struct_type"]["properties"][0]["tag_data"];
                                    if (targetChecker != null && string.Equals(targetChecker.Value<string>(), "Quest"))
                                    {
                                        JToken primaryAssetNameToken = token["struct_type"]["properties"][0]["tag_data"]["struct_type"]["properties"][1]["tag_data"];
                                        if (primaryAssetNameToken != null)
                                        {
                                            //this will catch the full path if asset exists to be able to grab his PakReader and List<FPakEntry>
                                            string primaryAssetNameFullPath = AssetEntries.AssetEntriesDict.Where(x => x.Key.Contains("/" + primaryAssetNameToken.Value<string>())).Select(d => d.Key).FirstOrDefault();
                                            if (!string.IsNullOrEmpty(primaryAssetNameFullPath))
                                            {
                                                new UpdateMyProcessEvents(System.IO.Path.GetFileNameWithoutExtension(primaryAssetNameFullPath), "Waiting").Update();
                                                GetQuestData(BundleProperties, primaryAssetNameFullPath.Substring(0, primaryAssetNameFullPath.LastIndexOf(".", StringComparison.InvariantCultureIgnoreCase)));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
