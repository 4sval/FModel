using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using CUE4Parse_Conversion;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using FModel.Extensions;
using FModel.Services;
using FModel.Settings;
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

    private readonly UObject _export;
    private readonly int _vertexSize = 13; // VertexIndex + Position + Normal + Tangent + UV + TextureLayer
    private const int _faceSize = 3;

    public readonly string Path;
    public readonly string Name;
    public readonly string Type;
    public readonly bool HasVertexColors;
    public readonly bool HasMorphTargets;
    public readonly int NumTexCoords;
    public uint[] Indices;
    public float[] Vertices;
    public Section[] Sections;
    public Material[] Materials;

    public bool HasSkeleton => Skeleton is { IsLoaded: true };
    public readonly Skeleton Skeleton;

    public int TransformsCount;
    public readonly List<Transform> Transforms;

    public readonly Morph[] Morphs;

    public bool Show;
    public bool Wireframe;
    public bool IsSetup;
    public bool IsSelected;
    public int SelectedInstance;
    public float MorphTime;

    protected Model(UObject export)
    {
        _export = export;
        Path = _export.GetPathName();
        Name = Path.SubstringAfterLast('/').SubstringBefore('.');
        Type = export.ExportType;
        NumTexCoords = 1;
        Transforms = new List<Transform>();
    }

    public Model(UStaticMesh export, CStaticMesh staticMesh) : this(export, staticMesh, Transform.Identity) {}
    public Model(UStaticMesh export, CStaticMesh staticMesh, Transform transform) : this(export, export.Materials, null, staticMesh.LODs[0], staticMesh.LODs[0].Verts, transform) {}
    private Model(USkeletalMesh export, CSkeletalMesh skeletalMesh, Transform transform) : this(export, export.Materials, export.Skeleton, skeletalMesh.LODs[0], skeletalMesh.LODs[0].Verts, transform) {}
    public Model(USkeletalMesh export, CSkeletalMesh skeletalMesh) : this(export, skeletalMesh, Transform.Identity)
    {
        var morphTargets = export.MorphTargets;
        if (morphTargets is not { Length: > 0 })
            return;

        var length = morphTargets.Length;

        HasMorphTargets = true;
        Morphs = new Morph[length];
        for (var i = 0; i < Morphs.Length; i++)
        {
            Morphs[i] = new Morph(Vertices, _vertexSize, morphTargets[i].Load<UMorphTarget>());
            ApplicationService.ApplicationView.Status.UpdateStatusLabel($"{Morphs[i].Name} ... {i}/{length}");
        }
        ApplicationService.ApplicationView.Status.UpdateStatusLabel("");
    }

    private Model(UObject export, ResolvedObject[] materials, FPackageIndex skeleton, CBaseMeshLod lod, CMeshVertex[] vertices, Transform transform = null) : this(export)
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

        if (skeleton != null)
        {
            Skeleton = new Skeleton(skeleton);
            _vertexSize += 8; // + BoneIds + BoneWeights
        }

        Indices = new uint[lod.Indices.Value.Length];
        for (int i = 0; i < Indices.Length; i++)
        {
            Indices[i] = (uint) lod.Indices.Value[i];
        }

        Vertices = new float[lod.NumVerts * _vertexSize];
        for (int i = 0; i < vertices.Length; i++)
        {
            var count = 0;
            var baseIndex = i * _vertexSize;
            var vert = vertices[i];
            Vertices[baseIndex + count++] = i;
            Vertices[baseIndex + count++] = vert.Position.X * Constants.SCALE_DOWN_RATIO;
            Vertices[baseIndex + count++] = vert.Position.Z * Constants.SCALE_DOWN_RATIO;
            Vertices[baseIndex + count++] = vert.Position.Y * Constants.SCALE_DOWN_RATIO;
            Vertices[baseIndex + count++] = vert.Normal.X;
            Vertices[baseIndex + count++] = vert.Normal.Z;
            Vertices[baseIndex + count++] = vert.Normal.Y;
            Vertices[baseIndex + count++] = vert.Tangent.X;
            Vertices[baseIndex + count++] = vert.Tangent.Z;
            Vertices[baseIndex + count++] = vert.Tangent.Y;
            Vertices[baseIndex + count++] = vert.UV.U;
            Vertices[baseIndex + count++] = vert.UV.V;
            Vertices[baseIndex + count++] = lod.ExtraUV.IsValueCreated ? lod.ExtraUV.Value[0][i].U : .5f;

            if (HasVertexColors)
            {
                var color = lod.VertexColors[i];
                Vertices[baseIndex + count++] = color.R;
                Vertices[baseIndex + count++] = color.G;
                Vertices[baseIndex + count++] = color.B;
                Vertices[baseIndex + count++] = color.A;
            }

            if (HasSkeleton)
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
        }

        Sections = new Section[lod.Sections.Value.Length];
        for (var s = 0; s < Sections.Length; s++)
        {
            var section = lod.Sections.Value[s];
            Sections[s] = new Section(section.MaterialIndex, section.NumFaces * _faceSize, section.FirstIndex, Materials[section.MaterialIndex]);
        }

        AddInstance(transform ?? Transform.Identity);
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
        _vao.BindInstancing(); // VertexAttributePointer
    }

    public void Setup(Options options)
    {
        _handle = GL.CreateProgram();
        var broken = GL.GetInteger(GetPName.MaxTextureUnits) == 0;

        _ebo = new BufferObject<uint>(Indices, BufferTarget.ElementArrayBuffer);
        _vbo = new BufferObject<float>(Vertices, BufferTarget.ArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_vbo, _ebo);

        _vao.VertexAttributePointer(0, 1, VertexAttribPointerType.Int, _vertexSize, 0); // vertex index
        _vao.VertexAttributePointer(1, 3, VertexAttribPointerType.Float, _vertexSize, 1); // position
        _vao.VertexAttributePointer(2, 3, VertexAttribPointerType.Float, _vertexSize, 4); // normal
        _vao.VertexAttributePointer(3, 3, VertexAttribPointerType.Float, _vertexSize, 7); // tangent
        _vao.VertexAttributePointer(4, 2, VertexAttribPointerType.Float, _vertexSize, 10); // uv
        if (!broken) _vao.VertexAttributePointer(5, 1, VertexAttribPointerType.Float, _vertexSize, 12); // texture index
        _vao.VertexAttributePointer(6, 4, VertexAttribPointerType.Float, _vertexSize, 13); // color
        _vao.VertexAttributePointer(7, 4, VertexAttribPointerType.Float, _vertexSize, 17); // boneids
        _vao.VertexAttributePointer(8, 4, VertexAttribPointerType.Float, _vertexSize, 21); // boneweights

        SetupInstances(); // instanced models transform

        // setup all used materials for use in different UV channels
        for (var i = 0; i < Materials.Length; i++)
        {
            if (!Materials[i].IsUsed) continue;
            Materials[i].Setup(options, broken ? 1 : NumTexCoords);
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
            _vao.VertexAttributePointer(13, 3, VertexAttribPointerType.Float, 3, 0); // morph position
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
        shader.SetUniform("uNumTexCoords", NumTexCoords);
        shader.SetUniform("uHasVertexColors", HasVertexColors);

        GL.PolygonMode(MaterialFace.FrontAndBack, Wireframe ? PolygonMode.Line : PolygonMode.Fill);
        foreach (var section in Sections)
        {
            if (!section.Show) continue;
            Materials[section.MaterialIndex].Render(shader);
            GL.DrawElementsInstanced(PrimitiveType.Triangles, section.FacesCount, DrawElementsType.UnsignedInt, section.FirstFaceIndexPtr, TransformsCount);
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
        foreach (var section in Sections)
        {
            if (!section.Show) continue;
            GL.DrawElementsInstanced(PrimitiveType.Triangles, section.FacesCount, DrawElementsType.UnsignedInt, section.FirstFaceIndexPtr, TransformsCount);
        }
        _vao.Unbind();
    }

    public void Outline(Shader shader)
    {
        GL.Enable(EnableCap.StencilTest);
        GL.Disable(EnableCap.DepthTest);
        GL.StencilFunc(StencilFunction.Notequal, 1, 0xFF);

        _vao.Bind();
        shader.SetUniform("uMorphTime", MorphTime);

        GL.PolygonMode(MaterialFace.FrontAndBack, Wireframe ? PolygonMode.Line : PolygonMode.Fill);
        foreach (var section in Sections)
        {
            if (!section.Show) continue;
            GL.DrawElementsInstancedBaseInstance(PrimitiveType.Triangles, section.FacesCount, DrawElementsType.UnsignedInt, section.FirstFaceIndexPtr, TransformsCount, SelectedInstance);
        }
        _vao.Unbind();

        GL.StencilFunc(StencilFunction.Always, 0, 0xFF);
        GL.Enable(EnableCap.DepthTest);
        GL.Disable(EnableCap.StencilTest);
    }

    public bool TrySave(out string label, out string savedFilePath)
    {
        var exportOptions = new ExporterOptions
        {
            TextureFormat = UserSettings.Default.TextureExportFormat,
            LodFormat = UserSettings.Default.LodExportFormat,
            MeshFormat = UserSettings.Default.MeshExportFormat,
            Platform = UserSettings.Default.OverridedPlatform,
            ExportMorphTargets = UserSettings.Default.SaveMorphTargets
        };
        var toSave = new Exporter(_export, exportOptions);
        return toSave.TryWriteToDir(new DirectoryInfo(UserSettings.Default.ModelDirectory), out label, out savedFilePath);
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
