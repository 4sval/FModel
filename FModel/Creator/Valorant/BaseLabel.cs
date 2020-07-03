using FModel.Creator.Labels;
using PakReader.Parsers.Class;
using PakReader.Parsers.PropertyTagData;
using SkiaSharp;
using System.Collections.Generic;

namespace FModel.Creator.Valorant
{
    public class BaseLabel
    {
        public List<BaseUIData> IconImages;
        public int Width = 512; // keep it 512 (or a multiple of 512) if you don't want blurry icons
        public int Height = 512;
        public int Margin = 4;

        public BaseLabel()
        {
            IconImages = new List<BaseUIData>();
        }

        public BaseLabel(IUExport export) : this()
        {
            if (export.GetExport<ArrayProperty>("ExplicitAssets") is ArrayProperty explicitAssetsArray)
            {
                foreach (var o in explicitAssetsArray.Value)
                {
                    if (o is SoftObjectProperty s)
                        ExplicitAsset.GetAsset(this, s);
                }
            }
        }

        public void Draw(SKCanvas c)
        {
            int yPos = Margin;
            foreach (BaseUIData icon in IconImages)
            {
                if (icon.IconImage != null)
                {
                    c.DrawBitmap(icon.IconImage, new SKRect(0, yPos, icon.Width, yPos + icon.Height),
                        new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true });

                    yPos += icon.Height;
                }
            }
        }
    }
}
