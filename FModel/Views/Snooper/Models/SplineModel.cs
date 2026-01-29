using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets.Exports.Component.SplineMesh;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using FModel.Views.Snooper.Buffers;
using FModel.Views.Snooper.Shading;
using OpenTK.Graphics.OpenGL4;

namespace FModel.Views.Snooper.Models;

public class SplineModel : StaticModel
{
    [StructLayout(LayoutKind.Sequential)]
    public struct GpuParams
    {
        public int ForwardAxis;
        public float SplineBoundaryMin;
        public float SplineBoundaryMax;
        public bool bSmoothInterpRollScale;

        public FVector MeshOrigin;
        public int _padding0;
        public FVector MeshBoxExtent;
        public int _padding1;

        public FVector StartPos;
        public float StartRoll;
        public FVector StartTangent;
        public int _padding2;
        public FVector2D StartScale;
        public FVector2D StartOffset;
        public FVector EndPos;
        public float EndRoll;
        public FVector EndTangent;
        public int _padding3;
        public FVector2D EndScale;
        public FVector2D EndOffset;

        public FVector SplineUpDir;
        public int _padding4;

        public GpuParams(USplineMeshComponent splineMesh)
        {
            ForwardAxis = (int)splineMesh.ForwardAxis;
            SplineBoundaryMin = splineMesh.SplineBoundaryMin;
            SplineBoundaryMax = splineMesh.SplineBoundaryMax;
            bSmoothInterpRollScale = splineMesh.bSmoothInterpRollScale;

            var b = splineMesh.GetLoadedStaticMesh()?.RenderData?.Bounds ?? new FBoxSphereBounds();
            MeshOrigin = b.Origin * Constants.SCALE_DOWN_RATIO;
            MeshBoxExtent = b.BoxExtent * Constants.SCALE_DOWN_RATIO;

            var p = splineMesh.SplineParams;
            StartPos = p.StartPos * Constants.SCALE_DOWN_RATIO;
            StartRoll = p.StartRoll;
            StartTangent = p.StartTangent * Constants.SCALE_DOWN_RATIO;
            StartScale = p.StartScale;
            StartOffset = p.StartOffset;
            EndPos = p.EndPos * Constants.SCALE_DOWN_RATIO;
            EndRoll = p.EndRoll;
            EndTangent = p.EndTangent * Constants.SCALE_DOWN_RATIO;
            EndScale = p.EndScale;
            EndOffset = p.EndOffset;

            SplineUpDir = splineMesh.SplineUpDir;
        }
    }

    private readonly List<GpuParams> _splineParams;
    private BufferObject<GpuParams> _ssbo;

    public SplineModel(UStaticMesh export, CStaticMesh staticMesh, USplineMeshComponent splineMesh, Transform transform = null) : base(export, staticMesh, transform)
    {
        _splineParams = [new GpuParams(splineMesh)];

        Type = "SplineMesh";
        IsVisible = true;
        IsTwoSided = true;
    }

    public void AddComponent(USplineMeshComponent splineMesh)
    {
        _splineParams.Add(new GpuParams(splineMesh));
    }

    public override void Setup(Options options)
    {
        base.Setup(options);

        _ssbo = new BufferObject<GpuParams>(_splineParams.ToArray(), BufferTarget.ShaderStorageBuffer);
    }

    public void Render(Shader shader)
    {
        shader.SetUniform("uIsSpline", true);
        _ssbo.BindBufferBase(3);
    }

    public override void Dispose()
    {
        base.Dispose();
        _ssbo?.Dispose();
    }
}
