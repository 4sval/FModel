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
    public FQuat Rotation = new (0f);
    public FVector Scale = FVector.OneVector;

    public Matrix4x4 Matrix =>
        Matrix4x4.CreateScale(Scale.X, Scale.Z, Scale.Y) *
        Matrix4x4.CreateFromQuaternion(new Quaternion(Rotation.X, Rotation.Z, Rotation.Y, -Rotation.W)) *
        Matrix4x4.CreateTranslation(Position.X, Position.Z, Position.Y)
        * Relation;

    public void ImGuiTransform(float speed)
    {
        const int width = 100;

        ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
        if (ImGui.TreeNode("Location"))
        {
            ImGui.PushID(1);
            ImGui.SetNextItemWidth(width);
            ImGui.DragFloat("X", ref Position.X, speed, 0f, 0f, "%.2f m");

            ImGui.SetNextItemWidth(width);
            ImGui.DragFloat("Y", ref Position.Z, speed, 0f, 0f, "%.2f m");

            ImGui.SetNextItemWidth(width);
            ImGui.DragFloat("Z", ref Position.Y, speed, 0f, 0f, "%.2f m");

            ImGui.PopID();
            ImGui.TreePop();
        }

        ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
        if (ImGui.TreeNode("Rotation"))
        {
            ImGui.PushID(2);
            ImGui.SetNextItemWidth(width);
            ImGui.DragFloat("X", ref Rotation.X, .5f, 0f, 0f, "%.1f°");

            ImGui.SetNextItemWidth(width);
            ImGui.DragFloat("Y", ref Rotation.Z, .5f, 0f, 0f, "%.1f°");

            ImGui.SetNextItemWidth(width);
            ImGui.DragFloat("Z", ref Rotation.Y, .5f, 0f, 0f, "%.1f°");

            ImGui.SetNextItemWidth(width);
            ImGui.DragFloat("W", ref Rotation.W, .5f, 0f, 0f, "%.1f°");

            ImGui.PopID();
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Scale"))
        {
            ImGui.PushID(3);
            ImGui.SetNextItemWidth(width);
            ImGui.DragFloat("X", ref Scale.X, speed, 0f, 0f, "%.3f");

            ImGui.SetNextItemWidth(width);
            ImGui.DragFloat("Y", ref Scale.Z, speed, 0f, 0f, "%.3f");

            ImGui.SetNextItemWidth(width);
            ImGui.DragFloat("Z", ref Scale.Y, speed, 0f, 0f, "%.3f");

            ImGui.PopID();
            ImGui.TreePop();
        }
    }
}
