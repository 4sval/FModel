using System;
using System.Collections.ObjectModel;
using System.Linq;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse_Conversion.Textures;
using FModel.Extensions;
using FModel.Framework;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using SharpDX;

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

        private bool _showWireframe;
        public bool ShowWireframe
        {
            get => _showWireframe;
            set => SetProperty(ref _showWireframe, value);
        }

        private Geometry3D _mesh;
        public Geometry3D Mesh
        {
            get => _mesh;
            set => SetProperty(ref _mesh, value);
        }
        
        private Material _meshMat;
        public Material MeshMat
        {
            get => _meshMat;
            set => SetProperty(ref _meshMat, value);
        }
        
        public ObservableCollection<Geometry3D> Lods { get; }

        public ModelViewerViewModel()
        {
            Lods = new ObservableCollection<Geometry3D>();
            EffectManager = new DefaultEffectsManager();
            Cam = new PerspectiveCamera
            {
                NearPlaneDistance = 0.1,
                FarPlaneDistance = 10000000,
                FieldOfView = 80
            };
        }

        public void NextLod() => Mesh = Lods.Next(Mesh);
        public void PreviousLod() => Mesh = Lods.Previous(Mesh);

        public void LoadExport(UObject export)
        {
            Lods.Clear();
            Mesh = null;
            MeshMat = PhongMaterials.Bisque;
            
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
        
        private void LoadStaticMesh(UStaticMesh mesh)
        {
            if (!mesh.TryConvert(out var convertedMesh) || convertedMesh.LODs.Count <= 0)
            {
                return;
            }
            
            SetupCameraAndAxis(convertedMesh.BoundingBox.Min, convertedMesh.BoundingBox.Max);

            var pushedMaterial = false;
            foreach (var lod in convertedMesh.LODs)
            {
                if (lod.SkipLod) continue;
                
                PushLod(lod.Verts, lod.Indices.Value);
                if (!pushedMaterial)
                {
                    PushMaterial(lod.Sections.Value);
                    pushedMaterial = true;
                }
            }
            Mesh = Lods.First();
        }
        
        private void LoadSkeletalMesh(USkeletalMesh mesh)
        {
            if (!mesh.TryConvert(out var convertedMesh) || convertedMesh.LODs.Count <= 0)
            {
                return;
            }

            SetupCameraAndAxis(convertedMesh.BoundingBox.Min, convertedMesh.BoundingBox.Max);

            var pushedMaterial = false;
            foreach (var lod in convertedMesh.LODs)
            {
                if (lod.SkipLod) continue;
                
                PushLod(lod.Verts, lod.Indices.Value);
                if (!pushedMaterial)
                {
                    PushMaterial(lod.Sections.Value);
                    pushedMaterial = true;
                }
            }
            Mesh = Lods.First();
        }

        private void PushLod(CMeshVertex[] verts, FRawStaticIndexBuffer indices)
        {
            var builder = new MeshBuilder {TextureCoordinates = new Vector2Collection()};
            for (var i = 0; i < verts.Length; i++)
            {
                builder.AddNode(
                    new Vector3(verts[i].Position.X, -verts[i].Position.Y, verts[i].Position.Z),
                    new Vector3(verts[i].Normal.X, verts[i].Normal.Y, verts[i].Normal.Z),
                    new Vector2(verts[i].UV.U, verts[i].UV.V));
            }
            
            for (var i = 0; i < indices.Length; i++)
            {
                builder.TriangleIndices.Add(indices[i]);
            }

            Lods.Add(builder.ToMesh());
        }

        private void PushMaterial(CMeshSection[] sections)
        {
            for (var j = 0; j < sections.Length; j++)
            {
                if (sections[j].Material == null || !sections[j].Material.TryLoad<UMaterialInterface>(out var unrealMaterial))
                    continue;

                var parameters = new CMaterialParams();
                unrealMaterial.GetParams(parameters);
                if (parameters.Diffuse is not UTexture2D diffuse) continue;
                MeshMat = new DiffuseMaterial {DiffuseMap = new TextureModel(diffuse.Decode()?.Encode().AsStream())};
                break;
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
            lineBuilder.AddLine(new Vector3(0, 0, 0), new Vector3(max.X, 0, 0));
            XAxis = lineBuilder.ToLineGeometry3D();
            lineBuilder = new LineBuilder();
            lineBuilder.AddLine(new Vector3(0, 0, 0), new Vector3(0, max.Y, 0));
            YAxis = lineBuilder.ToLineGeometry3D();
            lineBuilder = new LineBuilder();
            lineBuilder.AddLine(new Vector3(0, 0, 0), new Vector3(0, 0, max.Z));
            ZAxis = lineBuilder.ToLineGeometry3D();
        }
    }
}