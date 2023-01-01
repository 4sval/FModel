using System;
using System.Numerics;
using CUE4Parse.UE4.Objects.Core.Math;
using FModel.Settings;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace FModel.Views.Snooper;

public class Camera
{
    public enum WorldMode
    {
        FlyCam,
        Arcball
    }

    public Vector3 Position;
    public Vector3 Direction;
    public Vector3 Focus => Position - Direction;
    public Vector3 Up = Vector3.UnitY;
    public WorldMode Mode = UserSettings.Default.CameraMode;

    public float Yaw = -90f;
    public float Pitch = 0f;
    public float Zoom = 60f;
    public float Speed = 1f;
    public float Far = 100f;
    public float Near => 0.01f;
    public float AspectRatio = 16f / 9f;

    public Camera()
    {
        Position = new Vector3(0, 1, 1);
        Direction = Vector3.Zero;

        InitDirection();
    }

    public Camera(FBox box, float far)
    {
        Far = far;
        Teleport(FVector.ZeroVector, box, true);
    }

    public Camera(Vector3 position, Vector3 direction, float far, float speed)
    {
        Position = position;
        Direction = direction;
        Far = far;
        Speed = speed;

        InitDirection();
    }

    public void Teleport(FVector instancePos, FBox box, bool updateSpeed = false)
    {
        box.GetCenterAndExtents(out var center, out var extents);
        center = center.ToMapVector();
        center += instancePos;
        var distance = extents.AbsMax();

        Position = new Vector3(instancePos.X, center.Y, instancePos.Z + distance * 2);
        Direction = new Vector3(center.X, center.Y, center.Z);
        if (updateSpeed) Speed = distance;

        InitDirection();
    }

    private void InitDirection()
    {
        // trigonometric math to calculate the cam's yaw/pitch based on position and direction to look
        var yaw = MathF.Atan((-Position.X - Direction.X) / (Position.Z - Direction.Z));
        var pitch = MathF.Atan((Position.Y - Direction.Y) / (Position.Z - Direction.Z));
        Modify(Helper.RadiansToDegrees(yaw), Helper.RadiansToDegrees(pitch));
    }

