using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using FModel.Settings;
using FModel.Views.Snooper.Buffers;
using FModel.Views.Snooper.Models.Animations;
using FModel.Views.Snooper.Shading;
using OpenTK.Graphics.OpenGL4;

namespace FModel.Views.Snooper.Models;

public class VertexAttribute
{
    public int Size;
    public bool Enabled;
}

public enum EAttribute
{
    Index,
    Position,
    Normals,
    Tangent,
    UVs,
    Layer,
    Colors,
    BonesId,
    BonesWeight
}

public class Model : IDisposable
{
    private int _handle;
    private const int _LOD_INDEX = 0;

    private BufferObject<uint> _ebo;
    private BufferObject<float> _vbo;
    private BufferObject<float> _morphVbo;
    private BufferObject<Matrix4x4> _matrixVbo;
    private VertexArrayObject<float, uint> _vao;

    protected int VertexSize => _vertexAttributes.Where(x => x.Enabled).Sum(x => x.Size);
    protected bool HasVertexColors => _vertexAttributes[(int) EAttribute.Colors].Enabled;
    private readonly List<VertexAttribute> _vertexAttributes = new()
    {
        new VertexAttribute { Size = 1, Enabled = true },   // VertexIndex
        new VertexAttribute { Size = 3, Enabled = true },   // Position
        new VertexAttribute { Size = 3, Enabled = true },   // Normal
        new VertexAttribute { Size = 3, Enabled = true },   // Tangent
        new VertexAttribute { Size = 2, Enabled = true },   // UV
        new VertexAttribute { Size = 1, Enabled = true },   // TextureLayer
        new VertexAttribute { Size = 4, Enabled = false },  // Colors
        new VertexAttribute { Size = 4, Enabled = false },  // BoneIds
        new VertexAttribute { Size = 4, Enabled = false }   // BoneWeights
    };
    private const int _faceSize = 3;

    private readonly UObject _export;
    public readonly string Path;
    public readonly string Name;
    public readonly string Type;
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

    public bool HasMorphTargets => Morphs.Length > 0;
    public readonly Morph[] Morphs;

    private string _attachedTo = string.Empty;
    private readonly List<string> _attachedFor = new ();
    public bool IsAttached => _attachedTo.Length > 0;
    public bool IsAttachment => _attachedFor.Count > 0;
    public string AttachIcon => IsAttachment ? "link_has" : IsAttached ? "link_on" : "link_off";
    public string AttachTooltip => IsAttachment ? $"Is Attachment For:\n{string.Join("\n", _attachedFor)}" : IsAttached ? $"Is Attached To {_attachedTo}" : "Not Attached To Any Socket Nor Attachment For Any Model";

    public int TransformsCount;
    public readonly List<Transform> Transforms;
    private Matrix4x4 _previousMatrix;

