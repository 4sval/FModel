using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Media3D;
using CUE4Parse_Conversion;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.Utils;
using CUE4Parse_Conversion.Materials;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Meshes.glTF;
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
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
using SharpGLTF.Transforms;
using SkiaSharp;
using Camera = HelixToolkit.Wpf.SharpDX.Camera;
using Exporter = CUE4Parse_Conversion.Exporter;
using Geometry3D = HelixToolkit.SharpDX.Core.Geometry3D;
using PerspectiveCamera = HelixToolkit.Wpf.SharpDX.PerspectiveCamera;
using Vector2 = SharpDX.Vector2;
using Vector3 = SharpDX.Vector3;
using VERTEX = SharpGLTF.Geometry.VertexTypes.VertexPositionNormalTangent;

namespace FModel.ViewModels;

public class ModelViewerViewModel : ViewModel
{
    private ThreadWorkerViewModel _threadWorkerView => ApplicationService.ThreadWorkerView;

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

    private ModelAndCam _selectedModel; // selected mesh
    public ModelAndCam SelectedModel
    {
        get => _selectedModel;
        set => SetProperty(ref _selectedModel, value);
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

    public void LoadExport(UObject export)
    {
#if DEBUG
        LoadHDRi();
#endif

        ModelAndCam p;
        if (AppendMode && CanAppend)
        {
            p = SelectedModel;
            _loadedModels.Add(new ModelAndCam(export) { IsVisible = false });
        }
        else
        {
            p = new ModelAndCam(export);
            _loadedModels.Add(p);
        }

        switch (export)
        {
            case UStaticMesh st:
                LoadStaticMesh(st, p);
                break;
            case USkeletalMesh sk:
                LoadSkeletalMesh(sk, p);
                break;
            case UMaterialInstance mi:
                LoadMaterialInstance(mi, p);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(export));
        }

        if (AppendMode && CanAppend) return;
        SelectedModel = p;
        Cam.UpDirection = new Vector3D(0, 1, 0);
        Cam.Position = p.Position;
        Cam.LookDirection = p.LookDirection;
    }

    #region PUBLIC METHODS

    public void RenderingToggle()
    {
        if (SelectedModel == null) return;
        SelectedModel.RenderingToggle = !SelectedModel.RenderingToggle;
    }

    public void WirefreameToggle()
    {
        if (SelectedModel == null) return;
        SelectedModel.WireframeToggle = !SelectedModel.WireframeToggle;
    }

    public void MaterialColorToggle()
    {
        if (SelectedModel == null) return;
        SelectedModel.ShowMaterialColor = !SelectedModel.ShowMaterialColor;
    }

    public void DiffuseOnlyToggle()
    {
        if (SelectedModel == null) return;
        SelectedModel.DiffuseOnlyToggle = !SelectedModel.DiffuseOnlyToggle;
    }

    public void FocusOnSelectedMesh()
    {
        Cam.AnimateTo(SelectedModel.Position, SelectedModel.LookDirection, new Vector3D(0, 1, 0), 500);
    }

