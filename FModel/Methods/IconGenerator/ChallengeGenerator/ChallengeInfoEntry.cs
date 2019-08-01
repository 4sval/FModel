using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FModel
{
    public struct BundleInfoEntry : IEquatable<BundleInfoEntry>
    {
        internal BundleInfoEntry(string QuestDescription, long QuestCount, string RewardId, string RewardQuantity)
        {
            questDescr = QuestDescription;
            questCount = QuestCount;
            rewardItemId = RewardId;
            rewardItemQuantity = RewardQuantity;
        }
        public string questDescr { get; set; }
        public long questCount { get; set; }
        public string rewardItemId { get; set; }
        public string rewardItemQuantity { get; set; }

        bool IEquatable<BundleInfoEntry>.Equals(BundleInfoEntry other)
        {
            throw new NotImplementedException();
        }
    }
}
