using System;
using System.Linq;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Internationalization;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Creator.Bases.FN;
using FModel.Framework;
using FModel.ViewModels;
using FModel.ViewModels.ApiEndpoints.Models;
using FModel.Views.Resources.Controls;
using SkiaSharp;
using SkiaSharp.HarfBuzz;
using static System.Net.Mime.MediaTypeNames;
using static CUE4Parse.UE4.Objects.Core.i18N.FTextHistory;

namespace FModel.Creator.Bases.VAL;


public class BasePurchasableItem : UCreator
{
    private string ItemName { get; set; }
    private string ItemCost { get; set; }

    public BasePurchasableItem(UObject uObject, EIconStyle style) : base(uObject, style)
    {
        Background = new[] { SKColor.Parse("#262630"), SKColor.Parse("#1f1f26") };
        Border = new[] { SKColor.Parse("#1f1f26"), SKColor.Parse("#262630") };

        Width = 640;
        Height = 360;

        StarterTextPos = 270;
        ImageMargin = 64;
    }

    public override void ParseForInfo()
    {
        if (Object.TryGetValue(out UTexture2D NewShopImage, "NewShopImage"))
            Preview = Utils.GetBitmap(NewShopImage);

        if (Object.TryGetValue(out int itemCost, "Cost"))
            ItemCost = itemCost.ToString("N0");

        if (Object.TryGetValue(out FText ShopCategoryText, "ShopCategoryText"))
        {            
            if (Utils.TryLoadObject((ShopCategoryText.TextHistory as StringTableEntry).TableId.ToString(), out UStringTable Table))
            {
                if (Object.TryGetValue(out UObject Equippable, "EquippableClass"))
                {
                    string WeaponName = Equippable.Name.Substring(0, Equippable.Name.Length - 2);
                    foreach (var KeyMeta in Table.StringTable.KeysToMetaData)
                    {
                        if (KeyMeta.Key.Contains($"{WeaponName}_DisplayName"))
                        {
                            FLogger.Append(ELog.Error, () => FLogger.Text(KeyMeta.Value, Constants.WHITE, true));
                            ItemName = KeyMeta.Value;

                            break;
                        }
                    }
                }
            }
        }

        if (ItemName == string.Empty)
            ItemName = "Unknown";
    }

    protected void DrawItemPreview(SKCanvas c)
    {
        if (Preview != null)
            c.DrawBitmap(Preview, (Width - Preview.Width) / 2, (Height - Preview.Height) / 2, ImagePaint);
    }

    protected void DrawItemInfo(SKCanvas c)
    {
        if (string.IsNullOrWhiteSpace(ItemCost))
            return;

        DisplayNamePaint.Typeface = Utils.Typefaces.Bottom;

        DisplayNamePaint.TextSize = 35;
        while (DisplayNamePaint.MeasureText(ItemCost) > Width - Margin * 2)
        {
            DisplayNamePaint.TextSize -= 1;
        }

        var shaper = new CustomSKShaper(DisplayNamePaint.Typeface);
        var x = (Margin * 2.5f) + 15;
        var y = StarterTextPos + _NAME_TEXT_SIZE;

        DisplayNamePaint.TextAlign = SKTextAlign.Left;
        c.DrawShapedText(shaper, ItemName, x, y - 14, DisplayNamePaint);
        c.DrawShapedText(shaper, ItemCost, x, y + 24, DisplayNamePaint);
    }

    public override SKBitmap[] Draw()
    {
        var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var c = new SKCanvas(ret);

        DrawBackground(c);
        DrawItemPreview(c);
        DrawItemInfo(c);

        return new[] { ret };
    }
}
