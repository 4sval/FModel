using System;
using System.Collections.Generic;
using System.Numerics;
using CUE4Parse_Conversion;
using CUE4Parse.UE4.Objects.Core.Math;
using FModel.Views.Snooper.Buffers;
using FModel.Views.Snooper.Shading;

namespace FModel.Views.Snooper.Models;

public interface IRenderableModel : IExportableThing, IDisposable
{
    protected int Handle { get; set; }
    protected BufferObject<uint> Ebo { get; set; }
    protected BufferObject<float> Vbo { get; set; }
    protected BufferObject<Matrix4x4> MatrixVbo { get; set; }
    protected VertexArrayObject<float, uint> Vao { get; set; }

    public string Path { get; }
    public string Name { get; }
    public string Type { get; }
    public int UvCount { get; }
    public uint[] Indices { get; protected set; }
    public float[] Vertices { get; protected set; }
    public Section[] Sections { get; protected set; }
    public List<Transform> Transforms { get; }
    public Attachment Attachments { get; }

    public FBox Box { get; protected init; }
    public List<Socket> Sockets { get; }
    public List<Collision> Collisions { get; }
    public Material[] Materials { get; protected init; }
    public bool IsTwoSided { get; internal set; }
    public bool IsProp { get; internal set; }

    public bool HasSockets { get; }
    public bool HasCollisions { get; }
    public int TransformsCount { get; }

    public bool IsSetup { get; set; }
    public bool IsVisible { get; set; }
    public bool IsSelected { get; set; }
    public bool ShowWireframe { get; set; }
    public bool ShowCollisions { get; set; }
    public int SelectedInstance { get; set; }

    public void Setup(Options options);
    public void SetupInstances();
    public void Render(Shader shader, Texture checker = null, bool outline = false);
    public void RenderCollision(Shader shader);
    public void PickingRender(Shader shader);
    public void Update(Options options);
    public void AddInstance(Transform transform);

    public Transform GetTransform();
}

public interface IExportableThing
{
    public void AddToExportSession(ExportSession session);
}
