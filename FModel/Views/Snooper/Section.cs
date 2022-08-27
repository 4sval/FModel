using System;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse_Conversion.Textures;
using Silk.NET.OpenGL;

namespace FModel.Views.Snooper;

public class Section : IDisposable
{
    private uint _handle;
    private GL _gl;

    private Texture _albedoMap;
    // private Texture _normalMap;

    public uint FacesCount;
    public int FirstFaceIndex;
    public CMaterialParams Parameters;

    public Section(uint facesCount, int firstFaceIndex, CMeshSection section)
    {
        FacesCount = facesCount;
        FirstFaceIndex = firstFaceIndex;
        Parameters = new CMaterialParams();
        if (section.Material != null && section.Material.TryLoad(out var material) && material is UMaterialInterface unrealMaterial)
        {
            unrealMaterial.GetParams(Parameters);
        }
    }

    public void Setup(GL gl)
    {
        _gl = gl;

        _handle = _gl.CreateProgram();

        if (Parameters.Diffuse is UTexture2D { IsVirtual: false } diffuse && diffuse.GetFirstMip() is { } mip)
        {
            TextureDecoder.DecodeTexture(mip, diffuse.Format, diffuse.isNormalMap, out var data, out _);
            _albedoMap = new Texture(_gl, data, (uint) mip.SizeX, (uint) mip.SizeY);
        }
    }

    public void Bind(Shader shader)
    {
        if (Parameters.IsNull)
            return;

        shader.SetUniform("material.albedo", 0);
        _albedoMap?.Bind(TextureUnit.Texture0);
    }

    public void Dispose()
    {
        _albedoMap?.Dispose();
        _gl.DeleteProgram(_handle);
    }
}
