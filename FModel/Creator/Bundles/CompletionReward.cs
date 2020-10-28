using FModel.Utils;
using System;
using FModel.PakReader.Parsers.PropertyTagData;

namespace FModel.Creator.Bundles
{
    public class CompletionReward
    {
        private const string _TRIGGER1 = "<text color=\"FFF\" case=\"upper\" fontface=\"black\">";
        private const string _TRIGGER2 = "</>";
        public string CompletionText;
        public Reward Reward;

        public CompletionReward(IntProperty completionCount)
        {
            string all = Localizations.GetLocalization("AthenaChallengeDetailsEntry", "CompletionRewardFormat_All", "Complete <text color=\"FFF\" case=\"upper\" fontface=\"black\">all {0} challenges</> to earn the reward item");
            string allFormated = ReformatString(all, completionCount.Value.ToString(), true);
            string any = Localizations.GetLocalization("AthenaChallengeDetailsEntry", "CompletionRewardFormat", "Complete <text color=\"FFF\" case=\"upper\" fontface=\"black\">any {0} challenges</> to earn the reward item");
            string anyFormated = ReformatString(any, completionCount.Value.ToString(), false);
            CompletionText = completionCount.Value >= 0 ? anyFormated : allFormated;

            Reward = null;
        }

        public CompletionReward(IntProperty completionCount, IntProperty quantity, SoftObjectProperty itemDefinition) : this(completionCount)
        {
            Reward = new Reward(quantity, itemDefinition);
        }

        public CompletionReward(IntProperty completionCount, IntProperty quantity, string reward) : this(completionCount)
        {
            Reward = new Reward(quantity, reward);
        }

        private string ReformatString(string s, string completionCount, bool isAll)
        {
            s = s.Replace("({0})", "{0}").Replace("{QuestNumber}", "<text color=\"FFF\" case=\"upper\" fontface=\"black\">{0}</>");

            int index = s.IndexOf("|plural(", StringComparison.CurrentCultureIgnoreCase);
            if (index > -1)
            {
                int i = s.Substring(index).IndexOf(')', StringComparison.CurrentCultureIgnoreCase);
                s = s.Replace(s.Substring(index, i + 1), string.Empty).Replace("{0} {0}", "{0}");
            }

            int index1 = s.IndexOf(_TRIGGER1, StringComparison.CurrentCultureIgnoreCase);
            if (index1 < 0) index1 = 0;
            string partOne = s.Substring(0, index1);

            string partTemp = s.Substring(index1 + _TRIGGER1.Length);
            int index2 = partTemp.IndexOf(_TRIGGER2, StringComparison.CurrentCultureIgnoreCase);
            if (index2 < 0) index2 = 0;
            string partUpper = partTemp.Substring(0, index2).ToUpper().Replace("{0}", isAll ? string.Empty : completionCount);

            string partTwo = partTemp.Substring(index2 + _TRIGGER2.Length);

            return string.Format("{0}{1}{2}", partOne, partUpper, partTwo).Replace("  ", " ").Replace(" ,", ",");
        }
    }
}
