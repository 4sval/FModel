using System.Collections.Generic;
using System.Numerics;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets.Exports.Component.SplineMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using FModel.Views.Snooper.Buffers;
using FModel.Views.Snooper.Shading;
using OpenTK.Graphics.OpenGL4;

namespace FModel.Views.Snooper.Models;

public class SplineModel : StaticModel
{
    public BufferObject<Matrix4x4> SplineParamsVbo { get; set; }

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

        SplineParamsVbo = new BufferObject<Matrix4x4>(SplineParamsCount, BufferTarget.ShaderStorageBuffer);
        for (int instance = 0; instance < SplineParamsCount; instance++)
        {
            var matrix = Matrix4x4.Identity;
            matrix.M11 = SplineParams[instance].StartPos.X * Constants.SCALE_DOWN_RATIO;
            matrix.M12 = SplineParams[instance].StartPos.Z * Constants.SCALE_DOWN_RATIO;
            matrix.M13 = SplineParams[instance].StartPos.Y * Constants.SCALE_DOWN_RATIO;
            matrix.M14 = SplineParams[instance].StartOffset.X;
            matrix.M21 = SplineParams[instance].StartTangent.X;
            matrix.M22 = SplineParams[instance].StartTangent.Z;
            matrix.M23 = SplineParams[instance].StartTangent.Y;
            matrix.M24 = SplineParams[instance].StartOffset.Y;
            matrix.M31 = SplineParams[instance].EndPos.X * Constants.SCALE_DOWN_RATIO;
            matrix.M32 = SplineParams[instance].EndPos.Z * Constants.SCALE_DOWN_RATIO;
            matrix.M33 = SplineParams[instance].EndPos.Y * Constants.SCALE_DOWN_RATIO;
            matrix.M34 = SplineParams[instance].EndOffset.X;
            matrix.M41 = SplineParams[instance].EndTangent.X;
            matrix.M42 = SplineParams[instance].EndTangent.Z;
            matrix.M43 = SplineParams[instance].EndTangent.Y;
            matrix.M44 = SplineParams[instance].EndOffset.Y;

            SplineParamsVbo.Update(instance, matrix);
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
