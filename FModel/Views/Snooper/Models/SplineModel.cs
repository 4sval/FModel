using System.Collections.Generic;
using System.Numerics;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets.Exports.Component.SplineMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using FModel.Views.Snooper.Buffers;
using FModel.Views.Snooper.Shading;
using OpenTK.Graphics.OpenGL4;

namespace FModel.Views.Snooper.Models;

public struct GPUSplineParams
{
    public FVector StartPos;
    public float StartRoll;
    public FVector StartTangent;
    public float _padding0;
    public FVector2D StartScale;
    public FVector2D StartOffset;

    public FVector EndPos;
    public float EndRoll;
    public FVector EndTangent;
    public float _padding1;
    public FVector2D EndScale;
    public FVector2D EndOffset;
}

public class SplineModel : StaticModel
{
    public BufferObject<GPUSplineParams> SplineParamsVbo { get; set; }

    public List<FSplineMeshParams> SplineParams { get; }

    public int SplineParamsCount => SplineParams.Count;

    public SplineModel(UStaticMesh export, CStaticMesh staticMesh, FSplineMeshParams splineMeshParams, Transform transform = null)
        : base(export, staticMesh, transform)
    {
        SplineParams = [splineMeshParams];
    }

    public override void Setup(Options options)
    {
        base.Setup(options);

        SplineParamsVbo = new BufferObject<GPUSplineParams>(SplineParamsCount, BufferTarget.ShaderStorageBuffer);
        for (int instance = 0; instance < SplineParamsCount; instance++)
        {
            var p = SplineParams[instance];
            SplineParamsVbo.Update(instance, new GPUSplineParams
            {
                StartPos = p.StartPos * Constants.SCALE_DOWN_RATIO,
                StartRoll = p.StartRoll,
                StartTangent = p.StartTangent,
                StartScale = p.StartScale,
                StartOffset = p.StartOffset,

                EndPos = p.EndPos  * Constants.SCALE_DOWN_RATIO,
                EndRoll = p.EndRoll,
                EndTangent = p.EndTangent,
                EndScale = p.EndScale,
                EndOffset = p.EndOffset
            });
        }
    }

    public override void Render(Shader shader, Texture checker = null, bool outline = false)
    {
        if (!outline)
        {
            shader.SetUniform("uIsSpline", true);
            SplineParamsVbo.BindBufferBase(3);
        }
        base.Render(shader, checker, outline);
    }

    public override void Dispose()
    {
        base.Dispose();
        SplineParamsVbo?.Dispose();
    }
}
