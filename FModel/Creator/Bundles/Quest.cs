using FModel.Creator.Texts;
using FModel.PakReader;
using FModel.PakReader.Parsers.Class;
using FModel.PakReader.Parsers.PropertyTagData;

namespace FModel.Creator.Bundles
{
    public class Quest
    {
        public string Description;
        public int Count;
        public Reward Reward;

        public Quest()
        {
            Description = "";
            Count = 0;
            Reward = null;
        }

        public Quest(UObject obj) : this()
        {
            if (obj.TryGetValue("Description", out var d) && d is TextProperty description)
                Description = Text.GetTextPropertyBase(description);
            if (obj.TryGetValue("ObjectiveCompletionCount", out var o) && o is IntProperty objectiveCompletionCount)
                Count = objectiveCompletionCount.Value;

            if (obj.TryGetValue("Objectives", out var v1) && v1 is ArrayProperty a1 &&
                a1.Value.Length > 0 && a1.Value[0] is StructProperty s && s.Value is UObject objectives)
            {
                if (string.IsNullOrEmpty(Description) && objectives.TryGetValue("Description", out var od) && od is TextProperty objectivesDescription)
                    Description = Text.GetTextPropertyBase(objectivesDescription);

                if (Count == 0 && objectives.TryGetValue("Count", out var c) && c is IntProperty count)
                    Count = count.Value;
            }

            if (obj.TryGetValue("RewardsTable", out var v4) && v4 is ObjectProperty rewardsTable)
            {
                Package p = Utils.GetPropertyPakPackage(rewardsTable.Value.Resource.OuterIndex.Resource.ObjectName.String);
                if (p.HasExport() && !p.Equals(default))
                {
                    var u = p.GetExport<UDataTable>();
                    if (u != null && u.TryGetValue("Default", out var i) && i is UObject r &&
                        r.TryGetValue("TemplateId", out var i1) && i1 is NameProperty templateId &&
                        r.TryGetValue("Quantity", out var i2) && i2 is IntProperty quantity)
                    {
                        Reward = new Reward(quantity, templateId);
                    }
                }
            }

            if (Reward == null && obj.TryGetValue("Rewards", out var v2) && v2 is ArrayProperty rewards)
            {
                foreach (StructProperty reward in rewards.Value)
                {
                    if (reward.Value is UObject r1 &&
                        r1.TryGetValue("ItemPrimaryAssetId", out var i1) && i1 is StructProperty itemPrimaryAssetId &&
                        r1.TryGetValue("Quantity", out var i2) && i2 is IntProperty quantity)
                    {
                        if (itemPrimaryAssetId.Value is UObject r2 &&
                            r2.TryGetValue("PrimaryAssetType", out var t1) && t1 is StructProperty primaryAssetType &&
                            r2.TryGetValue("PrimaryAssetName", out var t2) && t2 is NameProperty primaryAssetName)
                        {
                            if (primaryAssetType.Value is UObject r3 && r3.TryGetValue("Name", out var k) && k is NameProperty name)
                            {
                                if (!name.Value.String.Equals("Quest") && !name.Value.String.Equals("Token") &&
                                    !name.Value.String.Equals("ChallengeBundle") && !name.Value.String.Equals("GiftBox"))
                                {
                                    Reward = new Reward(quantity, primaryAssetName);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if (Reward == null && obj.TryGetValue("HiddenRewards", out var v3) && v3 is ArrayProperty hiddenRewards)
            {
                foreach (StructProperty reward in hiddenRewards.Value)
                {
                    if (reward.Value is UObject r1 &&
                        r1.TryGetValue("TemplateId", out var i1) && i1 is NameProperty templateId &&
                        r1.TryGetValue("Quantity", out var i2) && i2 is IntProperty quantity)
                    {
                        Reward = new Reward(quantity, templateId);
                        break;
                    }
                }
            }
        }
    }
}
