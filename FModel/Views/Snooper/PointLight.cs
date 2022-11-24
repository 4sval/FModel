using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.Math;

namespace FModel.Views.Snooper;

public class PointLight : Light
{
    public PointLight(Texture icon, UObject point, FVector position) : base(icon, point, position)
    {

    }

    public override void Render(int i, Shader shader)
    {
        shader.SetUniform($"uLights[{i}].Color", Color);
        shader.SetUniform($"uLights[{i}].Position", Transform.Position);
        shader.SetUniform($"uLights[{i}].Intensity", Intensity);
        shader.SetUniform($"uLights[{i}].Linear", Linear);
        shader.SetUniform($"uLights[{i}].Quadratic", Quadratic);
    }
}
