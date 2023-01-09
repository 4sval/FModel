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
using CUE4Parse.UE4.Objects.Core.Math;
using FModel.Extensions;
using FModel.Services;
using FModel.Settings;
using FModel.Views.Snooper.Buffers;
using FModel.Views.Snooper.Models.Animations;
using FModel.Views.Snooper.Shading;
using OpenTK.Graphics.OpenGL4;

namespace FModel.Views.Snooper.Models;

public class Model : IDisposable
{
    private int _handle;

    private BufferObject<uint> _ebo;
    private BufferObject<float> _vbo;
    private BufferObject<float> _morphVbo;
    private BufferObject<Matrix4x4> _matrixVbo;
    private VertexArrayObject<float, uint> _vao;

    private readonly UObject _export;
    protected readonly int VertexSize = 13; // VertexIndex + Position + Normal + Tangent + UV + TextureLayer
    private const int _faceSize = 3;

    public readonly string Path;
    public readonly string Name;
    public readonly string Type;
    public readonly bool HasVertexColors;
    public readonly bool HasMorphTargets;
    public readonly int UvCount;
    public readonly FBox Box;
    public uint[] Indices;
    public float[] Vertices;
    public Section[] Sections;
    public Material[] Materials;
    public bool TwoSided;

    public bool HasSkeleton => Skeleton is { IsLoaded: true };
    public readonly Skeleton Skeleton;

    public bool HasSockets => Sockets.Length > 0;
    public readonly Socket[] Sockets;