    public async Task SaveLoadedModels()
    {
        if (_loadedModels.Count < 1) return;

        var folderBrowser = new VistaFolderBrowserDialog { ShowNewFolderButton = true };
        if (folderBrowser.ShowDialog() == false) return;

        await _threadWorkerView.Begin(_ =>
        {
            var exportOptions = new ExporterOptions
            {
                TextureFormat = UserSettings.Default.TextureExportFormat,
                LodFormat = UserSettings.Default.LodExportFormat,
                MeshFormat = UserSettings.Default.MeshExportFormat,
                Platform = UserSettings.Default.OverridedPlatform,
                ExportMorphTargets = UserSettings.Default.SaveMorphTargets
            };
            foreach (var model in _loadedModels)
            {
                var toSave = new Exporter(model.Export, exportOptions);
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
        });
    }

    public void SaveAsScene()
    {
        if (_loadedModels.Count < 1) return;

        var fileBrowser = new VistaSaveFileDialog
        {
            Title = "Save Loaded Models As...",
            DefaultExt = ".glb",
            Filter = "glTF Binary File (*.glb)|*.glb|glTF ASCII File (*.gltf)|*.gltf|All Files(*.*)|*.*",
            AddExtension = true,
            OverwritePrompt = true,
            CheckPathExists = true
        };

        if (fileBrowser.ShowDialog() == false || string.IsNullOrEmpty(fileBrowser.FileName)) return;

        var sceneBuilder = new SceneBuilder();
        var materialExports = new List<MaterialExporter>();
        foreach (var model in _loadedModels)
        {
            switch (model.Export)
            {
                case UStaticMesh sm:
                {
                    var mesh = new MeshBuilder<VERTEX, VertexColorXTextureX, VertexEmpty>(sm.Name);
                    if (sm.TryConvert(out var convertedMesh) && convertedMesh.LODs.Count > 0)
                    {
                        var lod = convertedMesh.LODs.First();
                        for (var i = 0; i < lod.Sections.Value.Length; i++)
                        {
                            Gltf.ExportStaticMeshSections(i, lod, lod.Sections.Value[i], materialExports, mesh);
                        }

                        sceneBuilder.AddRigidMesh(mesh, AffineTransform.Identity);
                    }

                    break;
                }
                case USkeletalMesh sk:
                {
                    var mesh = new MeshBuilder<VERTEX, VertexColorXTextureX, VertexJoints4>(sk.Name);

                    if (sk.TryConvert(out var convertedMesh) && convertedMesh.LODs.Count > 0)
                    {
                        var lod = convertedMesh.LODs.First();
                        for (var i = 0; i < lod.Sections.Value.Length; i++)
                        {
                            Gltf.ExportSkelMeshSections(i, lod, lod.Sections.Value[i], materialExports, mesh);
                        }

                        var armatureNodeBuilder = new NodeBuilder(sk.Name + ".ao");
                        var armature = Gltf.CreateGltfSkeleton(convertedMesh.RefSkeleton, armatureNodeBuilder);
                        sceneBuilder.AddSkinnedMesh(mesh, Matrix4x4.Identity, armature);
                    }

                    break;
                }
            }
        }

        var scene = sceneBuilder.ToGltf2();
        var fileName = fileBrowser.FileName;
        if (fileName.EndsWith(".glb", StringComparison.OrdinalIgnoreCase))
            scene.SaveGLB(fileName);
        else if (fileName.EndsWith(".gltf", StringComparison.OrdinalIgnoreCase))
            scene.SaveGLTF(fileName);
        else if (fileName.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
            scene.SaveAsWavefront(fileName);
        else
            throw new ArgumentOutOfRangeException(nameof(fileName), $@"Unknown file format {fileName.SubstringAfterWithLast('.')}");

        if (!CheckIfSaved(fileName)) return;
        foreach (var materialExport in materialExports)
        {
            materialExport.TryWriteToDir(new DirectoryInfo(StringUtils.SubstringBeforeWithLast(fileName, '\\')), out _);
        }
    }

    public void CopySelectedMaterialName()
    {
        if (SelectedModel is not { } m || m.SelectedGeometry is null)
            return;

        Clipboard.SetText(m.SelectedGeometry.DisplayName.TrimEnd());
    }

    #endregion

    public bool TryOverwriteMaterial(UMaterialInstance materialInstance)
    {
        if (SelectedModel?.SelectedGeometry == null || _loadedModels.Count < 1) return false;

        var (m, _, _) = LoadMaterial(materialInstance);

        var obj = new ResolvedLoadedObject(materialInstance);
        switch (_loadedModels[SelectedModel.SelectedGeometry.ExportIndex].Export)
        {
            case UStaticMesh { Materials: { } } st:
                st.Materials[SelectedModel.SelectedGeometry.MaterialIndex] = obj;
                break;
            case USkeletalMesh sk:
                sk.Materials[SelectedModel.SelectedGeometry.MaterialIndex].Material = obj;
                break;
            case UMaterialInstance:
                SelectedModel.SwapExport(materialInstance);
                break;
        }

        SelectedModel.SelectedGeometry.Material = m;
        return m != null;
    }

    private void LoadMaterialInstance(UMaterialInstance materialInstance, ModelAndCam cam)
    {
        var builder = new MeshBuilder();
        builder.AddBox(Vector3.Zero, 10, 10, 10);
        cam.TriangleCount = 12; // no need to count

        SetupCameraAndAxis(new FBox(new FVector(-8), new FVector(8)), cam);
        var (m, isRendering, isTransparent) = LoadMaterial(materialInstance);

        cam.Group3d.Add(new CustomMeshGeometryModel3D
        {
            Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), -45)),
            DisplayName = materialInstance.Name, Geometry = builder.ToMeshGeometry3D(), MaterialIndex = 0,
            Material = m, IsTransparent = isTransparent, IsRendering = isRendering, ExportIndex = _loadedModels.Count - 1
        });
    }

    private void LoadStaticMesh(UStaticMesh mesh, ModelAndCam cam)
    {
        if (!mesh.TryConvert(out var convertedMesh) || convertedMesh.LODs.Count <= 0)
        {
            return;
        }

        SetupCameraAndAxis(convertedMesh.BoundingBox, cam);
        foreach (var lod in convertedMesh.LODs.Where(lod => !lod.SkipLod))
        {
            PushLod(lod.Sections.Value, lod.Verts, lod.Indices.Value, cam);
            break;
        }
    }

    private void LoadSkeletalMesh(USkeletalMesh mesh, ModelAndCam cam)
    {
        if (!mesh.TryConvert(out var convertedMesh) || convertedMesh.LODs.Count <= 0)
        {
            return;
        }

        SetupCameraAndAxis(convertedMesh.BoundingBox, cam);
        foreach (var lod in convertedMesh.LODs.Where(lod => !lod.SkipLod))
        {
            PushLod(lod.Sections.Value, lod.Verts, lod.Indices.Value, cam);
            break;
        }
    }

    private void PushLod(CMeshSection[] sections, CMeshVertex[] verts, FRawStaticIndexBuffer indices, ModelAndCam cam)
    {
        for (var i = 0; i < sections.Length; i++) // each section is a mesh part with its own material
        {
            var section = sections[i];
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

                    builder.AddNode(p, n, new Vector2(vert.UV.U, vert.UV.V));
                    builder.TriangleIndices.Add(j * 3 + t); // one mesh part is "j * 3 + t" use "id" if you're building the full mesh
                }
            }

            if (section.Material == null || !section.Material.TryLoad(out var o) || o is not UMaterialInterface material)
            {
                cam.Group3d.Add(new CustomMeshGeometryModel3D
                {
                    DisplayName = section.Material?.Name.ToString() ?? $"material_{section.MaterialIndex}", MaterialIndex = section.MaterialIndex,
                    Geometry = builder.ToMeshGeometry3D(), Material = new PBRMaterial(), IsTransparent = false,
                    IsRendering = true, ExportIndex = _loadedModels.Count - 1
                });
            }
            else
            {
                var (m, isRendering, isTransparent) = LoadMaterial(material);
                cam.Group3d.Add(new CustomMeshGeometryModel3D
                {
                    DisplayName = material.Name, MaterialIndex = section.MaterialIndex,
                    Geometry = builder.ToMeshGeometry3D(), Material = m, IsTransparent = isTransparent,
                    IsRendering = isRendering, ExportIndex = _loadedModels.Count - 1
                });
            }
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
            if (!parameters.HasTopDiffuseTexture && parameters.DiffuseColor is { A: > 0 } diffuseColor)
            {
                m.AlbedoColor = new Color4(diffuseColor.R, diffuseColor.G, diffuseColor.B, diffuseColor.A);
            }
            else if (parameters.Diffuse is UTexture2D diffuse)
            {
                m.AlbedoMap = new TextureModel(diffuse.Decode(UserSettings.Default.OverridedPlatform)?.Encode(SKEncodedImageFormat.Png, 100).AsStream());
            }

            if (parameters.Normal is UTexture2D normal)
            {
                m.NormalMap = new TextureModel(normal.Decode(UserSettings.Default.OverridedPlatform)?.Encode(SKEncodedImageFormat.Png, 100).AsStream());
            }

            if (parameters.Specular is UTexture2D specular)
            {
                var mip = specular.GetFirstMip();
                var platform = UserSettings.Default.OverridedPlatform;
                TextureDecoder.DecodeTexture(mip, specular.Format, specular.isNormalMap, platform, out var data, out var colorType);

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
                            {
                                for (var i = 0; i < mip.SizeX * mip.SizeY; i++)
                                {
                                    d[offset] = 0;
                                    (d[offset + 1], d[offset + 2]) = (d[offset + 2], d[offset + 1]); // swap G and B
                                    offset += 4;
                                }
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
                                    {
                                        for (var i = 0; i < mip.SizeX * mip.SizeY; i++)
                                        {
                                            (d[offset], d[offset + 2]) = (d[offset + 2], d[offset]); // swap R and B
                                            (d[offset], d[offset + 1]) = (d[offset + 1], d[offset]); // swap R and G
                                            offset += 4;
                                        }
                                    }
                                }

                                break;
                            case "MRAS": // R: Metallic, B: Roughness, B: AO, A: Specular   (Legacy PBR)
                            case "MRA": // R: Metallic, B: Roughness, B: AO                (Environment PBR)
                            case "MRS": // R: Metallic, G: Roughness, B: Specular          (Weapon PBR)
                                unsafe
                                {
                                    var offset = 0;
                                    fixed (byte* d = data)
                                    {
                                        for (var i = 0; i < mip.SizeX * mip.SizeY; i++)
                                        {
                                            (d[offset], d[offset + 2]) = (d[offset + 2], d[offset]); // swap R and B
                                            offset += 4;
                                        }
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
                            {
                                for (var i = 0; i < mip.SizeX * mip.SizeY; i++)
                                {
                                    (d[offset], d[offset + 2]) = (d[offset + 2], d[offset]); // swap R and B
                                    offset += 4;
                                }
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

            if (parameters.HasTopEmissiveTexture && parameters.Emissive is UTexture2D emissive && parameters.EmissiveColor is { A: > 0 } emissiveColor)
            {
                m.EmissiveColor = new Color4(emissiveColor.R, emissiveColor.G, emissiveColor.B, emissiveColor.A);
                m.EmissiveMap = new TextureModel(emissive.Decode(UserSettings.Default.OverridedPlatform)?.Encode(SKEncodedImageFormat.Png, 100).AsStream());
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

    private bool CheckIfSaved(string path)
    {
        if (File.Exists(path))
        {
            Log.Information("Successfully saved {FileName}", path);
            FLogger.AppendInformation();
            FLogger.AppendText($"Successfully saved {path}", Constants.WHITE, true);
            return true;
        }

        Log.Error("{FileName} could not be saved", path);
        FLogger.AppendError();
        FLogger.AppendText($"Could not save '{path}'", Constants.WHITE, true);
        return false;
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
    public UObject Export { get; private set; }
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

    private bool _renderingToggle;
    public bool RenderingToggle
    {
        get => _renderingToggle;
        set
        {
            SetProperty(ref _renderingToggle, value);
            foreach (var g in Group3d)
            {
                if (g is not CustomMeshGeometryModel3D geometryModel)
                    continue;

                geometryModel.IsRendering = !geometryModel.IsRendering;
            }
        }
    }

    private bool _wireframeToggle;
    public bool WireframeToggle
    {
        get => _wireframeToggle;
        set
        {
            SetProperty(ref _wireframeToggle, value);
            foreach (var g in Group3d)
            {
                if (g is not CustomMeshGeometryModel3D geometryModel)
                    continue;

                geometryModel.RenderWireframe = !geometryModel.RenderWireframe;
            }
        }
    }

    private bool _showMaterialColor;
    public bool ShowMaterialColor
    {
        get => _showMaterialColor;
        set
        {
            SetProperty(ref _showMaterialColor, value);
            for (var i = 0; i < Group3d.Count; i++)
            {
                if (Group3d[i] is not CustomMeshGeometryModel3D { Material: PBRMaterial material } m)
                    continue;

                var index = B(i);
                material.RenderAlbedoMap = !_showMaterialColor;

                if (_showMaterialColor)
                {
                    m.Tag = material.AlbedoColor;
                    material.AlbedoColor = new Color4(_table[C(index)] / 255, _table[C(index >> 1)] / 255, _table[C(index >> 2)] / 255, 1);
                }
                else material.AlbedoColor = (Color4) m.Tag;
            }
        }
    }

    private bool _diffuseOnlyToggle;
    public bool DiffuseOnlyToggle
    {
        get => _diffuseOnlyToggle;
        set
        {
            SetProperty(ref _diffuseOnlyToggle, value);
            foreach (var g in Group3d)
            {
                if (g is not CustomMeshGeometryModel3D { Material: PBRMaterial material })
                    continue;

                material.RenderAmbientOcclusionMap = !material.RenderAmbientOcclusionMap;
                material.RenderDisplacementMap = !material.RenderDisplacementMap;
                // material.RenderEmissiveMap = !material.RenderEmissiveMap;
                // material.RenderEnvironmentMap = !material.RenderEnvironmentMap;
                material.RenderIrradianceMap = !material.RenderIrradianceMap;
                material.RenderRoughnessMetallicMap = !material.RenderRoughnessMetallicMap;
                material.RenderShadowMap = !material.RenderShadowMap;
                material.RenderNormalMap = !material.RenderNormalMap;
            }
        }
    }

    private CustomMeshGeometryModel3D _selectedGeometry; // selected material
    public CustomMeshGeometryModel3D SelectedGeometry
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

    private readonly float[] _table = { 255 * 0.9f, 25 * 3.0f, 255 * 0.6f, 255 * 0.0f };
    private readonly int[] _table2 = { 0, 1, 2, 4, 7, 3, 5, 6 };

    public ModelAndCam(UObject export)
    {
        Export = export;
        TriangleCount = 0;
        Group3d = new ObservableElement3DCollection();
    }

    private int B(int x) => (x & 0xFFF8) | _table2[x & 7] ^ 7;
    private int C(int x) => (x & 1) | ((x >> 2) & 2);

    public void SwapExport(UObject e)
    {
        Export = e;
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

public class CustomMeshGeometryModel3D : MeshGeometryModel3D
{
    public string DisplayName { get; set; }
    public int MaterialIndex { get; set; }
    public int ExportIndex { get; set; }
}
