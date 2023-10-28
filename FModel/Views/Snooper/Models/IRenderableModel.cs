using System;
using System.Collections.Generic;
using System.Numerics;
using FModel.Views.Snooper.Buffers;
using FModel.Views.Snooper.Shading;

namespace FModel.Views.Snooper.Models;

public interface IRenderableModel : IDisposable
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

    public bool IsSetup { get; set; }
    public bool IsVisible { get; set; }
    public bool IsSelected { get; set; }
    public bool ShowWireframe { get; set; }

    public void Setup(Options options);
    public void SetupInstances();
    public void Render(Shader shader, Texture checker = null, bool outline = false);
    public void PickingRender(Shader shader);
    public void Update(Options options);
    public void AddInstance(Transform transform);
}
