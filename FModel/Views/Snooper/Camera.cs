using System;
using System.Numerics;

namespace FModel.Views.Snooper;

public class Camera
{
    public Vector3 Position { get; set; }
    public Vector3 Direction { get; private set; }
    public Vector3 Up = Vector3.UnitY;

    public float Yaw { get; set; } = -90f;
    public float Pitch { get; set; } = 0f;
    public float Zoom { get; set; } = 45f;
    public float Speed { get; set; } = 1f;
    public float Near { get; } = 0.01f;
    public float Far { get; } = 100f;
    public float AspectRatio => 16f / 9f;

    public Camera(Vector3 position, Vector3 direction, float near, float far, float speed)
    {
        Position = position;
        Direction = direction;
        Near = near;
        Far = far;
        Speed = speed;

        // trigonometric math to calculate the cam's yaw/pitch based on position and direction to look
        var yaw = MathF.Atan((-Position.X - Direction.X) / (Position.Z - Direction.Z));
        var pitch = MathF.Atan((Position.Y - Direction.Y) / (Position.Z - Direction.Z));
        ModifyDirection(Helper.RadiansToDegrees(yaw), Helper.RadiansToDegrees(pitch));
    }

    public void ModifyZoom(float zoomAmount)
    {
        //We don't want to be able to zoom in too close or too far away so clamp to these values
        Zoom = Math.Clamp(Zoom - zoomAmount, 1.0f, 45f);
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

    public Matrix4x4 GetViewMatrix()
    {
        return Matrix4x4.CreateLookAt(Position, Position + Direction, Up);
    }

    public Matrix4x4 GetProjectionMatrix()
    {
        return Matrix4x4.CreatePerspectiveFieldOfView(Helper.DegreesToRadians(Zoom), AspectRatio, Near, Far);
    }
}
