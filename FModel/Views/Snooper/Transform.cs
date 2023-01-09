using System.Numerics;
using CUE4Parse.UE4.Objects.Core.Math;
using ImGuiNET;

namespace FModel.Views.Snooper;

public class Transform
{
    public static Transform Identity
    {
        get => new ();
    }

    public Matrix4x4 Relation = Matrix4x4.Identity;
    public FVector Position = FVector.ZeroVector;
    public FQuat Rotation = FQuat.Identity;
    public FVector Scale = FVector.OneVector;

    public Matrix4x4 Matrix =>
        Matrix4x4.CreateScale(Scale.X, Scale.Z, Scale.Y) *
        Matrix4x4.CreateFromQuaternion(Quaternion.Normalize(new Quaternion(Rotation.X, Rotation.Z, Rotation.Y, -Rotation.W))) *
        Matrix4x4.CreateTranslation(Position.X, Position.Z, Position.Y)
        * Relation;

    public void ImGuiTransform(float speed)
    {
        const float width = 100f;

        ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
        if (ImGui.TreeNode("Location"))
        {
            ImGui.SetNextItemWidth(width);
            ImGui.DragFloat("X", ref Position.X, speed, 0f, 0f, "%.2f m");

            ImGui.SetNextItemWidth(width);
            ImGui.DragFloat("Y", ref Position.Z, speed, 0f, 0f, "%.2f m");

            ImGui.SetNextItemWidth(width);
            ImGui.DragFloat("Z", ref Position.Y, speed, 0f, 0f, "%.2f m");

            ImGui.TreePop();
        }

        ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
        if (ImGui.TreeNode("Rotation"))
        {
            ImGui.SetNextItemWidth(width);
            ImGui.DragFloat("W", ref Rotation.W, .005f, 0f, 0f, "%.3f rad");

            ImGui.SetNextItemWidth(width);
            ImGui.DragFloat("X", ref Rotation.X, .005f, 0f, 0f, "%.3f rad");

            ImGui.SetNextItemWidth(width);
            ImGui.DragFloat("Y", ref Rotation.Z, .005f, 0f, 0f, "%.3f rad");

            ImGui.SetNextItemWidth(width);
            ImGui.DragFloat("Z", ref Rotation.Y, .005f, 0f, 0f, "%.3f rad");

            ImGui.TreePop();
        }

        ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
        if (ImGui.TreeNode("Scale"))
        {
            ImGui.SetNextItemWidth(width);
            ImGui.DragFloat("X", ref Scale.X, speed, 0f, 0f, "%.3f");

            ImGui.SetNextItemWidth(width);
            ImGui.DragFloat("Y", ref Scale.Z, speed, 0f, 0f, "%.3f");

            ImGui.SetNextItemWidth(width);
            ImGui.DragFloat("Z", ref Scale.Y, speed, 0f, 0f, "%.3f");

            ImGui.TreePop();
        }
    }
}
