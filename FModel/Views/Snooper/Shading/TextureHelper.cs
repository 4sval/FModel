using OpenTK.Graphics.OpenGL4;

namespace FModel.Views.Snooper.Shading;

public static class TextureHelper
{
    /// <summary>
    /// Red : Specular (not used anymore)
    /// Green : Metallic
    /// Blue : Roughness
    /// </summary>
    public static void FixChannels(string game, Texture texture)
    {
        switch (game)
        {
            // R: Whatever (AO / S / E / ...)
            // G: Roughness
            // B: Metallic
            case "GAMEFACE":
            case "HK_PROJECT":
            case "COSMICSHAKE":
            case "PHOENIX":
            case "ATOMICHEART":
            {
                texture.SwizzleMask =
                [
                    (int) PixelFormat.Red,
                    (int) PixelFormat.Blue,
                    (int) PixelFormat.Green,
                    (int) PixelFormat.Alpha
                ];
                break;
            }
            // R: Metallic
            // G: Roughness
            // B: Whatever (AO / S / E / ...)
            case "SHOOTERGAME":
            case "DIVINEKNOCKOUT":
            case "MOONMAN":
            {
                texture.SwizzleMask =
                [
                    (int) PixelFormat.Blue,
                    (int) PixelFormat.Red,
                    (int) PixelFormat.Green,
                    (int) PixelFormat.Alpha
                ];
                break;
            }
            // R: Roughness
            // G: Metallic
            // B: Whatever (AO / S / E / ...)
            case "CCFF7R":
            case "PJ033":
            {
                texture.SwizzleMask =
                [
                    (int) PixelFormat.Blue,
                    (int) PixelFormat.Green,
                    (int) PixelFormat.Red,
                    (int) PixelFormat.Alpha
                ];
                break;
            }
        }
        texture.Swizzle();
    }
}
