using PakReader.Pak;
using PakReader.Parsers.Class;
using PakReader.Parsers.Objects;
using PakReader.Parsers.PropertyTagData;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Windows;

namespace FModel.Creator.Icons
{
    static class UserFacingFlag
    {
        public static void GetUserFacingFlags(List<string> uffs, BaseIcon icon, string exportType)
        {
            if (uffs.Count > 0)
            {
                PakPackage p = Utils.GetPropertyPakPackage("/Game/Items/ItemCategories"); //PrimaryCategories - SecondaryCategories - TertiaryCategories
                if (p.HasExport() && !p.Equals(default))
                {
                    var o = p.GetExport<UObject>();
                    if (o != null && o.TryGetValue("TertiaryCategories", out var tertiaryCategories) && tertiaryCategories is ArrayProperty tertiaryArray)
                    {
                        icon.UserFacingFlags = new SKBitmap[uffs.Count];
                        for (int i = 0; i < uffs.Count; i++)
                        {
                            if (uffs[i].Equals("Cosmetics.UserFacingFlags.HasUpgradeQuests"))
                            {
                                if (exportType.Equals("AthenaPetCarrierItemDefinition"))
                                    icon.UserFacingFlags[i] = SKBitmap.Decode(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/T-Icon-Pets-64.png")).Stream);
                                else
                                    icon.UserFacingFlags[i] = SKBitmap.Decode(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/T-Icon-Quests-64.png")).Stream);
                            }
                            else
                            {
                                foreach (StructProperty structProp in tertiaryArray.Value)
                                {
                                    if (structProp.Value is UObject mainUObject &&
                                        mainUObject.TryGetValue("TagContainer", out var struc1) && struc1 is StructProperty tagContainer && tagContainer.Value is FGameplayTagContainer f && f.GameplayTags.TryGetGameplayTag(uffs[i], out var _) &&
                                        mainUObject.TryGetValue("CategoryBrush", out var struc2) && struc2 is StructProperty categoryBrush && categoryBrush.Value is UObject categoryUObject &&
                                        categoryUObject.TryGetValue("Brush_XXS", out var struc3) && struc3 is StructProperty brushXXS && brushXXS.Value is UObject brushUObject &&
                                        brushUObject.TryGetValue("ResourceObject", out var object1) && object1 is ObjectProperty resourceObject)
                                    {
                                        icon.UserFacingFlags[i] = Utils.GetObjectTexture(resourceObject);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void DrawUserFacingFlags(SKCanvas c, BaseIcon icon)
        {
            if (icon.UserFacingFlags != null)
            {
                int size = 25;
                int x = icon.Margin * (int)2.5;
                foreach (SKBitmap b in icon.UserFacingFlags)
                {
                    if (b == null)
                        continue;
                    
                    c.DrawBitmap(b.Resize(size, size), new SKPoint(x, icon.Margin * (int)2.5), new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true });
                    x += size;
                }
            }
        }
    }
}
