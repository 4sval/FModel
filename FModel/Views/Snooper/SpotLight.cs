using System;
using System.Numerics;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using ImGuiNET;

namespace FModel.Views.Snooper;

public class SpotLight : Light
{
    public Vector2 Direction; // ???
    public float Attenuation;
    public float ConeAngle;

    public SpotLight(FGuid model, Texture icon, UObject parent, UObject spot, FVector position) : base(model, icon, parent, spot, position)
    {
        if (!spot.TryGetValue(out Attenuation, "AttenuationRadius", "SourceRadius"))
            Attenuation = 1.0f;

        Attenuation *= Constants.SCALE_DOWN_RATIO;
        Direction = Vector2.Zero;
        ConeAngle = (spot.GetOrDefault("InnerConeAngle", 50.0f) + spot.GetOrDefault("OuterConeAngle", 60.0f)) / 2.0f;
        ConeAngle = MathF.Cos(Helper.DegreesToRadians(ConeAngle));
    }

    public override void Render(int i, Shader shader)
    {
        base.Render(i, shader);
        shader.SetUniform($"uLights[{i}].Direction", Direction);
        shader.SetUniform($"uLights[{i}].Attenuation", Attenuation);
        shader.SetUniform($"uLights[{i}].ConeAngle", ConeAngle);

        shader.SetUniform($"uLights[{i}].Type", 1);
    }

    public override void ImGuiLight()
    {
        base.ImGuiLight();
        SnimGui.Layout("Direction");ImGui.PushID(3);
        ImGui.DragFloat2("", ref Direction, 0.01f);
        ImGui.PopID();SnimGui.Layout("Attenuation");ImGui.PushID(4);
        ImGui.DragFloat("", ref Attenuation, 0.1f);ImGui.PopID();

        var angle = Helper.RadiansToDegrees(MathF.Acos(ConeAngle));
        SnimGui.Layout("Cone Angle");ImGui.PushID(5);
        ImGui.DragFloat("", ref angle, 0.1f, 0.0f, 90.0f, "%.1f°");ImGui.PopID();
        ConeAngle = MathF.Cos(Helper.DegreesToRadians(angle));
    }
}
