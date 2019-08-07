using System;

namespace FModel
{
    public struct BundleInfoEntry : IEquatable<BundleInfoEntry>
    {
        internal BundleInfoEntry(string QuestDescription, long QuestCount, string RewardId, string RewardQuantity, string UnlockType)
        {
            questDescr = QuestDescription;
            questCount = QuestCount;
            rewardItemId = RewardId;
            rewardItemQuantity = RewardQuantity;
            questUnlockType = UnlockType;
        }
        public string questDescr { get; set; }
        public long questCount { get; set; }
        public string rewardItemId { get; set; }
        public string rewardItemQuantity { get; set; }
        public string questUnlockType { get; set; }

        bool IEquatable<BundleInfoEntry>.Equals(BundleInfoEntry other)
        {
            throw new NotImplementedException();
        }
    }
}
