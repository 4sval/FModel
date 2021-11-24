using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Media3D;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse_Conversion.Textures;
using FModel.Framework;
using FModel.Services;
using FModel.Settings;
using FModel.Views.Resources.Controls;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using Ookii.Dialogs.Wpf;
using Serilog;
using SharpDX;
using SkiaSharp;
using Camera = HelixToolkit.Wpf.SharpDX.Camera;
using Geometry3D = HelixToolkit.SharpDX.Core.Geometry3D;
using PerspectiveCamera = HelixToolkit.Wpf.SharpDX.PerspectiveCamera;

namespace FModel.ViewModels
{
    public class ModelViewerViewModel : ViewModel
    {
        private ThreadWorkerViewModel _threadWorkerView => ApplicationService.ThreadWorkerView;

        #region BINDINGS
        private EffectsManager _effectManager;
        public EffectsManager EffectManager
        {
            get => _effectManager;
            set => SetProperty(ref _effectManager, value);
        }

        private Camera _cam;
        public Camera Cam
        {
            get => _cam;
            set => SetProperty(ref _cam, value);
        }

        private Geometry3D _xAxis;
        public Geometry3D XAxis
        {
            get => _xAxis;
            set => SetProperty(ref _xAxis, value);
        }

        private Geometry3D _yAxis;
        public Geometry3D YAxis
        {
            get => _yAxis;
            set => SetProperty(ref _yAxis, value);
        }

        private Geometry3D _zAxis;
        public Geometry3D ZAxis
        {
            get => _zAxis;
            set => SetProperty(ref _zAxis, value);
        }

        private ModelAndCam _selectedModel; // selected mesh
        public ModelAndCam SelectedModel
        {
            get => _selectedModel;
            set
            {
                SetProperty(ref _selectedModel, value);
                if (_selectedModel == null) return;

                XAxis = _selectedModel.XAxis;
                YAxis = _selectedModel.YAxis;
                ZAxis = _selectedModel.ZAxis;
                Cam.UpDirection = new Vector3D(0, 1, 0);
                Cam.Position = _selectedModel.Position;
                Cam.LookDirection = _selectedModel.LookDirection;
            }
        }

        private readonly ObservableCollection<ModelAndCam> _loadedModels; // mesh list
        public ICollectionView LoadedModelsView { get; }

        private bool _appendMode;
        public bool AppendMode
        {
            get => _appendMode;
            set => SetProperty(ref _appendMode, value);
        }

        public bool CanAppend => SelectedModel != null;

        public TextureModel HDRi { get; private set; }
        #endregion

        private readonly FGame _game;
        private readonly int[] _facesIndex = { 1, 0, 2 };

        public ModelViewerViewModel(FGame game)
        {
            _game = game;
            _loadedModels = new ObservableCollection<ModelAndCam>();

            EffectManager = new DefaultEffectsManager();
            LoadedModelsView = new ListCollectionView(_loadedModels);
            Cam = new PerspectiveCamera { NearPlaneDistance = 0.1, FarPlaneDistance = double.PositiveInfinity, FieldOfView = 90 };
            LoadHDRi();
        }

        private void LoadHDRi()
        {
            var cubeMap = Application.GetResourceStream(new Uri("/FModel;component/Resources/approaching_storm_cubemap.dds", UriKind.Relative));
            HDRi = TextureModel.Create(cubeMap?.Stream);
        }

        public async Task LoadExport(UObject export)
        {
#if DEBUG
            LoadHDRi();
#endif

            ModelAndCam p;
            if (AppendMode && CanAppend)
            {
                p = SelectedModel;
                _loadedModels.Add(new ModelAndCam(export) {IsVisible = false});
            }
            else
            {
                p = new ModelAndCam(export);
                _loadedModels.Add(p);
            }

            bool valid = false;
            await _threadWorkerView.Begin(_ =>
            {
                valid = export switch
                {
                    UStaticMesh st => TryLoadStaticMesh(st, p),
                    USkeletalMesh sk => TryLoadSkeletalMesh(sk, p),
                    UMaterialInstance mi => TryLoadMaterialInstance(mi, p),
                    _ => throw new ArgumentOutOfRangeException(nameof(export))
                };
            });
            if (!valid) return;
            SelectedModel = p;
        }

        #region PUBLIC METHODS
        public void RenderingToggle()
        {
            if (SelectedModel == null) return;
            foreach (var g in SelectedModel.Group3d)
            {
                if (g is not MeshGeometryModel3D geometryModel)
                    continue;

                geometryModel.IsRendering = !geometryModel.IsRendering;
            }
        }

