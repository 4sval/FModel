using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
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
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using SharpDX;
using SkiaSharp;
using Camera = HelixToolkit.Wpf.SharpDX.Camera;
using Geometry3D = HelixToolkit.SharpDX.Core.Geometry3D;
using PerspectiveCamera = HelixToolkit.Wpf.SharpDX.PerspectiveCamera;

namespace FModel.ViewModels
{
    public class ModelViewerViewModel : ViewModel
    {
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

        private bool _appendModeEnabled;
        public bool AppendModeEnabled
        {
            get => _appendModeEnabled;
            set => SetProperty(ref _appendModeEnabled, value);
        }

        private MeshGeometryModel3D _selectedGeometry;
        public MeshGeometryModel3D SelectedGeometry
        {
            get => _selectedGeometry;
            set
            {
                SetProperty(ref _selectedGeometry, value);
                if (_selectedGeometry == null || !_geometries.TryGetValue(_selectedGeometry, out var camAxis)) return;

                XAxis = camAxis.XAxis;
                YAxis = camAxis.YAxis;
                ZAxis = camAxis.ZAxis;
            }
        }

        private ObservableElement3DCollection _group3d;
        public ObservableElement3DCollection Group3d
        {
            get => _group3d;
            set => SetProperty(ref _group3d, value);
        }

        public TextureModel HDRi { get; private set; }

        private readonly FGame _game;
        private readonly int[] _facesIndex = { 1, 0, 2 };
        private readonly List<UObject> _meshes;
        private readonly Dictionary<MeshGeometryModel3D, CamAxisHolder> _geometries;

        public ModelViewerViewModel(FGame game)
        {
            _game = game;
            _meshes = new List<UObject>();
            _geometries = new Dictionary<MeshGeometryModel3D, CamAxisHolder>();

            EffectManager = new DefaultEffectsManager();
            Group3d = new ObservableElement3DCollection();
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
            if (!AppendModeEnabled) Clear();

            _meshes.Add(export);
            switch (export)
            {
                case UStaticMesh st:
                    LoadStaticMesh(st);
                    break;
                case USkeletalMesh sk:
                    LoadSkeletalMesh(sk);
                    break;
                case UMaterialInstance mi:
                    LoadMaterialInstance(mi);
                    break;
                default: // idiot
                    throw new ArgumentOutOfRangeException();
            }

            if (_geometries.Count < 1) return;
            foreach (var geometry in _geometries.Keys)
            {
                // Tag is used as a flag to tell if the geometry is already in Group3d
                if (geometry.Tag is not (bool and false)) continue;

                geometry.Tag = true;
                Group3d.Add(geometry);
            }

            if (AppendModeEnabled || Group3d[0] is not MeshGeometryModel3D selected ||
                !_geometries.TryGetValue(selected, out var camAxis)) return;

            SelectedGeometry = selected;
            Cam.UpDirection = new Vector3D(0, 1, 0);
            Cam.Position = camAxis.Position;
            Cam.LookDirection = camAxis.LookDirection;
        }

        public void RenderingToggle()
        {
            foreach (var g in Group3d)
            {
                if (g is not MeshGeometryModel3D geometryModel)
                    continue;

                geometryModel.IsRendering = !geometryModel.IsRendering;
            }
        }

        public void WirefreameToggle()
        {
            foreach (var g in Group3d)
            {
                if (g is not MeshGeometryModel3D geometryModel)
                    continue;

                geometryModel.RenderWireframe = !geometryModel.RenderWireframe;
            }
        }

        public void DiffuseOnlyToggle()
        {
            foreach (var g in Group3d)
            {
                if (g is not MeshGeometryModel3D geometryModel)
                    continue;

                if (geometryModel.Material is PBRMaterial mat)
                {
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
        }

        public void FocusOnSelectedGeometry()
        {
            if (!_geometries.TryGetValue(_selectedGeometry, out var camAxis)) return;
            Cam.AnimateTo(camAxis.Position, camAxis.LookDirection, new Vector3D(0, 1, 0), 500);
        }

        private void LoadMaterialInstance(UMaterialInstance materialInstance)
        {
            var builder = new MeshBuilder();
            builder.AddSphere(Vector3.Zero, 10);

            var camAxis = SetupCameraAndAxis(new FBox(new FVector(-15), new FVector(15)));
            var (m, isRendering) = LoadMaterial(materialInstance);

            _geometries.Add(new MeshGeometryModel3D
            {
                Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1,0,0), -90)),
                Name = materialInstance.Name.Replace('-', '_'), Geometry = builder.ToMeshGeometry3D(),
                Material = m, IsRendering = isRendering, Tag = false // flag
            }, camAxis);
        }

        private void LoadStaticMesh(UStaticMesh mesh)
        {
            if (!mesh.TryConvert(out var convertedMesh) || convertedMesh.LODs.Count <= 0)
            {
                return;
            }

            var camAxis = SetupCameraAndAxis(convertedMesh.BoundingBox);
            foreach (var lod in convertedMesh.LODs)
            {
                if (lod.SkipLod) continue;
                PushLod(lod.Sections.Value, lod.Verts, lod.Indices.Value, camAxis);
                break;
            }
        }

        private void LoadSkeletalMesh(USkeletalMesh mesh)
        {
            if (!mesh.TryConvert(out var convertedMesh) || convertedMesh.LODs.Count <= 0)
            {
                return;
            }

            var camAxis = SetupCameraAndAxis(convertedMesh.BoundingBox);
            foreach (var lod in convertedMesh.LODs)
            {
                if (lod.SkipLod) continue;
                PushLod(lod.Sections.Value, lod.Verts, lod.Indices.Value, camAxis);
                break;
            }
        }

