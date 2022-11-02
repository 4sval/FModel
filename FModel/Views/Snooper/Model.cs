using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports.Material;
using OpenTK.Graphics.OpenGL4;

namespace FModel.Views.Snooper;

public class Model : IDisposable
{
    private int _handle;

    private BufferObject<uint> _ebo;
    private BufferObject<float> _vbo;
    private BufferObject<float> _morphVbo;
    private BufferObject<Matrix4x4> _matrixVbo;
    private VertexArrayObject<float, uint> _vao;

    private readonly int _vertexSize = 10; // VertexIndex + Position + Normal + UV + TextureLayer
    private readonly uint[] _facesIndex = { 1, 0, 2 };
    private const int _faceSize = 3; // just so we don't have to do .Length

    public readonly string Name;
    public readonly string Type;
    public readonly bool HasVertexColors;
    public readonly bool HasBones;
    public readonly bool HasMorphTargets;
    public readonly int NumTexCoords;
    public uint[] Indices;
    public float[] Vertices;
    public Section[] Sections;
    public Material[] Materials;
    public readonly List<CSkelMeshBone> Skeleton;

    public int TransformsCount;
    public readonly List<Transform> Transforms;

    public readonly Morph[] Morphs;

    public bool Show;
    public bool Wireframe;
    public bool IsSetup;
    public bool IsSelected;
    public bool DisplayVertexColors;
    public bool DisplayBones;
    public int SelectedInstance;
    public float MorphTime;

    protected Model(string name, string type)
    {
        Name = name;
        Type = type;
        NumTexCoords = 1;
        Transforms = new List<Transform>();
    }

    public Model(string name, string type, ResolvedObject[] materials, CStaticMesh staticMesh) : this(name, type, materials, staticMesh, Transform.Identity) {}
    public Model(string name, string type, ResolvedObject[] materials, CStaticMesh staticMesh, Transform transform) : this(name, type, materials, staticMesh.LODs[0], staticMesh.LODs[0].Verts, transform) {}
    public Model(string name, string type, ResolvedObject[] materials, CSkeletalMesh skeletalMesh) : this(name, type, materials, skeletalMesh, Transform.Identity) {}
    public Model(string name, string type, ResolvedObject[] materials, CSkeletalMesh skeletalMesh, Transform transform) : this(name, type, materials, skeletalMesh.LODs[0], skeletalMesh.LODs[0].Verts, transform, skeletalMesh.RefSkeleton) {}
    public Model(string name, string type, ResolvedObject[] materials, FPackageIndex[] morphTargets, CSkeletalMesh skeletalMesh) : this(name, type, materials, skeletalMesh)
    {
        if (morphTargets is not { Length: > 0 })
            return;

        HasMorphTargets = true;
        Morphs = new Morph[morphTargets.Length];
        for (var i = 0; i < Morphs.Length; i++)
        {
            Morphs[i] = new Morph(Vertices, _vertexSize, morphTargets[i].Load<UMorphTarget>());
        }
    }

