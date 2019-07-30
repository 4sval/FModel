using csharp_wick;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FModel
{
    class SchematicItemInfos
    {
        public static List<SchematicInfoEntry> schematicInfoList { get; set; }

        public static JToken setSchematicData(JToken schematicAsset)
        {
            schematicInfoList = new List<SchematicInfoEntry>();
            JToken toReturn = null;

            JToken craftingRecipe = schematicAsset["CraftingRecipe"];
            if (craftingRecipe != null)
            {
                JToken dataTable = craftingRecipe["DataTable"];
                if (dataTable != null)
                {
                    string dataTableFilePath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary[dataTable.Value<string>()], dataTable.Value<string>());
                    if (!string.IsNullOrEmpty(dataTableFilePath))
                    {
                        if (dataTableFilePath.Contains(".uasset") || dataTableFilePath.Contains(".uexp") || dataTableFilePath.Contains(".ubulk"))
                        {
                            JohnWick.MyAsset = new PakAsset(dataTableFilePath.Substring(0, dataTableFilePath.LastIndexOf('.')));
                            try
                            {
                                if (JohnWick.MyAsset.GetSerialized() != null)
                                {
                                    dynamic AssetData = JsonConvert.DeserializeObject(JohnWick.MyAsset.GetSerialized());
                                    JArray AssetArray = JArray.FromObject(AssetData);

                                    JToken weaponRowName = ((JObject)AssetArray[0]).GetValue(craftingRecipe["RowName"].Value<string>(), StringComparison.OrdinalIgnoreCase);
                                    if (weaponRowName != null)
                                    {
                                        toReturn = getSchematicRecipeResult(weaponRowName);
                                        registerSchematicRecipeCosts(weaponRowName);
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
            }
            else { throw new ArgumentException("Not enough informations to create an icon about this schematic - Missing: \"CraftingRecipe\""); }

            return toReturn;
        }

        public static JToken getSchematicRecipeResult(JToken schematicDataTable)
        {
            JToken toReturn = null;

            JToken recipeResults = schematicDataTable["RecipeResults"];
            if (recipeResults != null)
            {
                JToken primaryAssetName = recipeResults[0]["ItemPrimaryAssetId"]["PrimaryAssetName"];
                if (primaryAssetName != null)
                {
                    string primaryAssetNameInDict = ThePak.AllpaksDictionary.Where(x => string.Equals(x.Key, primaryAssetName.Value<string>(), StringComparison.CurrentCultureIgnoreCase)).Select(d => d.Key).FirstOrDefault();
                    string primaryAssetTableFilePath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary[primaryAssetNameInDict], primaryAssetNameInDict);
                    if (!string.IsNullOrEmpty(primaryAssetTableFilePath))
                    {
                        if (primaryAssetTableFilePath.Contains(".uasset") || primaryAssetTableFilePath.Contains(".uexp") || primaryAssetTableFilePath.Contains(".ubulk"))
                        {
                            JohnWick.MyAsset = new PakAsset(primaryAssetTableFilePath.Substring(0, primaryAssetTableFilePath.LastIndexOf('.')));
                            try
                            {
                                if (JohnWick.MyAsset.GetSerialized() != null)
                                {
                                    dynamic primaryAssetData = JsonConvert.DeserializeObject(JohnWick.MyAsset.GetSerialized());
                                    JArray primaryAssetArray = JArray.FromObject(primaryAssetData);
                                    toReturn = primaryAssetArray[0];
                                }
                            }
                            catch (JsonSerializationException)
                            {
                                //do not crash when JsonSerialization does weird stuff
                            }
                        }
                    }
                }
            }

            return toReturn;
        }

        public static void registerSchematicRecipeCosts(JToken schematicDataTable)
        {
            JToken recipeCosts = schematicDataTable["RecipeCosts"];
            if (recipeCosts != null)
            {
                JArray recipeCostsArray = recipeCosts.Value<JArray>();
                foreach (JToken token in recipeCostsArray)
                {
                    JToken primaryAssetName = token["ItemPrimaryAssetId"]["PrimaryAssetName"];
                    JToken quantity = token["Quantity"];
                    if (primaryAssetName != null && quantity != null)
                    {
                        SchematicInfoEntry currentEntry = new SchematicInfoEntry(primaryAssetName.Value<string>(), quantity.Value<string>());
                        bool isAlreadyAdded = schematicInfoList.Any(item => item.theIngredientItemDefinition.Equals(currentEntry.theIngredientItemDefinition, StringComparison.InvariantCultureIgnoreCase) && item.theIngredientQuantity == currentEntry.theIngredientQuantity);
                        if (!isAlreadyAdded) { schematicInfoList.Add(currentEntry); }
                    }
                }
            }
        }
    }
}
