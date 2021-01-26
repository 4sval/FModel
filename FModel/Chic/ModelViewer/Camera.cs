using OpenTK.Mathematics;
using System;

namespace FModel.Chic.ModelViewer
{
    //Based on Asriel's Silver: https://github.com/WorkingRobot/Silver/blob/master/ModelViewer
    public class Camera
    {
        public Vector3 Position = Vector3.Zero;
        public Vector3 Orientation = new Vector3((float)Math.PI, 0, 0);
        public float MovementSpeed = 0.1f;
        public float MouseSensitivity = 0.01f;

        public Matrix4 GetScreenProjectionMatrix(float aspect)
            => Matrix4.LookAt(Vector3.Zero, new Vector3(0, 0, -1), Vector3.UnitY) * Matrix4.CreateOrthographic(aspect * 100, 100, 0, 10);

        public Matrix4 GetViewProjectionMatrix(float aspect)
            => GetViewMatrix() * Matrix4.CreatePerspectiveFieldOfView(1.3f, aspect, .1f, 1000f);

        public Matrix4 GetViewMatrix()
        {
            Vector3 lookAt = new Vector3
            {
                X = (float)(Math.Sin(Orientation.X) * Math.Cos(Orientation.Y)),
                Y = (float)Math.Sin(Orientation.Y),
                Z = (float)(Math.Cos(Orientation.X) * Math.Cos(Orientation.Y))
            };

            return Matrix4.LookAt(Position, Position + lookAt, Vector3.UnitY);
        }

        public void Move(float x, float y, float z)
        {
            Vector3 offset = new Vector3();
            Vector3 forward = new Vector3((float)Math.Sin(Orientation.X), 0, (float)Math.Cos(Orientation.X));
            Vector3 right = new Vector3(-forward.Z, 0, forward.X);

            offset += x * right;
            offset += y * forward;
            offset.Y += z;

            offset.NormalizeFast();
            offset = Vector3.Multiply(offset, MovementSpeed);

            Position += offset;
        }

        public void AddRotation(float x, float y)
        {
            x = x * MouseSensitivity;
            y = y * MouseSensitivity;

            Orientation.X = (Orientation.X + x) % ((float)Math.PI * 2f);
            Orientation.Y = Math.Max(Math.Min(Orientation.Y + y, (float)Math.PI / 2f - 0.1f), (float)-Math.PI / 2f + 0.1f);
        }
    }
}
