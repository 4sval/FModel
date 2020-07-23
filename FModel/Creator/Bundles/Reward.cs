using FModel.Creator.Bases;
using PakReader.Pak;
using PakReader.Parsers.Class;
using PakReader.Parsers.PropertyTagData;
using SkiaSharp;
using System;

namespace FModel.Creator.Bundles
{
    public class Reward
    {
        public int RewardQuantity;
        public SKBitmap RewardIcon;
        public string RewardFillColor;
        public string RewardBorderColor;
        public bool IsCountShifted;

        public Reward()
        {
            RewardQuantity = 0;
            RewardIcon = null;
            RewardFillColor = "";
            RewardBorderColor = "";
            IsCountShifted = false;
        }

        public Reward(IntProperty quantity, NameProperty primaryAssetName) : this(quantity, primaryAssetName.Value.String) { }
        public Reward(IntProperty quantity, string assetName) : this()
        {
            RewardQuantity = quantity.Value;

            if (assetName.Contains(':'))
            {
                string[] parts = assetName.Split(':');
                if (parts[0].Equals("HomebaseBannerIcon", StringComparison.CurrentCultureIgnoreCase))
                {
                    PakPackage p = Utils.GetPropertyPakPackage("/Game/Banners/BannerIcons");
                    if (p.HasExport() && !p.Equals(default))
                    {
                        var c = p.GetExport<UDataTable>();
                        if (c != null && c.TryGetCaseInsensitiveValue(parts[1], out var s) && s is UObject banner)
                        {
                            RewardIcon = new BaseIcon(banner, "BannerIcons.uasset").IconImage.Resize(64, 64);
                        }
                    }
                }
                else GetReward(parts[1]);
            }
            else GetReward(assetName);
        }

        public Reward(IntProperty quantity, SoftObjectProperty itemDefinition) : this()
        {
            RewardQuantity = quantity.Value;

            PakPackage p = Utils.GetPropertyPakPackage(itemDefinition.Value.AssetPathName.String);
            if (p.HasExport() && !p.Equals(default))
            {
                var d = p.GetExport<UObject>();
                if (d != null)
                {
                    int s1 = itemDefinition.Value.AssetPathName.String.LastIndexOf('/');
                    if (s1 < 0) s1 = 0;
                    int s2 = itemDefinition.Value.AssetPathName.String.LastIndexOf('.') - s1;
                    switch (itemDefinition.Value.AssetPathName.String)
                    {
                        case "/Game/Items/PersistentResources/AthenaBattleStar.AthenaBattleStar":
                            IsCountShifted = true;
                            RewardFillColor = "FFDB67";
                            RewardBorderColor = "8F4A20";
                            RewardIcon = Utils.GetTexture("/Game/UI/Foundation/Textures/Icons/Items/T-FNBR-BattlePoints").Resize(48, 48);
                            break;
                        case "/Game/Items/PersistentResources/AthenaSeasonalXP.AthenaSeasonalXP":
                            IsCountShifted = true;
                            RewardFillColor = "E6FDB1";
                            RewardBorderColor = "51830F";
                            RewardIcon = Utils.GetTexture("/Game/UI/Foundation/Textures/Icons/Items/T-FNBR-XPMedium").Resize(48, 48);
                            break;
                        case "/Game/Items/Currency/MtxGiveaway.MtxGiveaway":
                            IsCountShifted = true;
                            RewardFillColor = "DCE6FF";
                            RewardBorderColor = "64A0AF";
                            RewardIcon = Utils.GetTexture("/Game/UI/Foundation/Textures/Icons/Items/T-Items-MTX").Resize(48, 48);
                            break;
                        default:
                            IsCountShifted = false;
                            RewardIcon = new BaseIcon(d, itemDefinition.Value.AssetPathName.String.Substring(s1, s2) + ".uasset").IconImage.Resize(64, 64);
                            break;
                    }
                }
            }
        }

        private void GetReward(string trigger)
        {
            switch (trigger.ToLower())
            {
                case "athenabattlestar":
                    IsCountShifted = true;
                    RewardFillColor = "FFDB67";
                    RewardBorderColor = "8F4A20";
                    RewardIcon = Utils.GetTexture("/Game/UI/Foundation/Textures/Icons/Items/T-FNBR-BattlePoints").Resize(48, 48);
                    break;
                case "athenaseasonalxp":
                    IsCountShifted = true;
                    RewardFillColor = "E6FDB1";
                    RewardBorderColor = "51830F";
                    RewardIcon = Utils.GetTexture("/Game/UI/Foundation/Textures/Icons/Items/T-FNBR-XPMedium").Resize(48, 48);
                    break;
                case "mtxgiveaway":
                    IsCountShifted = true;
                    RewardFillColor = "DCE6FF";
                    RewardBorderColor = "64A0AF";
                    RewardIcon = Utils.GetTexture("/Game/UI/Foundation/Textures/Icons/Items/T-Items-MTX").Resize(48, 48);
                    break;
                default:
                    {
                        string path = Utils.GetFullPath($"/FortniteGame/Content/Athena/.*?/{trigger}.*").Replace("FortniteGame/Content", "Game");
                        PakPackage p = Utils.GetPropertyPakPackage(path);
                        if (p.HasExport() && !p.Equals(default))
                        {
                            var d = p.GetExport<UObject>();
                            if (d != null)
                            {
                                int i = path.LastIndexOf('/');
                                IsCountShifted = false;
                                RewardIcon = new BaseIcon(d, path.Substring(i > 0 ? i : 0) + ".uasset").IconImage.Resize(64, 64);
                            }
                        }
                        break;
                    }
            }
        }
    }
}