    public int TransformsCount;
    public readonly List<Transform> Transforms;
    private Matrix4x4 _previousMatrix;

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
        UvCount = 1;
        Box = new FBox(new FVector(-2f), new FVector(2f));
        Transforms = new List<Transform>();
    }

    public Model(UStaticMesh export, CStaticMesh staticMesh) : this(export, staticMesh, Transform.Identity) {}
    public Model(UStaticMesh export, CStaticMesh staticMesh, Transform transform) : this(export, export.Materials, null, staticMesh.LODs.Count, staticMesh.LODs[0], staticMesh.LODs[0].Verts, transform)
    {
        Box = staticMesh.BoundingBox * Constants.SCALE_DOWN_RATIO;

        Sockets = new Socket[export.Sockets.Length];
        for (int i = 0; i < Sockets.Length; i++)
        {
            if (export.Sockets[i].Load<UStaticMeshSocket>() is not { } socket) continue;
            Sockets[i] = new Socket(socket);
        }
    }
    private Model(USkeletalMesh export, CSkeletalMesh skeletalMesh, Transform transform) : this(export, export.Materials, export.Skeleton, skeletalMesh.LODs.Count, skeletalMesh.LODs[0], skeletalMesh.LODs[0].Verts, transform)
    {
        Box = skeletalMesh.BoundingBox * Constants.SCALE_DOWN_RATIO;

        var sockets = new List<FPackageIndex>();
        sockets.AddRange(export.Sockets);
        if (HasSkeleton) sockets.AddRange(Skeleton.UnrealSkeleton.Sockets);

        Sockets = new Socket[sockets.Count];
        for (int i = 0; i < Sockets.Length; i++)
        {
            if (sockets[i].Load<USkeletalMeshSocket>() is not { } socket) continue;

            if (!Skeleton.BonesIndexByName.TryGetValue(socket.BoneName.Text, out var boneIndex) ||
                !Skeleton.BonesTransformByIndex.TryGetValue(boneIndex, out var boneTransform))
            {
                Sockets[i] = new Socket(socket);
            }
            else
            {
                Sockets[i] = new Socket(socket, boneTransform);
            }
        }
    }
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
            Morphs[i] = new Morph(Vertices, VertexSize, morphTargets[i].Load<UMorphTarget>());
            ApplicationService.ApplicationView.Status.UpdateStatusLabel($"{Morphs[i].Name} ... {i}/{length}");
        }
        ApplicationService.ApplicationView.Status.UpdateStatusLabel("");
    }

    private Model(UObject export, ResolvedObject[] materials, FPackageIndex skeleton, int numLods, CBaseMeshLod lod, CMeshVertex[] vertices, Transform transform = null) : this(export)
    {
        var t = transform ?? Transform.Identity;
        var hasCustomUvs = lod.ExtraUV.IsValueCreated;
        UvCount = hasCustomUvs ? Math.Max(lod.NumTexCoords, numLods) : lod.NumTexCoords;
        TwoSided = lod.IsTwoSided;

        Materials = new Material[materials.Length];
        for (int m = 0; m < Materials.Length; m++)
        {
            if ((materials[m]?.TryLoad(out var material) ?? false) && material is UMaterialInterface unrealMaterial)
                Materials[m] = new Material(unrealMaterial); else Materials[m] = new Material();
        }

        if (lod.VertexColors is { Length: > 0})
        {
            HasVertexColors = true;
            VertexSize += 4; // + Color
        }

        if (skeleton != null)
        {
            Skeleton = new Skeleton(skeleton, t);
            VertexSize += 8; // + BoneIds + BoneWeights
        }

        Indices = new uint[lod.Indices.Value.Length];
        for (int i = 0; i < Indices.Length; i++)
        {
            Indices[i] = (uint) lod.Indices.Value[i];
        }

        Vertices = new float[lod.NumVerts * VertexSize];
        for (int i = 0; i < vertices.Length; i++)
        {
            var count = 0;
            var baseIndex = i * VertexSize;
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
            Vertices[baseIndex + count++] = hasCustomUvs ? lod.ExtraUV.Value[0][i].U : .5f;

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
            Sections[s] = new Section(section.MaterialIndex, section.NumFaces * _faceSize, section.FirstIndex);
            if (section.IsValid) Sections[s].SetupMaterial(Materials[section.MaterialIndex]);
        }

        AddInstance(t);
    }

    public void AddInstance(Transform transform)
    {
        TransformsCount++;
        Transforms.Add(transform);
        _previousMatrix = transform.Matrix;
    }

    public void UpdateMatrices(Options options)
    {
        UpdateMatrices();
        if (!HasSkeleton)
            return;

        for (int s = 0; s < Sockets.Length; s++)
        {
            for (int g = 0; g < Sockets[s].AttachedModels.Count; g++)
            {
                if (!options.TryGetModel(Sockets[s].AttachedModels[g], out var attachedModel))
                    continue;

                attachedModel.Transforms[attachedModel.SelectedInstance].Relation = Sockets[s].Transform.Matrix;
                attachedModel.UpdateMatrices();
            }
        }
    }
    private void UpdateMatrices()
    {
        var matrix = Transforms[SelectedInstance].Matrix;
        if (matrix == _previousMatrix) return;

        _matrixVbo.Bind();
        _matrixVbo.Update(SelectedInstance, matrix);
        _matrixVbo.Unbind();

        var delta = matrix - _previousMatrix;
        foreach (var socket in Sockets)
        {
            socket.UpdateSocketMatrix(delta);
        }
        if (HasSkeleton) Skeleton.UpdateRootBoneMatrix(delta);

        _previousMatrix = matrix;
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

        _vao.VertexAttributePointer(0, 1, VertexAttribPointerType.Int, VertexSize, 0); // vertex index
        _vao.VertexAttributePointer(1, 3, VertexAttribPointerType.Float, VertexSize, 1); // position
        _vao.VertexAttributePointer(2, 3, VertexAttribPointerType.Float, VertexSize, 4); // normal
        _vao.VertexAttributePointer(3, 3, VertexAttribPointerType.Float, VertexSize, 7); // tangent
        _vao.VertexAttributePointer(4, 2, VertexAttribPointerType.Float, VertexSize, 10); // uv
        if (!broken) _vao.VertexAttributePointer(5, 1, VertexAttribPointerType.Float, VertexSize, 12); // texture index
        _vao.VertexAttributePointer(6, 4, VertexAttribPointerType.Float, VertexSize, 13); // color
        _vao.VertexAttributePointer(7, 4, VertexAttribPointerType.Float, VertexSize, 17); // boneids
        _vao.VertexAttributePointer(8, 4, VertexAttribPointerType.Float, VertexSize, 21); // boneweights

        SetupInstances(); // instanced models transform

        // setup all used materials for use in different UV channels
        for (var i = 0; i < Materials.Length; i++)
        {
            if (!Materials[i].IsUsed) continue;
            Materials[i].Setup(options, broken ? 1 : UvCount);
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
            _vao.VertexAttributePointer(13, 3, VertexAttribPointerType.Float, Morph.VertexSize, 0); // morph position
            _vao.VertexAttributePointer(14, 3, VertexAttribPointerType.Float, Morph.VertexSize, 0); // morph tangent
            _vao.Unbind();
        }

        for (int section = 0; section < Sections.Length; section++)
        {
            if (!Show) Show = Sections[section].Show;
        }

        IsSetup = true;
    }

    public void Render(Shader shader, bool outline = false)
    {
        if (outline) GL.Disable(EnableCap.DepthTest);
        if (TwoSided) GL.Disable(EnableCap.CullFace);
        if (IsSelected)
        {
            GL.Enable(EnableCap.StencilTest);
            GL.StencilFunc(outline ? StencilFunction.Notequal : StencilFunction.Always, 1, 0xFF);
        }

        _vao.Bind();
        shader.SetUniform("uMorphTime", MorphTime);
        if (!outline)
        {
            shader.SetUniform("uUvCount", UvCount);
            shader.SetUniform("uHasVertexColors", HasVertexColors);
        }

        GL.PolygonMode(MaterialFace.FrontAndBack, Wireframe ? PolygonMode.Line : PolygonMode.Fill);
        foreach (var section in Sections)
        {
            if (!section.Show) continue;
            if (!outline) Materials[section.MaterialIndex].Render(shader);
            GL.DrawElementsInstanced(PrimitiveType.Triangles, section.FacesCount, DrawElementsType.UnsignedInt, section.FirstFaceIndexPtr, TransformsCount);
        }
        _vao.Unbind();

        if (IsSelected)
        {
            GL.StencilFunc(StencilFunction.Always, 0, 0xFF);
            GL.Disable(EnableCap.StencilTest);
        }
        if (TwoSided) GL.Enable(EnableCap.CullFace);
        if (outline) GL.Enable(EnableCap.DepthTest);
    }

    public void PickingRender(Shader shader)
    {
        if (TwoSided) GL.Disable(EnableCap.CullFace);

        _vao.Bind();
        shader.SetUniform("uMorphTime", MorphTime);
        foreach (var section in Sections)
        {
            if (!section.Show) continue;
            GL.DrawElementsInstanced(PrimitiveType.Triangles, section.FacesCount, DrawElementsType.UnsignedInt, section.FirstFaceIndexPtr, TransformsCount);
        }
        _vao.Unbind();

        if (TwoSided) GL.Enable(EnableCap.CullFace);
    }

    public bool TrySave(out string label, out string savedFilePath)
    {
        var exportOptions = new ExporterOptions
        {
            LodFormat = UserSettings.Default.LodExportFormat,
            MeshFormat = UserSettings.Default.MeshExportFormat,
            MaterialFormat = UserSettings.Default.MaterialExportFormat,
            TextureFormat = UserSettings.Default.TextureExportFormat,
            SocketFormat = UserSettings.Default.SocketExportFormat,
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

        GL.DeleteProgram(_handle);
    }
}
