using OpenTK;
using FModel.PakReader;
using System;
using System.Linq;
using OpenTK.Mathematics;

namespace FModel.Chic.ModelViewer
{
    class UMeshSection : Volume
    {
        internal const int LOD_LEVEL = 0;

        Vector2[] texCoords;
        Vector3[] colorData;
        Vector3[] verts;
        Vector3[] normals;
        int[] inds;

        public override int VertCount => verts.Length;
        public override int NormalCount => normals.Length;
        public override int ColorDataCount => colorData.Length;
        public override int TextureCoordsCount => texCoords.Length;
        public override int IndiceCount => inds.Length;

        public new Material Material;

        public override string Shader => "textured";

        internal UMeshSection(CSkeletalMesh mesh, USkeletalMesh umesh, int section, int[] disableOverrides, ModelInterface window, Func<string, PakPackage> packageFunc)
        {
            var cLod = mesh.Lods[LOD_LEVEL];
            var uLod = umesh.LODModels[LOD_LEVEL];
            var sec = uLod.Sections[section];

            if (sec.disabled || Array.IndexOf(disableOverrides, sec.material_index) != -1)
            {
                Enabled = false;
                return;
            }

            texCoords = new Vector2[sec.num_vertices];
            colorData = new Vector3[sec.num_vertices];
            verts = new Vector3[sec.num_vertices];
            normals = new Vector3[sec.num_vertices];
            inds = new int[sec.num_triangles * 3];
            int i;
            for (i = 0; i < sec.num_vertices; i++)
            {
                var v = cLod.Verts[i + sec.base_vertex_index];
                var posV = v.Position.v;
                var norm = (FVector)(FPackedNormal)v.Normal;
                texCoords[i] = new Vector2(v.UV.U, 1 - v.UV.V);
                verts[i] = new Vector3(posV[0], posV[1], posV[2]);
                normals[i] = new Vector3(norm.X, norm.Y, norm.Z);
            }
            for (i = 0; i < sec.num_triangles * 3; i++)
            {
                inds[i] = (int)(cLod.Indices[(int)(i + sec.base_index)] - sec.base_vertex_index);
            }
            Material = GetMaterial(umesh.MaterialAssets[sec.material_index], window, packageFunc);
            TextureID = Material.DiffuseMap;
        }

        public Material GetMaterial(string matPath, ModelInterface window, Func<string, PakPackage> packageFunc)
        {
            if (matPath == default)
            {
                window.QueueLoad("/Engine/Content/EngineResources/WhiteSquareTexture");

                return new Material()
                {
                    AmbientColor = new Vector3(0.5f),
                    DiffuseColor = new Vector3(0.5f),
                    SpecularColor = new Vector3(0.02f),
                    SpecularExponent = 10f,

                    AmbientMap = null,
                    DiffuseMap = "/Engine/Content/EngineResources/WhiteSquareTexture",
                    NormalMap = null,
                    SpecularMap = null
                };
            }
            matPath = ValidifyPath(matPath);
            var package = packageFunc(matPath);
            if (package == null)
            {
                window.QueueLoad("/Engine/Content/EngineResources/WhiteSquareTexture");

                return new Material()
                {
                    AmbientColor = new Vector3(0.5f),
                    DiffuseColor = new Vector3(0.5f),
                    SpecularColor = new Vector3(0.02f),
                    SpecularExponent = 10f,

                    AmbientMap = null,
                    DiffuseMap = "/Engine/Content/EngineResources/WhiteSquareTexture",
                    NormalMap = null,
                    SpecularMap = null
                };
            }
            var m = package.Exports.FirstOrDefault(o => o is UMaterial) as UMaterial;
            if (m.DiffuseMap == null)
            {
                m.DiffuseMap = "/Engine/Content/EngineResources/WhiteSquareTexture";
            }
            window.ValidifyQueueLoad(m.DiffuseMap);

            return new Material()
            {
                AmbientColor = new Vector3(0.5f),
                DiffuseColor = new Vector3(0.5f),
                SpecularColor = new Vector3(0.02f),
                SpecularExponent = 10f,

                AmbientMap = null,
                DiffuseMap = m.DiffuseMap,
                NormalMap = null,
                SpecularMap = null
            };
        }

        public static string ValidifyPath(string inp) => inp.Contains("Engine/Content") ? inp : ("/FortniteGame/Content/" + inp.Substring(inp.IndexOf("Game/") + 5).Split('.')[0]);

        public override void CalculateModelMatrix()
        {
            ModelMatrix = Matrix4.CreateScale(Scale) * Matrix4.CreateRotationX(Rotation.X) * Matrix4.CreateRotationY(Rotation.Y) * Matrix4.CreateRotationZ(Rotation.Z) * Matrix4.CreateTranslation(Position);
        }

        public override Vector3[] GetColorData()
        {
            return new Vector3[ColorDataCount];
        }

        public override int[] GetIndices(int offset = 0)
        {
            int[] indices = new int[inds.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                indices[i] = inds[i] + offset;
            }
            return indices;
        }

        public override Vector2[] GetTextureCoords()
        {
            return texCoords;
        }

        public override Vector3[] GetVerts()
        {
            return verts;
        }

        public override Vector3[] GetNormals()
        {
            return normals;
        }
    }
}