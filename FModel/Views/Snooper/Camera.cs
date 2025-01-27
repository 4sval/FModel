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
    public WorldMode Mode;
    public Vector3 PositionArc => Position - Direction;
    public Vector3 DirectionArc => Direction - Position;
    public Vector3 Up => Vector3.UnitY;

    public float Zoom = 60f;
    public float Speed = 1f;
    public float Far = 100f;
    public float Near => 0.01f;
    public float AspectRatio = 16f / 9f;

    public Camera()
    {
        Position = new Vector3(0, 1, 1);
        Direction = Vector3.Zero;
        Mode = UserSettings.Default.CameraMode;
    }

    public void Setup(FBox box) => Teleport(FVector.ZeroVector, box, true);
    public void Teleport(Vector3 instancePos, FBox box, bool updateAll = false)
    {
        box.GetCenterAndExtents(out var center, out var extents);
        center += new FVector(instancePos.X, instancePos.Z, instancePos.Y);
        var distance = extents.AbsMax();

        Position = new Vector3(instancePos.X, center.Z, instancePos.Z + distance * 2);
        Direction = new Vector3(center.X, center.Z, center.Y);
        if (updateAll)
        {
            Far = Math.Max(Far, box.Max.AbsMax() * 50f);
            Speed = Math.Max(Speed, distance);
        }
    }

    public void Modify(Vector2 mouseDelta)
    {
        var lookSensitivity = Mode switch
        {
            WorldMode.FlyCam => 0.002f,
            WorldMode.Arcball => 0.003f,
            _ => throw new ArgumentOutOfRangeException()
        };
        mouseDelta *= lookSensitivity;

        const float tolerance = 0.001f;
        var rotationX = Matrix4x4.CreateFromAxisAngle(-Up, mouseDelta.X);
        switch (Mode)
        {
            case WorldMode.FlyCam:
            {
                Direction = Vector3.Transform(DirectionArc, rotationX) + Position;

                var right = Vector3.Normalize(Vector3.Cross(Up, DirectionArc));

                var currentPitch = MathF.Acos(Vector3.Dot(DirectionArc, Up) / (DirectionArc.Length() * Up.Length()));
                var newPitch = currentPitch + mouseDelta.Y;
                var clampedPitch = Math.Clamp(newPitch, tolerance, MathF.PI - tolerance);
                var pitchDelta = clampedPitch - currentPitch;

                var rotationY = Matrix4x4.CreateFromAxisAngle(right, pitchDelta);
                Direction = Vector3.Transform(DirectionArc, rotationY) + Position;
                break;
            }
            case WorldMode.Arcball:
            {
                Position = Vector3.Transform(PositionArc, rotationX) + Direction;

                var right = Vector3.Normalize(Vector3.Cross(-Up, PositionArc));

                var currentPitch = MathF.Acos(Vector3.Dot(PositionArc, -Up) / (PositionArc.Length() * Up.Length()));
                var newPitch = currentPitch + mouseDelta.Y;
                var clampedPitch = Math.Clamp(newPitch, tolerance, MathF.PI - tolerance);
                var pitchDelta = clampedPitch - currentPitch;

                var rotationY = Matrix4x4.CreateFromAxisAngle(right, pitchDelta);
                Position = Vector3.Transform(PositionArc, rotationY) + Direction;
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void Modify(KeyboardState keyboard, float time)
    {
        if (!keyboard.IsAnyKeyDown) return;
        var multiplier = keyboard.IsKeyDown(Keys.LeftShift) ? 2f : 1f;
        var moveSpeed = Speed * multiplier * time;
        var moveAxis = Vector3.Normalize(-PositionArc);
        var panAxis = Vector3.Normalize(Vector3.Cross(moveAxis, Up));

        switch (Mode)
        {
            case WorldMode.FlyCam:
            {
                if (keyboard.IsKeyDown(Keys.W)) // forward
                {
                    var d = moveSpeed * moveAxis;
                    Position += d;
                    Direction += d;
                }
                if (keyboard.IsKeyDown(Keys.S)) // backward
                {
                    var d = moveSpeed * moveAxis;
                    Position -= d;
                    Direction -= d;
                }
                break;
            }
            case WorldMode.Arcball:
            {
                if (keyboard.IsKeyDown(Keys.W)) // forward
                    Position += moveSpeed * moveAxis;
                if (keyboard.IsKeyDown(Keys.S)) // backward
                    Position -= moveSpeed * moveAxis;
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (keyboard.IsKeyDown(Keys.A)) // left
        {
            var d = panAxis * moveSpeed;
            Position -= d;
            Direction -= d;
        }
        if (keyboard.IsKeyDown(Keys.D)) // right
        {
            var d = panAxis * moveSpeed;
            Position += d;
            Direction += d;
        }
        if (keyboard.IsKeyDown(Keys.E)) // up
        {
            var d = moveSpeed * Up;
            Position += d;
            Direction += d;
        }
        if (keyboard.IsKeyDown(Keys.Q)) // down
        {
            var d = moveSpeed * Up;
            Position -= d;
            Direction -= d;
        }

        if (keyboard.IsKeyDown(Keys.C)) // zoom in
            ModifyZoom(+.5f);
        if (keyboard.IsKeyDown(Keys.X)) // zoom out
            ModifyZoom(-.5f);
    }

    private void ModifyZoom(float zoomAmount)
    {
        //We don't want to be able to zoom in too close or too far away so clamp to these values
        Zoom = Math.Clamp(Zoom - zoomAmount, 1.0f, 89f);
    }

    public Matrix4x4 GetViewMatrix() => Matrix4x4.CreateLookAt(Position, Direction, Up);
    public Matrix4x4 GetProjectionMatrix()
        => Matrix4x4.CreatePerspectiveFieldOfView(Helper.DegreesToRadians(Zoom), AspectRatio, Near, Far);

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
