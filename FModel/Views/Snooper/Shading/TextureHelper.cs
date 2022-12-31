using CUE4Parse.UE4.Assets.Exports.Texture;

namespace FModel.Views.Snooper.Shading;

public static class TextureHelper
{
    private static readonly string _game = Services.ApplicationService.ApplicationView.CUE4Parse.Provider.GameName;

    /// <summary>
    /// Red : Specular (not used anymore)
    /// Green : Metallic
    /// Blue : Roughness
    /// </summary>
    public static void FixChannels(UTexture2D o, FTexture2DMipMap mip, ref byte[] data)
    {
        // only if it makes a big difference pls
        switch (_game)
        {
            case "hk_project":
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
            // R: Metallic
            // G: Roughness
            // B: Whatever (AO / S / E / ...)
            case "shootergame":
            case "divineknockout":
            {
                unsafe
                {
                    var offset = 0;
                    fixed (byte* d = data)
                    {
                        for (var i = 0; i < mip.SizeX * mip.SizeY; i++)
                        {
                            (d[offset], d[offset + 1]) = (d[offset + 1], d[offset]); // GRB
                            (d[offset], d[offset + 2]) = (d[offset + 2], d[offset]); // RBG
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
        }
    }
}
