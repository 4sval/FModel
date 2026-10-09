using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using CUE4Parse.GameTypes.Nascar.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Exports.GeometryCollection;
using CUE4Parse.UE4.Assets.Exports.Houdini;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Engine;
using Editor;
using Editor.Managers;
using FModel.Framework;
using FModel.Services;
using FModel.Settings;
using Snooper.Hosting;
using Snooper.Rendering.Actors;
using Snooper.Rendering.Components;
using Snooper.Rendering.Components.Mesh;
using Snooper.Rendering.Components.Primitive;
using Snooper.Rendering.Components.Transforms;

namespace FModel.ViewModels;

public class SnooperViewModel : ViewModel, IDisposable
{
    public static SnooperViewModel Instance { get; } = new();

    private readonly SnooperHost _host;
    private readonly SunActor _sun;
    private readonly EnvironmentActor _environment;

    /// <summary>
    /// Mirrors <see cref="Bridge.PendingRequest"/> for bindings.
    /// </summary>
    public AssetRequest PendingRequest
    {
        get;
        private set => SetProperty(ref field, value);
    }

    private SnooperViewModel()
    {
        var scale = GetDpiScale();
        var htz = GetMaxRefreshFrequency();
        var width = Convert.ToInt32(SystemParameters.MaximizedPrimaryScreenWidth * .9 * scale);
        var height = Convert.ToInt32(SystemParameters.MaximizedPrimaryScreenHeight * .85 * scale);

        Bridge.Host = new FModelBridgeHost();
        Bridge.PendingRequestChanged += request => PendingRequest = request;

        _host = new SnooperHost(() => new EditorWindow(htz, width, height, ApplicationService.ApplicationView.CUE4Parse.Provider, false, true));
        _sun = new SunActor();
        _environment = new EnvironmentActor();
    }

    public void Load(UObject? obj)
    {
        SetOptions();

        Actor? actor = obj switch
        {
            UStaticMesh sm => new MeshActor(sm),
            UIRMesh ir => new MeshActor(ir),
            UHoudiniStaticMesh hsm => new MeshActor(hsm),
            USkinnedAsset sa => new MeshActor(sa),
            UGeometryCollection gc => new MeshActor(gc),
            UAnimationAsset anim => new MeshActor(anim),
            UBlueprintGeneratedClass bp => new BlueprintActor(bp),
            UWorld w => new WorldActor(w),
            _ => null
        };

        if (actor != null && actor.Components.Count == 0 && actor.Children.Count == 0)
        {
            // there's nothing to show
            return;
        }

        var editor = _host.Window;
        editor.Invoke(() =>
        {
            if (editor.Manager.RootActor == null)
            {
                // FIFO load the scene
                var scene = new Actor("Scene");
                scene.Components.Add(new BoxComponent(Vector3.Zero, Vector3.One)); // just so debug system is always loaded
                editor.Manager.LoadScene(scene);
                editor.Manager.LoadScene(_sun);
                editor.Manager.LoadScene(_environment);
                editor.Manager.LoadScene(new GridActor(actor is not MeshActor));
                editor.Manager.LoadScene(new CameraActor());
            }

            if (actor != null)
                editor.Manager.LoadScene(actor);

            editor.Show();

            if (actor?.RootComponent is MeshComponent mesh)
            {
                if (mesh.SnapToGround())
                    mesh.UpdateWorldMatrix(); // just so teleport works properly
                mesh.TeleportTo();

                if (editor.Manager is InterfaceManager ui)
                    ui.SelectComponent(mesh);
            }
        });
    }

    public bool TryDeliver(UObject obj)
    {
        if (!Bridge.TryDeliver(obj)) return false;

        var editor = _host.Window;
        editor.Invoke(editor.Show);
        return true;
    }

    private void SetOptions()
    {
        var settings = UserSettings.Default;
        var options = Bridge.Options;

        options.NaniteMeshFormat = settings.NaniteMeshExportFormat;
        options.MaterialDepth = settings.MaterialExportFormat;
        options.TexturePlatform = settings.CurrentDir.TexturePlatform;
        options.LoadMorphTargets = settings.SaveMorphTargets;
        options.MaxTextureMipSize = settings.PreviewMaxTextureSize;
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
