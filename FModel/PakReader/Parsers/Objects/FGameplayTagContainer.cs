using System.Collections.Generic;

namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FGameplayTagContainer : IUStruct
    {
        // It's technically a TArray<FGameplayTag> but FGameplayTag is just a fancy wrapper around an FName
        public readonly FName[] GameplayTags;

        internal FGameplayTagContainer(PackageReader reader)
        {
            GameplayTags = reader.ReadTArray(reader.ReadFName);
        }

        public string[] GetValue()
        {
            var ret = new string[GameplayTags.Length];
            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = GameplayTags[i].String;
            }
            return ret;
        }
    }

    public static class FGameplayTagContainerExtension
    {
        public static bool TryGetGameplayTag(this FName[] gpt, string startWith, out FName fname)
        {
            foreach (FName gp in gpt)
            {
                if (gp.String != null && gp.String.StartsWith(startWith))
                {
                    fname = gp;
                    return true;
                }
            }
            fname = default;
            return false;
        }

        public static List<string> GetAllGameplayTag(this FName[] gpt, params string[] startWith)
        {
            List<string> ret = new List<string>();
            foreach (FName gp in gpt)
            {
                foreach (string s in startWith)
                    if (gp.String != null && gp.String.StartsWith(s))
                        ret.Add(gp.String);
            }
            return ret;
        }
    }
}
