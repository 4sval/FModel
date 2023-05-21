using System;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.Misc;
using FModel.Views.Snooper.Shading;
using ImGuiNET;

namespace FModel.Views.Snooper.Lights;

public class PointLight : Light
{
    public float Linear;
    public float Quadratic;

    public PointLight(Texture icon, UObject point) : base(icon, point)
    {
        if (!point.TryGetValue(out float radius, "SourceRadius", "AttenuationRadius"))
            radius = 1.0f;

        radius *= Constants.SCALE_DOWN_RATIO;
        Linear = 4.5f / radius;
        Quadratic = 75.0f / MathF.Pow(radius, 2.0f);
    }

    public PointLight(FGuid model, Texture icon, UObject parent, UObject point, Transform transform) : base(model, icon, parent, point, transform)
    {
        if (!point.TryGetValue(out float radius, "AttenuationRadius", "SourceRadius"))
            radius = 1.0f;

        radius *= Constants.SCALE_DOWN_RATIO;
        Linear = 4.5f / radius;
        Quadratic = 75.0f / MathF.Pow(radius, 2.0f);
    }

    public override void Render(int i, Shader shader)
    {
        base.Render(i, shader);
        shader.SetUniform($"uLights[{i}].Linear", Linear);
        shader.SetUniform($"uLights[{i}].Quadratic", Quadratic);

        shader.SetUniform($"uLights[{i}].Type", 0);
    }

    public override void ImGuiLight()
    {
        base.ImGuiLight();
        SnimGui.Layout("Linear");ImGui.PushID(3);
        ImGui.DragFloat("", ref Linear, 0.1f);
        ImGui.PopID();SnimGui.Layout("Quadratic");ImGui.PushID(4);
        ImGui.DragFloat("", ref Quadratic, 0.1f);ImGui.PopID();
    }
}
