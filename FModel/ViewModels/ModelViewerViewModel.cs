using System;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse_Conversion.Meshes;
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

        public ModelViewerViewModel()
        {
            EffectManager = new DefaultEffectsManager();
            Cam = new PerspectiveCamera
            {
                NearPlaneDistance = 0.1,
                FarPlaneDistance = 10000000,
                FieldOfView = 80
            };
        }

        public void LoadExport(UObject export)
        {
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
            if (!mesh.TryConvert(out var convertedMesh) || convertedMesh.LODs.Length <= 0)
            {
                return;
            }

            var max = convertedMesh.BoundingBox.Max.Max();
            Cam.UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
            Cam.Position = new System.Windows.Media.Media3D.Point3D(max / 2, max, max / 2);
            Cam.LookDirection = new System.Windows.Media.Media3D.Vector3D(-Cam.Position.X, -Cam.Position.Y / 2, -Cam.Position.Z);
            
            var builder = new MeshBuilder();
            for (var i = 0; i < convertedMesh.LODs.Length; i++)
            {
                for (var j = 0; j < convertedMesh.LODs[i].NumVerts; j++)
                {
                    var suv = convertedMesh.LODs[i].Verts[j];
                    builder.Positions.Add(new Vector3(suv.Position.X, suv.Position.Y, suv.Position.Z));
                    builder.Normals.Add(new Vector3(suv.Normal.X, suv.Normal.Y, suv.Normal.Z));
                }

                var numIndices = convertedMesh.LODs[i].Indices.Value.Length;
                for (var j = 0; j < numIndices; j++)
                {
                    builder.TriangleIndices.Add(convertedMesh.LODs[i].Indices.Value[j]);
                }
                break;
            }
            Mesh = builder.ToMesh();
            MeshMat = DiffuseMaterials.White;
        }
        
        private void LoadSkeletalMesh(USkeletalMesh mesh)
        {
            if (!mesh.TryConvert(out var convertedMesh) || convertedMesh.LODs.Length <= 0)
            {
                return;
            }

            var max = convertedMesh.BoundingBox.Max.Max();
            Cam.UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
            Cam.Position = new System.Windows.Media.Media3D.Point3D(max / 2, max, max / 2);
            Cam.LookDirection = new System.Windows.Media.Media3D.Vector3D(-Cam.Position.X, -Cam.Position.Y / 2, -Cam.Position.Z);
            
            var builder = new MeshBuilder();
            for (var i = 0; i < convertedMesh.LODs.Length; i++)
            {
                for (var j = 0; j < convertedMesh.LODs[i].NumVerts; j++)
                {
                    var suv = convertedMesh.LODs[i].Verts[j];
                    builder.Positions.Add(new Vector3(suv.Position.X, suv.Position.Y, suv.Position.Z));
                    builder.Normals.Add(new Vector3(suv.Normal.X, suv.Normal.Y, suv.Normal.Z));
                }

                var numIndices = convertedMesh.LODs[i].Indices.Value.Length;
                for (var j = 0; j < numIndices; j++)
                {
                    builder.TriangleIndices.Add(convertedMesh.LODs[i].Indices.Value[j]);
                }
                break;
            }
            Mesh = builder.ToMesh();
            MeshMat = DiffuseMaterials.White;
        }
    }
}