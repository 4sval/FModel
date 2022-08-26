using System;
using System.Numerics;

namespace FModel.Views.Snooper;

public class Camera
{
    public Vector3 Position { get; set; }
    public Vector3 Direction { get; private set; }
    public Vector3 Up = Vector3.UnitY;

    public float AspectRatio { get; }
    public float Yaw { get; set; } = -90f;
    public float Pitch { get; set; }
    public float Zoom = 45f;

    public Camera(Vector3 position, Vector3 direction, float aspectRatio = 16f / 9f)
    {
        Position = position;
        Direction = direction;
        AspectRatio = aspectRatio;

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

        Direction = Vector3.Normalize(CalculateDirection());
    }

    public Matrix4x4 GetViewMatrix()
    {
        return Matrix4x4.CreateLookAt(Position, Position + Direction, Up);
    }

    public Matrix4x4 GetProjectionMatrix()
    {
        return Matrix4x4.CreatePerspectiveFieldOfView(Helper.DegreesToRadians(Zoom), AspectRatio, 0.1f, 100.0f);
    }

    private Vector3 CalculateDirection()
    {
        var direction = Vector3.Zero;
        direction.X = MathF.Cos(Helper.DegreesToRadians(Yaw)) * MathF.Cos(Helper.DegreesToRadians(Pitch));
        direction.Y = MathF.Sin(Helper.DegreesToRadians(Pitch));
        direction.Z = MathF.Sin(Helper.DegreesToRadians(Yaw)) * MathF.Cos(Helper.DegreesToRadians(Pitch));
        return direction;
    }
}