    private Model(string name, string type, ResolvedObject[] materials, CBaseMeshLod lod, CMeshVertex[] vertices, Transform transform, List<CSkelMeshBone> skeleton = null) : this(name, type)
    {
        NumTexCoords = lod.NumTexCoords;

        Materials = new Material[materials.Length];
        for (int m = 0; m < Materials.Length; m++)
        {
            if ((materials[m]?.TryLoad(out var material) ?? false) && material is UMaterialInterface unrealMaterial)
                Materials[m] = new Material(unrealMaterial); else Materials[m] = new Material();
        }

        if (lod.VertexColors is { Length: > 0})
        {
            HasVertexColors = true;
            _vertexSize += 4; // + Color
        }

        if (skeleton is { Count: > 0 })
        {
            HasBones = true;
            Skeleton = skeleton;
            _vertexSize += 8; // + BoneIds + BoneWeights
        }

        var sections = lod.Sections.Value;
        Sections = new Section[sections.Length];
        Indices = new uint[sections.Sum(section => section.NumFaces * _faceSize)];
        Vertices = new float[Indices.Length * _vertexSize];

        for (var s = 0; s < sections.Length; s++)
        {
            var section = sections[s];
            Sections[s] = new Section(section.MaterialIndex, section.NumFaces * _faceSize, section.FirstIndex, Materials[section.MaterialIndex]);
            for (uint face = 0; face < section.NumFaces; face++)
            {
                foreach (var f in _facesIndex)
                {
                    var count = 0;
                    var i = face * _faceSize + f;
                    var index = section.FirstIndex + i;
                    var baseIndex = index * _vertexSize;
                    var indice = lod.Indices.Value[index];

                    var vert = vertices[indice];
                    Vertices[baseIndex + count++] = indice;
                    Vertices[baseIndex + count++] = vert.Position.X * Constants.SCALE_DOWN_RATIO;
                    Vertices[baseIndex + count++] = vert.Position.Z * Constants.SCALE_DOWN_RATIO;
                    Vertices[baseIndex + count++] = vert.Position.Y * Constants.SCALE_DOWN_RATIO;
                    Vertices[baseIndex + count++] = vert.Normal.X;
                    Vertices[baseIndex + count++] = vert.Normal.Z;
                    Vertices[baseIndex + count++] = vert.Normal.Y;
                    Vertices[baseIndex + count++] = vert.UV.U;
                    Vertices[baseIndex + count++] = vert.UV.V;
                    Vertices[baseIndex + count++] = lod.ExtraUV.IsValueCreated ? lod.ExtraUV.Value[0][indice].U : .5f;

                    if (HasVertexColors)
                    {
                        var color = lod.VertexColors[indice];
                        Vertices[baseIndex + count++] = color.R;
                        Vertices[baseIndex + count++] = color.G;
                        Vertices[baseIndex + count++] = color.B;
                        Vertices[baseIndex + count++] = color.A;
                    }

                    if (HasBones)
                    {
                        var skelVert = (CSkelMeshVertex) vert;
                        var weightsHash = skelVert.UnpackWeights();
                        Vertices[baseIndex + count++] = skelVert.Bone[0];
                        Vertices[baseIndex + count++] = skelVert.Bone[1];
                        Vertices[baseIndex + count++] = skelVert.Bone[2];
                        Vertices[baseIndex + count++] = skelVert.Bone[3];
                        Vertices[baseIndex + count++] = weightsHash[0];
                        Vertices[baseIndex + count++] = weightsHash[1];
                        Vertices[baseIndex + count++] = weightsHash[2];
                        Vertices[baseIndex + count++] = weightsHash[3];
                    }

                    Indices[index] = i;
                }
            }
        }

        AddInstance(transform);
    }

    public void AddInstance(Transform transform)
    {
        TransformsCount++;
        Transforms.Add(transform);
    }

    public void UpdateMatrix(int instance)
    {
        _matrixVbo.Bind();
        _matrixVbo.Update(instance, Transforms[instance].Matrix);
        _matrixVbo.Unbind();
    }

    public void UpdateMorph(int index)
    {
        _morphVbo.Bind();
        _morphVbo.Update(Morphs[index].Vertices);
        _morphVbo.Unbind();
    }

    public void SetupInstances()
    {
        var instanceMatrix = new Matrix4x4[TransformsCount];
        for (var i = 0; i < instanceMatrix.Length; i++)
            instanceMatrix[i] = Transforms[i].Matrix;
        _matrixVbo = new BufferObject<Matrix4x4>(instanceMatrix, BufferTarget.ArrayBuffer);
        _vao.BindInstancing(); // VertexAttributePointer 7, 8, 9, 10
    }

