using csharp_wick;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace FModel
{
    static class BundleInfos
    {
        public static List<BundleInfoEntry> BundleData { get; set; }
        public static Color getSecondaryColor(JToken myBundle)
        {
            int Red = 0;
            int Green = 0;
            int Blue = 0;

            JToken displayStyle = myBundle["DisplayStyle"];
            if (displayStyle != null)
            {
                JToken secondaryColor = displayStyle["SecondaryColor"];
                if (secondaryColor != null)
                {
                    JToken r = secondaryColor["r"];
                    JToken g = secondaryColor["g"];
                    JToken b = secondaryColor["b"];
                    if (r != null && g != null && b != null)
                    {
                        Red = (int)(r.Value<double>() * 255);
                        Green = (int)(g.Value<double>() * 255);
                        Blue = (int)(b.Value<double>() * 255);
                    }
                }
            }

            if (Red + Green + Blue <= 75 || getLastFolder(BundleDesign.BundlePath) == "LTM") { return getAccentColor(myBundle); }
            else { return Color.FromArgb(255, Red, Green, Blue); }
        }
        public static Color getAccentColor(JToken myBundle)
        {
            int Red = 0;
            int Green = 0;
            int Blue = 0;

            JToken displayStyle = myBundle["DisplayStyle"];
            if (displayStyle != null)
            {
                JToken accentColor = displayStyle["AccentColor"];
                if (accentColor != null)
                {
                    JToken r = accentColor["r"];
                    JToken g = accentColor["g"];
                    JToken b = accentColor["b"];
                    if (r != null && g != null && b != null)
                    {
                        Red = (int)(r.Value<double>() * 255);
                        Green = (int)(g.Value<double>() * 255);
                        Blue = (int)(b.Value<double>() * 255);
                    }
                }
            }

            return Color.FromArgb(255, Red, Green, Blue);
        }
        public static string getBundleDisplayName(JToken theItem)
        {
            string text = string.Empty;

            JToken displayName = theItem["DisplayName"];
            if (displayName != null)
            {
                JToken key = displayName["key"];
                JToken sourceString = displayName["source_string"];
                if (key != null && sourceString != null)
                {
                    text = SearchResource.getTextByKey(key.Value<string>(), sourceString.Value<string>());
                }
            }

            return text.ToUpper();
        }
        public static string getLastFolder(string pathToExtractedBundle)
        {
            string folderAndFileNameWithExtension = pathToExtractedBundle.Substring(pathToExtractedBundle.Substring(0, pathToExtractedBundle.LastIndexOf("\\", StringComparison.Ordinal)).LastIndexOf("\\", StringComparison.Ordinal) + 1).ToUpper();
            return folderAndFileNameWithExtension.Substring(0, folderAndFileNameWithExtension.LastIndexOf("\\", StringComparison.Ordinal)); //just the folder now
        }

        /// <summary>
        /// main method to set the data to get it out of this class
        /// foreach questfile to getQuestData()
        /// </summary>
        /// <param name="myBundle"></param>
        public static void getBundleData(JToken myBundle)
        {
            BundleData = new List<BundleInfoEntry>();

            JToken questInfos = myBundle["QuestInfos"];
            if (questInfos != null)
            {
                JArray questInfosArray = questInfos.Value<JArray>();
                foreach (JToken token in questInfosArray)
                {
                    JToken questDefinition = token["QuestDefinition"];
                    if (questDefinition != null)
                    {
                        JToken assetPathName = questDefinition["asset_path_name"];
                        if (assetPathName != null)
                        {
                            string questName = Path.GetFileName(assetPathName.Value<string>()).Substring(0, Path.GetFileName(assetPathName.Value<string>()).LastIndexOf(".", StringComparison.Ordinal));
                            getQuestData(questName, token);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// extract quest and add description, count, reward item, reward quantity to List<BundleInfoEntry> BundleData
        /// loop if stage exist
        /// </summary>
        /// <param name="questFile"></param>
        private static void getQuestData(string questFile, JToken questInfo)
        {
            try
            {
                string questFilePath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary[questFile], questFile);
                if (questFilePath != null)
                {
                    if (questFilePath.Contains(".uasset") || questFilePath.Contains(".uexp") || questFilePath.Contains(".ubulk"))
                    {
                        JohnWick.MyAsset = new PakAsset(questFilePath.Substring(0, questFilePath.LastIndexOf('.')));
                        try
                        {
                            if (JohnWick.MyAsset.GetSerialized() != null)
                            {
                                new UpdateMyState("Parsing " + questFile + "...", "Waiting").ChangeProcessState();

                                //prestige challenge check
                                JToken questUnlockType = questInfo["QuestUnlockType"];
                                string unlockType = string.Empty;
                                if (questUnlockType != null && questUnlockType.Value<string>().Equals("EChallengeBundleQuestUnlockType::BundleLevelup"))
                                {
                                    unlockType = questUnlockType.Value<string>();
                                }

                                dynamic AssetData = JsonConvert.DeserializeObject(JohnWick.MyAsset.GetSerialized());
                                JArray AssetArray = JArray.FromObject(AssetData);

                                //fortbyte check
                                JToken rewards = AssetArray[0]["Rewards"];
                                JToken assetTypeToken = null;
                                bool isFortbyte = false;
                                if (rewards != null)
                                {
                                    JArray rewardsArray = rewards.Value<JArray>();
                                    assetTypeToken = rewardsArray.Where(item => item["ItemPrimaryAssetId"]["PrimaryAssetType"]["Name"].Value<string>().Equals("Token")).FirstOrDefault();
                                    if (assetTypeToken != null)
                                    {
                                        isFortbyte = rewardsArray.Any(item => item["ItemPrimaryAssetId"]["PrimaryAssetName"].Value<string>().Equals("AthenaFortbyte"));
                                    }
                                }

                                JToken objectives = AssetArray[0]["Objectives"];
                                if (objectives != null)
                                {
                                    long questCount = 0;
                                    string descriptionKey = string.Empty;
                                    string descriptionSource = string.Empty;

                                    JArray objectivesArray = objectives.Value<JArray>();
                                    foreach (JToken token in objectivesArray)
                                    {
                                        //quest count
                                        JToken count = token["Count"];
                                        if (count != null)
                                        {
                                            questCount = count.Value<long>();
                                            JToken objectiveCompletionCount = AssetArray[0]["ObjectiveCompletionCount"];
                                            if (objectiveCompletionCount != null && objectiveCompletionCount.Value<long>() > 0)
                                            {
                                                questCount = objectiveCompletionCount.Value<long>();
                                            }
                                        }

                                        //quest description
                                        JToken description = token["Description"];
                                        if (description != null)
                                        {
                                            JToken key = description["key"];
                                            JToken sourceString = description["source_string"];
                                            if (key != null && sourceString != null)
                                            {
                                                descriptionKey = key.Value<string>();
                                                descriptionSource = sourceString.Value<string>();
                                            }
                                        }
                                        JToken descriptionMain = AssetArray[0]["Description"];
                                        if (descriptionMain != null)
                                        {
                                            JToken key = descriptionMain["key"];
                                            JToken sourceString = descriptionMain["source_string"];
                                            if (key != null && sourceString != null)
                                            {
                                                descriptionKey = key.Value<string>();
                                                descriptionSource = sourceString.Value<string>();
                                            }
                                        }
                                    }

                                    string questDescription = SearchResource.getTextByKey(descriptionKey, descriptionSource);
                                    if (string.IsNullOrEmpty(questDescription)) { questDescription = " "; }

                                    if (rewards != null && !isFortbyte)
                                    {
                                        //quest rewards
                                        JArray rewardsArray = rewards.Value<JArray>();
                                        try
                                        {
                                            string rewardId = rewardsArray.Where(item => !item["ItemPrimaryAssetId"]["PrimaryAssetType"]["Name"].Value<string>().Equals("Quest") && !item["ItemPrimaryAssetId"]["PrimaryAssetType"]["Name"].Value<string>().Equals("Token")).FirstOrDefault()["ItemPrimaryAssetId"]["PrimaryAssetName"].Value<string>();
                                            string rewardQuantity = rewardsArray.Where(item => !item["ItemPrimaryAssetId"]["PrimaryAssetType"]["Name"].Value<string>().Equals("Quest") && !item["ItemPrimaryAssetId"]["PrimaryAssetType"]["Name"].Value<string>().Equals("Token")).FirstOrDefault()["Quantity"].Value<string>();

                                            BundleInfoEntry currentData = new BundleInfoEntry(questDescription, questCount, rewardId, rewardQuantity, unlockType);
                                            bool isAlreadyAdded = BundleData.Any(item => item.questDescr.Equals(currentData.questDescr, StringComparison.InvariantCultureIgnoreCase) && item.questCount == currentData.questCount);
                                            if (!isAlreadyAdded) { BundleData.Add(currentData); }
                                        }
                                        catch (NullReferenceException)
                                        {
                                            JToken hiddenRewards = AssetArray[0]["HiddenRewards"];
                                            if (hiddenRewards != null)
                                            {
                                                string rewardId = hiddenRewards[0]["TemplateId"].Value<string>();
                                                string rewardQuantity = hiddenRewards[0]["Quantity"].Value<string>();

                                                BundleInfoEntry currentData = new BundleInfoEntry(questDescription, questCount, rewardId, rewardQuantity, unlockType);
                                                bool isAlreadyAdded = BundleData.Any(item => item.questDescr.Equals(currentData.questDescr, StringComparison.InvariantCultureIgnoreCase) && item.questCount == currentData.questCount);
                                                if (!isAlreadyAdded) { BundleData.Add(currentData); }
                                            }
                                        }

                                        //quest stage
                                        foreach (JToken token in rewardsArray)
                                        {
                                            string qAssetType = token["ItemPrimaryAssetId"]["PrimaryAssetType"]["Name"].Value<string>();
                                            string qAssetName = token["ItemPrimaryAssetId"]["PrimaryAssetName"].Value<string>();

                                            if (qAssetType == "Quest")
                                            {
                                                getQuestData(qAssetName, questInfo);
                                            }
                                        }
                                    }
                                    else if (isFortbyte && assetTypeToken != null)
                                    {
                                        //thank you Quest_BR_S9_Fortbyte_04
                                        JToken weight = AssetArray[0]["Weight"];
                                        JToken weightToUse = null;
                                        if (weight != null)
                                        {
                                            weightToUse = weight;
                                        }

                                        BundleInfoEntry currentData = new BundleInfoEntry(questDescription, questCount, assetTypeToken["ItemPrimaryAssetId"]["PrimaryAssetName"].Value<string>(), weightToUse == null ? "01" : weightToUse.Value<string>(), unlockType);
                                        bool isAlreadyAdded = BundleData.Any(item => item.questDescr.Equals(currentData.questDescr, StringComparison.InvariantCultureIgnoreCase) && item.questCount == currentData.questCount);
                                        if (!isAlreadyAdded) { BundleData.Add(currentData); }
                                    }
                                    else
                                    {
                                        BundleInfoEntry currentData = new BundleInfoEntry(questDescription, questCount, "", "", unlockType);
                                        bool isAlreadyAdded = BundleData.Any(item => item.questDescr.Equals(currentData.questDescr, StringComparison.InvariantCultureIgnoreCase) && item.questCount == currentData.questCount);
                                        if (!isAlreadyAdded) { BundleData.Add(currentData); }
                                    }
                                }
                            }
                        }
                        catch (JsonSerializationException)
                        {
                            //do not crash when JsonSerialization does weird stuff
                        }
                    }
                }
            }
            catch (KeyNotFoundException)
            {
                new UpdateMyConsole("[FModel] Can't extract " + questFile, Color.Red, true);
            }
        }
    }
}
