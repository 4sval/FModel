using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using SkiaSharp;

namespace FModel.Creator.Bases.FN
{
    public class BaseOfferDisplayData : UCreator
    {
        private List<SKImage> _offerImages;

        public BaseOfferDisplayData(UObject uObject, EIconStyle style) : base(uObject, style)
        {
        }

        public override void ParseForInfo()
        {
            if (Object.ExportType != "AthenaItemShopOfferDisplayData")
                return;

            if (!Object.TryGetValue(out UMaterialInterface[] presentations, "Presentations"))
                    return;

            _offerImages = new List<SKImage>();
            foreach (var p in presentations)
            {
                var offerImage = new BaseMaterialInstance(p, Style);
                offerImage.ParseForInfo();
                _offerImages.Add(offerImage.Draw());
            }
        }

        public override SKImage Draw()
        {
            int imageOrder;

            if (_offerImages.Count < 4)
                imageOrder = _offerImages.Count;
            else if (_offerImages.Count == 4)
                imageOrder = 2;
            else if (_offerImages.Count <= 9)
                imageOrder = 3;
            else imageOrder = 5;

            Width = 512 * imageOrder;
            Height = _offerImages.Count / imageOrder;

            if (_offerImages.Count % imageOrder != 0)
                Height++;

            Height *= 512;

            using var bitmap = new SKBitmap(Width, Height);
            using var canvas = new SKCanvas(bitmap);
            var point = new SKPoint(0, 0);

            for (int i = 0, placement = 0; i < _offerImages.Count; i++)
            {
                if (placement >= imageOrder)
                {
                    placement = 0;
                    point.Y += 512;
                }
                point.X = 512 * placement;

                canvas.DrawImage(_offerImages[i], point);
                placement++;
            }

            return SKImage.FromBitmap(bitmap);
        }
    }
}
