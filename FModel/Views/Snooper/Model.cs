using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse_Conversion.Meshes.PSK;
using Silk.NET.OpenGL;

namespace FModel.Views.Snooper;

public class Model : IDisposable
{
    private uint _handle;
    private GL _gl;

    private BufferObject<uint> _ebo;
    private BufferObject<float> _vbo;
    private VertexArrayObject<float, uint> _vao;

    private Shader _shader;

    private uint _vertexSize = 8; // Position + Normal + UV
    private const uint _faceSize = 3; // just so we don't have to do .Length
    private readonly uint[] _facesIndex = { 1, 0, 2 };

    public readonly string Name;
    public readonly string Type;
    public readonly bool HasVertexColors;
    public readonly bool HasBones;
    public uint[] Indices;
    public float[] Vertices;
    public Section[] Sections;
    public readonly List<CSkelMeshBone> Skeleton;

    public readonly Transform Transforms = Transform.Identity;
    public readonly string[] TransformsLabels = {
        "X Location", "Y", "Z",
        "X Rotation", "Y", "Z",
        "X Scale", "Y", "Z"
    };
    public bool DisplayVertexColors;
    public bool DisplayBones;

    protected Model(string name, string type)
    {
        Name = name;
        Type = type;
    }

    public Model(string name, string type, CBaseMeshLod lod, CMeshVertex[] vertices, List<CSkelMeshBone> skeleton = null) : this(name, type)
    {
        HasVertexColors = lod.VertexColors != null;
        if (HasVertexColors) _vertexSize += 4; // + Color

        Skeleton = skeleton;
        HasBones = Skeleton != null;
        if (HasBones) _vertexSize += 8; // + BoneIds + BoneWeights

        var sections = lod.Sections.Value;
        Sections = new Section[sections.Length];
        Indices = new uint[sections.Sum(section => section.NumFaces * _faceSize)];
        Vertices = new float[Indices.Length * _vertexSize];

        for (var s = 0; s < sections.Length; s++)
        {
            var section = sections[s];
            Sections[s] = new Section(section.MaterialName, section.MaterialIndex, (uint) section.NumFaces * _faceSize, section.FirstIndex, section);
            for (uint face = 0; face < section.NumFaces; face++)
            {
                foreach (var f in _facesIndex)
                {
                    var count = 0;
                    var i = face * _faceSize + f;
                    var index = section.FirstIndex + i;
                    var indice = lod.Indices.Value[index];

                    var vert = vertices[indice];
                    Vertices[index * _vertexSize + count++] = vert.Position.X * Constants.SCALE_DOWN_RATIO;
                    Vertices[index * _vertexSize + count++] = vert.Position.Z * Constants.SCALE_DOWN_RATIO;
                    Vertices[index * _vertexSize + count++] = vert.Position.Y * Constants.SCALE_DOWN_RATIO;
                    Vertices[index * _vertexSize + count++] = vert.Normal.X;
                    Vertices[index * _vertexSize + count++] = vert.Normal.Z;
                    Vertices[index * _vertexSize + count++] = vert.Normal.Y;
                    Vertices[index * _vertexSize + count++] = vert.UV.U;
                    Vertices[index * _vertexSize + count++] = vert.UV.V;


                    if (HasVertexColors)
                    {
                        var color = lod.VertexColors[indice];
                        Vertices[index * _vertexSize + count++] = color.R;
                        Vertices[index * _vertexSize + count++] = color.G;
                        Vertices[index * _vertexSize + count++] = color.B;
                        Vertices[index * _vertexSize + count++] = color.A;
                    }

                    if (HasBones)
                    {
                        var skelVert = (CSkelMeshVertex) vert;
                        var weightsHash = skelVert.UnpackWeights();
                        Vertices[index * _vertexSize + count++] = skelVert.Bone[0];
                        Vertices[index * _vertexSize + count++] = skelVert.Bone[1];
                        Vertices[index * _vertexSize + count++] = skelVert.Bone[2];
                        Vertices[index * _vertexSize + count++] = skelVert.Bone[3];
                        Vertices[index * _vertexSize + count++] = weightsHash[0];
                        Vertices[index * _vertexSize + count++] = weightsHash[1];
                        Vertices[index * _vertexSize + count++] = weightsHash[2];
                        Vertices[index * _vertexSize + count++] = weightsHash[3];
                    }

                    Indices[index] = i;
                }
            }
        }
    }

    public void Setup(GL gl)
    {
        _gl = gl;

        _handle = _gl.CreateProgram();

        _shader = new Shader(_gl);

        _ebo = new BufferObject<uint>(_gl, Indices, BufferTargetARB.ElementArrayBuffer);
        _vbo = new BufferObject<float>(_gl, Vertices, BufferTargetARB.ArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo, _ebo);

        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, _vertexSize, 0); // position
        _vao.VertexAttributePointer(1, 3, VertexAttribPointerType.Float, _vertexSize, 3); // normal
        _vao.VertexAttributePointer(2, 2, VertexAttribPointerType.Float, _vertexSize, 6); // uv
        _vao.VertexAttributePointer(3, 4, VertexAttribPointerType.Float, _vertexSize, 8); // color
        _vao.VertexAttributePointer(4, 4, VertexAttribPointerType.Int, _vertexSize, 12); // boneids
        _vao.VertexAttributePointer(5, 4, VertexAttribPointerType.Float, _vertexSize, 16); // boneweights

        for (int section = 0; section < Sections.Length; section++)
        {
            Sections[section].Setup(_gl);
        }
    }

    public void Bind(Camera camera)
    {
        _vao.Bind();

        _shader.Use();

        _shader.SetUniform("uModel", Transforms.Matrix);
        _shader.SetUniform("uView", camera.GetViewMatrix());
        _shader.SetUniform("uProjection", camera.GetProjectionMatrix());
        _shader.SetUniform("viewPos", camera.Position);

        _shader.SetUniform("material.diffuseMap", 0);
        _shader.SetUniform("material.normalMap", 1);
        _shader.SetUniform("material.specularMap", 2);
        _shader.SetUniform("material.emissionMap", 3);

        _shader.SetUniform("light.position", camera.Position);

        _shader.SetUniform("display_vertex_colors", DisplayVertexColors);

        for (int section = 0; section < Sections.Length; section++)
        {
            Sections[section].Bind(_shader);
        }
    }

    public void Dispose()
    {
        _ebo.Dispose();
        _vbo.Dispose();
        _vao.Dispose();
        _shader.Dispose();
        for (int section = 0; section < Sections.Length; section++)
        {
            Sections[section].Dispose();
        }
        _gl.DeleteProgram(_handle);
    }
}