        private void PushLod(CMeshSection[] sections, CMeshVertex[] verts, FRawStaticIndexBuffer indices, CamAxisHolder camAxis)
        {
            foreach (var section in sections) // each section is a mesh part with its own material
            {
                var builder = new MeshBuilder();
                // NumFaces * 3 (triangle) = next section FirstIndex
                for (var j = 0; j < section.NumFaces; j++) // draw a triangle for each face
                {
                    foreach (var t in _facesIndex) // triangle face 1 then 0 then 2
                    {
                        var id = section.FirstIndex + j * 3 + t;
                        var vert = verts[indices[id]];
                        var p = new Vector3(vert.Position.X, vert.Position.Z, -vert.Position.Y); // up direction is Y
                        var n = new Vector3(vert.Normal.X, vert.Normal.Z, -vert.Normal.Y);
                        n.Normalize();
                        var uv = new Vector2(vert.UV.U, vert.UV.V);
                        builder.AddNode(p, n, uv);
                        builder.TriangleIndices.Add(j * 3 + t); // one mesh part is "j * 3 + t" use "id" if you're building the full mesh
                    }
                }

                if (section.Material == null || !section.Material.TryLoad<UMaterialInterface>(out var unrealMaterial))
                    continue;

                var (m, isRendering) = LoadMaterial(unrealMaterial);

                _geometries.Add(new MeshGeometryModel3D
                {
                    Name = unrealMaterial.Name.Replace('-', '_'), Geometry = builder.ToMeshGeometry3D(),
                    Material = m, IsRendering = isRendering, Tag = false // flag
                }, camAxis);
            }
        }

        private (PBRMaterial material, bool isRendering) LoadMaterial(UMaterialInterface unrealMaterial)
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

                if (_game == FGame.FortniteGame)
                {
                    // Fortnite's Specular Texture Channels
                    // R Specular
                    // G Metallic
                    // B Roughness
                    if (parameters.Specular is UTexture2D specular)
                    {
                        var mip = specular.GetFirstMip();
                        TextureDecoder.DecodeTexture(mip, specular.Format, specular.isNormalMap,
                            out var data, out var colorType);

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

                        var width = mip.SizeX;
                        var height = mip.SizeY;
                        using var bitmap = new SKBitmap(new SKImageInfo(width, height, colorType, SKAlphaType.Unpremul));
                        unsafe
                        {
                            fixed (byte* p = data)
                            {
                                bitmap.SetPixels(new IntPtr(p));
                            }
                        }

                        // R -> AO G -> Roughness B -> Metallic
                        m.RoughnessMetallicMap = new TextureModel(bitmap.Encode(SKEncodedImageFormat.Png, 100).AsStream());
                        m.RenderAmbientOcclusionMap = false; // red channel is not ao
                        m.MetallicFactor = 1;
                        m.RoughnessFactor = 1;
                    }
                }
                // if (parameters.Specular is UTexture2D specular)
                //     m.AmbientOcculsionMap = new TextureModel(specular.Decode()?.Encode().AsStream());
                // if (parameters.Specular is UTexture2D specularPower)
                // {
                //     m.RoughnessFactor = parameters.MobileSpecularPower;
                //     m.RoughnessMetallicMap = new TextureModel(specularPower.Decode()?.Encode().AsStream());
                // }
                // if (parameters.Emissive is UTexture2D emissive)
                // {
                //     m.EmissiveColor = Color4.White; // FortniteGame/Content/Characters/Player/Female/Medium/Bodies/F_MED_Obsidian/Meshes/F_MED_Obsidian.uasset
                //     m.EmissiveMap = new TextureModel(emissive.Decode()?.Encode().AsStream());
                // }
            }
            else
            {
                m = new PBRMaterial { AlbedoColor = new Color4(1, 0, 0, 1) }; //PhongMaterials.Red;
            }

            return (m, isRendering);
        }

        private CamAxisHolder SetupCameraAndAxis(FBox box)
        {
            var ret = new CamAxisHolder();
            var meanX = (box.Max.X + box.Min.X) / 2;
            var meanY = (box.Max.Y + box.Min.Y) / 2;
            var meanZ = (box.Max.Z + box.Min.Z) / 2;

            var lineBuilder = new LineBuilder();
            lineBuilder.AddLine(new Vector3(box.Min.X, meanZ, -meanY), new Vector3(box.Max.X, meanZ, -meanY));
            ret.XAxis = lineBuilder.ToLineGeometry3D();
            lineBuilder = new LineBuilder();
            lineBuilder.AddLine(new Vector3(meanX, box.Min.Z, -meanY), new Vector3(meanX, box.Max.Z, -meanY));
            ret.YAxis = lineBuilder.ToLineGeometry3D();
            lineBuilder = new LineBuilder();
            lineBuilder.AddLine(new Vector3(meanX, meanZ, -box.Min.Y), new Vector3(meanX, meanZ, -box.Max.Y));
            ret.ZAxis = lineBuilder.ToLineGeometry3D();

            ret.Position = new Point3D(box.Max.X + meanX * 2, meanZ, -box.Min.Y - meanY * 2);
            ret.LookDirection = new Vector3D(-ret.Position.X + meanX, 0, -ret.Position.Z - meanY);
            return ret;
        }

        private void Clear()
        {
            _meshes.Clear();
            _geometries.Clear();
            foreach (var g in Group3d.ToList())
            {
                g.Dispose();
                Group3d.Remove(g);
            }
        }
    }

    public class CamAxisHolder
    {
        public Point3D Position;
        public Vector3D LookDirection;
        public Geometry3D XAxis;
        public Geometry3D YAxis;
        public Geometry3D ZAxis;
    }
}
