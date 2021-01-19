using FModel.Creator.Bases;
using FModel.Creator.Icons;
using FModel.Creator.Rarities;
using FModel.Creator.Stats;
using FModel.Creator.Texts;
using FModel.PakReader.Parsers.Class;
using FModel.PakReader.Parsers.Objects;
using FModel.ViewModels.ImageBox;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace FModel.Chic
{
    static class ChicIcon
    {
        public static bool GenerateIcon(IUExport export, string exportType, ref string assetName)
        {
            BaseIcon icon = new BaseIcon(export, exportType, ref assetName);
            int height = icon.Size + icon.AdditionalSize;
            using (var ret = new SKBitmap(icon.Size, height, SKColorType.Rgba8888, SKAlphaType.Premul))
            using (var c = new SKCanvas(ret))
            {
                icon.Margin = 0;

                //Background
                ChicRarity.DrawRarity(c, icon);

                //Draw item icon
                LargeSmallImage.DrawPreviewImage(c, icon);

                //Draw Text Background
                ChicText.DrawBackground(c, icon);
                //Display Name
                ChicText.DrawDisplayName(c, icon);
                //Description
                Text.DrawDescription(c, icon);

                if (!icon.ShortDescription.Equals(icon.DisplayName) && !icon.ShortDescription.Equals(icon.Description))
                {
                    //Draw Item Type
                    Text.DrawToBottom(c, icon, ETextSide.Left, icon.ShortDescription);
                }
                //Draw Source
                Text.DrawToBottom(c, icon, ETextSide.Right, icon.CosmeticSource);

                //Draw Flags
                UserFacingFlag.DrawUserFacingFlags(c, icon);

                // has more things to show
                if (height > icon.Size)
                {
                    Statistics.DrawStats(c, icon);
                }

                //Watermark
                Watermark.DrawWatermark(c); // watermark should only be applied on icons with width = 512
                
                //Shows the image
                ImageBoxVm.imageBoxViewModel.Set(ret, assetName);
            }

            return true;
        }
    }
}
