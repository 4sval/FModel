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

    public SpotLight(Texture icon, UObject parent, UObject spot, FVector position) : base(icon, parent, spot, position)
    {
        if (!spot.TryGetValue(out Attenuation, "AttenuationRadius", "SourceRadius"))
            Attenuation = 1.0f;

        Attenuation *= Constants.SCALE_DOWN_RATIO;
        Direction = Vector3.Zero;
        Direction.Y -= Attenuation;
        ConeAngle = (spot.GetOrDefault("InnerConeAngle", 50.0f) + spot.GetOrDefault("OuterConeAngle", 60.0f)) / 2.0f;
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
