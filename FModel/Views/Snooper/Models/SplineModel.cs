using System.Collections.Generic;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets.Exports.Component.SplineMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using FModel.Views.Snooper.Shading;

namespace FModel.Views.Snooper.Models;

public class SplineModel : StaticModel
{
    public readonly List<FSplineMeshParams> SplineParams;

    public SplineModel(UStaticMesh export, CStaticMesh staticMesh, FSplineMeshParams splineMeshParams, Transform transform = null)
        : base(export, staticMesh, transform)
    {
        SplineParams = [splineMeshParams];
    }

    public override void Setup(Options options)
    {
        // setup the spline mesh
        base.Setup(options);
    }

    public override void Render(Shader shader, Texture checker = null, bool outline = false)
    {
        // send the spline mesh params to the shader
        base.Render(shader, checker, outline);
    }
}