    public void Modify(Vector2 mouseDelta)
    {
        var lookSensitivity = Mode switch
        {
            WorldMode.FlyCam => 0.1f,
            WorldMode.Arcball => 0.01f,
            _ => throw new ArgumentOutOfRangeException()
        };
        mouseDelta *= lookSensitivity;
        Modify(mouseDelta.X, mouseDelta.Y);
    }
    private void Modify(float xOffset, float yOffset)
    {
        switch (Mode)
        {
            case WorldMode.FlyCam:
            {
                Yaw += xOffset;
                Pitch -= yOffset;
                Pitch = Math.Clamp(Pitch, -89f, 89f);

                var direction = Vector3.Zero;
                var yaw = Helper.DegreesToRadians(Yaw);
                var pitch = Helper.DegreesToRadians(Pitch);
                direction.X = MathF.Cos(yaw) * MathF.Cos(pitch);
                direction.Y = MathF.Sin(pitch);
                direction.Z = MathF.Sin(yaw) * MathF.Cos(pitch);
                Direction = Vector3.Normalize(direction);
                break;
            }
            case WorldMode.Arcball:
            {
                var up = -Up;
                var rotationX = Matrix4x4.CreateFromAxisAngle(up, xOffset);
                Position = Vector3.Transform(Focus, rotationX) + Direction;

                var right = Vector3.Normalize(Vector3.Cross(up, Focus));
                var rotationY = Matrix4x4.CreateFromAxisAngle(right, yOffset);
                Position = Vector3.Transform(Focus, rotationY) + Direction;
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void Modify(KeyboardState keyboard, float time)
    {
        var multiplier = keyboard.IsKeyDown(Keys.LeftShift) ? 2f : 1f;
        var moveSpeed = Speed * multiplier * time;

        var focus = Mode switch
        {
            WorldMode.FlyCam => Direction,
            WorldMode.Arcball => -Focus,
            _ => throw new ArgumentOutOfRangeException()
        };

        if (keyboard.IsKeyDown(Keys.W))
            Position += moveSpeed * focus;
        if (keyboard.IsKeyDown(Keys.S))
            Position -= moveSpeed * focus;

        switch (Mode)
        {
            case WorldMode.FlyCam:
            {
                if (keyboard.IsKeyDown(Keys.A))
                    Position -= Vector3.Normalize(Vector3.Cross(focus, Up)) * moveSpeed;
                if (keyboard.IsKeyDown(Keys.D))
                    Position += Vector3.Normalize(Vector3.Cross(focus, Up)) * moveSpeed;
                if (keyboard.IsKeyDown(Keys.E))
                    Position += moveSpeed * Up;
                if (keyboard.IsKeyDown(Keys.Q))
                    Position -= moveSpeed * Up;
                break;
            }
            case WorldMode.Arcball:
            {
                if (keyboard.IsKeyDown(Keys.A))
                {
                    var d = Vector3.Normalize(Vector3.Cross(focus, Up)) * moveSpeed;
                    Position -= d;
                    Direction -= d;
                }
                if (keyboard.IsKeyDown(Keys.D))
                {
                    var d = Vector3.Normalize(Vector3.Cross(focus, Up)) * moveSpeed;
                    Position += d;
                    Direction += d;
                }
                if (keyboard.IsKeyDown(Keys.E))
                {
                    var d = moveSpeed * Up;
                    Position += d;
                    Direction += d;
                }
                if (keyboard.IsKeyDown(Keys.Q))
                {
                    var d = moveSpeed * Up;
                    Position -= d;
                    Direction -= d;
                }
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (keyboard.IsKeyDown(Keys.X))
            ModifyZoom(-.5f);
        if (keyboard.IsKeyDown(Keys.C))
            ModifyZoom(+.5f);
    }

    private void ModifyZoom(float zoomAmount)
    {
        //We don't want to be able to zoom in too close or too far away so clamp to these values
        Zoom = Math.Clamp(Zoom - zoomAmount, 1.0f, 89f);
    }

    public Matrix4x4 GetViewMatrix()
    {
        return Mode switch
        {
            WorldMode.FlyCam => Matrix4x4.CreateLookAt(Position, Position + Direction, Up),
            WorldMode.Arcball => Matrix4x4.CreateLookAt(Position, Direction, Up),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public Matrix4x4 GetProjectionMatrix()
    {
        return Matrix4x4.CreatePerspectiveFieldOfView(Helper.DegreesToRadians(Zoom), AspectRatio, Near, Far);
    }

    private const float _step = 0.01f;
    private const float _zero = 0.000001f; // doesn't actually work if _infinite is used as max value /shrug
    private const float _infinite = 0.0f;
    private const ImGuiSliderFlags _clamp = ImGuiSliderFlags.AlwaysClamp;
    public void ImGuiCamera()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8, 3));
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(0, 1));
        if (ImGui.BeginTable("camera_editor", 2))
        {
            SnimGui.Layout("Mode");
            ImGui.PushID(1);var m = (int) Mode;
            ImGui.Combo("world_mode", ref m, "Fly Cam\0Arcball\0");
            Mode = (WorldMode) m;ImGui.PopID();

            SnimGui.Layout("Speed");ImGui.PushID(2);
            ImGui.DragFloat("", ref Speed, _step, _zero, _infinite, "%.2f m/s", _clamp);
            ImGui.PopID();SnimGui.Layout("Far Plane");ImGui.PushID(3);
            ImGui.DragFloat("", ref Far, 0.1f, 0.1f, Far * 2f, "%.2f m", _clamp);
            ImGui.PopID();

            ImGui.EndTable();
        }
        ImGui.PopStyleVar(2);
    }
}
