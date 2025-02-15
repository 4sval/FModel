using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.Utils;
using FModel.Settings;
using FModel.Views.Snooper.Buffers;
using FModel.Views.Snooper.Shading;
using OpenTK.Graphics.OpenGL4;

namespace FModel.Views.Snooper.Models;

public class VertexAttribute
{
    public int Size;
    public VertexAttribPointerType Type;
    public bool Enabled;
}

public abstract class UModel : IRenderableModel
{
    protected const int LodLevel = 0;

    private readonly UObject _export;
    private readonly List<VertexAttribute> _vertexAttributes =
    [
        new VertexAttribute { Size = 1, Type = VertexAttribPointerType.Int, Enabled = false },    // VertexIndex
        new VertexAttribute { Size = 3, Type = VertexAttribPointerType.Float, Enabled = true },   // Position
        new VertexAttribute { Size = 3, Type = VertexAttribPointerType.Float, Enabled = false },  // Normal
        new VertexAttribute { Size = 3, Type = VertexAttribPointerType.Float, Enabled = false },  // Tangent
        new VertexAttribute { Size = 2, Type = VertexAttribPointerType.Float, Enabled = false },  // UV
        new VertexAttribute { Size = 1, Type = VertexAttribPointerType.Float, Enabled = false },  // TextureLayer
        new VertexAttribute { Size = 1, Type = VertexAttribPointerType.Float, Enabled = false },  // Colors
        new VertexAttribute { Size = 4, Type = VertexAttribPointerType.Float, Enabled = false },  // BoneIds
        new VertexAttribute { Size = 4, Type = VertexAttribPointerType.Float, Enabled = false }   // BoneWeights
    ];

    public int Handle { get; set; }
    public BufferObject<uint> Ebo { get; set; }
    public BufferObject<float> Vbo { get; set; }
    public BufferObject<Matrix4x4> MatrixVbo { get; set; }
    public VertexArrayObject<float, uint> Vao { get; set; }

    public string Path { get; }
    public string Name { get; }
    public string Type { get; }
    public int UvCount { get; }
    public uint[] Indices { get; set; }
    public float[] Vertices { get; set; }
    public Section[] Sections { get; set; }
    public List<Transform> Transforms { get; }
    public Attachment Attachments { get; }

    public FBox Box;
    public readonly List<Socket> Sockets;
    public readonly List<Collision> Collisions;
    public Material[] Materials;
    public bool IsTwoSided;
    public bool IsProp;

    public int VertexSize => _vertexAttributes.Where(x => x.Enabled).Sum(x => x.Size);
    public bool HasVertexColors => _vertexAttributes[(int) EAttribute.Colors].Enabled;
    public bool HasSockets => Sockets.Count > 0;
    public bool HasCollisions => Collisions.Count > 0;
    public int TransformsCount => Transforms.Count;

    public bool IsSetup { get; set; }
    public bool IsVisible { get; set; }
    public bool IsSelected { get; set; }
    public bool ShowWireframe { get; set; }
    public bool ShowCollisions { get; set; }
    public int SelectedInstance;

    protected UModel()
    {
        _export = null;
        UvCount = 1;

        Box = new FBox(new FVector(-2f), new FVector(2f));
        Sockets = new List<Socket>();
        Collisions = new List<Collision>();
        Transforms = new List<Transform>();
    }

    protected UModel(UObject export)
    {
        _export = export;
        Path = _export.GetPathName();
        Name = Path.SubstringAfterLast('/').SubstringBefore('.');
        Type = export.ExportType;
        UvCount = 1;

        Box = new FBox(new FVector(-2f), new FVector(2f));
        Sockets = new List<Socket>();
        Collisions = new List<Collision>();
        Transforms = new List<Transform>();
        Attachments = new Attachment(Name);

        _vertexAttributes[(int) EAttribute.Index].Enabled =
            _vertexAttributes[(int) EAttribute.Normals].Enabled =
                _vertexAttributes[(int) EAttribute.Tangent].Enabled =
                    _vertexAttributes[(int) EAttribute.UVs].Enabled =
                        _vertexAttributes[(int) EAttribute.Layer].Enabled = true;
    }

