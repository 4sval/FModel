using FModel.Creator.Rarities;
using FModel.Creator.Texts;
using FModel.PakReader;
using FModel.PakReader.Parsers.Class;
using FModel.PakReader.Parsers.PropertyTagData;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace FModel.Creator.Bases
{
    public class BaseItemAccess
    {
        private readonly SKPaint descriptionPaint = new SKPaint
        {
            IsAntialias = true,
            FilterQuality = SKFilterQuality.High,
            Typeface = Text.TypeFaces.DescriptionTypeface,
            TextSize = 13,
            Color = SKColors.White,
        };

        public BaseIcon Item;
        public string SItem;
        public SKBitmap Lock;
        public SKBitmap Unlock;
        public string DisplayName;
        public string Description;
        public string UnlockDescription;
        public int Size = 512; // keep it 512 (or a multiple of 512) if you don't want blurry icons

        public BaseItemAccess()
        {
            Item = new BaseIcon();
            SItem = "";
            Lock = Utils.GetTexture("/Game/UI/Foundation/Textures/Icons/Locks/T-Icon-Lock-128").Resize(24, 24);
            Unlock = Utils.GetTexture("/Game/UI/Foundation/Textures/Icons/Locks/T-Icon-Unlocked-128").Resize(24, 24);
            DisplayName = "";
            Description = "";
            UnlockDescription = "";
        }

        public BaseItemAccess(IUExport export) : this()
        {
            if (export.GetExport<ObjectProperty>("access_item") is ObjectProperty accessItem)
            {
                SItem = accessItem.Value.Resource.ObjectName.String;
                Package p = Utils.GetPropertyPakPackage(accessItem.Value.Resource.OuterIndex.Resource.ObjectName.String);
                if (p.HasExport() && !p.Equals(default))
                {
                    var d = p.GetExport<UObject>();
                    if (d != null)
                    {
                        Item = new BaseIcon(d, SItem + ".uasset", true);
                    }
                }
            }

            if (export.GetExport<TextProperty>("DisplayName") is TextProperty displayName)
                DisplayName = Text.GetTextPropertyBase(displayName);
            if (export.GetExport<TextProperty>("Description") is TextProperty description)
                Description = Text.GetTextPropertyBase(description);
            if (export.GetExport<TextProperty>("UnlockDescription") is TextProperty unlockDescription)
                UnlockDescription = Text.GetTextPropertyBase(unlockDescription);
        }

        public void Draw(SKCanvas c)
        {
            Rarity.DrawRarity(c, Item);

            int size = 45;
            int left = Size / 2;
            SKPaint namePaint = new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High,
                Typeface = Text.TypeFaces.DisplayNameTypeface,
                TextSize = size,
                Color = SKColors.White,
                TextAlign = SKTextAlign.Center,
            };
            if ((ELanguage)Properties.Settings.Default.AssetsLanguage == ELanguage.Arabic)
            {
                SKShaper shaper = new SKShaper(namePaint.Typeface);
                float shapedTextWidth;

                while (true)
                {
                    SKShaper.Result shapedText = shaper.Shape(DisplayName, namePaint);
                    shapedTextWidth = shapedText.Points[^1].X + namePaint.TextSize / 2f;

                    if (shapedTextWidth > (Size - (Item.Margin * 2)))
                    {
                        namePaint.TextSize -= 2;
                    }
                    else
                    {
                        break;
                    }
                }

                c.DrawShapedText(shaper, DisplayName, (Size - shapedTextWidth) / 2f, Item.Margin * 8 + size, namePaint);
            }
            else
            {
                while (namePaint.MeasureText(DisplayName) > (Size - (Item.Margin * 2)))
                {
                    namePaint.TextSize = size -= 2;
                }
                c.DrawText(DisplayName, left, Item.Margin * 8 + size, namePaint);
            }

            int topBase = Item.Margin + size * 2;
            c.DrawBitmap(Lock, new SKRect(50, topBase, 50 + Lock.Width, topBase + Lock.Height),
                new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true });
            Helper.DrawMultilineText(c, UnlockDescription, Size, Item.Margin, ETextSide.Left,
                new SKRect(70 + Lock.Width, topBase + 10, Size - 50, 256), descriptionPaint, out topBase);

            c.DrawBitmap(Unlock, new SKRect(50, topBase, 50 + Unlock.Width, topBase + Unlock.Height),
                new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true });
            Helper.DrawMultilineText(c, Description, Size, Item.Margin, ETextSide.Left,
                new SKRect(70 + Unlock.Width, topBase + 10, Size - 50, 256), descriptionPaint, out topBase);

            int h = Size - Item.Margin - topBase;
            c.DrawBitmap(Item.IconImage ?? Item.FallbackImage, new SKRect(left - h / 2, topBase, left + h / 2, Size - Item.Margin),
                new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true });

            c.DrawText(SItem, Size - (Item.Margin * 2.5f), Size - (Item.Margin * 2.5f), new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High,
                Typeface = Text.TypeFaces.BottomDefaultTypeface ?? Text.TypeFaces.DefaultTypeface,
                TextSize = 15,
                Color = SKColors.White,
                TextAlign = SKTextAlign.Right,
            });
        }
    }
}
