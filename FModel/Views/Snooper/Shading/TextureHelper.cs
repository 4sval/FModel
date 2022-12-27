using CUE4Parse.UE4.Assets.Exports.Texture;

namespace FModel.Views.Snooper.Shading;

public static class TextureHelper
{
    private static readonly string _game = Services.ApplicationService.ApplicationView.CUE4Parse.Provider.GameName;

    /// <summary>
    /// Red : Specular (if possible)
    /// Blue : Roughness
    /// Green : Metallic
    /// </summary>
    public static void FixChannels(UTexture2D o, FTexture2DMipMap mip, ref byte[] data)
    {
        // only if it makes a big difference pls
        switch (_game)
        {
            case "hk_project":
            case "gameface":
            case "divineknockout":
            {
                unsafe
                {
                    var offset = 0;
                    fixed (byte* d = data)
                    {
                        for (var i = 0; i < mip.SizeX * mip.SizeY; i++)
                        {
                            (d[offset + 1], d[offset + 2]) = (d[offset + 2], d[offset + 1]); // RBG
                            offset += 4;
                        }
                    }
                }
                break;
            }
            // R: Roughness
            // G: Metallic
            // B: Whatever (AO / S / E / ...)
            case "ccff7r":
            {
                unsafe
                {
                    var offset = 0;
                    fixed (byte* d = data)
                    {
                        for (var i = 0; i < mip.SizeX * mip.SizeY; i++)
                        {
                            (d[offset + 1], d[offset + 2]) = (d[offset + 2], d[offset + 1]); // RBG
                            (d[offset], d[offset + 1]) = (d[offset + 1], d[offset]); // BRG
                            offset += 4;
                        }
                    }
                }
                break;
            }
            case "shootergame":
            {
                var packedPBRType = o.Name[(o.Name.LastIndexOf('_') + 1)..];
                switch (packedPBRType)
                {
                    case "MRAE": // R: Metallic, G: Roughness, B: AO (0-127) & Emissive (128-255)   (Character PBR)
                        unsafe
                        {
                            var offset = 0;
                            fixed (byte* d = data)
                            {
                                for (var i = 0; i < mip.SizeX * mip.SizeY; i++)
                                {
                                    (d[offset], d[offset + 1]) = (d[offset + 1], d[offset]); // RMAE
                                    // (d[offset], d[offset + 2]) = (d[offset + 2], d[offset]); // AEMR
                                    offset += 4;
                                }
                            }
                        }
                        break;
                    case "MRAS": // R: Metallic, G: Roughness, B: AO, A: Specular   (Legacy PBR)
                    case "MRA": // R: Metallic, G: Roughness, B: AO                (Environment PBR)
                    case "MRS": // R: Metallic, G: Roughness, B: Specular          (Weapon PBR)
                        unsafe
                        {
                            var offset = 0;
                            fixed (byte* d = data)
                            {
                                for (var i = 0; i < mip.SizeX * mip.SizeY; i++)
                                {
                                    (d[offset], d[offset + 2]) = (d[offset + 2], d[offset]); // SRM
                                    (d[offset + 1], d[offset + 2]) = (d[offset + 2], d[offset + 1]); // SMR
                                    offset += 4;
                                }
                            }
                        }
                        break;
                }
                break;
            }
        }
    }
}
