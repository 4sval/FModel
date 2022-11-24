using System;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.Math;

namespace FModel.Views.Snooper;

public class PointLight : Light
{
    public readonly float Linear;
    public readonly float Quadratic;

    public PointLight(Texture icon, UObject point, FVector position) : base(icon, point, position)
    {
        var radius = point.GetOrDefault("AttenuationRadius", 0.0f) * Constants.SCALE_DOWN_RATIO;
        Linear = 4.5f / radius;
        Quadratic = 75.0f / MathF.Pow(radius, 2);
    }

    public override void Render(int i, Shader shader)
    {
        shader.SetUniform($"uLights[{i}].Color", Color);
        shader.SetUniform($"uLights[{i}].Position", Transform.Position);
        shader.SetUniform($"uLights[{i}].Intensity", Intensity);
        shader.SetUniform($"uLights[{i}].Linear", Linear);
        shader.SetUniform($"uLights[{i}].Quadratic", Quadratic);

        shader.SetUniform($"uLights[{i}].Type", 0);
    }
}
