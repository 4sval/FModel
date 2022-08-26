using System;
using System.Linq;
using System.Numerics;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse_Conversion.Textures;
using Silk.NET.OpenGL;

namespace FModel.Views.Snooper;

public class Mesh : IDisposable
{
    private uint _handle;
    private GL _gl;

    private BufferObject<uint> _ebo;
    private BufferObject<float> _vbo;
    private VertexArrayObject<float, uint> _vao;

    private Shader _shader;
    private Texture[] _albedoMap;
    // private Texture _normalMap;

    private const int _vertexSize = 8; // Position + Normals + UV
    private const uint _faceSize = 3; // just so we don't have to do .Length
    private readonly uint[] _facesIndex = { 1, 0, 2 };

    public uint[] Indices;
    public float[] Vertices;
    public CMaterialParams[] Params;

    public Mesh(CBaseMeshLod lod, CMeshVertex[] vertices)
    {
        var sections = lod.Sections.Value;
        Params = new CMaterialParams[sections.Length];
        Indices = new uint[sections.Sum(section => section.NumFaces * _faceSize)];
        Vertices = new float[Indices.Length * _vertexSize];

        for (var s = 0; s < sections.Length; s++)
        {
            var section = sections[s];
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

                    Indices[index] = (uint) index;
                }
            }

            Params[s] = new CMaterialParams();
            if (section.Material != null && section.Material.TryLoad(out var material) && material is UMaterialInterface unrealMaterial)
            {
                unrealMaterial.GetParams(Params[s]);
            }
        }
    }

    public void Setup(GL gl)
    {
        _gl = gl;

        _handle = _gl.CreateProgram();

        _ebo = new BufferObject<uint>(_gl, Indices, BufferTargetARB.ElementArrayBuffer);
        _vbo = new BufferObject<float>(_gl, Vertices, BufferTargetARB.ArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo, _ebo);

        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, _vertexSize, 0); // position
        _vao.VertexAttributePointer(1, 3, VertexAttribPointerType.Float, _vertexSize, 3); // normals
        _vao.VertexAttributePointer(2, 2, VertexAttribPointerType.Float, _vertexSize, 6); // uv

        _shader = new Shader(_gl);

        _albedoMap = new Texture[Params.Length];
        for (int i = 0; i < _albedoMap.Length; i++)
        {
            if (Params[i].Diffuse is UTexture2D { IsVirtual: false } diffuse && diffuse.GetFirstMip() is { } mip)
            {
                TextureDecoder.DecodeTexture(mip, diffuse.Format, diffuse.isNormalMap, out var data, out _);
                _albedoMap[i] = new Texture(_gl, data, (uint) mip.SizeX, (uint) mip.SizeY);
            }
        }
    }

    public unsafe void Bind(Camera camera)
    {
        _vao.Bind();

        _shader.Use();

        _shader.SetUniform("uModel", Matrix4x4.Identity);
        _shader.SetUniform("uView", camera.GetViewMatrix());
        _shader.SetUniform("uProjection", camera.GetProjectionMatrix());
        // _shader.SetUniform("viewPos", _camera.Position);

        for (int i = 0; i < _albedoMap.Length; i++)
        {
            _shader.SetUniform("material.albedo", i);
            _albedoMap[i].Bind(TextureUnit.Texture0 + i);
        }

        _gl.DrawElements(PrimitiveType.Triangles, (uint) Indices.Length, DrawElementsType.UnsignedInt, null);
    }

    public void Dispose()
    {
        _ebo.Dispose();
        _vbo.Dispose();
        _vao.Dispose();
        _shader.Dispose();
        _gl.DeleteProgram(_handle);
    }
}
