using System;
using System.Numerics;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.Math;

namespace FModel.Views.Snooper;

public class SpotLight : Light
{
    public Vector3 Direction; // ???
    public float Attenuation;
    public float ConeAngle;

    public SpotLight(Texture icon, UObject spot, FVector position) : base(icon, spot, position)
    {
        Direction = Vector3.Zero;
        Attenuation = spot.GetOrDefault("AttenuationRadius", 0.0f) * Constants.SCALE_DOWN_RATIO;
        Direction.Y -= Attenuation;
        ConeAngle = (spot.GetOrDefault("InnerConeAngle", 50f) + spot.GetOrDefault("OuterConeAngle", 60f)) / 2f;
        ConeAngle = MathF.Cos(Helper.DegreesToRadians(ConeAngle));
    }

    public override void Render(int i, Shader shader)
    {
        shader.SetUniform($"uLights[{i}].Color", Color);
        shader.SetUniform($"uLights[{i}].Position", Transform.Position);
        shader.SetUniform($"uLights[{i}].Intensity", Intensity);
        shader.SetUniform($"uLights[{i}].Direction", Direction);
        shader.SetUniform($"uLights[{i}].Attenuation", Attenuation);
        shader.SetUniform($"uLights[{i}].ConeAngle", ConeAngle);

        shader.SetUniform($"uLights[{i}].Type", 1);
    }
}
