using System;
using ImGuiNET;
using OpenTK.Mathematics;

namespace FModel.Views.Snooper;

public class Camera
{
    public Vector3 Position;
    public Vector3 Direction;
    public Vector3 Up = Vector3.UnitY;

    public float Yaw = -90f;
    public float Pitch = 0f;
    public float Zoom = 60f;
    public float Speed = 1f;
    public float Near = 0.01f;
    public float Far = 100f;
    public float AspectRatio = 16f / 9f;

    public Camera()
    {
        Position = new Vector3(0, 1, 1);
        Direction = Vector3.Zero;

        InitDirection();
    }

    public Camera(Vector3 position, Vector3 direction, float near, float far, float speed)
    {
        Position = position;
        Direction = direction;
        Near = near;
        Far = far;
        Speed = speed;

        InitDirection();
    }

    private void InitDirection()
    {
        // trigonometric math to calculate the cam's yaw/pitch based on position and direction to look
        var yaw = MathF.Atan((-Position.X - Direction.X) / (Position.Z - Direction.Z));
        var pitch = MathF.Atan((Position.Y - Direction.Y) / (Position.Z - Direction.Z));
        ModifyDirection(Helper.RadiansToDegrees(yaw), Helper.RadiansToDegrees(pitch));
    }

    public void ModifyZoom(float zoomAmount)
    {
        //We don't want to be able to zoom in too close or too far away so clamp to these values
        Zoom = Math.Clamp(Zoom - zoomAmount, 1.0f, 89f);
    }

    public void ModifyDirection(float xOffset, float yOffset)
    {
        Yaw += xOffset;
        Pitch -= yOffset;

        //We don't want to be able to look behind us by going over our head or under our feet so make sure it stays within these bounds
        Pitch = Math.Clamp(Pitch, -89f, 89f);

        var direction = Vector3.Zero;
        var yaw = Helper.DegreesToRadians(Yaw);
        var pitch = Helper.DegreesToRadians(Pitch);
        direction.X = MathF.Cos(yaw) * MathF.Cos(pitch);
        direction.Y = MathF.Sin(pitch);
        direction.Z = MathF.Sin(yaw) * MathF.Cos(pitch);
        Direction = Vector3.Normalize(direction);
    }

    public Matrix4 GetViewMatrix()
    {
        return Matrix4.LookAt(Position, Position + Direction, Up);
    }

    public Matrix4 GetProjectionMatrix()
    {
        return CreatePerspectiveFieldOfView(Helper.DegreesToRadians(Zoom), AspectRatio, Near, Far);
    }

    /// <summary>
    /// OpenTK function causes a gap between the faded out grid & the skybox
    /// so we use the System.Numerics function instead with OpenTK types
    /// </summary>
    private Matrix4 CreatePerspectiveFieldOfView(float fieldOfView, float aspectRatio, float nearPlaneDistance, float farPlaneDistance)
    {
        if (fieldOfView is <= 0.0f or >= MathF.PI)
            throw new ArgumentOutOfRangeException(nameof(fieldOfView));

        if (nearPlaneDistance <= 0.0f)
            throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance));

        if (farPlaneDistance <= 0.0f)
            throw new ArgumentOutOfRangeException(nameof(farPlaneDistance));

        if (nearPlaneDistance >= farPlaneDistance)
            throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance));

        float yScale = 1.0f / MathF.Tan(fieldOfView * 0.5f);
        float xScale = yScale / aspectRatio;

        Matrix4 result = Matrix4.Zero;

        result.M11 = xScale;
        result.M12 = result.M13 = result.M14 = 0.0f;

        result.M22 = yScale;
        result.M21 = result.M23 = result.M24 = 0.0f;

        result.M31 = result.M32 = 0.0f;
        float negFarRange = float.IsPositiveInfinity(farPlaneDistance) ? -1.0f : farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
        result.M33 = negFarRange;
        result.M34 = -1.0f;

        result.M41 = result.M42 = result.M44 = 0.0f;
        result.M43 = nearPlaneDistance * negFarRange;

        return result;
    }

    private const float _step = 0.01f;
    private const float _zero = 0.000001f; // doesn't actually work if _infinite is used as max value /shrug
    private const float _infinite = 0.0f;
    private const ImGuiSliderFlags _clamp = ImGuiSliderFlags.AlwaysClamp;
    public void ImGuiCamera()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new System.Numerics.Vector2(8, 3));
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new System.Numerics.Vector2(0, 1));
        if (ImGui.BeginTable("camera_editor", 2))
        {
            SnimGui.Layout("Speed");ImGui.PushID(1);
            ImGui.DragFloat("", ref Speed, _step, _zero, _infinite, "%.2f s/m", _clamp);
            ImGui.PopID();SnimGui.Layout("Far Plane");ImGui.PushID(2);
            ImGui.DragFloat("", ref Far, 0.1f, 0.1f, Far * 2f, "%.2f m", _clamp);
            ImGui.PopID();

            ImGui.EndTable();
        }
        ImGui.PopStyleVar(2);
    }
}
