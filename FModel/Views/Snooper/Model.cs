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

    private Shader _shader;

    private uint _vertexSize = 8; // Position + Normal + UV
    private const uint _faceSize = 3; // just so we don't have to do .Length
    private readonly uint[] _facesIndex = { 1, 0, 2 };

    public readonly string Name;
    public readonly bool HasVertexColors;
    public readonly uint[] Indices;
    public readonly float[] Vertices;
    public readonly Section[] Sections;

    private string[] _transforms = {
        "X Location", "Y", "Z",
        "X Rotation", "Y", "Z",
        "X Scale", "Y", "Z"
    };
    private bool _display_vertex_colors;
    private Transform _transform = Transform.Identity;

    public Model(string name, CBaseMeshLod lod, CMeshVertex[] vertices)
    {
        Name = name;
        HasVertexColors = lod.VertexColors != null;
        if (HasVertexColors) _vertexSize += 4; // + Color

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

                    if (HasVertexColors)
                    {
                        var color = lod.VertexColors[indice];
                        Vertices[index * _vertexSize + 8] = color.R;
                        Vertices[index * _vertexSize + 9] = color.G;
                        Vertices[index * _vertexSize + 10] = color.B;
                        Vertices[index * _vertexSize + 11] = color.A;
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

        for (int section = 0; section < Sections.Length; section++)
        {
            Sections[section].Setup(_gl);
        }
    }

    public void Bind(Camera camera)
    {
        _vao.Bind();

        _shader.Use();

        _shader.SetUniform("uModel", _transform.Matrix);
        _shader.SetUniform("uView", camera.GetViewMatrix());
        _shader.SetUniform("uProjection", camera.GetProjectionMatrix());
        _shader.SetUniform("viewPos", camera.Position);

        _shader.SetUniform("material.diffuseMap", 0);
        _shader.SetUniform("material.normalMap", 1);
        _shader.SetUniform("material.specularMap", 2);
        _shader.SetUniform("material.emissionMap", 3);

        _shader.SetUniform("light.position", camera.Position);

        _shader.SetUniform("display_vertex_colors", _display_vertex_colors);

        ImGui.Text($"Entity: {Name}");
        if (ImGui.TreeNode("Transform"))
        {
            const int width = 100;
            var speed = camera.Speed / 100;
            var index = 0;

            ImGui.SetNextItemWidth(width); ImGui.PushID(index);
            ImGui.DragFloat(_transforms[index], ref _transform.Position.X, speed, 0f, 0f, "%.2f m");
            ImGui.PopID();

            index++; ImGui.SetNextItemWidth(width); ImGui.PushID(index);
            ImGui.DragFloat(_transforms[index], ref _transform.Position.Z, speed, 0f, 0f, "%.2f m");
            ImGui.PopID();

            index++; ImGui.SetNextItemWidth(width); ImGui.PushID(index);
            ImGui.DragFloat(_transforms[index], ref _transform.Position.Y, speed, 0f, 0f, "%.2f m");
            ImGui.PopID();

            ImGui.Spacing();

            index++; ImGui.SetNextItemWidth(width); ImGui.PushID(index);
            ImGui.DragFloat(_transforms[index], ref _transform.Rotation.X, 1f, 0f, 0f, "%.1f°");
            ImGui.PopID();

            index++; ImGui.SetNextItemWidth(width); ImGui.PushID(index);
            ImGui.DragFloat(_transforms[index], ref _transform.Rotation.Z, 1f, 0f, 0f, "%.1f°");
            ImGui.PopID();

            index++; ImGui.SetNextItemWidth(width); ImGui.PushID(index);
            ImGui.DragFloat(_transforms[index], ref _transform.Rotation.Y, 1f, 0f, 0f, "%.1f°");
            ImGui.PopID();

            ImGui.Spacing();

            index++; ImGui.SetNextItemWidth(width); ImGui.PushID(index);
            ImGui.DragFloat(_transforms[index], ref _transform.Scale.X, speed, 0f, 0f, "%.3f");
            ImGui.PopID();

            index++; ImGui.SetNextItemWidth(width); ImGui.PushID(index);
            ImGui.DragFloat(_transforms[index], ref _transform.Scale.Z, speed, 0f, 0f, "%.3f");
            ImGui.PopID();

            index++; ImGui.SetNextItemWidth(width); ImGui.PushID(index);
            ImGui.DragFloat(_transforms[index], ref _transform.Scale.Y, speed, 0f, 0f, "%.3f");
            ImGui.PopID();

            ImGui.TreePop();
        }
        if (HasVertexColors) ImGui.Checkbox("Display Vertex Colors", ref _display_vertex_colors);
        ImGui.Separator();

        for (int section = 0; section < Sections.Length; section++)
        {
            Sections[section].Bind(section, _shader);
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