        public void WirefreameToggle()
        {
            if (SelectedModel == null) return;
            foreach (var g in SelectedModel.Group3d)
            {
                if (g is not MeshGeometryModel3D geometryModel)
                    continue;

                geometryModel.RenderWireframe = !geometryModel.RenderWireframe;
            }
        }

        public void DiffuseOnlyToggle()
        {
            if (SelectedModel == null) return;
            foreach (var g in SelectedModel.Group3d)
            {
                if (g is not MeshGeometryModel3D { Material: PBRMaterial mat })
                    continue;

                //mat.RenderAmbientOcclusionMap = !mat.RenderAmbientOcclusionMap;
                mat.RenderDisplacementMap = !mat.RenderDisplacementMap;
                //mat.RenderEmissiveMap = !mat.RenderEmissiveMap;
                mat.RenderEnvironmentMap = !mat.RenderEnvironmentMap;
                mat.RenderIrradianceMap = !mat.RenderIrradianceMap;
                mat.RenderRoughnessMetallicMap = !mat.RenderRoughnessMetallicMap;
                mat.RenderShadowMap = !mat.RenderShadowMap;
                mat.RenderNormalMap = !mat.RenderNormalMap;
            }
        }

        public void FocusOnSelectedMesh()
        {
            Cam.AnimateTo(SelectedModel.Position, SelectedModel.LookDirection, new Vector3D(0, 1, 0), 500);
        }

        public void SaveLoadedModels()
        {
            if (_loadedModels.Count < 1) return;

            var folderBrowser = new VistaFolderBrowserDialog {ShowNewFolderButton = true};
            if (folderBrowser.ShowDialog() == false) return;

            foreach (var model in _loadedModels)
            {
                var toSave = new CUE4Parse_Conversion.Exporter(model.Export, UserSettings.Default.TextureExportFormat, UserSettings.Default.LodExportFormat, UserSettings.Default.MeshExportFormat);
                if (toSave.TryWriteToDir(new DirectoryInfo(folderBrowser.SelectedPath), out var savedFileName))
                {
                    Log.Information("Successfully saved {FileName}", savedFileName);
                    FLogger.AppendInformation();
                    FLogger.AppendText($"Successfully saved {savedFileName}", Constants.WHITE, true);
                }
                else
                {
                    Log.Error("{FileName} could not be saved", savedFileName);
                    FLogger.AppendError();
                    FLogger.AppendText($"Could not save '{savedFileName}'", Constants.WHITE, true);
                }
            }
        }

        public void CopySelectedMaterialName()
        {
            if (SelectedModel is not { } m || m.SelectedGeometry is null)
                return;

            Clipboard.SetText(m.SelectedGeometry.Name.TrimEnd());
        }

        public async Task<bool> TryChangeSelectedMaterial(UMaterialInstance materialInstance)
        {
            if (SelectedModel is not { } model || model.SelectedGeometry is null)
                return false;

            PBRMaterial m = null;
            await _threadWorkerView.Begin(_ =>
            {
                var (material, _, _) = LoadMaterial(materialInstance);
                m = material;
            });

            if (m == null) return false;
            model.SelectedGeometry.Material = m;
            return true;
        }
        #endregion