    public void Setup(Cache cache)
    {
        _handle = GL.CreateProgram();

        _ebo = new BufferObject<uint>(Indices, BufferTarget.ElementArrayBuffer);
        _vbo = new BufferObject<float>(Vertices, BufferTarget.ArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_vbo, _ebo);

        _vao.VertexAttributePointer(0, 1, VertexAttribPointerType.Int, _vertexSize, 0); // vertex index
        _vao.VertexAttributePointer(1, 3, VertexAttribPointerType.Float, _vertexSize, 1); // position
        _vao.VertexAttributePointer(2, 3, VertexAttribPointerType.Float, _vertexSize, 4); // normal
        _vao.VertexAttributePointer(3, 2, VertexAttribPointerType.Float, _vertexSize, 7); // uv
        _vao.VertexAttributePointer(4, 1, VertexAttribPointerType.Float, _vertexSize, 9); // texture index
        _vao.VertexAttributePointer(5, 4, VertexAttribPointerType.Float, _vertexSize, 10); // color
        _vao.VertexAttributePointer(6, 4, VertexAttribPointerType.Int, _vertexSize, 14); // boneids
        _vao.VertexAttributePointer(7, 4, VertexAttribPointerType.Float, _vertexSize, 18); // boneweights

        SetupInstances(); // instanced models transform

        // setup all used materials for use in different UV channels
        for (var i = 0; i < Materials.Length; i++)
        {
            if (!Materials[i].IsUsed) continue;
            Materials[i].Setup(cache, NumTexCoords);
        }

        if (HasMorphTargets)
        {
            for (uint morph = 0; morph < Morphs.Length; morph++)
            {
                Morphs[morph].Setup();
                if (morph == 0)
                    _morphVbo = new BufferObject<float>(Morphs[morph].Vertices, BufferTarget.ArrayBuffer);
            }
            _vao.Bind();
            _vao.VertexAttributePointer(12, 3, VertexAttribPointerType.Float, 3, 0); // morph position
            _vao.Unbind();
        }

        for (int section = 0; section < Sections.Length; section++)
        {
            if (!Show) Show = Sections[section].Show;
            Sections[section].Setup();
        }

        IsSetup = true;
    }

    public void Render(Shader shader)
    {
        if (IsSelected)
        {
            GL.Enable(EnableCap.StencilTest);
            GL.StencilFunc(StencilFunction.Always, 1, 0xFF);
        }

        _vao.Bind();
        shader.SetUniform("uMorphTime", MorphTime);
        shader.SetUniform("display_vertex_colors", DisplayVertexColors);

        GL.PolygonMode(MaterialFace.FrontAndBack, Wireframe ? PolygonMode.Line : PolygonMode.Fill);
        for (int section = 0; section < Sections.Length; section++)
        {
            Materials[Sections[section].MaterialIndex].Render(shader);
            Sections[section].Render(TransformsCount);
        }
        _vao.Unbind();

        if (IsSelected)
        {
            GL.StencilFunc(StencilFunction.Always, 0, 0xFF);
            GL.Disable(EnableCap.StencilTest);
        }
    }

    public void SimpleRender(Shader shader)
    {
        _vao.Bind();
        shader.SetUniform("uMorphTime", MorphTime);
        for (int section = 0; section < Sections.Length; section++)
        {
            Sections[section].Render(TransformsCount);
        }
        _vao.Unbind();
    }

    public void Outline(Shader shader)
    {
        GL.StencilMask(0x00);
        GL.Disable(EnableCap.DepthTest);
        GL.StencilFunc(StencilFunction.Notequal, 1, 0xFF);

        _vao.Bind();
        shader.Use();
        shader.SetUniform("uMorphTime", MorphTime);

        GL.PolygonMode(MaterialFace.FrontAndBack, Wireframe ? PolygonMode.Line : PolygonMode.Fill);
        for (int section = 0; section < Sections.Length; section++)
        {
            if (!Sections[section].Show) continue;
            GL.DrawArraysInstancedBaseInstance(PrimitiveType.Triangles, Sections[section].FirstFaceIndex, Sections[section].FacesCount, TransformsCount, SelectedInstance);
        }
        _vao.Unbind();

        GL.StencilFunc(StencilFunction.Always, 0, 0xFF);
        GL.Enable(EnableCap.DepthTest);
        GL.StencilMask(0xFF);
    }

    public void Dispose()
    {
        _ebo.Dispose();
        _vbo.Dispose();
        _matrixVbo.Dispose();
        _vao.Dispose();
        if (HasMorphTargets)
        {
            _morphVbo.Dispose();
            for (var morph = 0; morph < Morphs.Length; morph++)
            {
                Morphs[morph].Dispose();
            }
        }

        for (int section = 0; section < Sections.Length; section++)
        {
            Sections[section].Dispose();
        }

        GL.DeleteProgram(_handle);
    }
}
