﻿using System;
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

    private Shader _shader;

    private BufferObject<uint> _ebo;
    private BufferObject<float> _vbo;
    private VertexArrayObject<float, uint> _vao;

    private uint _vertexSize = 8; // Position + Normal + UV
    private const uint _faceSize = 3; // just so we don't have to do .Length
    private readonly uint[] _facesIndex = { 1, 0, 2 };

    public readonly string Name;
    public readonly bool HasVertexColors;
    public readonly uint[] Indices;
    public readonly float[] Vertices;
    public readonly Section[] Sections;

    private bool _display_vertex_colors;

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
        ImGui.Text($"Entity: {Name}");
        if (HasVertexColors)
            ImGui.Checkbox("Display Vertex Colors", ref _display_vertex_colors);

        _vao.Bind();

        _shader.Use();

        _shader.SetUniform("uModel", Matrix4x4.Identity);
        _shader.SetUniform("uView", camera.GetViewMatrix());
        _shader.SetUniform("uProjection", camera.GetProjectionMatrix());
        _shader.SetUniform("viewPos", camera.Position);
        _shader.SetUniform("display_vertex_colors", _display_vertex_colors);

        _shader.SetUniform("material.diffuseMap", 0);
        _shader.SetUniform("material.normalMap", 1);
        _shader.SetUniform("material.specularMap", 2);
        _shader.SetUniform("material.emissionMap", 3);

        ImGui.BeginTable("Sections", 2, ImGuiTableFlags.RowBg);
        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Material", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableHeadersRow();
        for (int section = 0; section < Sections.Length; section++)
        {
            Sections[section].Bind(_shader, Indices.Length);
            // if (!Sections[section].Show) continue;
            _gl.DrawArrays(PrimitiveType.Triangles, Sections[section].FirstFaceIndex, Sections[section].FacesCount);
        }
        ImGui.EndTable();

        _shader.SetUniform("light.position", camera.Position);

        ImGui.Separator();
    }

    public void Dispose()
    {
        _shader.Dispose();
        _ebo.Dispose();
        _vbo.Dispose();
        _vao.Dispose();
        for (int section = 0; section < Sections.Length; section++)
        {
            Sections[section].Dispose();
        }
        _gl.DeleteProgram(_handle);
    }
}