        private bool TryLoadMaterialInstance(UMaterialInstance materialInstance, ModelAndCam cam)
        {
            var builder = new MeshBuilder();
            builder.AddSphere(Vector3.Zero, 10);
            cam.TriangleCount = 1984; // no need to count

            SetupCameraAndAxis(new FBox(new FVector(-11), new FVector(11)), cam);
            var (m, isRendering, isTransparent) = LoadMaterial(materialInstance);

            Application.Current.Dispatcher.Invoke(() =>
            {
                cam.Group3d.Add(new MeshGeometryModel3D
                {
                    Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1,0,0), -90)),
                    Name = FixName(materialInstance.Name), Geometry = builder.ToMeshGeometry3D(),
                    Material = m, IsTransparent = isTransparent, IsRendering = isRendering
                });
            });
            return true;
        }

        private bool TryLoadStaticMesh(UStaticMesh mesh, ModelAndCam cam)
        {
            if (!mesh.TryConvert(out var convertedMesh) || convertedMesh.LODs.Count <= 0)
            {
                return false;
            }

            SetupCameraAndAxis(convertedMesh.BoundingBox, cam);
            foreach (var lod in convertedMesh.LODs)
            {
                if (lod.SkipLod) continue;
                PushLod(lod.Sections.Value, lod.Verts, lod.Indices.Value, cam);
                break;
            }

            return true;
        }

        private bool TryLoadSkeletalMesh(USkeletalMesh mesh, ModelAndCam cam)
        {
            if (!mesh.TryConvert(out var convertedMesh) || convertedMesh.LODs.Count <= 0)
            {
                return false;
            }

            SetupCameraAndAxis(convertedMesh.BoundingBox, cam);
            foreach (var lod in convertedMesh.LODs)
            {
                if (lod.SkipLod) continue;
                PushLod(lod.Sections.Value, lod.Verts, lod.Indices.Value, cam);
                break;
            }

            return true;
        }

        private void PushLod(CMeshSection[] sections, CMeshVertex[] verts, FRawStaticIndexBuffer indices, ModelAndCam cam)
        {
            foreach (var section in sections) // each section is a mesh part with its own material
            {
                var builder = new MeshBuilder();
                cam.TriangleCount += section.NumFaces; // NumFaces * 3 (triangle) = next section FirstIndex
                for (var j = 0; j < section.NumFaces; j++) // draw a triangle for each face
                {
                    foreach (var t in _facesIndex) // triangle face 1 then 0 then 2
                    {
                        var id = section.FirstIndex + j * 3 + t;
                        var vert = verts[indices[id]];
                        var p = new Vector3(vert.Position.X, vert.Position.Z, vert.Position.Y); // up direction is Y
                        var n = new Vector3(vert.Normal.X, vert.Normal.Z, vert.Normal.Y);
                        n.Normalize();
                        var uv = new Vector2(vert.UV.U, vert.UV.V);
                        builder.AddNode(p, n, uv);
                        builder.TriangleIndices.Add(j * 3 + t); // one mesh part is "j * 3 + t" use "id" if you're building the full mesh
                    }
                }

                if (section.Material == null || !section.Material.TryLoad<UMaterialInterface>(out var unrealMaterial))
                    continue;

                var (m, isRendering, isTransparent) = LoadMaterial(unrealMaterial);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    cam.Group3d.Add(new MeshGeometryModel3D
                    {
                        Name = FixName(unrealMaterial.Name), Geometry = builder.ToMeshGeometry3D(),
                        Material = m, IsTransparent = isTransparent, IsRendering = isRendering
                    });
                });
            }
        }

        private (PBRMaterial material, bool isRendering, bool isTransparent) LoadMaterial(UMaterialInterface unrealMaterial)
        {
            var m = new PBRMaterial { RenderShadowMap = true, EnableAutoTangent = true, RenderEnvironmentMap = true };
            var parameters = new CMaterialParams();
            unrealMaterial.GetParams(parameters);

            var isRendering = !parameters.IsNull;
            if (isRendering)
            {
                if (parameters.Diffuse is UTexture2D diffuse)
                    m.AlbedoMap = new TextureModel(diffuse.Decode()?.Encode().AsStream());
                if (parameters.Normal is UTexture2D normal)
                    m.NormalMap = new TextureModel(normal.Decode()?.Encode().AsStream());
                if (parameters.Specular is UTexture2D specular)
                {
                    var mip = specular.GetFirstMip();
                    TextureDecoder.DecodeTexture(mip, specular.Format, specular.isNormalMap,
                        out var data, out var colorType);

                    switch (_game)
                    {
                        case FGame.FortniteGame:
                        {
                            // Fortnite's Specular Texture Channels
                            // R Specular
                            // G Metallic
                            // B Roughness
                            unsafe
                            {
                                var offset = 0;
                                fixed (byte* d = data)
                                    for (var i = 0; i < mip.SizeX * mip.SizeY; i++)
                                    {
                                        d[offset] = 0;
                                        (d[offset+1], d[offset+2]) = (d[offset+2], d[offset+1]); // swap G and B
                                        offset += 4;
                                    }
                            }
                            parameters.RoughnessValue = 1;
                            parameters.MetallicValue = 1;
                            break;
                        }
                        case FGame.ShooterGame:
                        {
                            var packedPBRType = specular.Name[(specular.Name.LastIndexOf('_') + 1)..];
                            switch (packedPBRType)
                            {
                                case "MRAE": // R: Metallic, G: AO (0-127) & Emissive (128-255), B: Roughness   (Character PBR)
                                    unsafe
                                    {
                                        var offset = 0;
                                        fixed (byte* d = data)
                                            for (var i = 0; i < mip.SizeX * mip.SizeY; i++)
                                            {
                                                (d[offset], d[offset+2]) = (d[offset+2], d[offset]); // swap R and B
                                                (d[offset], d[offset+1]) = (d[offset+1], d[offset]); // swap R and G
                                                offset += 4;
                                            }
                                    }
                                    break;
                                case "MRAS": // R: Metallic, B: Roughness, B: AO, A: Specular   (Legacy PBR)
                                case "MRA":  // R: Metallic, B: Roughness, B: AO                (Environment PBR)
                                case "MRS":  // R: Metallic, G: Roughness, B: Specular          (Weapon PBR)
                                    unsafe
                                    {
                                        var offset = 0;
                                        fixed (byte* d = data)
                                            for (var i = 0; i < mip.SizeX * mip.SizeY; i++)
                                            {
                                                (d[offset], d[offset+2]) = (d[offset+2], d[offset]); // swap R and B
                                                offset += 4;
                                            }
                                    }
                                    break;
                            }
                            parameters.RoughnessValue = 1;
                            parameters.MetallicValue = 1;
                            break;
                        }
                        case FGame.Gameface:
                        {
                            // GTA's Specular Texture Channels
                            // R Metallic
                            // G Roughness
                            // B Specular
                            unsafe
                            {
                                var offset = 0;
                                fixed (byte* d = data)
                                    for (var i = 0; i < mip.SizeX * mip.SizeY; i++)
                                    {
                                        (d[offset], d[offset+2]) = (d[offset+2], d[offset]); // swap R and B
                                        offset += 4;
                                    }
                            }
                            break;
                        }
                    }

                    using var bitmap = new SKBitmap(new SKImageInfo(mip.SizeX, mip.SizeY, colorType, SKAlphaType.Unpremul));
                    unsafe
                    {
                        fixed (byte* p = data)
                        {
                            bitmap.SetPixels(new IntPtr(p));
                        }
                    }

                    // R -> AO G -> Roughness B -> Metallic
                    m.RoughnessMetallicMap = new TextureModel(bitmap.Encode(SKEncodedImageFormat.Png, 100).AsStream());
                    m.RoughnessFactor = parameters.RoughnessValue;
                    m.MetallicFactor = parameters.MetallicValue;
                    m.RenderAmbientOcclusionMap = parameters.SpecularValue > 0;
                }
            }
            else
            {
                m.AlbedoColor = new Color4(1, 0, 0, 1);
            }

            return (m, isRendering, parameters.IsTransparent);
        }

        private void SetupCameraAndAxis(FBox box, ModelAndCam cam)
        {
            if (AppendMode && CanAppend) return;
            var center = box.GetCenter();

            var lineBuilder = new LineBuilder();
            lineBuilder.AddLine(new Vector3(box.Min.X, center.Z, center.Y), new Vector3(box.Max.X, center.Z, center.Y));
            cam.XAxis = lineBuilder.ToLineGeometry3D();
            lineBuilder = new LineBuilder();
            lineBuilder.AddLine(new Vector3(center.X, box.Min.Z, center.Y), new Vector3(center.X, box.Max.Z, center.Y));
            cam.YAxis = lineBuilder.ToLineGeometry3D();
            lineBuilder = new LineBuilder();
            lineBuilder.AddLine(new Vector3(center.X, center.Z, box.Min.Y), new Vector3(center.X, center.Z, box.Max.Y));
            cam.ZAxis = lineBuilder.ToLineGeometry3D();

            cam.Position = new Point3D(box.Max.X + center.X * 2, center.Z, box.Min.Y + center.Y * 2);
            cam.LookDirection = new Vector3D(-cam.Position.X + center.X, 0, -cam.Position.Z + center.Y);
        }

        private string FixName(string input)
        {
            if (input.Length < 1)
                return "Material_Has_No_Name";

            if (int.TryParse(input[0].ToString(), out _))
                input = input[1..];

            return input.Replace('-', '_');
        }

        public void Clear()
        {
            foreach (var g in _loadedModels.ToList())
            {
                g.Dispose();
                _loadedModels.Remove(g);
            }
        }
    }

    public class ModelAndCam : ViewModel
    {
        public UObject Export { get; }
        public Point3D Position { get; set; }
        public Vector3D LookDirection { get; set; }
        public Geometry3D XAxis { get; set; }
        public Geometry3D YAxis { get; set; }
        public Geometry3D ZAxis { get; set; }
        public int TriangleCount { get; set; }

        private bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        private MeshGeometryModel3D _selectedGeometry; // selected material
        public MeshGeometryModel3D SelectedGeometry
        {
            get => _selectedGeometry;
            set => SetProperty(ref _selectedGeometry, value);
        }

        private ObservableElement3DCollection _group3d; // material list
        public ObservableElement3DCollection Group3d
        {
            get => _group3d;
            set => SetProperty(ref _group3d, value);
        }

        public ModelAndCam(UObject export)
        {
            Export = export;
            TriangleCount = 0;
            Group3d = new ObservableElement3DCollection();
        }

        public void Dispose()
        {
            TriangleCount = 0;
            SelectedGeometry = null;
            foreach (var g in Group3d.ToList())
            {
                g.Dispose();
                Group3d.Remove(g);
            }
        }
    }
}