    protected UModel(UObject export, CBaseMeshLod lod, IReadOnlyList<ResolvedObject> materials, IReadOnlyList<CMeshVertex> vertices, int numLods, Transform transform = null) : this(export)
    {
        var hasCustomUvs = lod.ExtraUV.IsValueCreated;
        UvCount = hasCustomUvs ? Math.Max(lod.NumTexCoords, numLods) : lod.NumTexCoords;
        IsTwoSided = lod.IsTwoSided;

        Indices = new uint[lod.Indices.Value.Length];
        for (int i = 0; i < Indices.Length; i++)
        {
            Indices[i] = (uint) lod.Indices.Value[i];
        }

        Materials = new Material[materials.Count];
        for (int m = 0; m < Materials.Length; m++)
        {
            if ((materials[m]?.TryLoad(out var material) ?? false) && material is UMaterialInterface unrealMaterial)
                Materials[m] = new Material(unrealMaterial); else Materials[m] = new Material();
        }

        _vertexAttributes[(int) EAttribute.Colors].Enabled = lod.VertexColors is { Length: > 0};
        _vertexAttributes[(int) EAttribute.BonesId].Enabled =
            _vertexAttributes[(int) EAttribute.BonesWeight].Enabled = vertices is CSkelMeshVertex[];

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
            Vertices[baseIndex + count++] = hasCustomUvs ? lod.ExtraUV.Value[0][i].U - 1 : .5f;

            if (HasVertexColors)
            {
                Vertices[baseIndex + count++] = lod.VertexColors[i].ToPackedARGB();
            }

            if (vert is CSkelMeshVertex skelVert)
            {
                int max = skelVert.Influences.Count;
                for (int j = 0; j < 8; j++)
                {
                    var boneID = j < max ? skelVert.Influences[j].Bone : (short) 0;
                    var weight = j < max ? skelVert.Influences[j].RawWeight : (byte) 0;

                    // Pack bone ID and weight
                    Vertices[baseIndex + count++] = (boneID << 16) | weight;
                }
            }
        }

        Sections = new Section[lod.Sections.Value.Length];
        for (var s = 0; s < Sections.Length; s++)
        {
            var section = lod.Sections.Value[s];
            Sections[s] = new Section(section.MaterialIndex, section.NumFaces * 3, section.FirstIndex);
            if (section.IsValid) Sections[s].SetupMaterial(Materials[section.MaterialIndex]);
        }

