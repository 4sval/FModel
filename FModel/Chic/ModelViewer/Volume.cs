using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace FModel.Chic.ModelViewer
{
    public abstract class Volume
    {
        public Vector3 Position = Vector3.Zero;
        public Vector3 Rotation = Vector3.Zero;
        public Vector3 Scale = Vector3.One;

        public virtual int VertCount { get; }
        public virtual int IndiceCount { get; }
        public virtual int ColorDataCount { get; }
        public virtual int NormalCount => Normals.Length;
        public virtual int TextureCoordsCount { get; }

        public abstract string Shader { get; }
        public virtual bool Enabled { get; set; } = true;
        public virtual bool WorldSpace { get; set; } = true;

        public Matrix4 ModelMatrix = Matrix4.Identity;
        public Matrix4 ModelViewProjectionMatrix = Matrix4.Identity;

        Vector3[] Normals = new Vector3[0];

        public Material Material = new Material();

        public abstract Vector3[] GetVerts();
        public abstract int[] GetIndices(int offset = 0);
        public abstract Vector3[] GetColorData();
        public abstract void CalculateModelMatrix();

        public virtual Vector3[] GetNormals() => Normals;

        public void CalculateNormals()
        {
            Vector3[] normals = new Vector3[VertCount];
            Vector3[] verts = GetVerts();
            int[] inds = GetIndices();

            for (int i = 0; i < IndiceCount; i += 3)
            {
                Vector3 v1 = verts[inds[i]];
                Vector3 v2 = verts[inds[i + 1]];
                Vector3 v3 = verts[inds[i + 2]];

                normals[inds[i]] += Vector3.Cross(v2 - v1, v3 - v1);
                normals[inds[i + 1]] += Vector3.Cross(v2 - v1, v3 - v1);
                normals[inds[i + 2]] += Vector3.Cross(v2 - v1, v3 - v1);
            }

            for (int i = 0; i < NormalCount; i++)
            {
                normals[i] = normals[i].Normalized();
            }

            Normals = normals;
        }

        public string TextureID;
        public abstract Vector2[] GetTextureCoords();
    }
}