    public bool Show;
    public bool Wireframe;
    public bool IsSetup { get; private set; }
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
        Sockets = Array.Empty<Socket>();
        Morphs = Array.Empty<Morph>();
        Transforms = new List<Transform>();
    }

    public Model(UStaticMesh export, CStaticMesh staticMesh) : this(export, staticMesh, Transform.Identity) {}
    public Model(UStaticMesh export, CStaticMesh staticMesh, Transform transform) : this(export, export.Materials, staticMesh.LODs, transform)
    {
        Box = staticMesh.BoundingBox * Constants.SCALE_DOWN_RATIO;

        Sockets = new Socket[export.Sockets.Length];
        for (int i = 0; i < Sockets.Length; i++)
        {
            if (export.Sockets[i].Load<UStaticMeshSocket>() is not { } socket) continue;
            Sockets[i] = new Socket(socket);
        }
    }

    public Model(USkeletalMesh export, CSkeletalMesh skeletalMesh) : this(export, skeletalMesh, Transform.Identity) {}
    private Model(USkeletalMesh export, CSkeletalMesh skeletalMesh, Transform transform) : this(export, export.Materials, skeletalMesh.LODs, transform)
    {
        Box = skeletalMesh.BoundingBox * Constants.SCALE_DOWN_RATIO;
        Skeleton = new Skeleton(export.Skeleton, export.ReferenceSkeleton);

        var sockets = new List<FPackageIndex>();
        sockets.AddRange(export.Sockets);
        if (HasSkeleton) sockets.AddRange(Skeleton.UnrealSkeleton.Sockets);

        Sockets = new Socket[sockets.Count];
        for (int i = 0; i < Sockets.Length; i++)
        {
            if (sockets[i].Load<USkeletalMeshSocket>() is not { } socket) continue;
            Sockets[i] = new Socket(socket);
        }

        Morphs = new Morph[export.MorphTargets.Length];
        for (var i = 0; i < Morphs.Length; i++)
        {
            Morphs[i] = new Morph(Vertices, VertexSize, export.MorphTargets[i].Load<UMorphTarget>());
        }
    }

    private Model(UObject export, IReadOnlyList<ResolvedObject> materials, IReadOnlyList<CStaticMeshLod> lods, Transform transform = null)
        : this(export, materials, lods[_LOD_INDEX], lods[_LOD_INDEX].Verts, lods.Count, transform) {}
    private Model(UObject export, IReadOnlyList<ResolvedObject> materials, IReadOnlyList<CSkelMeshLod> lods, Transform transform = null)
        : this(export, materials, lods[_LOD_INDEX], lods[_LOD_INDEX].Verts, lods.Count, transform) {}
    private Model(UObject export, IReadOnlyList<ResolvedObject> materials, CBaseMeshLod lod, IReadOnlyList<CMeshVertex> vertices, int numLods, Transform transform = null) : this(export)
    {
        var hasCustomUvs = lod.ExtraUV.IsValueCreated;
        UvCount = hasCustomUvs ? Math.Max(lod.NumTexCoords, numLods) : lod.NumTexCoords;
        TwoSided = lod.IsTwoSided;

        Materials = new Material[materials.Count];
        for (int m = 0; m < Materials.Length; m++)
        {
            if ((materials[m]?.TryLoad(out var material) ?? false) && material is UMaterialInterface unrealMaterial)
                Materials[m] = new Material(unrealMaterial); else Materials[m] = new Material();
        }

        _vertexAttributes[(int) EAttribute.Colors].Enabled = lod.VertexColors is { Length: > 0};
        _vertexAttributes[(int) EAttribute.BonesId].Enabled =
            _vertexAttributes[(int) EAttribute.BonesWeight].Enabled = vertices is CSkelMeshVertex[];

        Indices = new uint[lod.Indices.Value.Length];
        for (int i = 0; i < Indices.Length; i++)
        {
            Indices[i] = (uint) lod.Indices.Value[i];
        }

        Vertices = new float[lod.NumVerts * VertexSize];
        for (int i = 0; i < vertices.Count; i++)
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

            if (vert is CSkelMeshVertex skelVert)
            {
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

        var t = transform ?? Transform.Identity;
        _previousMatrix = t.Matrix;
        AddInstance(t);
    }

    public void AddInstance(Transform transform)
    {
        TransformsCount++;
        Transforms.Add(transform);
    }

    public void UpdateMatrices(Options options)
    {
        var worldMatrix = UpdateMatrices();
        foreach (var socket in Sockets)
        {
            var boneMatrix = Matrix4x4.Identity;
            if (HasSkeleton && Skeleton.BonesIndexByLoweredName.TryGetValue(socket.BoneName.Text.ToLower(), out var boneIndex))
            {
                if (Skeleton.Anim?.TrackIndexByBoneIndex.TryGetValue(boneIndex, out var trackIndex) ?? false)
                    boneMatrix = Skeleton.Anim.InterpolateBoneTransform(trackIndex);
                else if (Skeleton.BonesTransformByIndex.TryGetValue(boneIndex, out var boneTransform))
                    boneMatrix = boneTransform.Matrix;
            }

            var socketRelation = boneMatrix * worldMatrix;
            foreach (var attached in socket.AttachedModels)
            {
                if (!options.TryGetModel(attached, out var attachedModel))
                    continue;

                attachedModel.Transforms[attachedModel.SelectedInstance].Relation = socket.Transform.LocalMatrix * socketRelation;
                attachedModel.UpdateMatrices(options);
            }
        }
    }
    private Matrix4x4 UpdateMatrices()
    {
        var matrix = Transforms[SelectedInstance].Matrix;
        if (matrix == _previousMatrix) return matrix;

        _matrixVbo.Bind();
        _matrixVbo.Update(SelectedInstance, matrix);
        _matrixVbo.Unbind();

        _previousMatrix = matrix;
        return matrix;
    }

    public void UpdateMorph(int index)
    {
        _morphVbo.Bind();
        _morphVbo.Update(Morphs[index].Vertices);
        _morphVbo.Unbind();
    }

    public void AttachModel(Model attachedTo, Socket socket)
    {
        _attachedTo = $"'{socket.Name}' from '{attachedTo.Name}'{(!socket.BoneName.IsNone ? $" at '{socket.BoneName}'" : "")}";
        attachedTo._attachedFor.Add($"'{Name}'");
        // reset PRS to 0 so it's attached to the actual position (can be transformed relative to the socket later by the user)
        Transforms[SelectedInstance].Position = FVector.ZeroVector;
        Transforms[SelectedInstance].Rotation = FQuat.Identity;
        Transforms[SelectedInstance].Scale = FVector.OneVector;
    }

    public void DetachModel(Model attachedTo)
    {
        _attachedTo = string.Empty;
        attachedTo._attachedFor.Remove($"'{Name}'");
        Transforms[SelectedInstance].Relation = _previousMatrix;
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
        var broken = GL.GetInteger(GetPName.MaxTextureCoords) == 0;

        _ebo = new BufferObject<uint>(Indices, BufferTarget.ElementArrayBuffer);
        _vbo = new BufferObject<float>(Vertices, BufferTarget.ArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_vbo, _ebo);

        var offset = 0;
        for (int i = 0; i < _vertexAttributes.Count; i++)
        {
            var attribute = _vertexAttributes[i];
            if (!attribute.Enabled) continue;

            if (i != 5 || !broken)
            {
                _vao.VertexAttributePointer((uint) i, attribute.Size, i == 0 ? VertexAttribPointerType.Int : VertexAttribPointerType.Float, VertexSize, offset);
            }
            offset += attribute.Size;
        }

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

    public void Render(float deltaSeconds, Shader shader, bool outline = false)
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
        if (HasSkeleton) Skeleton.SetUniform(shader, deltaSeconds, outline);
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
        if (HasSkeleton) Skeleton.SetUniform(shader);

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
        Skeleton?.Dispose();
        for (int socket = 0; socket < Sockets.Length; socket++)
        {
            Sockets[socket]?.Dispose();
        }
        if (HasMorphTargets) _morphVbo.Dispose();
        for (var morph = 0; morph < Morphs.Length; morph++)
        {
            Morphs[morph]?.Dispose();
        }

        GL.DeleteProgram(_handle);
    }
}
