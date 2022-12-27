using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using FModel.Views.Snooper.Shading;
using ImGuiNET;

namespace FModel.Views.Snooper.Lights;

public class SpotLight : Light
{
    public float Attenuation;
    public float InnerConeAngle;
    public float OuterConeAngle;

    public SpotLight(FGuid model, Texture icon, UObject parent, UObject spot, FVector position) : base(model, icon, parent, spot, position)
    {
        if (!spot.TryGetValue(out Attenuation, "AttenuationRadius", "SourceRadius"))
            Attenuation = 1.0f;

        Attenuation *= Constants.SCALE_DOWN_RATIO;
        InnerConeAngle = spot.GetOrDefault("InnerConeAngle", 50.0f);
        OuterConeAngle = spot.GetOrDefault("OuterConeAngle", 60.0f);
    }

    public override void Render(int i, Shader shader)
    {
        base.Render(i, shader);
        shader.SetUniform($"uLights[{i}].Attenuation", Attenuation);
        shader.SetUniform($"uLights[{i}].InnerConeAngle", InnerConeAngle);
        shader.SetUniform($"uLights[{i}].OuterConeAngle", OuterConeAngle);

        shader.SetUniform($"uLights[{i}].Type", 1);
    }

    public override void ImGuiLight()
    {
        base.ImGuiLight();
        SnimGui.Layout("Attenuation");ImGui.PushID(3);
        ImGui.DragFloat("", ref Attenuation, 0.1f);ImGui.PopID();
        SnimGui.Layout("Inner Cone Angle");ImGui.PushID(4);
        ImGui.DragFloat("", ref InnerConeAngle, 0.1f, 0.0f, 90.0f, "%.1f°");ImGui.PopID();
        SnimGui.Layout("Outer Cone Angle");ImGui.PushID(5);
        ImGui.DragFloat("", ref OuterConeAngle, 0.1f, 0.0f, 90.0f, "%.1f°");ImGui.PopID();
    }
}
