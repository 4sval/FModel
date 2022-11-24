using System;
using System.Numerics;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.Math;

namespace FModel.Views.Snooper;

public class SpotLight : PointLight
{
    public Vector3 Direction; // ???
    public float Attenuation;
    public float ConeAngle;

    public SpotLight(Texture icon, UObject spot, FVector position) : base(icon, spot, position)
    {
        // var p = spot.GetOrDefault("RelativeLocation", FVector.ZeroVector);
        // var r = spot.GetOrDefault("RelativeRotation", FRotator.ZeroRotator);

        // Direction = position + r.UnrotateVector(p.ToMapVector()) * Constants.SCALE_DOWN_RATIO;
        Attenuation = spot.GetOrDefault("AttenuationRadius", 0.0f) * Constants.SCALE_DOWN_RATIO;
        ConeAngle = (spot.GetOrDefault("InnerConeAngle", 50f) + spot.GetOrDefault("OuterConeAngle", 60f)) / 2f;
        ConeAngle = MathF.Cos(Helper.DegreesToRadians(ConeAngle));
    }

    public new void Render(int i, Shader shader)
    {
        base.Render(i, shader);
        // shader.SetUniform($"uLights[{i}].Direction", Direction);
        // shader.SetUniform($"uLights[{i}].Attenuation", Attenuation);
        // shader.SetUniform($"uLights[{i}].ConeAngle", ConeAngle);
    }
}
