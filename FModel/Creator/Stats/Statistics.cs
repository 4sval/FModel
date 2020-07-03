using FModel.Creator.Fortnite;
using FModel.Creator.Texts;
using FModel.Utils;
using PakReader.Pak;
using PakReader.Parsers.Class;
using PakReader.Parsers.PropertyTagData;
using SkiaSharp;
using System;
using System.Windows;

namespace FModel.Creator.Stats
{
    static class Statistics
    {
        public static void GetAmmoData(BaseIcon icon, SoftObjectProperty ammoData)
        {
            PakPackage p = Utils.GetPropertyPakPackage(ammoData.Value.AssetPathName.String);
            if (p.HasExport() && !p.Equals(default))
            {
                var obj = p.GetExport<UObject>();
                if (obj != null)
                {
                    if (obj.TryGetValue("DisplayName", out var v1) && v1 is TextProperty displayName &&
                        obj.TryGetValue("SmallPreviewImage", out var v2) && v2 is SoftObjectProperty smallPreviewImage)
                    {
                        icon.Stats.Add(new Statistic
                        {
                            Icon = Utils.GetSoftObjectTexture(smallPreviewImage),
                            Description = Text.GetTextPropertyBase(displayName).ToUpper()
                        });
                    }
                }
            }
        }

        public static void GetWeaponStats(BaseIcon icon, StructProperty weaponStatHandle)
        {
            if (weaponStatHandle.Value is UObject o1 &&
                o1.TryGetValue("DataTable", out var c1) && c1 is ObjectProperty dataTable &&
                o1.TryGetValue("RowName", out var c2) && c2 is NameProperty rowName)
            {
                PakPackage p = Utils.GetPropertyPakPackage(dataTable.Value.Resource.OuterIndex.Resource.ObjectName.String);
                if (p.HasExport() && !p.Equals(default))
                {
                    var table = p.GetExport<UDataTable>();
                    if (table != null)
                    {
                        if (table.TryGetValue(rowName.Value.String, out var v1) && v1 is UObject stats)
                        {
                            if (stats.TryGetValue("ReloadTime", out var s1) && s1 is FloatProperty reloadTime && reloadTime.Value != 0)
                                icon.Stats.Add(new Statistic
                                {
                                    Icon = SKBitmap.Decode(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/T_ReloadTime_Weapon_Stats.png")).Stream),
                                    Description = $"{Localizations.GetLocalization(string.Empty, "6EA26D1A4252034FBD869A90F9A6E49A", "Reload Time")} ({Localizations.GetLocalization(string.Empty, "6BA53D764BA5CC13E821D2A807A72365", "seconds")}) : {reloadTime.Value:0.0}".ToUpper()
                                });

                            if (stats.TryGetValue("ClipSize", out var s2) && s2 is IntProperty clipSize && clipSize.Value != 0)
                                icon.Stats.Add(new Statistic
                                {
                                    Icon = SKBitmap.Decode(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/T_ClipSize_Weapon_Stats.png")).Stream),
                                    Description = $"{Localizations.GetLocalization(string.Empty, "068239DD4327B36124498C9C5F61C038", "Magazine Size")} : {clipSize.Value}".ToUpper()
                                });

                            if (stats.TryGetValue("DmgPB", out var s3) && s3 is FloatProperty dmgPB && dmgPB.Value != 0)
                                icon.Stats.Add(new Statistic
                                {
                                    Icon = SKBitmap.Decode(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/T_DamagePerBullet_Weapon_Stats.png")).Stream),
                                    Description = $"{Localizations.GetLocalization(string.Empty, "BF7E3CF34A9ACFF52E95EAAD4F09F133", "Damage to Player")} : {dmgPB.Value}".ToUpper()
                                });
                        }
                    }
                }
            }
        }

        public static void GetHeroStats(BaseIcon icon, ObjectProperty heroGameplayDefinition)
        {
            PakPackage p = Utils.GetPropertyPakPackage(heroGameplayDefinition.Value.Resource.OuterIndex.Resource.ObjectName.String);
            if (p.HasExport() && !p.Equals(default))
            {
                var obj = p.GetExport<UObject>();
                if (obj != null)
                {
                    if (obj.TryGetValue("HeroPerk", out var v1) && v1 is StructProperty s1 && s1.Value is UObject heroPerk)
                    {
                        GetAbilityKit(icon, heroPerk);
                    }

                    if (obj.TryGetValue("TierAbilityKits", out var v2) && v2 is ArrayProperty tierAbilityKits)
                    {
                        foreach (StructProperty abilityKit in tierAbilityKits.Value)
                        {
                            if (abilityKit.Value is UObject kit)
                            {
                                GetAbilityKit(icon, kit);
                            }
                        }
                    }
                }
            }
        }

        private static void GetAbilityKit(BaseIcon icon, UObject parent)
        {
            if (parent.TryGetValue("GrantedAbilityKit", out var v) && v is SoftObjectProperty grantedAbilityKit)
            {
                PakPackage k = Utils.GetPropertyPakPackage(grantedAbilityKit.Value.AssetPathName.String);
                if (k.HasExport() && !k.Equals(default))
                {
                    var kit = k.GetExport<UObject>();
                    if (kit != null &&
                        kit.GetExport<TextProperty>("DisplayName") is TextProperty displayName &&
                        kit.GetExport<StructProperty>("IconBrush") is StructProperty brush && brush.Value is UObject iconBrush &&
                        iconBrush.TryGetValue("ResourceObject", out var s) && s is ObjectProperty resourceObject)
                    {
                        icon.Stats.Add(new Statistic
                        {
                            Icon = Utils.GetObjectTexture(resourceObject),
                            Description = Text.GetTextPropertyBase(displayName).ToUpper()
                        });
                    }
                }
            }
        }

        public static void DrawStats(SKCanvas c, BaseIcon icon)
        {
            int size = 48;
            int iconSize = 40;
            int textSize = 25;
            int y = icon.Size;
            foreach (Statistic stat in icon.Stats)
            {
                c.DrawRect(new SKRect(0, y, icon.Size, y + size),
                    new SKPaint
                    {
                        IsAntialias = true,
                        FilterQuality = SKFilterQuality.High,
                        Shader = SKShader.CreateLinearGradient(
                            new SKPoint(icon.Size / 2, icon.Size),
                            new SKPoint(icon.Size, icon.Size / 4),
                            icon.RarityBorderColor,
                            SKShaderTileMode.Clamp)
                    });


                if ((EIconDesign)Properties.Settings.Default.AssetsIconDesign == EIconDesign.Flat)
                {
                    c.DrawRect(new SKRect(icon.Margin, y, icon.Size - icon.Margin, y + size - icon.Margin),
                        new SKPaint
                        {
                            IsAntialias = true,
                            FilterQuality = SKFilterQuality.High,
                            Color = icon.RarityBackgroundColors[0]
                        });
                }
                else
                {
                    c.DrawRect(new SKRect(icon.Margin, y, icon.Size - icon.Margin, y + size - icon.Margin),
                        new SKPaint
                        {
                            IsAntialias = true,
                            FilterQuality = SKFilterQuality.High,
                            Shader = SKShader.CreateRadialGradient(
                                new SKPoint(icon.Size / 2, icon.Size / 2),
                                icon.Size / 5 * 4,
                                icon.RarityBackgroundColors,
                                SKShaderTileMode.Clamp)
                        });
                }

                c.DrawRect(new SKRect(icon.Margin, y, icon.Size - icon.Margin, y + size - icon.Margin),
                    new SKPaint
                    {
                        IsAntialias = true,
                        FilterQuality = SKFilterQuality.High,
                        Color = new SKColor(0, 0, 50, 75)
                    });

                c.DrawBitmap(stat.Icon.Resize(iconSize, iconSize), new SKPoint(icon.Margin * (int)2.5, y + 4), new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true });

                var statPaint = new SKPaint
                {
                    IsAntialias = true,
                    FilterQuality = SKFilterQuality.High,
                    Typeface = Text.TypeFaces.DisplayNameTypeface,
                    TextSize = textSize,
                    Color = SKColors.White,
                    TextAlign = SKTextAlign.Center,
                };

                // resize if too long
                while (statPaint.MeasureText(stat.Description) > (icon.Size - (icon.Margin * 2) - iconSize))
                {
                    statPaint.TextSize = textSize -= 2;
                }

                c.DrawText(stat.Description, icon.Size / 2, y + 32, statPaint);

                y += size;
            }
        }
    }
}
