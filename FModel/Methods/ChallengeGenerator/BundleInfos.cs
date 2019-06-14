using csharp_wick;
using FModel.Parser.Challenges;
using FModel.Parser.Items;
using FModel.Parser.Quests;
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
        public static Color getSecondaryColor(ChallengeBundleIdParser myBundle)
        {
            int Red = (int)(myBundle.DisplayStyle.SecondaryColor.R * 255);
            int Green = (int)(myBundle.DisplayStyle.SecondaryColor.G * 255);
            int Blue = (int)(myBundle.DisplayStyle.SecondaryColor.B * 255);

            if (Red + Green + Blue <= 75 || getLastFolder(BundleDesign.BundlePath) == "LTM") { return getAccentColor(myBundle); }
            else { return Color.FromArgb(255, Red, Green, Blue); }
        }
        public static Color getAccentColor(ChallengeBundleIdParser myBundle)
        {
            int Red = (int)(myBundle.DisplayStyle.AccentColor.R * 255);
            int Green = (int)(myBundle.DisplayStyle.AccentColor.G * 255);
            int Blue = (int)(myBundle.DisplayStyle.AccentColor.B * 255);

            return Color.FromArgb(255, Red, Green, Blue);
        }
        public static string getBundleDisplayName(ItemsIdParser theItem)
        {
            return theItem.DisplayName.ToUpper();
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
        public static void getBundleData(ChallengeBundleIdParser myBundle)
        {
            BundleData = new List<BundleInfoEntry>();

            for (int i = 0; i < myBundle.QuestInfos.Length; i++)
            {
                string questName = Path.GetFileName(myBundle.QuestInfos[i].QuestDefinition.AssetPathName).Substring(0, Path.GetFileName(myBundle.QuestInfos[i].QuestDefinition.AssetPathName).LastIndexOf(".", StringComparison.Ordinal));
                getQuestData(questName);
            }
        }

        /// <summary>
        /// extract quest and add description, count, reward item, reward quantity to List<BundleInfoEntry> BundleData
        /// loop if stage exist
        /// </summary>
        /// <param name="questFile"></param>
        private static void getQuestData(string questFile)
        {
            try
            {
                string questFilePath;
                if (ThePak.CurrentUsedPakGuid != null && ThePak.CurrentUsedPakGuid != "0-0-0-0")
                {
                    questFilePath = JohnWick.ExtractAsset(ThePak.CurrentUsedPak, questFile);
                }
                else
                {
                    questFilePath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary[questFile], questFile);
                }

                if (questFilePath != null)
                {
                    if (questFilePath.Contains(".uasset") || questFilePath.Contains(".uexp") || questFilePath.Contains(".ubulk"))
                    {
                        JohnWick.MyAsset = new PakAsset(questFilePath.Substring(0, questFilePath.LastIndexOf('.')));
                        try
                        {
                            if (JohnWick.MyAsset.GetSerialized() != null)
                            {
                                QuestParser[] questParser = QuestParser.FromJson(JToken.Parse(JohnWick.MyAsset.GetSerialized()).ToString());
                                for (int x = 0; x < questParser.Length; x++)
                                {
                                    string oldQuest = string.Empty;
                                    long oldCount = 0;
                                    for (int p = 0; p < questParser[x].Objectives.Length; p++)
                                    {
                                        string newQuest = questParser[x].Objectives[p].Description;
                                        long newCount = questParser[x].Objectives[p].Count;

                                        if (questParser[x].BAthenaMustCompleteInSingleMatch != false && questParser[x].ObjectiveCompletionCount > 0)
                                            newCount = questParser[x].ObjectiveCompletionCount;

                                        if (newQuest != oldQuest && newCount != oldCount)
                                        {
                                            if (questParser[x].Rewards != null)
                                            {
                                                try
                                                {
                                                    string rewardId = questParser[x].Rewards.Where(item => item.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest").Where(item => item.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token").FirstOrDefault().ItemPrimaryAssetId.PrimaryAssetName;
                                                    string rewardQuantity = questParser[x].Rewards.Where(item => item.ItemPrimaryAssetId.PrimaryAssetType.Name != "Quest").Where(item => item.ItemPrimaryAssetId.PrimaryAssetType.Name != "Token").FirstOrDefault().Quantity.ToString();

                                                    BundleData.Add(new BundleInfoEntry(newQuest, newCount, rewardId, rewardQuantity));
                                                }
                                                catch (NullReferenceException)
                                                {
                                                    if (questParser[x].HiddenRewards != null)
                                                    {
                                                        string rewardId = questParser[x].HiddenRewards.FirstOrDefault().TemplateId;
                                                        string rewardQuantity = questParser[x].HiddenRewards.FirstOrDefault().Quantity.ToString();

                                                        BundleData.Add(new BundleInfoEntry(newQuest, newCount, rewardId, rewardQuantity));
                                                    }
                                                }

                                                //get stage
                                                for (int k = 0; k < questParser[x].Rewards.Length; k++)
                                                {
                                                    string qAssetType = questParser[x].Rewards[k].ItemPrimaryAssetId.PrimaryAssetType.Name;
                                                    string qAssetName = questParser[x].Rewards[k].ItemPrimaryAssetId.PrimaryAssetName;

                                                    if (qAssetType == "Quest")
                                                    {
                                                        getQuestData(qAssetName);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                BundleData.Add(new BundleInfoEntry(newQuest, newCount, "", ""));
                                            }

                                            oldQuest = newQuest;
                                            oldCount = newCount;
                                        }
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
                //do not stop when questFile doesn't exist
                //Console.WriteLine("Can't extract " + questFile);
            }
        }
    }
}
