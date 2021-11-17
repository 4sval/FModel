using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
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
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using RestSharp;
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

        private ObservableElement3DCollection _group3d;
        public ObservableElement3DCollection Group3d
        {
            get => _group3d;
            set => SetProperty(ref _group3d, value);
        }

        public TextureModel HDRi { get; private set; }

        private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;

        private readonly int[] _facesIndex = { 1, 0, 2 };

        public ModelViewerViewModel()
        {
            EffectManager = new DefaultEffectsManager();
            Group3d = new ObservableElement3DCollection();
            Cam = new PerspectiveCamera { NearPlaneDistance = 0.1, FarPlaneDistance = double.PositiveInfinity, FieldOfView = 80 };
            LoadHDRi();
        }

        private void LoadHDRi()
        {
            var hdri = new FileInfo(
                Path.Combine(UserSettings.Default.OutputDirectory, ".data", "approaching_storm.jpg"));
            if (!hdri.Exists)
            {
                var request = new RestRequest($"https://dl.polyhaven.org/file/ph-assets/HDRIs/extra/Tonemapped%20JPG/approaching_storm.jpg", Method.GET);
                var response = new RestClient().ExecuteAsync(request).Result;
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    var b = new Bitmap(1, 1);
                    b.SetPixel(0, 0, System.Drawing.Color.DarkGray);
                    var stream = new MemoryStream();
                    b.Save(stream, ImageFormat.Jpeg);
                    HDRi = TextureModel.Create(stream);
                    return;
                }
                // Blur makes reflections soft but these black borders...
                // var temp = SKImage.FromBitmap(SKBitmap.Decode(response.RawBytes));
                // var blur = SKImageFilter.CreateBlur(20, 20, SKShaderTileMode.Clamp); // this adds black borders. WHY?
                // var blurred = temp.ApplyImageFilter(blur, temp.Info.Rect, temp.Info.Rect, out SKRectI _, out SKPoint _);
                using (var writer = hdri.OpenWrite())
                    writer.Write(response.RawBytes);
            }
            HDRi = new TextureModel(hdri.ToString());
        }

        public void LoadExport(UObject export)
        {
            if (!AppendModeEnabled) Clear();
            switch (export)
            {
                case UStaticMesh st:
                    LoadStaticMesh(st);
                    break;
                case USkeletalMesh sk:
                    LoadSkeletalMesh(sk);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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

        private void LoadStaticMesh(UStaticMesh mesh)
        {
            if (!mesh.TryConvert(out var convertedMesh) || convertedMesh.LODs.Count <= 0)
            {
                return;
            }

            SetupCameraAndAxis(convertedMesh.BoundingBox.Min, convertedMesh.BoundingBox.Max);

            foreach (var lod in convertedMesh.LODs)
            {
                if (lod.SkipLod) continue;
                PushLod(lod.Sections.Value, lod.Verts, lod.Indices.Value);
                break;
            }
        }

        private void LoadSkeletalMesh(USkeletalMesh mesh)
        {
            if (!mesh.TryConvert(out var convertedMesh) || convertedMesh.LODs.Count <= 0)
            {
                return;
            }

            SetupCameraAndAxis(convertedMesh.BoundingBox.Min, convertedMesh.BoundingBox.Max);

            foreach (var lod in convertedMesh.LODs)
            {
                if (lod.SkipLod) continue;
                PushLod(lod.Sections.Value, lod.Verts, lod.Indices.Value);
                break;
            }
        }

        private void PushLod(CMeshSection[] sections, CMeshVertex[] verts, FRawStaticIndexBuffer indices)
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
                        var p = new Vector3(vert.Position.X, -vert.Position.Y, vert.Position.Z);
                        var n = new Vector3(vert.Normal.X, -vert.Normal.Y, vert.Normal.Z);
                        n.Normalize();
                        var uv = new Vector2(vert.UV.U, vert.UV.V);
                        builder.AddNode(p, n, uv);
                        builder.TriangleIndices.Add(j * 3 + t); // one mesh part is "j * 3 + t" use "id" if you're building the full mesh
                    }
                }

                if (section.Material == null || !section.Material.TryLoad<UMaterialInterface>(out var unrealMaterial))
                    continue;

                var m = new PBRMaterial() { RenderShadowMap = true, EnableAutoTangent = true, RenderEnvironmentMap = true };
                var parameters = new CMaterialParams();
                unrealMaterial.GetParams(parameters);

                var isRendering = !parameters.IsNull;
                if (isRendering)
                {
                    if (parameters.Diffuse is UTexture2D diffuse)
                        m.AlbedoMap = new TextureModel(diffuse.Decode()?.Encode().AsStream());
                    if (parameters.Normal is UTexture2D normal)
                        m.NormalMap = new TextureModel(normal.Decode()?.Encode().AsStream());

                    if (_applicationView.CUE4Parse.Game == FGame.FortniteGame)
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

                Group3d.Add(new MeshGeometryModel3D
                {
                    Name = unrealMaterial.Name,
                    Geometry = builder.ToMeshGeometry3D(),
                    Material = m,
                    IsRendering = isRendering
                });
            }
        }

        private void SetupCameraAndAxis(FVector min, FVector max)
        {
            var minOfMin = min.Min();
            var maxOfMax = max.Max();
            Cam.UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 0, 1);
            Cam.Position = new System.Windows.Media.Media3D.Point3D(maxOfMax, maxOfMax, (minOfMin + maxOfMax) / 1.25);
            Cam.LookDirection = new System.Windows.Media.Media3D.Vector3D(-Cam.Position.X, -Cam.Position.Y, 0);

            var lineBuilder = new LineBuilder();
            lineBuilder.AddLine(new Vector3(0, 0, 0), new Vector3(100, 0, 0));
            XAxis = lineBuilder.ToLineGeometry3D();
            lineBuilder = new LineBuilder();
            lineBuilder.AddLine(new Vector3(0, 0, 0), new Vector3(0, 100, 0));
            YAxis = lineBuilder.ToLineGeometry3D();
            lineBuilder = new LineBuilder();
            lineBuilder.AddLine(new Vector3(0, 0, 0), new Vector3(0, 0, 100));
            ZAxis = lineBuilder.ToLineGeometry3D();
        }

        private void Clear()
        {
            foreach (var g in Group3d.ToList())
            {
                g.Dispose();
                Group3d.Remove(g);
            }
        }
    }
}
