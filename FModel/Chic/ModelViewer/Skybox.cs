using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace FModel.Chic.ModelViewer
{
    public class Skybox : Volume
    {
        static readonly Vector3[] Verts = new Vector3[]
        {
            new Vector3(1, 1, -1),
            new Vector3(1, -1, -1),
            new Vector3(-1, -1, -1),
            new Vector3(-1, 1, -1),
            new Vector3(-1, -1, 1),
            new Vector3(-1, 1, 1),
            new Vector3(-1, 1, -1),
            new Vector3(-1, -1, -1),
            new Vector3(1, -1, 1),
            new Vector3(1, 1, 1),
            new Vector3(-1, -1, 1),
            new Vector3(-1, 1, 1),
            new Vector3(1, -1, -1),
            new Vector3(1, 1, -1),
            new Vector3(1, -1, 1),
            new Vector3(1, 1, 1),
            new Vector3(1, 1, -1),
            new Vector3(-1, 1, -1),
            new Vector3(1, 1, 1),
            new Vector3(-1, 1, 1),
            new Vector3(1, -1, -1),
            new Vector3(1, -1, 1),
            new Vector3(-1, -1, 1),
            new Vector3(-1, -1, -1)
        };

        static readonly Vector3[] Normals = new Vector3[]
        {
            new Vector3(0, 0, -1),
            new Vector3(0, 0, -1),
            new Vector3(0, 0, -1),
            new Vector3(0, 0, -1),
            new Vector3(-1, 0, 0),
            new Vector3(-1, 0, 0),
            new Vector3(-1, 0, 0),
            new Vector3(-1, 0, 0),
            new Vector3(0, 0, 1),
            new Vector3(0, 0, 1),
            new Vector3(0, 0, 1),
            new Vector3(0, 0, 1),
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, -1, 0),
            new Vector3(0, -1, 0),
            new Vector3(0, -1, 0),
            new Vector3(0, -1, 0)
        };

        static readonly Vector2[] TexCoords = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1/3f, 0),
            new Vector2(1/3f, .5f),
            new Vector2(0, .5f),
            new Vector2(2/3f, .5f),
            new Vector2(2/3f, 1),
            new Vector2(1/3f, 1),
            new Vector2(1/3f, .5f),
            new Vector2(0, 1),
            new Vector2(0, .5f),
            new Vector2(1/3f, 1),
            new Vector2(1/3f, .5f),
            new Vector2(1/3f, .5f),
            new Vector2(1/3f, 0),
            new Vector2(2/3f, .5f),
            new Vector2(2/3f, 0),
            new Vector2(2/3f, .5f),
            new Vector2(2/3f, 0),
            new Vector2(1, .5f),
            new Vector2(1, 0),
            new Vector2(1, .5f),
            new Vector2(1, 1),
            new Vector2(2/3f, 1),
            new Vector2(2/3f, .5f)
        };

        static readonly int[] inds = new int[]
        {
            0, 1, 2,
            0, 2, 3,
            4, 5, 6,
            4, 6, 7,
            8, 9, 10,
            9, 10, 11,
            12, 13, 14,
            13, 14, 15,
            16, 17, 18,
            17, 18, 19,
            20, 21, 22,
            20, 22, 23
        };

        public override string Shader => "textured";

        public override void CalculateModelMatrix()
        {
            ModelMatrix = Matrix4.CreateScale(Scale) * Matrix4.CreateRotationX(Rotation.X) * Matrix4.CreateRotationY(Rotation.Y) * Matrix4.CreateRotationZ(Rotation.Z) * Matrix4.CreateTranslation(Position);
        }

        public override Vector3[] GetColorData() => new Vector3[Verts.Length];

        public override int[] GetIndices(int offset = 0)
        {
            int[] indices = new int[inds.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                indices[i] = inds[i] + offset;
            }
            return indices;
        }

        public override Vector2[] GetTextureCoords() => TexCoords;
        public override Vector3[] GetVerts() => Verts;
        public override Vector3[] GetNormals() => Normals;
    }
}
