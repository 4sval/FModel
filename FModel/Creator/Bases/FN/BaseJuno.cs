using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.UObject;
using SkiaSharp;

namespace FModel.Creator.Bases.FN;

public class BaseJuno : BaseIcon
{
    private BaseIcon _character;

    public BaseJuno(UObject uObject, EIconStyle style) : base(uObject, style)
    {

    }

    public override void ParseForInfo()
    {
        if (Object.TryGetValue(out FSoftObjectPath baseCid, "BaseAthenaCharacterItemDefinition", "LowDetailsAssembledMeshSchema") &&
            Utils.TryLoadObject(baseCid.AssetPathName.Text, out UObject cid))
        {
            _character = new BaseIcon(cid, Style);
            _character.ParseForInfo();

            if (Object.TryGetValue(out FSoftObjectPath assembledMeshSchema, "AssembledMeshSchema") &&
                Utils.TryLoadObject(assembledMeshSchema.AssetPathName.Text, out UObject meshSchema) &&
                meshSchema.TryGetValue(out FInstancedStruct[] additionalData, "AdditionalData"))
            {
                foreach (var data in additionalData)
                {
                    if (data.NonConstStruct?.TryGetValue(out FSoftObjectPath largePreview, "LargePreviewImage", "SmallPreviewImage") ?? false)
                    {
                        _character.Preview = Utils.GetBitmap(largePreview);
                        break;
                    }
                }
            }
        }
    }

    public override SKBitmap[] Draw() => _character.Draw();
}
