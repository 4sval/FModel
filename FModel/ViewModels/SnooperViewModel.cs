using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.GeometryCollection;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Engine;
using Editor;
using FModel.Framework;
using FModel.Services;
using Snooper.Rendering.Actors;
using Snooper.Rendering.Components;
using Snooper.Rendering.Components.Light;
using Snooper.Rendering.Components.Mesh;
using Snooper.Rendering.Components.Primitive;
using Snooper.Rendering.Components.Transforms;

namespace FModel.ViewModels;

public class SnooperViewModel : ViewModel, IDisposable
{
    public static SnooperViewModel Instance { get; } = new();

    private readonly SnooperHost _host;

    private SnooperViewModel()
    {
        var scale = GetDpiScale();
        var htz = GetMaxRefreshFrequency();
        var width = Convert.ToInt32(SystemParameters.MaximizedPrimaryScreenWidth * .9 * scale);
        var height = Convert.ToInt32(SystemParameters.MaximizedPrimaryScreenHeight * .85 * scale);

        _host = new SnooperHost(() => new EditorWindow(htz, width, height, ApplicationService.ApplicationView.CUE4Parse.Provider, false, true));
    }

    public void Load(UObject? obj)
    {
        Actor? actor = obj switch
        {
            UStaticMesh sm => new MeshActor(sm),
            USkeletalMesh sk => new MeshActor(sk),
            UGeometryCollection gc => new MeshActor(gc),
            UWorld w => new WorldActor(w),
            _ => null
        };

        var editor = _host.Window;
        editor.Invoke(() =>
        {
            if (editor.Manager.RootActor == null)
                editor.Manager.LoadScene(CreateScene(actor is WorldActor));

            if (actor != null)
                editor.Manager.LoadScene(actor);

            editor.Show();

            if (actor?.RootComponent is MeshComponent mesh)
            {
                mesh.SnapToGround();
                mesh.UpdateWorldMatrix(); // just so teleport works properly
                mesh.TeleportTo();
            }
        });
    }

    private Actor CreateScene(bool transparentGrid)
    {
        var scene = new Actor("Scene");
        scene.Components.Add(new BoxComponent(Vector3.Zero, Vector3.One));

        var grid = new Actor("Grid");
        grid.Components.Add(transparentGrid ? new GridComponent() : new OpaqueGridComponent());
        scene.Children.Add(grid);

        var camera = new CameraActor("Camera");
        camera.CameraComponent.SetLocalTransform(new Transform(new Vector3(0, 1.14f, 3.5f), new Vector3(180, 18, 0)));
        scene.Children.Add(camera);

        const float distance = 500;
        var sun = new Actor("Sun Light");
        sun.Components.Add(new DirectionalLightComponent(MathF.PI, new Vector3(1.00f, 0.96f, 0.90f), new Transform(new Vector3(-distance, distance, distance), new Vector3(140, 37, 0)), "Directional Light"));
        scene.Children.Add(sun);

        return scene;
    }

    public void Run()
    {
        var editor = _host.Window;
        editor.Invoke(editor.Show);
    }

    [DllImport("user32.dll")]
    private static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

    [StructLayout(LayoutKind.Sequential)]
    private struct DEVMODE
    {
        private const int CCHDEVICENAME = 0x20;
        private const int CCHFORMNAME = 0x20;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public ScreenOrientation dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;

    }

    private static float GetDpiScale()
    {
        if (Screen.PrimaryScreen is not { } primaryScreen)
            return 1.0f;

        return (float)Math.Max(
            primaryScreen.Bounds.Width / SystemParameters.PrimaryScreenWidth,
            primaryScreen.Bounds.Height / SystemParameters.PrimaryScreenHeight
        );
    }

    private static int GetMaxRefreshFrequency()
    {
        var rf = 60;
        var vDevMode = new DEVMODE();
        var i = 0;
        while (EnumDisplaySettings(null, i, ref vDevMode))
        {
            i++;
            rf = Math.Max(rf, vDevMode.dmDisplayFrequency);
        }

        return rf;
    }

    public void Dispose() => _host.Dispose();
}