        AddInstance(transform ?? Transform.Identity);
    }

    public virtual void Setup(Options options)
    {
        Handle = GL.CreateProgram();
        Ebo = new BufferObject<uint>(Indices, BufferTarget.ElementArrayBuffer);
        Vbo = new BufferObject<float>(Vertices, BufferTarget.ArrayBuffer);
        Vao = new VertexArrayObject<float, uint>(Vbo, Ebo);

        var offset = 0;
        var broken = GL.GetInteger(GetPName.MaxTextureCoords) == 0;
        for (int i = 0; i < _vertexAttributes.Count; i++)
        {
            var attribute = _vertexAttributes[i];
            if (!attribute.Enabled) continue;

            if (i != 5 || !broken)
            {
                Vao.VertexAttributePointer((uint) i, attribute.Size, attribute.Type, VertexSize, offset);
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

        foreach (var collision in Collisions)
        {
            collision.Setup();
        }

        if (options.Models.Count == 1 && Sections.All(x => !x.Show))
        {
            IsVisible = true;
            foreach (var section in Sections)
            {
                section.Show = true;
            }
        }
        else foreach (var section in Sections)
        {
            if (!IsVisible) IsVisible = section.Show;
        }

        IsSetup = true;
    }

    public virtual void Render(Shader shader, Texture checker = null, bool outline = false)
    {
        if (outline) GL.Disable(EnableCap.DepthTest);
        if (IsTwoSided) GL.Disable(EnableCap.CullFace);
        if (IsSelected)
        {
            GL.Enable(EnableCap.StencilTest);
            GL.StencilFunc(outline ? StencilFunction.Notequal : StencilFunction.Always, 1, 0xFF);
        }

        if (this is SkeletalModel skeletalModel) skeletalModel.Render(shader);
        else shader.SetUniform("uIsAnimated", false);
        if (!outline)
        {
            shader.SetUniform("uUvCount", UvCount);
            shader.SetUniform("uOpacity", ShowCollisions && IsSelected ? 0.75f : 1f);
            shader.SetUniform("uHasVertexColors", HasVertexColors);
        }

        Vao.Bind();
        GL.PolygonMode(TriangleFace.FrontAndBack, ShowWireframe ? PolygonMode.Line : PolygonMode.Fill);
        foreach (var section in Sections)
        {
            if (!section.Show) continue;
            if (!outline)
            {
                if (checker != null)
                {
                    shader.SetUniform("uParameters.Diffuse[0].Sampler", 0);
                    checker.Bind(TextureUnit.Texture0);
                }
                else
                {
                    shader.SetUniform("uSectionColor", section.Color);
                    Materials[section.MaterialIndex].Render(shader);
                }
            }

            GL.DrawElementsInstanced(PrimitiveType.Triangles, section.FacesCount, DrawElementsType.UnsignedInt, section.FirstFaceIndexPtr, TransformsCount);
        }
        GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
        Vao.Unbind();

        if (IsSelected)
        {
            GL.StencilFunc(StencilFunction.Always, 0, 0xFF);
            GL.Disable(EnableCap.StencilTest);
        }
        if (IsTwoSided) GL.Enable(EnableCap.CullFace);
        if (outline) GL.Enable(EnableCap.DepthTest);
    }

    public void PickingRender(Shader shader)
    {
        if (IsTwoSided) GL.Disable(EnableCap.CullFace);
        if (this is SkeletalModel skeletalModel)
            skeletalModel.Render(shader);
        else shader.SetUniform("uIsAnimated", false);

        Vao.Bind();
        foreach (var section in Sections)
        {
            if (!section.Show) continue;
            GL.DrawElementsInstanced(PrimitiveType.Triangles, section.FacesCount, DrawElementsType.UnsignedInt, section.FirstFaceIndexPtr, TransformsCount);
        }
        Vao.Unbind();

        if (IsTwoSided) GL.Enable(EnableCap.CullFace);
    }

    public virtual void RenderCollision(Shader shader)
    {
        shader.SetUniform("uInstanceMatrix", GetTransform().Matrix);
        shader.SetUniform("uScaleDown", Constants.SCALE_DOWN_RATIO);
    }

    public void Update(Options options)
    {
        MatrixVbo.Bind();
        for (int instance = 0; instance < TransformsCount; instance++)
        {
            MatrixVbo.Update(instance, Transforms[instance].Matrix);
        }
        MatrixVbo.Unbind();

        var worldMatrix = GetTransform().Matrix;
        foreach (var socket in Sockets)
        {
            if (!socket.IsDaron) continue;

            var boneMatrix = Matrix4x4.Identity;
            if (this is SkeletalModel skeletalModel && skeletalModel.Skeleton.BonesByLoweredName.TryGetValue(socket.BoneName.Text.ToLower(), out var bone))
                boneMatrix = skeletalModel.Skeleton.GetBoneMatrix(bone);

            var socketRelation = boneMatrix * worldMatrix;
            foreach (var info in socket.AttachedModels)
            {
                if (!options.TryGetModel(info.Guid, out var attachedModel))
                    continue;

                attachedModel.Transforms[info.Instance].Relation = socket.Transform.LocalMatrix * socketRelation;
                attachedModel.Update(options);
            }
        }
    }

    public void AddInstance(Transform transform)
    {
        SelectedInstance = TransformsCount;
        Transforms.Add(transform);
    }

    public void SetupInstances()
    {
        MatrixVbo = new BufferObject<Matrix4x4>(TransformsCount, BufferTarget.ArrayBuffer);
        for (int instance = 0; instance < TransformsCount; instance++)
        {
            MatrixVbo.Update(instance, Transforms[instance].Matrix);
        }
        Vao.BindInstancing(); // VertexAttributePointer
    }

    public Transform GetTransform() => Transforms[SelectedInstance];
    public Matrix4x4 GetSocketTransform(int index)
    {
        var socket = Sockets[index];
        var worldMatrix = GetTransform().Matrix;
        var boneMatrix = Matrix4x4.Identity;
        if (this is SkeletalModel skeletalModel && skeletalModel.Skeleton.BonesByLoweredName.TryGetValue(socket.BoneName.Text.ToLower(), out var bone))
            boneMatrix = skeletalModel.Skeleton.GetBoneMatrix(bone);

        var socketRelation = boneMatrix * worldMatrix;
        return socket.Transform.LocalMatrix * socketRelation;
    }

    public bool Save(out string label, out string savedFilePath)
    {
        var toSave = new Exporter(_export, UserSettings.Default.ExportOptions);
        return toSave.TryWriteToDir(new DirectoryInfo(UserSettings.Default.ModelDirectory), out label, out savedFilePath);
    }

    public virtual void Dispose()
    {
        Ebo?.Dispose();
        Vbo?.Dispose();
        MatrixVbo?.Dispose();
        Vao?.Dispose();
        foreach (var socket in Sockets)
        {
            socket?.Dispose();
        }
        Sockets.Clear();
        foreach (var collision in Collisions)
        {
            collision?.Dispose();
        }
        Collisions.Clear();

        GL.DeleteProgram(Handle);
    }
}
