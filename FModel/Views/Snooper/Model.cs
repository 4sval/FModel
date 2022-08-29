using System;
using System.Linq;
using System.Numerics;
using CUE4Parse_Conversion.Meshes.PSK;
using ImGuiNET;
using Silk.NET.OpenGL;

namespace FModel.Views.Snooper;

public class Model : IDisposable
{
    private uint _handle;
    private GL _gl;

    private BufferObject<uint> _ebo;
    private BufferObject<float> _vbo;
    private VertexArrayObject<float, uint> _vao;

    private const int _vertexSize = 8; // Position + Normal + UV
    private const uint _faceSize = 3; // just so we don't have to do .Length
    private readonly uint[] _facesIndex = { 1, 0, 2 };

    private Shader _shader;

    public readonly string Name;
    public readonly uint[] Indices;
    public readonly float[] Vertices;
    public readonly Section[] Sections;

    public Model(string name, CBaseMeshLod lod, CMeshVertex[] vertices)
    {
        Name = name;

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
                    var i = face * _faceSize + f;
                    var index = section.FirstIndex + i;
                    var indice = lod.Indices.Value[index];

                    var vert = vertices[indice];
                    Vertices[index * _vertexSize] = vert.Position.X * Constants.SCALE_DOWN_RATIO;
                    Vertices[index * _vertexSize + 1] = vert.Position.Z * Constants.SCALE_DOWN_RATIO;
                    Vertices[index * _vertexSize + 2] = vert.Position.Y * Constants.SCALE_DOWN_RATIO;
                    Vertices[index * _vertexSize + 3] = vert.Normal.X;
                    Vertices[index * _vertexSize + 4] = vert.Normal.Z;
                    Vertices[index * _vertexSize + 5] = vert.Normal.Y;
                    Vertices[index * _vertexSize + 6] = vert.UV.U;
                    Vertices[index * _vertexSize + 7] = vert.UV.V;

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

        for (int section = 0; section < Sections.Length; section++)
        {
            Sections[section].Setup(_gl);
        }
    }

    public void Bind(Camera camera)
    {
        ImGui.Text($"Entity: {Name}");

        _vao.Bind();

        _shader.Use();

        _shader.SetUniform("uModel", Matrix4x4.Identity);
        _shader.SetUniform("uView", camera.GetViewMatrix());
        _shader.SetUniform("uProjection", camera.GetProjectionMatrix());
        _shader.SetUniform("viewPos", camera.Position);

        _shader.SetUniform("material.diffuse", 0);
        _shader.SetUniform("material.normal", 1);
        _shader.SetUniform("material.specular", 2);
        // _shader.SetUniform("material.metallic", 3);
        // _shader.SetUniform("material.emission", 4);
        _shader.SetUniform("material.shininess", 32f);

        var lightColor = Vector3.One;
        var diffuseColor = lightColor * new Vector3(0.5f);
        var ambientColor = diffuseColor * new Vector3(0.2f);

        _shader.SetUniform("light.ambient", ambientColor);
        _shader.SetUniform("light.diffuse", diffuseColor); // darkened
        _shader.SetUniform("light.specular", Vector3.One);
        _shader.SetUniform("light.position", camera.Position);

        ImGui.BeginTable("Sections", 2, ImGuiTableFlags.RowBg);
        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Material", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();
        for (int section = 0; section < Sections.Length; section++)
        {
            Sections[section].Bind(Indices.Length);
            _gl.DrawArrays(PrimitiveType.Triangles, Sections[section].FirstFaceIndex, Sections[section].FacesCount);
        }
        ImGui.EndTable();

        ImGui.Separator();
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
