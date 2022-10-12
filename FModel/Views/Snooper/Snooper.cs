using System;
using System.Threading;
using System.Windows;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace FModel.Views.Snooper;

public class Snooper
{
    private readonly FWindow _window;

    public Snooper()
    {
        const double ratio = .7;
        var x = SystemParameters.MaximizedPrimaryScreenWidth;
        var y = SystemParameters.MaximizedPrimaryScreenHeight;

        var options = NativeWindowSettings.Default;
        options.Size = new Vector2i(Convert.ToInt32(x * ratio), Convert.ToInt32(y * ratio));
        options.WindowBorder = WindowBorder.Fixed;
        options.Location = new Vector2i(Convert.ToInt32(x / 2.0) / (options.Size.X / 2), Convert.ToInt32(y / 2.0) / (options.Size.Y / 2));
        options.NumberOfSamples = Constants.SAMPLES_COUNT;
        options.Title = "Snooper";

        _window = new FWindow(GameWindowSettings.Default, options);
    }

    public void Run(CancellationToken cancellationToken, UObject export)
    {
        _window.Run();
    }

    public void SwapMaterial(UMaterialInstance mi)
    {
        _window.SwapMaterial(mi);
    }
}
