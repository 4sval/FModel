using FModel.Creator.Bases;
using SkiaSharp;
using System;
using FModel.PakReader;
using FModel.PakReader.Parsers.Class;
using FModel.PakReader.Parsers.Objects;
using FModel.PakReader.Parsers.PropertyTagData;

namespace FModel.Creator.Bundles
{
    public class Reward
    {
        public int RewardQuantity;
        public SKBitmap RewardIcon;
        public BaseIcon TheReward;
        public string RewardFillColor;
        public string RewardBorderColor;
        public bool IsCountShifted;

        public Reward()
        {
            RewardQuantity = 0;
            RewardIcon = null;
            TheReward = null;
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
                    Package p = Utils.GetPropertyPakPackage($"/Game/Items/BannerIcons/{parts[1]}.{parts[1]}");
                    if (p.HasExport() && !p.Equals(default))
                    {
                        if (p.GetExport<UObject>() is UObject banner)
                        {
                            TheReward = new BaseIcon(banner, $"{parts[1]}.uasset", false);
                            RewardIcon = TheReward.IconImage.Resize(64, 64);
                        }
                    }
                }
                else GetReward(parts[1]);
            }
            else GetReward(assetName);
        }
        public Reward(IntProperty quantity, FSoftObjectPath itemFullPath)
        {
            RewardQuantity = quantity.Value;
            Package p = Utils.GetPropertyPakPackage(itemFullPath.AssetPathName.String);
            if (p.HasExport() && !p.Equals(default))
            {
                var d = p.GetExport<UObject>();
                if (d != null)
                {
                    int i = itemFullPath.AssetPathName.String.LastIndexOf('/');
                    TheReward = new BaseIcon(d, itemFullPath.AssetPathName.String[(i > 0 ? i : 0)..] + ".uasset", false);
                    RewardIcon = TheReward.IconImage.Resize(80, 80);
                }
            }
        }

        public Reward(IntProperty quantity, SoftObjectProperty itemDefinition) : this()
        {
            RewardQuantity = quantity.Value;

            Package p = Utils.GetPropertyPakPackage(itemDefinition.Value.AssetPathName.String);
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
                            TheReward = new BaseIcon(d, itemDefinition.Value.AssetPathName.String.Substring(s1, s2) + ".uasset", false);
                            RewardIcon = TheReward.IconImage.Resize(64, 64);
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
                        Package p = Utils.GetPropertyPakPackage(path);
                        if (p!= null && p.HasExport() && !p.Equals(default))
                        {
                            var d = p.GetExport<UObject>();
                            if (d != null)
                            {
                                int i = path.LastIndexOf('/');
                                IsCountShifted = false;
                                TheReward = new BaseIcon(d, path[(i > 0 ? i : 0)..] + ".uasset", false);
                                RewardIcon = TheReward.IconImage.Resize(64, 64);
                            }
                        }
                        break;
                    }
            }
        }
    }
}
